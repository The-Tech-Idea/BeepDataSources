using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Extensions;
using TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource
    {
        private sealed record ProjectionGuardPlan(ConditionalStoredProjectionGuard Guard,EntityField[] Fields,
            Type[] Types,AppFilterQueryDefinition Query,string CountSql,string PageSql);

        private ProjectionGuardPlan PrepareProjectionGuardPlan(ConditionalStoredProjectionGuard guard)
        {
            if(!SupportsProjectionGuardedBatches)throw new NotSupportedException("Stored projection transaction guards are unavailable.");
            var entity=Entities?.SingleOrDefault(row=>string.Equals(row.EntityName,guard.EntityName,StringComparison.OrdinalIgnoreCase))
                ??throw new NotSupportedException("Projection guards require cached declared metadata.");
            var all=entity.Fields?.ToArray()??throw new NotSupportedException("Projection guard fields are undeclared.");
            var names=all.ToDictionary(field=>field.FieldName,StringComparer.OrdinalIgnoreCase);
            var fields=guard.Fields.Select(name=>names.TryGetValue(name,out var field)?field:throw new ArgumentException("Projection guards select declared columns, never expressions.")).ToArray();
            var keys=all.Where(field=>field.IsKey).ToArray();
            if(keys.Length==0||!keys.Select(key=>key.FieldName).ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(guard.KeyFields))
                throw new ArgumentException("The complete physical primary key is required for projection guards.");
            var request=guard.Request;
            foreach(var filter in request.CopyFilters().Concat(request.CopyAnyFilters()))
                if(!names.ContainsKey(filter.FieldName))throw new ArgumentException("Projection predicates must use declared fields.");
            var related=request.AnyRelatedFilters.Concat(request.RequiredRelatedGroups.SelectMany(group=>group.Alternatives));
            if(request.CopyFilters().Concat(request.CopyAnyFilters()).Concat(related.SelectMany(value=>value.CopyFilters().Concat(value.CopyAnyFilters())))
                .Any(filter=>filter.Operator=="equalsordinalignorecase")&&!SupportsOrdinalIgnoreCaseText)
                throw new NotSupportedException("Ordinal projection guard predicates are unavailable.");
            var table=DataSourceAppFilterExtensions.QuoteQueryIdentifier(DatasourceType==DataSourceType.SqlLite?entity.EntityName:QualifyWithSchema(entity.EntityName),DatasourceType);
            var required=request.CopyFilters();var any=request.CopyAnyFilters();
            foreach(var filter in required.Concat(any))filter.FieldName=names[filter.FieldName].FieldName;
            var qualified=new BoundedPageRequest(1,request.PageSize,request.MaxPayloadBytes,keys.Select(key=>new PageOrder(key.FieldName)),
                required,any,request.AnyRelatedFilters,request.RequiredRelatedGroups);
            var query=this.BuildBoundedSelectQueryDefinition(table,qualified,DatabaseDMLUtilities.GetBoundedPageProjection(DatasourceType,fields,request.MaxPayloadBytes),entity,
                name=>DataSourceAppFilterExtensions.QuoteQueryIdentifier(DatasourceType==DataSourceType.SqlLite?name:QualifyWithSchema(name),DatasourceType));
            var order=string.Join(", ",keys.Select(key=>DataSourceAppFilterExtensions.QuoteQueryIdentifier(key.FieldName,DatasourceType)));
            return new(guard,fields,fields.Select(field=>Type.GetType(field.Fieldtype??string.Empty)??throw new NotSupportedException("Projection scalar type is undeclared.")).ToArray(),query,
                DatabaseDMLUtilities.GetBoundedRecordCountQuery(DatasourceType,table,query.WhereClause),
                // Match the existing conditional owner's PostgreSQL shared-row locking;
                // the complete count/page also remains in the same serializable transaction.
                query.QueryText+" ORDER BY "+order+" "+DatabaseDMLUtilities.GetBoundedPagingSyntax(DatasourceType,0,request.PageSize)+
                    (DatasourceType==DataSourceType.Postgre?" FOR SHARE":string.Empty));
        }

        private async Task<bool> CheckProjectionGuardAsync(ProjectionGuardPlan plan,DbConnection connection,DbTransaction transaction,CancellationToken ct)
        {
            DbCommand Command(string sql)
            {
                var command=connection.CreateCommand();command.Transaction=transaction;command.CommandText=sql;command.CommandTimeout=10;ConfigureCommand(command);
                foreach(var pair in plan.Query.Parameters)
                {var parameter=command.CreateParameter();parameter.ParameterName=this.GetParameterPrefix()+pair.Key;RdbParameters.Bind(parameter,pair.Value);command.Parameters.Add(parameter);}
                return command;
            }
            await using(var count=Command(plan.CountSql))
            {
                var value=await count.ExecuteScalarAsync(ct).ConfigureAwait(false);
                if(value is null||value==DBNull.Value)throw new InvalidOperationException("Projection guard count was not acknowledged.");
                if(Convert.ToInt64(value,CultureInfo.InvariantCulture)!=plan.Guard.ExpectedCount)return false;
            }
            using var payload=new PagePayloadStream(plan.Guard.Request.MaxPayloadBytes);
            using(var writer=new Utf8JsonWriter(payload))
            {
                writer.WriteStartArray();var rows=0;
                await using var command=Command(plan.PageSql);
                await using var reader=await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess,ct).ConfigureAwait(false);
                if(reader.FieldCount!=plan.Fields.Length+1)throw new InvalidOperationException("Projection guard schema differs from declared fields.");
                for(var column=1;column<reader.FieldCount;column++)
                    if(!string.Equals(reader.GetName(column),plan.Fields[column-1].FieldName,StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Projection guard columns differ from declared metadata.");
                while(await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    ct.ThrowIfCancellationRequested();if(++rows>plan.Guard.ExpectedCount||reader.GetInt32(0)!=0)
                        throw new InvalidOperationException("Projection guard exceeded its row/cell bound.");
                    writer.WriteStartObject();
                    for(var column=1;column<reader.FieldCount;column++)
                    {
                        writer.WritePropertyName(plan.Fields[column-1].FieldName);
                        var value=ReadBoundedScalar(reader,column,plan.Types[column-1],plan.Guard.Request.MaxPayloadBytes);
                        if(value is null)writer.WriteNullValue();else JsonSerializer.Serialize(writer,value,plan.Types[column-1]);
                        writer.Flush();
                    }
                    writer.WriteEndObject();
                }
                if(rows!=plan.Guard.ExpectedCount)throw new InvalidOperationException("Projection guard count/page snapshot differs.");
                writer.WriteEndArray();writer.Flush();
            }
            using var document=JsonDocument.Parse(payload.ToArray(),new JsonDocumentOptions{MaxDepth=3});
            IEnumerable<StoredProjectionRow> Rows()
            {
                foreach(var row in document.RootElement.EnumerateArray())
                {
                    ct.ThrowIfCancellationRequested();var values=new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);
                    for(var column=0;column<plan.Fields.Length;column++)
                    {
                        var value=row.GetProperty(plan.Fields[column].FieldName);
                        values.Add(plan.Fields[column].FieldName,value.ValueKind==JsonValueKind.Null?null!:value.Deserialize(plan.Types[column])!);
                    }
                    yield return new(values);
                }
            }
            var digest=ConditionalStoredProjectionGuard.ComputeDigest(plan.Guard.Fields,plan.Guard.KeyFields,Rows());
            return digest.Count==plan.Guard.ExpectedCount&&digest.Digest==plan.Guard.ExpectedDigest;
        }
    }
}
