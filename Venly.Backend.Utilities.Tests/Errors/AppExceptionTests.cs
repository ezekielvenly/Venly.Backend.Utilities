using System.Net;
using Venly.Backend.Common.Errors;

namespace Venly.Backend.Utilities.Tests.Errors;

public class AppExceptionTests
{
    private sealed class ResourceNotFoundException(string message) : AppException(HttpStatusCode.NotFound, message)
    {
    }

    [Fact]
    public void Code_is_derived_from_a_single_word_type_name()
    {
        var exception = UnauthorizedException.InvalidCredentials();

        Assert.Equal("unauthorized", exception.Code);
    }

    [Fact]
    public void Code_is_derived_as_camelCase_from_a_compound_type_name()
    {
        var exception = new ResourceNotFoundException("not found");

        Assert.Equal("resourceNotFound", exception.Code);
    }

    [Fact]
    public void DumpLocation_captures_the_throw_sites_class_and_method()
    {
        var exception = ThrowAndCatch();

        Assert.Equal(nameof(AppExceptionTests), exception.CallerClass);
        Assert.Equal(nameof(ThrowAndCatch), exception.CallerMethod);
        Assert.True(exception.CallerLine > 0);
    }

    private static AppException ThrowAndCatch()
    {
        try
        {
            throw UnauthorizedException.InvalidCredentials().DumpLocation();
        }
        catch (AppException ex)
        {
            return ex;
        }
    }

    [Fact]
    public void Without_DumpLocation_caller_fields_stay_empty()
    {
        var exception = UnauthorizedException.InvalidCredentials();

        Assert.Equal(string.Empty, exception.CallerClass);
        Assert.Equal(string.Empty, exception.CallerMethod);
        Assert.Equal(0, exception.CallerLine);
    }
}
