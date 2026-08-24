using Venly.Backend.Common.Hmac;

namespace Venly.Backend.Common;

/// <summary>
/// Base for a controller whose callers are OTHER SERVICES: the gateway resolving permissions or reporting a
/// denial, or a peer service using one of the Venly.*.Helper clients. Every route is HMAC-signed with the
/// receiving service's <c>HmacSettings:Secret</c>.
///
/// No human ever reaches one of these directly, which is why an account-existence oracle or a permission
/// revocation is acceptable here and nowhere else. Routes on this base belong under an <c>/internal</c> prefix
/// and must not appear in GatewayService's published route table.
/// </summary>
[HmacAuthorize]
public abstract class ServiceController : BaseController
{
}
