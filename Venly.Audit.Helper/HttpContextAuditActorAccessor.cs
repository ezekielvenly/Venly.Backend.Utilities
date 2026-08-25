using Microsoft.AspNetCore.Http;
using Venly.Backend.Common.Authentication;
using Venly.Messaging.Events;

namespace Venly.Audit.Helper;

/// <summary>
/// Reads the actor off the gateway's principal headers.
///
/// <para>Headers rather than <c>HttpContext.User</c> claims, and that is the important choice: a downstream
/// service does not always validate the token itself — the gateway already did, and on an HMAC-only route
/// there is no token in the request at all. The headers are present on every hop the gateway signs, which
/// makes them the one source that works for both kinds of route.</para>
///
/// <para>The fallbacks are ordered so that every one of them under-claims rather than over-claims: no context
/// is System, a context with no principal is Service, and an unrecognised principal type is Service. None of
/// them can invent a staff actor.</para>
/// </summary>
public sealed class HttpContextAuditActorAccessor(IHttpContextAccessor accessor) : IAuditActorAccessor
{
    public AuditActor Current()
    {
        var context = accessor.HttpContext;

        if (context is null)
        {
            // No request in flight: a Kafka consumer, a hosted service, a Temporal activity.
            return new AuditActor(AuditActorType.System, null, null, null, null);
        }

        var headers = context.Request.Headers;

        var id = Value(headers[PrincipalHeaders.Id]);
        var type = Value(headers[PrincipalHeaders.Type]);
        var permission = Value(headers[PrincipalHeaders.Permission]);

        var actorType = type switch
        {
            SendGramAuth.StaffPrincipal => AuditActorType.Staff,
            SendGramAuth.CustomerPrincipal => AuditActorType.Customer,
            _ => AuditActorType.Service,
        };

        var snapshot = permission is null
            ? null
            : new Dictionary<string, string?>
            {
                ["checked"] = permission,
                ["source"] = "gateway",
            };

        return new AuditActor(
            Type: actorType,
            Id: actorType == AuditActorType.Service ? null : id,
            PermissionsSnapshot: snapshot,
            SourceIpHash: Value(headers[PrincipalHeaders.SourceIpHash]),
            CorrelationId: Value(headers[PrincipalHeaders.CorrelationId]));
    }

    private static string? Value(Microsoft.Extensions.Primitives.StringValues values)
    {
        var value = values.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
