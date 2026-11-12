using Aynesil.Application.Features.Assessment.Queries;
using Aynesil.Application.Features.PerformanceKpi.Queries;
using Aynesil.Application.Features.Scheduling.Queries;
using FluentAssertions;

namespace Aynesil.Tests.Api;

/// <summary>
/// Smoke-test bulgusu: zorunlu sorgu parametresi eksikken varsayılan değerle (0001-01-01, Guid.Empty)
/// sessizce hesap yapılıyordu veya 5xx dönüyordu; artık doğrulayıcı reddeder.
/// </summary>
public class QueryInputValidatorTests
{
    [Fact]
    public void EducatorDashboard_without_period_is_invalid()
    {
        var r = new GetEducatorDashboardQueryValidator()
            .Validate(new GetEducatorDashboardQuery(Guid.CreateVersion7(), default, default));
        r.IsValid.Should().BeFalse();
        r.Errors.Select(e => e.PropertyName).Should().Contain(["PeriodStart", "PeriodEnd"]);
    }

    [Fact]
    public void EducatorDashboard_without_educator_is_invalid()
    {
        var r = new GetEducatorDashboardQueryValidator()
            .Validate(new GetEducatorDashboardQuery(Guid.Empty, new(2026, 1, 1), new(2026, 1, 31)));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EducatorDashboard_with_reversed_period_is_invalid()
    {
        var r = new GetEducatorDashboardQueryValidator()
            .Validate(new GetEducatorDashboardQuery(Guid.CreateVersion7(), new(2026, 2, 1), new(2026, 1, 1)));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EducatorDashboard_with_valid_input_passes()
    {
        new GetEducatorDashboardQueryValidator()
            .Validate(new GetEducatorDashboardQuery(Guid.CreateVersion7(), new(2026, 1, 1), new(2026, 1, 31)))
            .IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    public void AssessmentHistory_requires_exactly_one_of_lead_or_student(bool lead, bool student, bool valid)
    {
        var q = new GetAssessmentHistoryQuery(lead ? Guid.CreateVersion7() : null, student ? Guid.CreateVersion7() : null);
        new GetAssessmentHistoryQueryValidator().Validate(q).IsValid.Should().Be(valid);
    }

    [Fact]
    public void SchoolCalendar_without_range_is_invalid()
    {
        new GetSchoolCalendarQueryValidator().Validate(new GetSchoolCalendarQuery(default, default))
            .IsValid.Should().BeFalse();
    }
}
