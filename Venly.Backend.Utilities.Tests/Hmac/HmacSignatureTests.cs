using System.Security.Cryptography;
using System.Text;
using Venly.Backend.Common.Hmac;

namespace Venly.Backend.Utilities.Tests.Hmac;

public class HmacSignatureTests
{
    [Fact]
    public void Compute_without_a_nonce_matches_the_documented_signing_string_shape()
    {
        var signature = HmacSignature.Compute("secret", 1700000000, "POST", "/connect/token", "grant_type=x");

        var expectedBodyHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes("grant_type=x"))).ToLowerInvariant();
        var expectedSigningString = $"1700000000\nPOST\n/connect/token\n{expectedBodyHash}";
        var expected = Convert.ToBase64String(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes("secret"), Encoding.UTF8.GetBytes(expectedSigningString)));

        Assert.Equal(expected, signature);
    }

    [Fact]
    public void Compute_with_a_nonce_folds_it_into_the_signing_string()
    {
        var withNonce = HmacSignature.Compute("secret", 1700000000, "POST", "/x", "body", "nonce-1");
        var withoutNonce = HmacSignature.Compute("secret", 1700000000, "POST", "/x", "body");

        Assert.NotEqual(withNonce, withoutNonce);
    }

    [Fact]
    public void Compute_is_deterministic_for_the_same_inputs()
    {
        var first = HmacSignature.Compute("secret", 1700000000, "POST", "/x", "body");
        var second = HmacSignature.Compute("secret", 1700000000, "POST", "/x", "body");

        Assert.Equal(first, second);
    }
}
