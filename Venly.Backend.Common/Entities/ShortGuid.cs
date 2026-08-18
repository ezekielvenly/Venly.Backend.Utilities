using System.Text;

namespace Venly.Backend.Common.Entities;

public sealed class ShortGuid
{
    private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string NewGuid(string seed)
    {
        if (string.IsNullOrWhiteSpace(seed) || seed.Length != 3)
            throw new ArgumentException("Id seed must be exactly 3 characters.", nameof(seed));

        var guid = Guid.NewGuid();
        var bytes = guid.ToByteArray();

        var sb = new StringBuilder();
        int bitBuffer = 0;
        int bitCount = 0;

        foreach (var b in bytes)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitCount += 8;
            while (bitCount >= 5)
            {
                bitCount -= 5;
                sb.Append(Base32Chars[(bitBuffer >> bitCount) & 0x1F]);
            }
        }

        if (bitCount > 0)
            sb.Append(Base32Chars[(bitBuffer << (5 - bitCount)) & 0x1F]);

        string idPart = sb.ToString().Substring(0, 19);

        return seed.ToUpperInvariant() + idPart;
    }
}
