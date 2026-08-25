using Microsoft.AspNetCore.Http;
using Venly.Audit.Helper;
using Venly.Backend.Common.Authentication;
using Venly.Messaging.Events;

namespace Venly.Backend.Utilities.Tests.Audit;

public class HttpContextAuditActorAccessorTests
{
    private static HttpContextAuditActorAccessor Build(HttpContext? context)
    {
        var accessor = A.Fake<IHttpContextAccessor>();
        A.CallTo(() => accessor.HttpContext).Returns(context);
        return new HttpContextAuditActorAccessor(accessor);
    }

    private static DefaultHttpContext WithHeaders(params (string Name, string Value)[] headers)
    {
        var context = new DefaultHttpContext();
        foreach (var (name, value) in headers)
            context.Request.Headers[name] = value;
        return context;
    }

    [Fact]
    public void A_staff_principal_header_becomes_a_staff_actor()
    {
        var accessor = Build(WithHeaders(
            (PrincipalHeaders.Id, "STFABCDEFGHIJKLMNOPQR"),
            (PrincipalHeaders.Type, "staff")));

        var actor = accessor.Current();

        Assert.Equal(AuditActorType.Staff, actor.Type);
        Assert.Equal("STFABCDEFGHIJKLMNOPQR", actor.Id);
    }

    [Fact]
    public void A_customer_principal_header_becomes_a_customer_actor()
    {
        var accessor = Build(WithHeaders(
            (PrincipalHeaders.Id, "CUSABCDEFGHIJKLMNOPQR"),
            (PrincipalHeaders.Type, "customer")));

        Assert.Equal(AuditActorType.Customer, accessor.Current().Type);
    }

    [Fact]
    public void No_http_context_at_all_is_the_system_actor()
    {
        // The Kafka consumer path. No request, no principal, and no id to attribute the action to.
        var actor = Build(null).Current();

        Assert.Equal(AuditActorType.System, actor.Type);
        Assert.Null(actor.Id);
    }

    [Fact]
    public void A_request_with_no_principal_header_is_the_service_actor()
    {
        // An HMAC-signed hop with no person behind it: a peer service called us directly.
        var actor = Build(WithHeaders(("X-Signature", "irrelevant"))).Current();

        Assert.Equal(AuditActorType.Service, actor.Type);
        Assert.Null(actor.Id);
    }

    [Fact]
    public void An_unrecognised_principal_type_is_service_rather_than_staff()
    {
        // Fails towards LESS authority. Guessing staff would attribute an action to a back-office operator
        // on the strength of a header nobody recognised.
        var actor = Build(WithHeaders(
            (PrincipalHeaders.Id, "XYZ1"),
            (PrincipalHeaders.Type, "robot"))).Current();

        Assert.Equal(AuditActorType.Service, actor.Type);
    }

    [Fact]
    public void The_checked_permission_becomes_the_permissions_snapshot()
    {
        var accessor = Build(WithHeaders(
            (PrincipalHeaders.Id, "STF1"),
            (PrincipalHeaders.Type, "staff"),
            (PrincipalHeaders.Permission, "staff.account.list")));

        var snapshot = accessor.Current().PermissionsSnapshot;

        Assert.NotNull(snapshot);
        Assert.Equal("staff.account.list", snapshot["checked"]);
        Assert.Equal("gateway", snapshot["source"]);
    }

    [Fact]
    public void No_permission_header_means_no_snapshot_rather_than_an_empty_one()
    {
        var accessor = Build(WithHeaders((PrincipalHeaders.Id, "STF1"), (PrincipalHeaders.Type, "staff")));

        // An empty object in the column would claim a snapshot was taken and found nothing, which is a
        // different fact from no snapshot having been taken at all.
        Assert.Null(accessor.Current().PermissionsSnapshot);
    }

    [Fact]
    public void The_source_ip_hash_and_correlation_id_come_straight_off_the_headers()
    {
        var accessor = Build(WithHeaders(
            (PrincipalHeaders.SourceIpHash, "deadbeef"),
            (PrincipalHeaders.CorrelationId, "corr-42")));

        var actor = accessor.Current();

        Assert.Equal("deadbeef", actor.SourceIpHash);
        Assert.Equal("corr-42", actor.CorrelationId);
    }
}
