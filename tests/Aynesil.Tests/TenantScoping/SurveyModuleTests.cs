using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.PerformanceKpi.Commands;
using Aynesil.Application.Features.Surveys.Commands;
using Aynesil.Application.Features.Surveys.Queries;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Educators.Entities;
using Aynesil.Domain.Modules.Ops.Entities;
using Aynesil.Domain.Modules.Scheduling.Entities;
using Aynesil.Domain.Modules.Students.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Anket modülü: soru düzenleme kilidi, sonuç özeti, bekleyen anketler, tekrar yanıt, zorunlu soru,
/// velinin yalnızca kendi çocuğuyla ilgili anket/geri bildirim kuralları ve KPI veli memnuniyeti bağı.
/// </summary>
public class SurveyModuleTests
{
    private static (Guardian Guardian, Student Child, GuardianPortalAccess Access) SignedInGuardian(TenantTestContext ctx)
    {
        var guardian = Guardian.Create(TenantA, "Ayşe", "Kaya");
        guardian.LinkPortalAccount(ctx.UserId);
        var child = Student.Create(TenantA, "Can", "Kaya");
        var access = new GuardianPortalAccess { CorporationId = TenantA, GuardianId = guardian.Id, StudentId = child.Id };
        return (guardian, child, access);
    }

    private static (SurveyDefinition Survey, SurveyQuestion Rating, SurveyQuestion Choice, SurveyAnswerOption Yes,
        SurveyAnswerOption No, SurveyQuestion Text) NewSurvey(string target = "guardian")
    {
        var survey = SurveyDefinition.Create(TenantA, "Memnuniyet", target);
        var rating = SurveyQuestion.Create(survey.Id, "Genel memnuniyet", "rating", true, 1);
        var choice = SurveyQuestion.Create(survey.Id, "İletişim", "multiple_choice", true, 2);
        var yes = SurveyAnswerOption.Create(choice.Id, "Evet", "10", 1);
        var no = SurveyAnswerOption.Create(choice.Id, "Hayır", "0", 2);
        var text = SurveyQuestion.Create(survey.Id, "Öneri", "text", false, 3);
        return (survey, rating, choice, yes, no, text);
    }

    private static object[] All((SurveyDefinition Survey, SurveyQuestion Rating, SurveyQuestion Choice,
        SurveyAnswerOption Yes, SurveyAnswerOption No, SurveyQuestion Text) s) =>
        [s.Survey, s.Rating, s.Choice, s.Yes, s.No, s.Text];

    private static SubmitSurveyResponseCommand Answer(
        (SurveyDefinition Survey, SurveyQuestion Rating, SurveyQuestion Choice, SurveyAnswerOption Yes,
            SurveyAnswerOption No, SurveyQuestion Text) s, Guid? studentId, decimal rating = 5, Guid? option = null,
        string? text = null, Guid? sessionId = null) =>
        new(s.Survey.Id, null, studentId, sessionId,
        [
            new QuestionAnswerInput(s.Rating.Id, null, null, rating),
            new QuestionAnswerInput(s.Choice.Id, null, option ?? s.Yes.Id, null),
            new QuestionAnswerInput(s.Text.Id, text, null, null),
        ]);

    private static async Task ShouldBlockAsync(Func<Task> act, MsgKey key)
    {
        var ex = await Record.ExceptionAsync(act);
        ex.Should().BeOfType<BusinessRuleViolationException>();
        ((BusinessRuleViolationException)ex!).Code.Should().Be(key);
    }

