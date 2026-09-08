using System;
using System.Data.Common;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// Regression tests for the Phase 7 cache and resilience work (K16, K17, K18).
/// </summary>
public class CacheAndResilienceTests
{
    internal sealed class Row
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private static SqliteHarness TableHarness()
    {
        var h = SqliteHarness.Create(entityType: typeof(Row));
        h.Execute("CREATE TABLE T (Id INTEGER PRIMARY KEY, Name TEXT)");
        h.Register(SqliteHarness.Entity("T",
            SqliteHarness.Field("Id", "System.Int32", isKey: true),
            SqliteHarness.Field("Name")));
        return h;
    }

    // ---- K18 / K17: entity-structure cache invalidation ----

    [Fact]
    public void GetEntityStructure_IsCachedBetweenCalls()
    {
        using var h = TableHarness();

        var first = h.Source.GetEntityStructure("T", refresh: true);
        var second = h.Source.GetEntityStructure("T", refresh: false);

        Assert.Same(first, second);
    }

    [Fact]
    public void ClearAllCaches_EmptiesTheStructureCache()
    {
        using var h = TableHarness();

        h.Source.GetEntityStructure("T", refresh: true);
        h.Source.ClearAllCaches();

        // Nothing observable breaks, and a subsequent lookup still returns a usable structure.
        // Note this does NOT force a database re-read for a registered entity — see
        // StructureCacheEviction_DoesNotByItselfForceAReRead below.
        var after = h.Source.GetEntityStructure("T", refresh: false);

        Assert.NotNull(after);
        Assert.Equal(2, after.Fields.Count);
    }

    [Fact]
    public void StructureCacheEviction_DoesNotByItselfForceAReRead()
    {
        using var h = TableHarness();

        var before = h.Source.GetEntityStructure("T", refresh: true);
        h.Source.ClearAllCaches();
        var after = h.Source.GetEntityStructure("T", refresh: false);

        // Pinning a limitation, not a fix. LoadEntityStructure resolves the entity out of the
        // Entities list and hands back that same instance, and GetEntityStructure(fnd, refresh:
        // false) returns it unchanged — so for an entity that is already registered, evicting the
        // cache re-runs the lookup but re-queries nothing. Cache invalidation only forces a real
        // read for entities that are NOT in Entities (where the loader sets refresh itself).
        //
        // Reading a changed schema still requires refresh: true. Tracked as the open half of K18.
        Assert.Same(before, after);
    }

    [Fact]
    public void StructureIsReReadWhenRefreshIsRequested()
    {
        using var h = TableHarness();

        var before = h.Source.GetEntityStructure("T", refresh: true);
        Assert.Equal(2, before.Fields.Count);

        h.Source.ExecuteSql("ALTER TABLE T ADD COLUMN Extra TEXT");

        var after = h.Source.GetEntityStructure("T", refresh: true);
        Assert.Equal(3, after.Fields.Count);
    }

    // ---- K16: transient classification ----

    private sealed class FakeTimeoutException : Exception
    {
        public FakeTimeoutException() : base("some provider wording") { }
    }

    private static bool IsTransient(RDBSource source, Exception ex) =>
        (bool)typeof(RDBSource)
            .GetMethod("IsTransientException", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(source, new object[] { ex })!;

    private static RDBSource NewSource() =>
        new RDBSource("resilience-probe", null, null, DataSourceType.SqlServer,
                      new ErrorsInfo { Flag = Errors.Ok });

    [Fact]
    public void DeterministicFailures_AreNotTreatedAsTransient()
    {
        var source = NewSource();

        // These are the ones that mattered: the classifier used to return true for ANY exception
        // whose type name contained "sqlexception" or "dbexception", so a primary-key violation or a
        // syntax error was retried three times with 1/2/4-second delays before failing anyway.
        Assert.False(IsTransient(source, new InvalidOperationException("Violation of PRIMARY KEY constraint")));
        Assert.False(IsTransient(source, new InvalidOperationException("Incorrect syntax near 'WHERE'")));
        Assert.False(IsTransient(source, new InvalidOperationException("Invalid object name 'Orders'")));
        Assert.False(IsTransient(source, new InvalidOperationException("permission denied for relation")));
    }

    [Theory]
    [InlineData("Execution timeout expired")]
    [InlineData("Transaction was deadlocked on lock resources")]
    [InlineData("A transport-level error has occurred")]
    [InlineData("connection reset by peer")]
    [InlineData("too many connections")]
    public void GenuinelyTransientMessages_AreStillTransient(string message)
    {
        Assert.True(IsTransient(NewSource(), new InvalidOperationException(message)));
    }

    [Fact]
    public void ATimeoutExceptionType_IsTransientWhateverTheMessageSays()
    {
        // The message tests are locale-dependent; the type test is the reliable half.
        Assert.True(IsTransient(NewSource(), new FakeTimeoutException()));
    }

    [Fact]
    public void ATransientCauseIsFoundThroughTheInnerExceptionChain()
    {
        var inner = new InvalidOperationException("deadlock detected");
        var outer = new InvalidOperationException("wrapper", new InvalidOperationException("wrapper2", inner));

        Assert.True(IsTransient(NewSource(), outer));
    }

    [Fact]
    public void ADeepInnerExceptionChain_TerminatesRatherThanSpinning()
    {
        // The walk used to recurse into InnerException with no depth bound and no cycle guard.
        Exception ex = new InvalidOperationException("permanent");
        for (int i = 0; i < 200; i++)
            ex = new InvalidOperationException($"layer {i}", ex);

        Assert.False(IsTransient(NewSource(), ex));
    }

    [Fact]
    public void HealthCheckQuery_UsesAFromClauseWhereTheEngineNeedsOne()
    {
        string Query(DataSourceType type)
        {
            var s = new RDBSource("hc", null, null, type, new ErrorsInfo { Flag = Errors.Ok });
            return (string)typeof(RDBSource)
                .GetMethod("GetHealthCheckQuery", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(s, null)!;
        }

        // Firebird and Hana previously fell through to a bare "SELECT 1", which is a syntax error on
        // both — so a healthy connection was reported unhealthy.
        Assert.Contains("RDB$DATABASE", Query(DataSourceType.FireBird));
        Assert.Contains("DUMMY", Query(DataSourceType.Hana));
        Assert.Contains("DUAL", Query(DataSourceType.Oracle));
        Assert.Equal("SELECT 1", Query(DataSourceType.SqlServer));
    }
}
