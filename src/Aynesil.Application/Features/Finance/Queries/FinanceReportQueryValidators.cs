using FluentValidation;

namespace Aynesil.Application.Features.Finance.Queries;

public sealed class GetRevenueReportQueryValidator : AbstractValidator<GetRevenueReportQuery>
{
    public GetRevenueReportQueryValidator()
    {
        RuleFor(x => x.PeriodStart).NotEqual(default(DateOnly)).WithMessage("Dönem başlangıcı (periodStart) zorunludur.");
        RuleFor(x => x.PeriodEnd).NotEqual(default(DateOnly)).WithMessage("Dönem bitişi (periodEnd) zorunludur.");
        RuleFor(x => x)
            .Must(x => x.PeriodStart == default || x.PeriodEnd == default || x.PeriodStart <= x.PeriodEnd)
            .WithName("PeriodEnd")
            .WithMessage("Dönem bitişi, başlangıçtan önce olamaz.");
    }
}

public sealed class GetCreditUsageReportQueryValidator : AbstractValidator<GetCreditUsageReportQuery>
{
    public GetCreditUsageReportQueryValidator()
    {
        RuleFor(x => x.PeriodStart).NotEqual(default(DateOnly)).WithMessage("Dönem başlangıcı (periodStart) zorunludur.");
        RuleFor(x => x.PeriodEnd).NotEqual(default(DateOnly)).WithMessage("Dönem bitişi (periodEnd) zorunludur.");
        RuleFor(x => x)
            .Must(x => x.PeriodStart == default || x.PeriodEnd == default || x.PeriodStart <= x.PeriodEnd)
            .WithName("PeriodEnd")
            .WithMessage("Dönem bitişi, başlangıçtan önce olamaz.");
    }
}
