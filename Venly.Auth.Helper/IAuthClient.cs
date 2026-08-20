namespace Venly.Auth.Helper;

/// <summary>
/// The typed client for AuthService's two verified-assertion grants. Credentials never cross this seam: the
/// calling service has already authenticated the subject and this asks AuthService to mint a token from a
/// signed claim that it did.
/// </summary>
public interface IAuthClient
{
    Task<TokenPair?> IssueStaffTokensAsync(
        string staffAccountId, IReadOnlyList<string> roles, CancellationToken ct);

    Task<TokenPair?> IssueCustomerTokensAsync(
        string customerId, string verificationTier, string deviceId, string riskLevel, CancellationToken ct);
}
