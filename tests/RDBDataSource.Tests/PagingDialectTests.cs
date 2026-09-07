using TheTechIdea.Beep.Helpers.RDBMSHelpers;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// Regression tests for the Phase 4 paging consolidation (K11).
///
/// There were two paging implementations with opposite defaults: the synchronous path used
/// <c>RDBMSHelper.GetPagingSyntax</c> (thirteen engines, default LIMIT/OFFSET), while
/// <c>RDBSource.Modernization</c> carried its own switch (five engines, default OFFSET/FETCH). The
/// async path now delegates to the same helper, so these pin the helper's dialect table.
/// </summary>
public class PagingDialectTests
{
    [Theory]
    [InlineData(DataSourceType.SqlServer)]
    [InlineData(DataSourceType.AzureSQL)]
    [InlineData(DataSourceType.SqlCompact)]
    [InlineData(DataSourceType.Oracle)]
    [InlineData(DataSourceType.DB2)]
    public void SqlServerFamilyAndOracle_UseOffsetFetch(DataSourceType type)
    {
        string syntax = RDBMSHelper.GetPagingSyntax(type, pageNumber: 3, pageSize: 20);

        Assert.Contains("OFFSET 40 ROWS", syntax);
        Assert.Contains("FETCH NEXT 20 ROWS ONLY", syntax);
    }

    [Theory]
    [InlineData(DataSourceType.Mysql)]
    [InlineData(DataSourceType.MariaDB)]
    [InlineData(DataSourceType.Postgre)]
    [InlineData(DataSourceType.SqlLite)]
    [InlineData(DataSourceType.SnowFlake)]
    [InlineData(DataSourceType.Cockroach)]
    public void LimitOffsetEngines_UseLimitOffset(DataSourceType type)
    {
        string syntax = RDBMSHelper.GetPagingSyntax(type, pageNumber: 3, pageSize: 20);

        Assert.Equal("LIMIT 20 OFFSET 40", syntax);
    }

    [Fact]
    public void AzureSql_NoLongerFallsThroughToLimitOffset()
    {
        // Azure SQL is SQL Server; LIMIT/OFFSET is a syntax error there, so every paged read failed.
        string syntax = RDBMSHelper.GetPagingSyntax(DataSourceType.AzureSQL, 1, 10);

        Assert.DoesNotContain("LIMIT", syntax);
    }

    [Fact]
    public void SqlCompact_NoLongerFallsThroughToLimitOffset()
    {
        string syntax = RDBMSHelper.GetPagingSyntax(DataSourceType.SqlCompact, 1, 10);

        Assert.DoesNotContain("LIMIT", syntax);
    }

    [Fact]
    public void Firebird_KeepsItsOwnRowsSyntax()
    {
        string syntax = RDBMSHelper.GetPagingSyntax(DataSourceType.FireBird, pageNumber: 2, pageSize: 10);

        Assert.Equal("ROWS 11 TO 20", syntax);
    }

    [Fact]
    public void PageNumberAndSize_AreClampedToSaneValues()
    {
        string syntax = RDBMSHelper.GetPagingSyntax(DataSourceType.Postgre, pageNumber: 0, pageSize: 0);

        // page 0 -> 1, size 0 -> 10, so offset 0
        Assert.Equal("LIMIT 10 OFFSET 0", syntax);
    }
}
