using Venly.Audit.Helper;

namespace Venly.Backend.Utilities.Tests.Audit;

public class AuditScopeTests
{
    [Fact]
    public void A_fresh_scope_carries_nothing()
    {
        var scope = new AuditScope();

        Assert.Null(scope.EntityId);
        Assert.Null(scope.BeforeValue);
        Assert.Null(scope.AfterValue);
        Assert.Null(scope.Rationale);
        Assert.False(scope.Suppressed);
    }

    [Fact]
    public void Setting_a_before_value_serialises_it_to_json()
    {
        var scope = new AuditScope();

        scope.SetBefore(new { status = "ACTIVE", tier = 2 });

        Assert.Equal("""{"status":"ACTIVE","tier":2}""", scope.BeforeValue);
    }

    [Fact]
    public void Setting_a_null_before_value_leaves_it_null_rather_than_the_string_null()
    {
        var scope = new AuditScope();

        scope.SetBefore(null);

        // "null" the four-character JSON literal would be a non-null jsonb value in the column, which reads as
        // "the previous state was recorded and it was nothing" rather than "no previous state was captured".
        Assert.Null(scope.BeforeValue);
    }

    [Fact]
    public void A_string_that_is_already_json_is_not_double_encoded()
    {
        var scope = new AuditScope();

        scope.SetAfter("""{"status":"CLOSED"}""");

        Assert.Equal("""{"status":"CLOSED"}""", scope.AfterValue);
    }

    [Fact]
    public void A_string_that_is_not_json_is_encoded_as_a_json_string()
    {
        var scope = new AuditScope();

        scope.SetAfter("CLOSED");

        Assert.Equal("\"CLOSED\"", scope.AfterValue);
    }

    [Fact]
    public void Suppressing_the_scope_is_recorded()
    {
        var scope = new AuditScope();

        scope.Suppress();

        Assert.True(scope.Suppressed);
    }

    [Fact]
    public void The_last_write_wins_for_each_field()
    {
        var scope = new AuditScope();

        scope.SetEntityId("CUS1");
        scope.SetEntityId("CUS2");
        scope.SetRationale("first");
        scope.SetRationale("second");

        Assert.Equal("CUS2", scope.EntityId);
        Assert.Equal("second", scope.Rationale);
    }
}
