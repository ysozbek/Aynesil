using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using FluentValidation;

namespace Aynesil.Application.Features.Leads.Queries;

public sealed class GetConversionReportQueryValidator : AbstractValidator<GetConversionReportQuery>
{
    public GetConversionReportQueryValidator()
    {
        RuleFor(x => x.From).NotEqual(default(DateTimeOffset)).WithKey(MsgKeys.Validation.Required, Msg.FieldArg("From"));
        RuleFor(x => x.To).NotEqual(default(DateTimeOffset)).WithKey(MsgKeys.Validation.Required, Msg.FieldArg("To"));
        RuleFor(x => x)
            .Must(x => x.From == default || x.To == default || x.From <= x.To)
            .WithName(_ => Msg.Field("To"))
            .WithKey(MsgKeys.Validation.MustNotBeBefore, Msg.FieldArg("To"), Msg.FieldArg("From"));
    }
}
