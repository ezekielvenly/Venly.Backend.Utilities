using Npgsql;
using Npgsql.NameTranslation;

namespace Venly.Database.Helper;

/// <summary>
/// How a C# enum becomes a PostgreSQL enum type: <c>snake_case</c> type names, <c>UPPER_SNAKE_CASE</c> labels.
///
/// <para>
/// So <c>StaffAccountStatus.InvitePending</c> is stored as <c>staff_account_status.'INVITE_PENDING'</c>. The
/// two halves are deliberately different, and each for its own reason.
/// </para>
/// <para>
/// <b>Labels are uppercase</b> because they are the values a person reads out of a table, and a status shouting
/// its own name is easier to scan in a column of them than one blending into the surrounding text. It also
/// makes a status visibly a status rather than free text.
/// </para>
/// <para>
/// <b>Type names stay lowercase</b> because PostgreSQL folds unquoted identifiers to lower case. An uppercase
/// type name would exist only as a quoted identifier, so every reference to it in migrations, casts and
/// <c>\dT</c> output would need quoting forever, and the first place someone forgot would be a confusing
/// "type does not exist".
/// </para>
/// <para>
/// The label is derived by uppercasing <see cref="NpgsqlSnakeCaseNameTranslator"/>'s output rather than by
/// splitting words again here. That keeps this exactly one transformation away from the library default —
/// which matters, since it means a label is always the old lowercase label uppercased, and the migration that
/// introduced this could be a plain <c>ALTER TYPE ... RENAME VALUE</c> per label with no column rewrite. It
/// also inherits the default's quirks rather than inventing new ones: <c>Tier1Verified</c> is
/// <c>TIER1VERIFIED</c>, not <c>TIER1_VERIFIED</c>.
/// </para>
/// </summary>
public sealed class VenlyEnumNameTranslator : INpgsqlNameTranslator
{
    private static readonly NpgsqlSnakeCaseNameTranslator SnakeCase = new();

    /// <summary>Shared instance. The translator is stateless, and both halves of an enum's configuration
    /// (<c>HasPostgresEnum</c> and <c>MapEnum</c>) must be given the SAME translation or they will disagree
    /// about what the type is called.</summary>
    public static readonly VenlyEnumNameTranslator Instance = new();

    public string TranslateTypeName(string clrName) => SnakeCase.TranslateTypeName(clrName);

    public string TranslateMemberName(string clrName) =>
        SnakeCase.TranslateMemberName(clrName).ToUpperInvariant();
}
