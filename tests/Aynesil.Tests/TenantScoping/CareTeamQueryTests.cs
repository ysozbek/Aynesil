using Aynesil.Application.Features.CareTeam.Queries;
using Aynesil.Domain.Modules.Educators.Entities;
using Aynesil.Domain.Modules.Scheduling.Entities;
using Aynesil.Domain.Modules.Students.Entities;
using FluentAssertions;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>Bakım ekibi arayüzünün dayandığı sorgular: seanslardan türetilen eğitmenler ve liste DTO'sundaki rowVersion.</summary>
public class CareTeamQueryTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static Session NewSession(Guid corp, int daysAgo, string? cancel = null)
    {
        var s = Session.Schedule(corp, Guid.NewGuid(), Now.AddDays(-daysAgo), Now.AddDays(-daysAgo).AddMinutes(45));
        if (cancel is not null) s.Cancel(cancel);
        return s;
    }

    [Fact]
    public async Task Session_educators_are_counted_per_educator_most_recent_first_and_skip_cancelled_sessions()
    {
        using var ctx = new TenantTestContext();
        var student = Guid.NewGuid();
        var ayse = Educator.Create(TenantA, "Ayşe", "Yılmaz");
        var can = Educator.Create(TenantA, "Can", "Demir");
        var onlyCancelled = Educator.Create(TenantA, "İptal", "Eden");
        var s1 = NewSession(TenantA, 10);
        var s2 = NewSession(TenantA, 2);
        var s3 = NewSession(TenantA, 5);
        var cancelled = NewSession(TenantA, 1, cancel: "test");
        await ctx.SeedAsync(ayse, can, onlyCancelled, s1, s2, s3, cancelled,
            SessionParticipant.Create(TenantA, s1.Id, student), SessionParticipant.Create(TenantA, s2.Id, student),
            SessionParticipant.Create(TenantA, s3.Id, student), SessionParticipant.Create(TenantA, cancelled.Id, student),
            SessionEducator.Assign(TenantA, s1.Id, ayse.Id), SessionEducator.Assign(TenantA, s2.Id, ayse.Id),
            SessionEducator.Assign(TenantA, s3.Id, can.Id), SessionEducator.Assign(TenantA, cancelled.Id, onlyCancelled.Id));

        var result = await ctx.Handler<GetStudentSessionEducatorsQueryHandler>()
            .Handle(new GetStudentSessionEducatorsQuery(student), default);

        result.Select(r => r.EducatorName).Should().Equal("Ayşe Yılmaz", "Can Demir");
        result[0].SessionCount.Should().Be(2);
        result[1].SessionCount.Should().Be(1);
    }

    [Fact]
    public async Task Session_educators_ignore_other_students_sessions()
    {
        using var ctx = new TenantTestContext();
        var student = Guid.NewGuid();
        var other = Guid.NewGuid();
        var edu = Educator.Create(TenantA, "Ayşe", "Yılmaz");
        var s = NewSession(TenantA, 3);
        await ctx.SeedAsync(edu, s, SessionParticipant.Create(TenantA, s.Id, other), SessionEducator.Assign(TenantA, s.Id, edu.Id));

        (await ctx.Handler<GetStudentSessionEducatorsQueryHandler>()
            .Handle(new GetStudentSessionEducatorsQuery(student), default)).Should().BeEmpty();
    }

    [Fact]
    public async Task Care_team_list_returns_the_row_version_needed_for_update_and_remove()
    {
        using var ctx = new TenantTestContext();
        var student = Student.Create(TenantA, "Ada", "Test");
        var edu = Educator.Create(TenantA, "Ayşe", "Yılmaz");
        var assignment = new StudentCareAssignment
        {
            CorporationId = TenantA, StudentId = student.Id, EducatorId = edu.Id, RoleId = Guid.NewGuid(),
            IsPrimary = true, Status = "active", ActiveFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), Reason = "ilk atama",
        };
        await ctx.SeedAsync(student, edu, assignment);

        var list = await ctx.Handler<GetStudentCareTeamQueryHandler>().Handle(new GetStudentCareTeamQuery(student.Id), default);

        list.Should().ContainSingle();
        list[0].RowVersion.Should().BeGreaterThan(0);
        list[0].Reason.Should().Be("ilk atama");
    }
}
