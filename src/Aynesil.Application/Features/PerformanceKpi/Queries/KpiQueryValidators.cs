using FluentValidation;

namespace Aynesil.Application.Features.PerformanceKpi.Queries;

/// <summary>
/// Dönem parametreleri sorgu dizesinden bağlanır; eksikse DateOnly varsayılanı (0001-01-01) gelir
/// ve sorgu sessizce yanlış/boş sonuç döner. Bu yüzden zorunlu alanlar burada reddedilir.
/// </summary>
internal static class KpiPeriodRules
{
    public static void Apply<T>(AbstractValidator<T> v,
        System.Linq.Expressions.Expression<Func<T, DateOnly>> start,
        System.Linq.Expressions.Expression<Func<T, DateOnly>> end)
    {
        v.RuleFor(start).NotEqual(default(DateOnly)).WithMessage("Dönem başlangıcı (periodStart) zorunludur.");
        v.RuleFor(end).NotEqual(default(DateOnly)).WithMessage("Dönem bitişi (periodEnd) zorunludur.");
        var s = start.Compile();
        var e = end.Compile();
        v.RuleFor(x => x)
            .Must(x => s(x) == default || e(x) == default || s(x) <= e(x))
            .WithName("PeriodEnd")
            .WithMessage("Dönem bitişi, başlangıçtan önce olamaz.");
    }
}

public sealed class GetEducatorDashboardQueryValidator : AbstractValidator<GetEducatorDashboardQuery>
{
    public GetEducatorDashboardQueryValidator()
    {
        RuleFor(x => x.EducatorId).NotEmpty().WithMessage("Eğitmen kimliği zorunludur.");
        KpiPeriodRules.Apply(this, x => x.PeriodStart, x => x.PeriodEnd);
    }
}

public sealed class GetManagerDashboardQueryValidator : AbstractValidator<GetManagerDashboardQuery>
{
    public GetManagerDashboardQueryValidator() =>
        KpiPeriodRules.Apply(this, x => x.PeriodStart, x => x.PeriodEnd);
}

public sealed class GetExecutiveDashboardQueryValidator : AbstractValidator<GetExecutiveDashboardQuery>
{
    public GetExecutiveDashboardQueryValidator() =>
        KpiPeriodRules.Apply(this, x => x.PeriodStart, x => x.PeriodEnd);
}

public sealed class GetKpiReportQueryValidator : AbstractValidator<GetKpiReportQuery>
{
    public GetKpiReportQueryValidator() =>
        KpiPeriodRules.Apply(this, x => x.PeriodStart, x => x.PeriodEnd);
}

public sealed class GetEducatorRankingQueryValidator : AbstractValidator<GetEducatorRankingQuery>
{
    public GetEducatorRankingQueryValidator()
    {
        RuleFor(x => x.KpiCode).NotEmpty().WithMessage("KPI kodu (kpiCode) zorunludur.");
        KpiPeriodRules.Apply(this, x => x.PeriodStart, x => x.PeriodEnd);
    }
}

public sealed class GetEducatorKpiTrendQueryValidator : AbstractValidator<GetEducatorKpiTrendQuery>
{
    public GetEducatorKpiTrendQueryValidator()
    {
        RuleFor(x => x.EducatorId).NotEmpty().WithMessage("Eğitmen kimliği zorunludur.");
        RuleFor(x => x.KpiCode).NotEmpty().WithMessage("KPI kodu (kpiCode) zorunludur.");
        RuleFor(x => x.MaxPeriods).InclusiveBetween(1, 12).WithMessage("maxPeriods 1–12 arasında olmalıdır.");
    }
}
