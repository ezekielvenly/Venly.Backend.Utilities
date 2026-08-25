using FluentValidation;

namespace Venly.Backend.Common.Validation;

/// <summary>
/// The rules every paginated query shares. Subclass it to add domain filters:
///
/// <code>
/// public sealed class AuditEntryQueryParametersValidator : QueryParametersValidator&lt;AuditEntryQueryParameters&gt;
/// {
///     public AuditEntryQueryParametersValidator() =&gt; RuleFor(p =&gt; p.ActorId).MaximumLength(64);
/// }
/// </code>
///
/// <para><b>Only three rules, and the omissions are deliberate.</b> <see cref="QueryParameters"/> guards some
/// of its own inputs in its setters, and a validation rule for something the type has already normalised can
/// never fire — it reads as protection while protecting nothing:</para>
///
/// <list type="bullet">
///   <item><c>SortOrder</c> is NOT validated. Its setter ignores anything that is not <c>asc</c> or <c>desc</c>,
///   so the getter never returns an invalid value and a rule here would always see a valid one. (Whether
///   silently ignoring a mistyped sort is the right behaviour is a separate question, and changing it would
///   change every existing caller.)</item>
///   <item><c>PageSize</c> is checked only for a positive lower bound. The setter already clamps the upper end
///   to 100, so an over-large value is unobservable by the time a validator runs.</item>
/// </list>
///
/// <para>What IS validated is the pair of inputs nothing guards, and both turn a 500 into a 400:
/// <c>PageNo</c> below 1 reaches <c>Skip((PageNo - 1) * PageSize)</c> as a negative count, which throws
/// <see cref="ArgumentOutOfRangeException"/>; <c>PageSize</c> of 0 or less does the same or silently returns an
/// empty page. The date pair is validated because a reversed range is a caller mistake that otherwise returns
/// zero rows and reads as "nothing happened".</para>
/// </summary>
public class QueryParametersValidator<T> : AbstractValidator<T>
    where T : QueryParameters
{
    protected QueryParametersValidator()
    {
        RuleFor(p => p.PageNo)
            .GreaterThan(0)
            .WithMessage("PageNo must be 1 or greater.");

        RuleFor(p => p.PageSize)
            .GreaterThan(0)
            .WithMessage("PageSize must be 1 or greater. It is capped at 100.");

        RuleFor(p => p)
            .Must(p => p.StartDate is null || p.EndDate is null || p.StartDate <= p.EndDate)
            .WithMessage("StartDate must not be after EndDate.")
            .OverridePropertyName(nameof(QueryParameters.StartDate));
    }
}
