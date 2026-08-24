using Microsoft.AspNetCore.Authorization;
using Venly.Backend.Common.Authentication;

namespace Venly.Backend.Common;

/// <summary>
/// Base for a controller a CLIENT reaches — an app or a browser, through the gateway. Authentication is the
/// bearer token AuthService minted, validated against the same parameters the gateway uses.
///
/// The default is closed: inheriting this requires a valid token on every action. A credential ceremony, where
/// the caller cannot yet have a token, opts out per action with <c>[AllowAnonymous]</c> — sign-in, registration,
/// verification, password reset. Those are anonymous because there is no principal yet, NOT because they are
/// unimportant, and each one is declared in GatewayService's <c>permissions.map.json</c> so the edge and the
/// service agree on which they are.
///
/// Where a route is restricted to one kind of principal, prefer
/// <c>[Authorize(Policy = SendGramAuth.StaffOnlyPolicy)]</c> on the action over the class default: a valid
/// customer token is still a valid token, and a staff-only surface that merely requires "authenticated" is
/// reachable by any customer who has signed in.
/// </summary>
[Authorize(AuthenticationSchemes = SendGramAuth.BearerScheme)]
public abstract class ClientController : BaseController
{
}