    // ── Soru düzenleme ────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_replaces_questions_and_options_when_no_responses()
    {
        using var ctx = new TenantTestContext();
        var s = NewSurvey();
        await ctx.SeedAsync(All(s));

        await ctx.Handler<UpdateSurveyDefinitionCommandHandler>().Handle(new UpdateSurveyDefinitionCommand(
            s.Survey.Id, "Yeni başlık", null, "guardian", null, true, 1,
            [
                new QuestionUpdateInput(s.Choice.Id, "İletişim (yeni)", "multiple_choice", true, 1,
                [
                    new AnswerOptionUpdateInput(s.Yes.Id, "Evet!", "10", 1),
                    new AnswerOptionUpdateInput(null, "Kısmen", "5", 2),
                ]),
                new QuestionUpdateInput(null, "Yeni soru", "yes_no", false, 2, []),
            ]), default);

        var questions = await ctx.Db.SurveyQuestions.Where(q => q.SurveyId == s.Survey.Id).OrderBy(q => q.SortOrder).ToListAsync();
        questions.Select(q => q.QuestionText).Should().Equal("İletişim (yeni)", "Yeni soru");
        (await ctx.Db.SurveyQuestions.AnyAsync(q => q.Id == s.Rating.Id)).Should().BeFalse("olmayan soru silinir");
        var options = await ctx.Db.SurveyAnswerOptions.Where(o => o.QuestionId == s.Choice.Id).OrderBy(o => o.SortOrder).ToListAsync();
        options.Select(o => o.OptionText).Should().Equal("Evet!", "Kısmen");
        (await ctx.Db.SurveyDefinitions.SingleAsync()).Title.Should().Be("Yeni başlık");
    }

