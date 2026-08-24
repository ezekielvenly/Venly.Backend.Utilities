using Microsoft.AspNetCore.Mvc;

namespace Venly.Backend.Common;

/// <summary>
/// The shared base for every controller. It carries <c>[ApiController]</c> and DELIBERATELY NO AUTHENTICATION.
///
/// It used to carry <c>[HmacAuthorize]</c>, which meant every route in every service demanded a service
/// signature — registration, sign-in and the login ceremonies included. That is the wrong instrument for those
/// routes: an HMAC signature proves the caller holds a service secret, so it authenticates the GATEWAY, never
/// the person behind it. AuthService's TokenController already had to opt out of this base class for exactly
/// that reason ("so a client that cannot sign can refresh"), which was the shape of the problem showing through.
///
/// Derive from one of the two subclasses instead, and derive from THIS one only when a single controller
/// genuinely serves both audiences — in which case every action must carry its own attribute:
/// <list type="bullet">
///   <item><see cref="ServiceController"/> — service-to-service. HMAC.</item>
///   <item><see cref="ClientController"/> — client-facing. Bearer token.</item>
/// </list>
///
/// Because this base authenticates nothing, an action that declares nothing is PUBLIC. That is what the
/// per-service controller-convention tests exist to catch: they fail the build unless every action declares
/// exactly one of <c>[HmacAuthorize]</c>, <c>[Authorize]</c> or <c>[AllowAnonymous]</c>.
/// </summary>
[ApiController]
public abstract class BaseController : ControllerBase
{
}
