namespace Venly.Audit.Helper;

/// <summary>
/// The action verbs that recur across services. ERD-audit's action_type is a text column and the set is open,
/// so this is a convenience and a naming convention rather than a closed enumeration — a service with a verb
/// of its own writes its own string.
///
/// The shape is <c>{entity}.{verb}</c> or <c>{domain}.{entity}.{verb}</c>, matching the permission-key shape
/// AdminService's catalogue already uses. Keeping the two shapes identical is what makes "which permission
/// allowed this action" answerable by looking at the two columns side by side.
/// </summary>
public static class AuditActions
{
    public const string Create = "create";
    public const string Amend = "amend";
    public const string Dispose = "dispose";
    public const string View = "view";
    public const string List = "list";
    public const string SignIn = "sign_in";
    public const string SignOut = "sign_out";
    public const string Grant = "grant";
    public const string Revoke = "revoke";

    /// <summary>The gateway's denial path. Pairs with a non-null denied_permission_key.</summary>
    public const string PermissionDenied = "permission.denied";

    /// <summary>The entity_type the gateway uses for a denial: the route, not a domain object.</summary>
    public const string RouteEntityType = "route";
}
