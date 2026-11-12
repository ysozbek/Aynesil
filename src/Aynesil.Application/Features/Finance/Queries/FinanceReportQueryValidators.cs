using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using FluentValidation;

namespace Aynesil.Application.Features.Finance.Queries;

public sealed class GetRevenueReportQueryValidator : AbstractValidator<GetRevenueReportQuery>
{
    public GetRevenueReportQueryValidator()
    {
        RuleFor(x => x.PeriodStart).NotEqual(default(DateOnly)).WithKey(MsgKeys.Validation.Required, Msg.FieldArg("PeriodStart"));
        RuleFor(x => x.PeriodEnd).NotEqual(default(DateOnly)).WithKey(MsgKeys.Validation.Required, Msg.FieldArg("PeriodEnd"));
        RuleFor(x => x)
            .Must(x => x.PeriodStart == default || x.PeriodEnd == default || x.PeriodStart <= x.PeriodEnd)
            .WithName(_ => Msg.Field("PeriodEnd"))
            .WithKey(MsgKeys.Validation.MustNotBeBefore, Msg.FieldArg("PeriodEnd"), Msg.FieldArg("PeriodStart"));
    }
}

public sealed class GetCreditUsageReportQueryValidator : AbstractValidator<GetCreditUsageReportQuery>
{
    public GetCreditUsageReportQueryValidator()
    {
        RuleFor(x => x.PeriodStart).NotEqual(default(DateOnly)).WithKey(MsgKeys.Validation.Required, Msg.FieldArg("PeriodStart"));
        RuleFor(x => x.PeriodEnd).NotEqual(default(DateOnly)).WithKey(MsgKeys.Validation.Required, Msg.FieldArg("PeriodEnd"));
        RuleFor(x => x)
            .Must(x => x.PeriodStart == default || x.PeriodEnd == default || x.PeriodStart <= x.PeriodEnd)
            .WithName(_ => Msg.Field("PeriodEnd"))
            .WithKey(MsgKeys.Validation.MustNotBeBefore, Msg.FieldArg("PeriodEnd"), Msg.FieldArg("PeriodStart"));
    }
}
