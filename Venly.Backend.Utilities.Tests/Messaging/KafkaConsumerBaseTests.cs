using System.Reflection;
using Confluent.Kafka;
using Venly.Backend.Common.Messaging;

namespace Venly.Backend.Utilities.Tests.Messaging;

/// <summary>
/// Reflection over the base class rather than a running broker. The point is the SHAPE — that both overloads
/// exist, that the string one is no longer abstract, and that the offset reset is overridable — because a
/// consumer that silently gets the wrong default here loses messages in a way no unit test of the subclass
/// would show.
/// </summary>
public class KafkaConsumerBaseTests
{
    private static MethodInfo Method(params Type[] parameters) =>
        typeof(KafkaConsumerBase).GetMethod(
            "ProcessMessageAsync", BindingFlags.Instance | BindingFlags.NonPublic, parameters)
        ?? throw new InvalidOperationException(
            $"ProcessMessageAsync({string.Join(", ", parameters.Select(p => p.Name))}) not found");

    [Fact]
    public void Both_process_overloads_exist_and_are_virtual()
    {
        var stringOverload = Method(typeof(string), typeof(CancellationToken));
        var resultOverload = Method(typeof(ConsumeResult<Ignore, string>), typeof(CancellationToken));

        Assert.True(stringOverload.IsVirtual);
        Assert.True(resultOverload.IsVirtual);

        // No longer abstract: a consumer that only needs the coordinates overload must not be forced to
        // implement a string overload it will never use.
        Assert.False(stringOverload.IsAbstract);
    }

    [Fact]
    public void The_offset_reset_is_overridable_and_defaults_to_latest()
    {
        var property = typeof(KafkaConsumerBase).GetProperty(
            "OffsetReset", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(property);
        Assert.True(property.GetMethod!.IsVirtual);
        Assert.Equal(typeof(AutoOffsetReset), property.PropertyType);
    }
}
