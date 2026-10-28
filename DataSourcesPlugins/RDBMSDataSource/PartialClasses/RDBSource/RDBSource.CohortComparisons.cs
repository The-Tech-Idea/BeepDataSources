using System;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Extensions;
using TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource
    {
        private sealed record CohortCountPlan(AppFilterQueryDefinition Query,string CountSql);

        private CohortCountPlan PrepareCohortCountPlan(ConditionalCohortSelector selector)
        {
            if(!SupportsCohortComparisons)throw new NotSupportedException("Complete cohort comparisons are unavailable.");
            var entity=Entities?.SingleOrDefault(value=>string.Equals(value.EntityName,selector.EntityName,StringComparison.OrdinalIgnoreCase))
                ??throw new NotSupportedException("Cohort counts require cached declared metadata.");
            var fields=entity.Fields?.ToDictionary(value=>value.FieldName,StringComparer.OrdinalIgnoreCase)
                ??throw new NotSupportedException("Cohort count fields are undeclared.");
            var key=fields.Values.FirstOrDefault(value=>value.IsKey)??throw new NotSupportedException("Cohort counts require a declared primary key.");
            var required=selector.Request.CopyFilters();var any=selector.Request.CopyAnyFilters();
            foreach(var filter in required.Concat(any))
                filter.FieldName=fields.TryGetValue(filter.FieldName,out var field)?field.FieldName:throw new ArgumentException("Cohort count predicates require declared columns.");
            string QuoteEntity(string name)=>DataSourceAppFilterExtensions.QuoteQueryIdentifier(DatasourceType==Utilities.DataSourceType.SqlLite?name:QualifyWithSchema(name),DatasourceType);
            var table=QuoteEntity(entity.EntityName);
            var request=new BoundedPageRequest(1,1,selector.Request.MaxPayloadBytes,[],required,any,
                selector.Request.AnyRelatedFilters,selector.Request.RequiredRelatedGroups);
            // Compile through the same metadata/predicate owner as bounded reads. Only the
            // generated COUNT is executed; no historical row page is materialized or capped.
            var query=this.BuildBoundedSelectQueryDefinition(table,request,
                [DataSourceAppFilterExtensions.QuoteQueryIdentifier(key.FieldName,DatasourceType)],entity,QuoteEntity);
            return new(query,DatabaseDMLUtilities.GetBoundedRecordCountQuery(DatasourceType,table,query.WhereClause));
        }

        private async Task<long> ReadCohortCountAsync(CohortCountPlan plan,DbConnection connection,DbTransaction transaction,CancellationToken ct)
        {
            await using var command=connection.CreateCommand();command.Transaction=transaction;
            command.CommandText=plan.CountSql;command.CommandTimeout=10;ConfigureCommand(command);
            foreach(var pair in plan.Query.Parameters)
            {
                var parameter=command.CreateParameter();parameter.ParameterName=this.GetParameterPrefix()+pair.Key;
                RdbParameters.Bind(parameter,pair.Value);command.Parameters.Add(parameter);
            }
            var value=await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
            if(value is null||value==DBNull.Value)throw new InvalidOperationException("The complete cohort count was not acknowledged.");
            var count=Convert.ToInt64(value,CultureInfo.InvariantCulture);
            if(count<0)throw new InvalidOperationException("A complete cohort count cannot be negative.");
            return count;
        }
    }
}
