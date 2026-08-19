using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Venly.Backend.Common.Errors;

public abstract class AppException : Exception
{
    public HttpStatusCode HttpStatusCode { get; }

    public virtual string Code => DeriveCode(GetType().Name);

    public string CallerClass { get; private set; } = string.Empty;
    public string CallerMethod { get; private set; } = string.Empty;
    public int CallerLine { get; private set; }

    protected AppException(HttpStatusCode httpStatusCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        HttpStatusCode = httpStatusCode;
    }

    public AppException DumpLocation(
        [CallerFilePath] string callerFile = "",
        [CallerMemberName] string callerMethod = "",
        [CallerLineNumber] int callerLine = 0)
    {
        CallerClass = Path.GetFileNameWithoutExtension(callerFile);
        CallerMethod = callerMethod;
        CallerLine = callerLine;
        return this;
    }

    private static string DeriveCode(string typeName)
    {
        var withoutSuffix = typeName.EndsWith("Exception", StringComparison.Ordinal)
            ? typeName[..^"Exception".Length]
            : typeName;

        var withSpaces = Regex.Replace(withoutSuffix, "([a-z])([A-Z])", "$1 $2");
        var words = withSpaces.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
            return string.Empty;

        var first = char.ToLowerInvariant(words[0][0]) + words[0][1..];
        var rest = words.Skip(1).Select(w => char.ToUpperInvariant(w[0]) + w[1..]);

        return string.Concat(new[] { first }.Concat(rest));
    }
}