    [Fact]
    public async Task Update_without_questions_leaves_questions_untouched()
    {
        using var ctx = new TenantTestContext();
        var s = NewSurvey();
        await ctx.SeedAsync(All(s));

        await ctx.Handler<UpdateSurveyDefinitionCommandHandler>().Handle(
            new UpdateSurveyDefinitionCommand(s.Survey.Id, "Başlık", null, "guardian", null, false, 1), default);

        (await ctx.Db.SurveyQuestions.CountAsync()).Should().Be(3);
        (await ctx.Db.SurveyDefinitions.SingleAsync()).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Update_questions_is_locked_once_responses_exist_but_header_still_edits()
    {
        using var ctx = new TenantTestContext();
        var s = NewSurvey();
        var response = SurveyResponse.Create(TenantA, s.Survey.Id);
        response.Submit();
        await ctx.SeedAsync([.. All(s), response]);

        await ShouldBlockAsync(() => ctx.Handler<UpdateSurveyDefinitionCommandHandler>().Handle(
            new UpdateSurveyDefinitionCommand(s.Survey.Id, "Başlık", null, "guardian", null, true, 1,
                [new QuestionUpdateInput(null, "Yeni", "text", false, 1, [])]), default),
            MsgKeys.Rule.SurveyQuestionsLocked);
        (await ctx.Db.SurveyQuestions.CountAsync()).Should().Be(3);

        await ctx.Handler<UpdateSurveyDefinitionCommandHandler>().Handle(
            new UpdateSurveyDefinitionCommand(s.Survey.Id, "Sadece başlık", null, "guardian", null, true, 1), default);
        (await ctx.Db.SurveyDefinitions.SingleAsync()).Title.Should().Be("Sadece başlık");
    }

    [Fact]
    public async Task Update_rejects_a_question_id_of_another_survey()
    {
        using var ctx = new TenantTestContext();
        var s = NewSurvey();
        var other = NewSurvey();
        await ctx.SeedAsync([.. All(s), .. All(other)]);

        var act = () => ctx.Handler<UpdateSurveyDefinitionCommandHandler>().Handle(
            new UpdateSurveyDefinitionCommand(s.Survey.Id, "B", null, "guardian", null, true, 1,
                [new QuestionUpdateInput(other.Rating.Id, "Çalıntı", "rating", false, 1, [])]), default);

        await act.Should().ThrowAsync<Aynesil.Application.Common.Exceptions.ValidationException>();
        (await ctx.Db.SurveyQuestions.AsNoTracking().SingleAsync(q => q.Id == other.Rating.Id)).QuestionText
            .Should().Be("Genel memnuniyet");
    }

    [Fact]
    public async Task Update_and_delete_cannot_touch_another_tenants_survey()
    {
        using var ctx = new TenantTestContext();
        var foreign = SurveyDefinition.Create(TenantB, "Yabancı");
        await ctx.SeedAsync(foreign);

        await TenantTestContext.ShouldRejectAsync(() => ctx.Handler<UpdateSurveyDefinitionCommandHandler>().Handle(
            new UpdateSurveyDefinitionCommand(foreign.Id, "Ele geçirildi", null, "guardian", null, true, 1), default));
        await TenantTestContext.ShouldRejectAsync(() => ctx.Handler<DeleteSurveyDefinitionCommandHandler>().Handle(
            new DeleteSurveyDefinitionCommand(foreign.Id), default));
        await TenantTestContext.ShouldRejectAsync(() => ctx.Handler<GetSurveyResultsQueryHandler>().Handle(
            new GetSurveyResultsQuery(foreign.Id), default));
        (await ctx.Db.SurveyDefinitions.SingleAsync()).Title.Should().Be("Yabancı");
    }

    [Fact]
    public async Task Delete_deactivates_when_responses_exist_and_soft_deletes_otherwise()
    {
        using var ctx = new TenantTestContext();
        var used = SurveyDefinition.Create(TenantA, "Yanıtlı");
        var unused = SurveyDefinition.Create(TenantA, "Boş");
        var response = SurveyResponse.Create(TenantA, used.Id);
        await ctx.SeedAsync(used, unused, response);

        var handler = ctx.Handler<DeleteSurveyDefinitionCommandHandler>();
        await handler.Handle(new DeleteSurveyDefinitionCommand(used.Id), default);
        await handler.Handle(new DeleteSurveyDefinitionCommand(unused.Id), default);

        var rows = await ctx.Db.SurveyDefinitions.IgnoreQueryFilters().AsNoTracking().ToListAsync();
        rows.Single(r => r.Id == used.Id).Should().Match<SurveyDefinition>(r => !r.IsActive && r.DeletedAt == null);
        rows.Single(r => r.Id == unused.Id).DeletedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData("multiple_choice", 1, false)]
    [InlineData("multiple_choice", 2, true)]
    [InlineData("rating", 0, true)]
    public void Create_validator_requires_two_options_for_multiple_choice(string type, int optionCount, bool valid)
    {
        var options = Enumerable.Range(1, optionCount).Select(i => new AnswerOptionInput($"S{i}", null, i)).ToList();
        var cmd = new CreateSurveyDefinitionCommand("Anket", "guardian", null, null,
            [new QuestionInput("Soru", type, false, 1, options)]);

        new CreateSurveyDefinitionCommandValidator().Validate(cmd).IsValid.Should().Be(valid);
    }

    // ── Yanıt kuralları ───────────────────────────────────────────────────────

    [Fact]
    public async Task Submit_blocks_second_response_for_same_survey_guardian_and_child()
    {
        using var ctx = new TenantTestContext();
        var (guardian, child, access) = SignedInGuardian(ctx);
        var s = NewSurvey();
        await ctx.SeedAsync([guardian, child, access, .. All(s)]);
        var handler = ctx.Handler<SubmitSurveyResponseCommandHandler>();

        await handler.Handle(Answer(s, child.Id), default);

        await ShouldBlockAsync(() => handler.Handle(Answer(s, child.Id), default), MsgKeys.Rule.SurveyAlreadySubmitted);
        (await ctx.Db.SurveyResponses.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Submit_allows_same_survey_for_a_second_child()
    {
        using var ctx = new TenantTestContext();
        var (guardian, child, access) = SignedInGuardian(ctx);
        var sibling = Student.Create(TenantA, "Ece", "Kaya");
        var siblingAccess = new GuardianPortalAccess { CorporationId = TenantA, GuardianId = guardian.Id, StudentId = sibling.Id };
        var s = NewSurvey();
        await ctx.SeedAsync([guardian, child, access, sibling, siblingAccess, .. All(s)]);
        var handler = ctx.Handler<SubmitSurveyResponseCommandHandler>();

        await handler.Handle(Answer(s, child.Id), default);
        await handler.Handle(Answer(s, sibling.Id), default);

        (await ctx.Db.SurveyResponses.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Submit_rejects_unanswered_required_question_and_out_of_range_rating()
    {
        using var ctx = new TenantTestContext();
        var (guardian, child, access) = SignedInGuardian(ctx);
        var s = NewSurvey();
        await ctx.SeedAsync([guardian, child, access, .. All(s)]);
        var handler = ctx.Handler<SubmitSurveyResponseCommandHandler>();

        var missingChoice = new SubmitSurveyResponseCommand(s.Survey.Id, null, child.Id, null,
            [new QuestionAnswerInput(s.Rating.Id, null, null, 4)]);
        var ex1 = await Record.ExceptionAsync(() => handler.Handle(missingChoice, default));
        ex1.Should().BeOfType<Aynesil.Application.Common.Exceptions.ValidationException>();
        ((Aynesil.Application.Common.Exceptions.ValidationException)ex1!).Errors.Should().NotBeEmpty();

        var act = () => handler.Handle(Answer(s, child.Id, rating: 9), default);
        await act.Should().ThrowAsync<Aynesil.Application.Common.Exceptions.ValidationException>();
        (await ctx.Db.SurveyResponses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Submit_rejects_survey_not_addressed_to_guardians()
    {
        using var ctx = new TenantTestContext();
        var (guardian, child, access) = SignedInGuardian(ctx);
        var s = NewSurvey(target: "educator");
        await ctx.SeedAsync([guardian, child, access, .. All(s)]);

        await ShouldBlockAsync(() => ctx.Handler<SubmitSurveyResponseCommandHandler>().Handle(Answer(s, child.Id), default),
            MsgKeys.Rule.SurveyTargetMismatch);
    }

    [Fact]
    public async Task Submit_rejects_session_of_a_child_the_guardian_cannot_access()
    {
        using var ctx = new TenantTestContext();
        var (guardian, child, access) = SignedInGuardian(ctx);
        var stranger = Student.Create(TenantA, "Başka", "Çocuk");
        var session = Session.Schedule(TenantA, Guid.CreateVersion7(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        var participant = SessionParticipant.Create(TenantA, session.Id, stranger.Id);
        var s = NewSurvey();
        await ctx.SeedAsync([guardian, child, access, stranger, session, participant, .. All(s)]);

        var act = () => ctx.Handler<SubmitSurveyResponseCommandHandler>().Handle(
            Answer(s, child.Id, sessionId: session.Id), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    // ── Sonuç özeti ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Results_summarize_each_question_from_submitted_responses_only()
    {
        using var ctx = new TenantTestContext();
        var s = NewSurvey();
        var draft = SurveyResponse.Create(TenantA, s.Survey.Id);   // submitted_at null → sayılmaz
        var done1 = SurveyResponse.Create(TenantA, s.Survey.Id); done1.Submit();
        var done2 = SurveyResponse.Create(TenantA, s.Survey.Id); done2.Submit();
        await ctx.SeedAsync([.. All(s), draft, done1, done2,
            SurveyQuestionResponse.Create(draft.Id, s.Rating.Id, null, null, 1m),
            SurveyQuestionResponse.Create(done1.Id, s.Rating.Id, null, null, 5m),
            SurveyQuestionResponse.Create(done2.Id, s.Rating.Id, null, null, 4m),
            SurveyQuestionResponse.Create(done1.Id, s.Choice.Id, null, s.Yes.Id, null),
            SurveyQuestionResponse.Create(done2.Id, s.Choice.Id, null, s.No.Id, null),
            SurveyQuestionResponse.Create(done1.Id, s.Text.Id, "Harika", null, null)]);

        var r = await ctx.Handler<GetSurveyResultsQueryHandler>().Handle(new GetSurveyResultsQuery(s.Survey.Id), default);

        r.ResponseCount.Should().Be(2);
        var rating = r.Questions.Single(q => q.QuestionId == s.Rating.Id);
        rating.AnsweredCount.Should().Be(2);
        rating.Average.Should().Be(4.5m);
        rating.Distribution.Select(b => b.Count).Should().Equal(0, 0, 0, 1, 1);
        var choice = r.Questions.Single(q => q.QuestionId == s.Choice.Id);
        choice.Distribution.Select(b => (b.Label, b.Count)).Should().Equal(("Evet", 1), ("Hayır", 1));
        choice.Average.Should().Be(5m, "seçenek değerleri sayısalsa ortalama alınır");
        r.Questions.Single(q => q.QuestionId == s.Text.Id).TextAnswers.Should().Equal("Harika");
    }

    // ── Bekleyen anketler ─────────────────────────────────────────────────────

    [Fact]
    public async Task Pending_lists_only_unanswered_active_guardian_surveys_per_accessible_child()
    {
        using var ctx = new TenantTestContext();
        var (guardian, child, access) = SignedInGuardian(ctx);
        var sibling = Student.Create(TenantA, "Ece", "Kaya");
        var siblingAccess = new GuardianPortalAccess { CorporationId = TenantA, GuardianId = guardian.Id, StudentId = sibling.Id };
        var revoked = Student.Create(TenantA, "Eski", "Çocuk");
        var revokedAccess = new GuardianPortalAccess { CorporationId = TenantA, GuardianId = guardian.Id, StudentId = revoked.Id };
        revokedAccess.RevokedAt = DateTimeOffset.UtcNow;
        var open = NewSurvey();
        var forEducators = NewSurvey("educator");
        var inactive = NewSurvey();
        inactive.Survey.Deactivate();
        var otherTenant = SurveyDefinition.Create(TenantB, "Yabancı");
        var done = SurveyResponse.Create(TenantA, open.Survey.Id, ctx.UserId, guardian.Id, child.Id);
        done.Submit();
        await ctx.SeedAsync([guardian, child, access, sibling, siblingAccess, revoked, revokedAccess,
            .. All(open), .. All(forEducators), .. All(inactive), otherTenant, done]);

        var pending = await ctx.Handler<GetPendingSurveysQueryHandler>().Handle(new GetPendingSurveysQuery(), default);

        var only = pending.Should().ContainSingle().Subject;
        only.SurveyId.Should().Be(open.Survey.Id);
        only.Students.Select(x => x.StudentId).Should().Equal(sibling.Id);   // cevaplanan çocuk ve erişimi kalkan çocuk yok
        only.Questions.Should().HaveCount(3);
    }

    [Fact]
    public async Task Pending_is_empty_for_staff_without_guardian_link()
    {
        using var ctx = new TenantTestContext();
        await ctx.SeedAsync(NewSurvey().Survey);

        (await ctx.Handler<GetPendingSurveysQueryHandler>().Handle(new GetPendingSurveysQuery(), default)).Should().BeEmpty();
    }

    // ── Veli geri bildirimi ───────────────────────────────────────────────────

    private static async Task<(Guardian G, Student Child, Educator Teacher, Session Session)> SeedTaughtChild(TenantTestContext ctx)
    {
        var (guardian, child, access) = SignedInGuardian(ctx);
        var teacher = Educator.Create(TenantA, "Mert", "Eğitmen");
        var session = Session.Schedule(TenantA, Guid.CreateVersion7(), DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(-1).AddHours(1));
        await ctx.SeedAsync(guardian, child, access, teacher, session,
            SessionParticipant.Create(TenantA, session.Id, child.Id),
            SessionEducator.Assign(TenantA, session.Id, teacher.Id));
        return (guardian, child, teacher, session);
    }

    [Fact]
    public async Task Feedback_by_guardian_for_own_childs_educator_and_session_is_saved_with_own_guardian_id()
    {
        using var ctx = new TenantTestContext();
        var (guardian, _, teacher, session) = await SeedTaughtChild(ctx);

        var dto = await ctx.Handler<CreateParentFeedbackCommandHandler>().Handle(
            new CreateParentFeedbackCommand(null, teacher.Id, session.Id, 5, "Süper"), default);

        dto.GuardianId.Should().Be(guardian.Id);
        dto.EducatorId.Should().Be(teacher.Id);
    }

    [Fact]
    public async Task Feedback_for_a_session_without_educator_credits_the_sessions_educator()
    {
        using var ctx = new TenantTestContext();
        var (_, _, teacher, session) = await SeedTaughtChild(ctx);

        var dto = await ctx.Handler<CreateParentFeedbackCommandHandler>().Handle(
            new CreateParentFeedbackCommand(null, null, session.Id, 4, null), default);

        dto.EducatorId.Should().Be(teacher.Id);
    }

    [Fact]
    public async Task Feedback_by_guardian_for_educator_or_session_unrelated_to_their_children_is_forbidden()
    {
        using var ctx = new TenantTestContext();
        await SeedTaughtChild(ctx);
        var strangerTeacher = Educator.Create(TenantA, "Yabancı", "Eğitmen");
        var strangerSession = Session.Schedule(TenantA, Guid.CreateVersion7(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        var otherChild = Student.Create(TenantA, "Başka", "Çocuk");
        await ctx.SeedAsync(strangerTeacher, strangerSession, otherChild,
            SessionParticipant.Create(TenantA, strangerSession.Id, otherChild.Id),
            SessionEducator.Assign(TenantA, strangerSession.Id, strangerTeacher.Id));
        var handler = ctx.Handler<CreateParentFeedbackCommandHandler>();

        await ((Func<Task>)(() => handler.Handle(new CreateParentFeedbackCommand(null, strangerTeacher.Id, null, 1, null), default)))
            .Should().ThrowAsync<ForbiddenAccessException>();
        await ((Func<Task>)(() => handler.Handle(new CreateParentFeedbackCommand(null, null, strangerSession.Id, 1, null), default)))
            .Should().ThrowAsync<ForbiddenAccessException>();
        (await ctx.Db.ParentFeedbacks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Feedback_list_is_scoped_to_the_signed_in_guardian_whatever_filter_is_sent()
    {
        using var ctx = new TenantTestContext();
        var (guardian, _, teacher, _) = await SeedTaughtChild(ctx);
        var other = Guardian.Create(TenantA, "Başka", "Veli");
        await ctx.SeedAsync(other,
            ParentFeedback.Create(TenantA, guardian.Id, teacher.Id, null, 4, "benim"),
            ParentFeedback.Create(TenantA, other.Id, teacher.Id, null, 1, "başkası"),
            ParentFeedback.Create(TenantB, null, null, null, 3, "yabancı kurum"));

        var all = await ctx.Handler<GetParentFeedbackQueryHandler>().Handle(
            new GetParentFeedbackQuery { GuardianId = other.Id }, default);
        var mine = await ctx.Handler<GetMyParentFeedbackQueryHandler>().Handle(new GetMyParentFeedbackQuery(), default);

        all.Items.Should().ContainSingle().Which.Comment.Should().Be("benim");
        mine.Items.Should().ContainSingle().Which.Should().Match<Aynesil.Application.Features.Surveys.Dtos.ParentFeedbackListItemDto>(
            f => f.Comment == "benim" && f.EducatorName == "Mert Eğitmen" && f.GuardianName == "Ayşe Kaya");
    }

    [Fact]
    public async Task Feedback_list_for_staff_sees_all_of_the_tenant_but_not_other_tenants()
    {
        using var ctx = new TenantTestContext();
        var g1 = Guardian.Create(TenantA, "Bir", "Veli");
        var g2 = Guardian.Create(TenantA, "İki", "Veli");
        await ctx.SeedAsync(g1, g2,
            ParentFeedback.Create(TenantA, g1.Id, null, null, 5, null),
            ParentFeedback.Create(TenantA, g2.Id, null, null, 3, null),
            ParentFeedback.Create(TenantB, null, null, null, 1, "yabancı"));

        var page = await ctx.Handler<GetParentFeedbackQueryHandler>().Handle(new GetParentFeedbackQuery(), default);

        page.TotalCount.Should().Be(2);
        (await ctx.Handler<GetMyParentFeedbackQueryHandler>().Handle(new GetMyParentFeedbackQuery(), default)).Items.Should().BeEmpty();
    }

    // ── KPI veli memnuniyeti ──────────────────────────────────────────────────

    [Fact]
    public async Task Guardian_feedback_ratings_feed_the_educator_kpi_parent_satisfaction()
    {
        using var ctx = new TenantTestContext();
        var (guardian, _, teacher, session) = await SeedTaughtChild(ctx);
        var create = ctx.Handler<CreateParentFeedbackCommandHandler>();
        await create.Handle(new CreateParentFeedbackCommand(null, teacher.Id, session.Id, 5, null), default);
        await create.Handle(new CreateParentFeedbackCommand(null, teacher.Id, session.Id, 4, "iyi"), default);
        await create.Handle(new CreateParentFeedbackCommand(null, teacher.Id, null, null, "puansız yorum"), default);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var snapshot = await ctx.Handler<ComputePerformanceSnapshotCommandHandler>().Handle(
            new ComputePerformanceSnapshotCommand(teacher.Id, today.AddDays(-30), today.AddDays(1)), default);

        snapshot.ParentFeedbackAvg.Should().Be(4.5m, "yalnızca puanlı geri bildirimler ortalamaya girer");
        guardian.Id.Should().NotBeEmpty();
    }
}
