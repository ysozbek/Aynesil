using FluentValidation;

namespace Aynesil.Application.Features.Leads.Queries;

public sealed class GetConversionReportQueryValidator : AbstractValidator<GetConversionReportQuery>
{
    public GetConversionReportQueryValidator()
    {
        RuleFor(x => x.From).NotEqual(default(DateTimeOffset)).WithMessage("Başlangıç (from) zorunludur.");
        RuleFor(x => x.To).NotEqual(default(DateTimeOffset)).WithMessage("Bitiş (to) zorunludur.");
        RuleFor(x => x)
            .Must(x => x.From == default || x.To == default || x.From <= x.To)
            .WithName("To")
            .WithMessage("Bitiş, başlangıçtan önce olamaz.");
    }
}
