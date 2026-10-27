using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace RDBDataSource.Tests;

/// <summary>Actual shared producer over SQLite; SQL is fixture setup only. No live PostgreSQL/SQL Server claim.</summary>
public sealed class BoundedPageTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(10000)]
    [InlineData(100000)]
    public async Task Large_two_company_catalog_returns_bounded_stable_pages_and_filtered_counts(int size)
    {
        using var h = new Harness();
        using (var transaction = h.Connection.BeginTransaction())
        using (var command = h.Connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Products VALUES($id,$company,'Y',$sku,$name,$barcode,'Active','12.345')";
            var id = command.Parameters.Add("$id", SqliteType.Text);
            var company = command.Parameters.Add("$company", SqliteType.Text);
            var sku = command.Parameters.Add("$sku", SqliteType.Text);
            var name = command.Parameters.Add("$name", SqliteType.Text);
            var barcode = command.Parameters.Add("$barcode", SqliteType.Text);
            for (var index = 0; index < size; index++)
            {
                id.Value = index.ToString("D8"); company.Value = index % 2 == 0 ? "a" : "b";
                sku.Value = "SKU-" + index; name.Value = "Repeated-" + index % 10; barcode.Value = "BAR-" + index;
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var first = await h.Source.ReadPageAsync("Products", Request());
        var second = await h.Source.ReadPageAsync("Products", Request(page: 2));
        timer.Stop();
        using var rows = JsonDocument.Parse(first.CopyPayload());
        using var next = JsonDocument.Parse(second.CopyPayload());
        Assert.Equal(size / 2, first.TotalRecords);
        Assert.Equal(25, rows.RootElement.GetArrayLength());
        Assert.Equal(25, next.RootElement.GetArrayLength());
        var ids = rows.RootElement.EnumerateArray().Select(row => row.GetProperty("Id").GetString()).ToArray();
        var otherIds = next.RootElement.EnumerateArray().Select(row => row.GetProperty("Id").GetString()).ToArray();
        Assert.Empty(ids.Intersect(otherIds));
        Assert.All(rows.RootElement.EnumerateArray(), row => {
            Assert.Equal("a", row.GetProperty("CompanyId").GetString());
            Assert.Equal(12.345m, row.GetProperty("Cost").GetDecimal());
        });
        Assert.True(first.PayloadBytes < 1024 * 1024);
        var repeat = await h.Source.ReadPageAsync("Products", Request());
        Assert.Equal(first.CopyPayload(), repeat.CopyPayload());
        var filtered = await h.Source.ReadPageAsync("Products", Request(search: "BAR-246"));
        using var searched = JsonDocument.Parse(filtered.CopyPayload());
        Assert.All(searched.RootElement.EnumerateArray(), row => Assert.Contains("246", row.GetProperty("Barcode").GetString()));
        Assert.True(filtered.TotalRecords > 0 && filtered.TotalRecords < first.TotalRecords);
        Assert.All(h.Source.PageConnections, connection => { Assert.NotNull(connection); Assert.NotSame(h.Connection, connection); });
        Assert.Same(h.Source.PageTransactions[0], h.Source.PageTransactions[1]);
        Assert.Contains(h.Commands, command => command.CommandText.Contains("LIMIT 25 OFFSET 25"));
        output.WriteLine("PAGE_MEASUREMENT " + JsonSerializer.Serialize(new { size, companyCount = first.TotalRecords,
            loaded = rows.RootElement.GetArrayLength(), first.PayloadBytes, twoPageMilliseconds = timer.ElapsedMilliseconds }));
    }

    [Fact]
    public async Task Search_alternatives_stay_inside_required_company_and_treat_wildcards_as_literal_text()
    {
        using var h = new Harness();
        h.Insert("1", "a", "100%_[$", "name");
        h.Insert("2", "b", "foreign", "100%_[$");
        h.Insert("3", "a", "other", "100%_[$");
        h.Insert("4", "a", "other", "100XX");
        var page = await h.Source.ReadPageAsync("Products", Request(search: "100%_[$"));
        using var rows = JsonDocument.Parse(page.CopyPayload());
        Assert.Equal(2, page.TotalRecords);
        Assert.Equal(new[] { "1", "3" }, rows.RootElement.EnumerateArray().Select(row => row.GetProperty("Id").GetString()).OrderBy(id => id));
        Assert.All(h.Commands, command => Assert.DoesNotContain("100%_[$", command.CommandText));
    }

    [Fact]
    public async Task Oversized_text_fails_at_the_producer_and_leaves_the_source_connection_owned_by_its_caller()
    {
        using var h = new Harness();
        h.Insert("1", "a", "SKU", new string('x', 20000));
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Source.ReadPageAsync("Products", Request(bytes: 512)));
        Assert.Equal(Errors.Failed, h.Source.ErrorObject.Flag);
        Assert.Equal(ConnectionState.Open, h.Connection.State);
        Assert.Null(h.Source.ActiveTransaction);
        using var command = h.Connection.CreateCommand(); command.CommandText = "SELECT COUNT(*) FROM Products";
        Assert.Equal(1L, command.ExecuteScalar());
    }

    [Fact]
    public async Task Encoded_unicode_and_escaping_cannot_exceed_the_payload_limit()
    {
        using var h = new Harness();
        h.Insert("1", "a", "SKU", new string('漢', 100));
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Source.ReadPageAsync("Products", Request(bytes: 512)));
    }

    [Fact]
    public async Task Invalid_predicates_sort_fragments_and_missing_tie_key_are_refused_before_commands()
    {
        using var h = new Harness();
        var filters = new[] { new AppFilter { FieldName = "CompanyId", Operator = "= 1 OR 1=1 --", FilterValue = "a" } };
        await Assert.ThrowsAsync<ArgumentException>(() => h.Source.ReadPageAsync("Products",
            new BoundedPageRequest(1,25,1024,new[] { new PageOrder("Id") },filters)));
        await Assert.ThrowsAsync<ArgumentException>(() => h.Source.ReadPageAsync("Products",
            new BoundedPageRequest(1,25,1024,new[] { new PageOrder("Name; DROP TABLE Products") })));
        await Assert.ThrowsAsync<ArgumentException>(() => h.Source.ReadPageAsync("Products",
            new BoundedPageRequest(1,25,1024,new[] { new PageOrder("Name") })));
        Assert.Empty(h.Commands);
    }

    [Fact]
    public async Task Cancellation_does_not_return_while_a_provider_command_is_still_acknowledging()
    {
        using var h = new Harness();
        h.Insert("1", "a", "SKU", "name");
        var arrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        h.Source.BeforePage = () => { arrived.TrySetResult(); release.Task.GetAwaiter().GetResult(); };
        using var cancellation = new CancellationTokenSource();
        var read = h.Source.ReadPageAsync("Products", Request(), cancellation.Token);
        try {
            await arrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            Assert.False(read.IsCompleted);
        }
        finally { release.TrySetResult(); }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
        Assert.Equal(ConnectionState.Open, h.Connection.State);
    }

    [Fact]
    public async Task Huge_offset_is_checked_long_and_returns_an_honest_empty_page()
    {
        using var h = new Harness(); h.Insert("1","a","SKU","name");
        var page = await h.Source.ReadPageAsync("Products", Request(page: int.MaxValue));
        Assert.Equal(1, page.TotalRecords); Assert.Equal("[]", System.Text.Encoding.UTF8.GetString(page.CopyPayload()));
        Assert.Contains(h.Commands, command => command.CommandText.Contains("OFFSET 53687091150"));
    }

    [Fact]
    public async Task Binary_values_round_trip_as_base64_and_oversized_binary_is_refused_in_SQL()
    {
        using var h = new Harness();
        using (var command = h.Connection.CreateCommand())
        {
            command.CommandText = "CREATE TABLE Files(Id TEXT PRIMARY KEY, Data BLOB); INSERT INTO Files VALUES('one',$data)";
            command.Parameters.AddWithValue("$data", new byte[] { 0,1,2,255 });
            command.ExecuteNonQuery();
        }
        h.Source.Entities.Add(new EntityStructure { EntityName="Files", Fields=new() {
            new EntityField { FieldName="Id", Fieldtype=typeof(string).FullName, IsKey=true },
            new EntityField { FieldName="Data", Fieldtype=typeof(byte[]).FullName }
        }});
        var request = new BoundedPageRequest(1,25,1024,new[] { new PageOrder("Id") });
        var result = await h.Source.ReadPageAsync("Files",request);
        using var rows = JsonDocument.Parse(result.CopyPayload());
        Assert.Equal(new byte[] { 0,1,2,255 },rows.RootElement[0].GetProperty("Data").GetBytesFromBase64());
        using (var command = h.Connection.CreateCommand())
        {
            command.CommandText = "UPDATE Files SET Data=$data"; command.Parameters.AddWithValue("$data",new byte[10000]);
            command.ExecuteNonQuery();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Source.ReadPageAsync("Files",request));
    }

    [Fact]
    public async Task SQLite_dynamic_storage_cannot_hide_an_oversized_text_value_in_a_numeric_field()
    {
        using var h = new Harness(); h.Insert("1","a","SKU","name");
        using (var command = h.Connection.CreateCommand())
        {
            command.CommandText = "UPDATE Products SET Cost=$text";
            command.Parameters.AddWithValue("$text",new string('9',20000)); command.ExecuteNonQuery();
        }
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => h.Source.ReadPageAsync("Products",Request(bytes:512)));
        Assert.Contains("provider cell exceeds",error.Message);
    }

    [Fact]
    public async Task Quoted_queries_use_physical_metadata_names_while_accepting_caller_case_variants()
    {
        using var h = new Harness(); h.Insert("1","a","SKU","name");
        foreach (var field in h.Source.Entities[0].Fields) field.FieldName = field.FieldName.ToUpperInvariant();
        var page = await h.Source.ReadPageAsync("Products",Request(search:"sku"));
        Assert.Equal(1,page.TotalRecords);
        var sql = h.Commands.Last().CommandText;
        // SQLite itself ignores identifier case; these tokens are required by case-sensitive quoted providers.
        Assert.Contains("\"COMPANYID\" =",sql);
        Assert.Contains("LOWER(\"SKU\")",sql);
        Assert.Contains("ORDER BY \"NAME\" ASC, \"ID\" ASC",sql);
        using var rows = JsonDocument.Parse(page.CopyPayload());
        Assert.Equal("a",rows.RootElement[0].GetProperty("COMPANYID").GetString());
    }


    [Fact]
    public async Task Nullable_scalars_preserve_database_nulls_and_populated_dates_amounts_and_flags()
    {
        using var h=new Harness();
        using(var command=h.Connection.CreateCommand())
        {
            command.CommandText="CREATE TABLE NullableRecords(Id TEXT PRIMARY KEY, CompanyId TEXT, ApprovedOn TEXT, Amount REAL, Flag INTEGER); INSERT INTO NullableRecords VALUES('1','a',NULL,NULL,NULL); INSERT INTO NullableRecords VALUES('2','a','2026-10-11T12:34:56.1234567',12.345,1)";
            command.ExecuteNonQuery();
        }
        h.Source.Entities.Add(new EntityStructure {EntityName="NullableRecords",Fields=new() {
            new EntityField {FieldName="Id",Fieldtype=typeof(string).FullName,IsKey=true},
            new EntityField {FieldName="CompanyId",Fieldtype=typeof(string).FullName},
            new EntityField {FieldName="ApprovedOn",Fieldtype=typeof(DateTime).FullName},
            new EntityField {FieldName="Amount",Fieldtype=typeof(decimal).FullName},
            new EntityField {FieldName="Flag",Fieldtype=typeof(bool).FullName}
        }});
        var response=await h.Source.ReadPageAsync("NullableRecords",new(1,25,1024*1024,new[] {new PageOrder("Id")},
            new[] {new AppFilter {FieldName="CompanyId",Operator="=",FilterValue="a"}}));
        using var json=JsonDocument.Parse(response.CopyPayload());
        Assert.Equal(2,response.TotalRecords);
        var absent=json.RootElement[0];var populated=json.RootElement[1];
        foreach(var field in new[] {"ApprovedOn","Amount","Flag"})Assert.Equal(JsonValueKind.Null,absent.GetProperty(field).ValueKind);
        Assert.Equal(new DateTime(2026,10,11,12,34,56).AddTicks(1234567),populated.GetProperty("ApprovedOn").GetDateTime());
        Assert.Equal(12.345m,populated.GetProperty("Amount").GetDecimal());Assert.True(populated.GetProperty("Flag").GetBoolean());
    }


    private static void RelatedTable(Harness h)
    {
        using var command=h.Connection.CreateCommand();
        command.CommandText="CREATE TABLE Assignments(Id TEXT PRIMARY KEY, ParentId TEXT, CompanyId TEXT, Actor TEXT, State TEXT, Expiry TEXT)"; command.ExecuteNonQuery();
        h.Source.Entities.Add(new EntityStructure{EntityName="Assignments",Fields=new[]{"Id","ParentId","CompanyId","Actor","State","Expiry"}.Select(name=>
            new EntityField{FieldName=name,Fieldtype=name=="Expiry"?typeof(DateTime).FullName:typeof(string).FullName,IsKey=name=="Id"}).ToList()});
    }
    private static void Assign(Harness h,string parent,string company,string actor,string state="Pending",DateTime? expiry=null)
    {
        using var command=h.Connection.CreateCommand(); command.CommandText="INSERT INTO Assignments VALUES($id,$parent,$company,$actor,$state,$expiry)";
        command.Parameters.AddWithValue("$id",Guid.NewGuid().ToString()); command.Parameters.AddWithValue("$parent",parent); command.Parameters.AddWithValue("$company",company);
        command.Parameters.AddWithValue("$actor",actor); command.Parameters.AddWithValue("$state",state); command.Parameters.AddWithValue("$expiry",(object?)expiry?.ToString("O")??DBNull.Value); command.ExecuteNonQuery();
    }
    private static PageRelatedFilter Relation(string actor,IEnumerable<AppFilter>? expiry=null)=>new("Assignments",
        new[]{new PageRelatedKey("Id","ParentId"),new PageRelatedKey("CompanyId","CompanyId")},
        new[]{new AppFilter{FieldName="Actor",Operator="equalsignorecase",FilterValue=actor},new AppFilter{FieldName="State",Operator="=",FilterValue="Pending"}},expiry);
    [Fact]
    public async Task Related_assignment_is_applied_before_count_and_page_without_duplicate_parents_or_cross_company_matches()
    {
        using var h=new Harness(); RelatedTable(h); var actor="reviewer'_%[$";
        for(var i=0;i<43;i++){var id=i.ToString("D2");h.Insert(id,"a","SKU","Needle "+id);if(i<31){Assign(h,id,"a",actor);Assign(h,id,"a",actor);}}
        h.Insert("foreign","b","SKU","Needle foreign");Assign(h,"foreign","b",actor);
        h.Insert("wrong-step-company","a","SKU","Needle wrong");Assign(h,"wrong-step-company","b",actor);
        var required=new[]{new AppFilter{FieldName="CompanyId",Operator="=",FilterValue="a"}};
        var search=new[]{new AppFilter{FieldName="Name",Operator="contains",FilterValue="Needle"}};
        var page=await h.Source.ReadPageAsync("Products",new(2,10,4096,new[]{new PageOrder("Id")},required,search,new[]{Relation(actor)}));
        using var json=JsonDocument.Parse(page.CopyPayload()); Assert.Equal(31,page.TotalRecords);Assert.Equal(10,json.RootElement.GetArrayLength());
        Assert.Equal(Enumerable.Range(10,10).Select(i=>i.ToString("D2")),json.RootElement.EnumerateArray().Select(row=>row.GetProperty("Id").GetString()));
        Assert.All(h.Commands,command=>{Assert.Contains("EXISTS (SELECT 1 FROM",command.CommandText);Assert.DoesNotContain(actor,command.CommandText);});
        Assert.Same(h.Source.PageTransactions[0],h.Source.PageTransactions[1]);
    }
    [Fact]
    public async Task Related_expiry_alternatives_exclude_expired_assignments_and_preserve_null_or_future_deadlines()
    {
        using var h=new Harness(); RelatedTable(h);var now=DateTime.UtcNow;
        foreach(var id in new[]{"expired","future","indefinite"})h.Insert(id,"a","SKU",id);
        Assign(h,"expired","a","actor",expiry:now.AddMinutes(-1));Assign(h,"future","a","actor",expiry:now.AddMinutes(1));Assign(h,"indefinite","a","actor");
        var relation=Relation("actor",new[]{new AppFilter{FieldName="Expiry",Operator="is null"},new AppFilter{FieldName="Expiry",Operator=">",FilterValue=now.ToString("O"),valueType=typeof(DateTime).FullName!}});
        var result=await h.Source.ReadPageAsync("Products",new(1,10,4096,new[]{new PageOrder("Id")},null,null,new[]{relation}));
        Assert.Equal(2,result.TotalRecords);using var json=JsonDocument.Parse(result.CopyPayload());
        Assert.Equal(new[]{"future","indefinite"},json.RootElement.EnumerateArray().Select(row=>row.GetProperty("Id").GetString()));
    }
    [Theory]
    [InlineData("entity")][InlineData("parent")][InlineData("child")][InlineData("filter")][InlineData("type")]
    public async Task Invalid_related_metadata_is_refused_before_physical_commands(string defect)
    {
        using var h=new Harness(); RelatedTable(h);
        var relation=new PageRelatedFilter(defect=="entity"?"Assignments; DROP":"Assignments",
            new[]{new PageRelatedKey(defect=="parent"?"Id; DROP":defect=="type"?"Cost":"Id",defect=="child"?"ParentId; DROP":"ParentId")},
            new[]{new AppFilter{FieldName=defect=="filter"?"Actor; DROP":"Actor",Operator="=",FilterValue="actor"}});
        var error=await Record.ExceptionAsync(()=>h.Source.ReadPageAsync("Products",new(1,10,4096,new[]{new PageOrder("Id")},null,null,new[]{relation})));
        Assert.True(error is ArgumentException or NotSupportedException,error?.ToString());Assert.Empty(h.Commands);
    }
    [Fact]
    public void Related_predicates_copy_values_and_enforce_nonempty_keys_and_bounded_groups()
    {
        var filter=new AppFilter{FieldName="Actor",Operator="=",FilterValue="original"};var keys=new[]{new PageRelatedKey("Id","ParentId")};
        var relation=new PageRelatedFilter("Assignments",keys,new[]{filter});filter.FilterValue="changed";keys[0]=new("Cost","Id");
        var copy=relation.CopyFilters();copy[0].FilterValue="also changed";Assert.Equal("original",relation.CopyFilters()[0].FilterValue);Assert.Equal("Id",relation.Keys[0].ParentField);
        var groups=new[]{relation};var request=new BoundedPageRequest(1,10,4096,new[]{new PageOrder("Id")},null,null,groups);groups[0]=Relation("other");
        Assert.Equal("original",request.AnyRelatedFilters[0].CopyFilters()[0].FilterValue);
        Assert.Throws<ArgumentException>(()=>new PageRelatedFilter("Assignments",Array.Empty<PageRelatedKey>()));
        Assert.Throws<ArgumentException>(()=>new BoundedPageRequest(1,10,4096,null,null,null,Enumerable.Repeat(relation,9)));
    }

    private static BoundedPageRequest Request(long page = 1, int bytes = 1024 * 1024, string? search = null) =>
        new(page, 25, bytes, new[] { new PageOrder("Name"), new PageOrder("Id") },
            new[] { new AppFilter { FieldName = "CompanyId", Operator = "=", FilterValue = "a" },
                new AppFilter { FieldName = "ActiveInd", Operator = "=", FilterValue = "Y" } },
            search == null ? null : new[] { "Sku", "Name", "Barcode" }.Select(field => new AppFilter {
                FieldName = field, Operator = "contains", FilterValue = search }));

    private sealed class Source : RDBSource
    {
        public List<IDbCommand> Commands { get; } = new();
        public List<IDbConnection?> PageConnections { get; } = new();
        public List<IDbTransaction?> PageTransactions { get; } = new();
        public Action? BeforePage { get; set; }
        public Source() : base("bounded-test", null!, null!, DataSourceType.SqlLite, new ErrorsInfo()) { }
        protected override void ConfigureCommand(IDbCommand command)
        {
            Commands.Add(command);
            PageConnections.Add(command.Connection); PageTransactions.Add(command.Transaction);
            if (command.CommandText.Contains("ORDER BY")) BeforePage?.Invoke();
        }
    }
    private sealed class Harness : IDisposable
    {
        public Source Source { get; } = new();
        public List<IDbCommand> Commands => Source.Commands;
        public SqliteConnection Connection { get; } = new("Data Source=bounded-" + Guid.NewGuid().ToString("N") + ";Mode=Memory;Cache=Shared");
        public Harness()
        {
            Connection.Open();
            ((RDBDataConnection)Source.Dataconnection).DbConn = Connection;
            ((RDBDataConnection)Source.Dataconnection).ConnectionStatus = ConnectionState.Open;
            using var command = Connection.CreateCommand();
            command.CommandText = "CREATE TABLE Products(Id TEXT PRIMARY KEY, CompanyId TEXT, ActiveInd TEXT, Sku TEXT, Name TEXT, Barcode TEXT, Status TEXT, Cost TEXT)";
            command.ExecuteNonQuery();
            Source.Entities = new() { new EntityStructure { EntityName = "Products", Fields = new[] { "Id", "CompanyId", "ActiveInd", "Sku", "Name", "Barcode", "Status", "Cost" }.Select(name =>
                new EntityField { FieldName = name, Fieldtype = name == "Cost" ? typeof(decimal).FullName : typeof(string).FullName, IsKey = name == "Id" }).ToList() } };
        }
        public void Insert(string id, string company, string sku, string name)
        {
            using var command = Connection.CreateCommand();
            command.CommandText = "INSERT INTO Products VALUES($id,$company,'Y',$sku,$name,'BAR','Active','12.345')";
            command.Parameters.AddWithValue("$id",id); command.Parameters.AddWithValue("$company",company);
            command.Parameters.AddWithValue("$sku",sku); command.Parameters.AddWithValue("$name",name); command.ExecuteNonQuery();
        }
        public void Dispose() { Source.Dispose(); Connection.Dispose(); }
    }
}
