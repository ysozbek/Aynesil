using Aynesil.Domain.Common;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using FluentValidation;

namespace Aynesil.Application.Features.Scheduling.Queries;

/// <summary>
/// Takvim aralığı sorgu dizesinden bağlanır; eksikse DateTimeOffset varsayılanı (0001-01-01) gelir.
/// </summary>
internal static class CalendarRangeRules
{
    public static void Apply<T>(AbstractValidator<T> v,
        System.Linq.Expressions.Expression<Func<T, DateTimeOffset>> from,
        System.Linq.Expressions.Expression<Func<T, DateTimeOffset>> to)
    {
        v.RuleFor(from).NotEqual(default(DateTimeOffset)).WithKey(MsgKeys.Validation.Required, Msg.FieldArg("From"));
        v.RuleFor(to).NotEqual(default(DateTimeOffset)).WithKey(MsgKeys.Validation.Required, Msg.FieldArg("To"));
        var f = from.Compile();
        var t = to.Compile();
        v.RuleFor(x => x)
            .Must(x => f(x) == default || t(x) == default || f(x) < t(x))
            .WithName(_ => Msg.Field("To"))
            .WithKey(MsgKeys.Validation.MustBeAfter, Msg.FieldArg("To"), Msg.FieldArg("From"));
    }
}

public sealed class GetSchoolCalendarQueryValidator : AbstractValidator<GetSchoolCalendarQuery>
{
    public GetSchoolCalendarQueryValidator() => CalendarRangeRules.Apply(this, x => x.From, x => x.To);
}

public sealed class GetCampusCalendarQueryValidator : AbstractValidator<GetCampusCalendarQuery>
{
    public GetCampusCalendarQueryValidator()
    {
        RuleFor(x => x.CampusId).NotEmpty();
        CalendarRangeRules.Apply(this, x => x.From, x => x.To);
    }
}

public sealed class GetRoomCalendarQueryValidator : AbstractValidator<GetRoomCalendarQuery>
{
    public GetRoomCalendarQueryValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        CalendarRangeRules.Apply(this, x => x.From, x => x.To);
    }
}

public sealed class GetEducatorCalendarQueryValidator : AbstractValidator<GetEducatorCalendarQuery>
{
    public GetEducatorCalendarQueryValidator()
    {
        RuleFor(x => x.EducatorId).NotEmpty();
        CalendarRangeRules.Apply(this, x => x.From, x => x.To);
    }
}

public sealed class GetStudentCalendarQueryValidator : AbstractValidator<GetStudentCalendarQuery>
{
    public GetStudentCalendarQueryValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        CalendarRangeRules.Apply(this, x => x.From, x => x.To);
    }
}

public sealed class GetCalendarEntriesQueryValidator : AbstractValidator<GetCalendarEntriesQuery>
{
    public GetCalendarEntriesQueryValidator() => CalendarRangeRules.Apply(this, x => x.From, x => x.To);
}
