using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.Assessment.Commands;
using Aynesil.Application.Features.Goals.Commands;
using Aynesil.Application.Features.Legal.Commands;
using Aynesil.Application.Features.Menus.Commands;
using Aynesil.Application.Features.Notifications.Commands;
using Aynesil.Application.Features.Programs.Commands;
using Aynesil.Application.Features.RefData.Commands;
using Aynesil.Application.Features.Roles.Commands;
using Aynesil.Application.Features.Surveys.Commands;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Assessment.Entities;
using Aynesil.Domain.Modules.Core.Entities;
using Aynesil.Domain.Modules.Education.Entities;
using Aynesil.Domain.Modules.Iam.Entities;
using Aynesil.Domain.Modules.Legal.Entities;
using Aynesil.Domain.Modules.Ops.Entities;
using Aynesil.Domain.Modules.Ref.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Y2, Y3, Y4, Y6 ve aynı kök neden: corporation_id kolonu olmayan alt tablolara (role_permission,
/// *_translation, şablon bölüm/madde, anket soru/seçenek) yazan handler'lar üst kaydın kiracısını
/// doğrular. Başka kiracının üst kaydı → 404; global/platform üst kaydı → 403 (PlatformRecordReadOnly).
/// InMemory sağlayıcısında RLS yoktur: red, handler'ın kendi kontrolünü kanıtlar.
/// </summary>
public class ChildTableOwnershipTests
{
    private static async Task ShouldBePlatformReadOnlyAsync(Func<Task> act)
    {
        var ex = await act.Should().ThrowAsync<ForbiddenAccessException>();
        ex.Which.Code.Should().Be(MsgKeys.Rule.PlatformRecordReadOnly);
    }

    private static async Task ShouldBeNotFoundAsync(Func<Task> act) =>
        await act.Should().ThrowAsync<NotFoundException>();

    // ── Y2: role_permission ───────────────────────────────────────────────────

    private static (Role Role, Permission Permission, RolePermission Grant) RoleWithGrant(Guid? corporationId)
    {
        var role = Role.Create(corporationId, "r" + Guid.NewGuid().ToString("N")[..6], "Rol");
        var permission = new Permission { Code = "student:read" + Guid.NewGuid().ToString("N")[..4], Resource = "student", Action = "read" };
        return (role, permission, new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
    }

    [Fact]
    public async Task Remove_role_permission_of_another_tenants_role_is_not_found_and_keeps_the_grant()
    {
        using var ctx = new TenantTestContext();
        var (role, permission, grant) = RoleWithGrant(TenantB);
        await ctx.SeedAsync(role, permission, grant);

        await ShouldBeNotFoundAsync(() => ctx.Handler<RemoveRolePermissionCommandHandler>()
            .Handle(new RemoveRolePermissionCommand(role.Id, permission.Id), default));

        (await ctx.Db.RolePermissions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Remove_role_permission_of_a_global_role_is_rejected()
    {
        using var ctx = new TenantTestContext();
        var (role, permission, grant) = RoleWithGrant(null);
        await ctx.SeedAsync(role, permission, grant);

        await ShouldBePlatformReadOnlyAsync(() => ctx.Handler<RemoveRolePermissionCommandHandler>()
            .Handle(new RemoveRolePermissionCommand(role.Id, permission.Id), default));

        (await ctx.Db.RolePermissions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Remove_role_permission_of_own_role_succeeds()
    {
        using var ctx = new TenantTestContext();
        var (role, permission, grant) = RoleWithGrant(TenantA);
        await ctx.SeedAsync(role, permission, grant);

        await ctx.Handler<RemoveRolePermissionCommandHandler>()
            .Handle(new RemoveRolePermissionCommand(role.Id, permission.Id), default);

        (await ctx.Db.RolePermissions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Assign_role_permission_to_a_global_role_is_rejected()
    {
        using var ctx = new TenantTestContext();
        var (role, permission, _) = RoleWithGrant(null);
        await ctx.SeedAsync(role, permission);

        await ShouldBePlatformReadOnlyAsync(() => ctx.Handler<AssignRolePermissionCommandHandler>()
            .Handle(new AssignRolePermissionCommand(role.Id, permission.Id), default));

        (await ctx.Db.RolePermissions.CountAsync()).Should().Be(0);
    }

    // ── Y3: assessment_section / assessment_item / assessment_template_translation ─

    private static async Task<(AssessmentTemplate Template, AssessmentSection Section, AssessmentItem Item)> SeedTemplateAsync(
        TenantTestContext ctx, Guid? corporationId)
    {
        var template = AssessmentTemplate.Create("t" + Guid.NewGuid().ToString("N")[..6], "Şablon", corporationId);
        var section = AssessmentSection.Create(template.Id, "s1");
        var item = AssessmentItem.Create(section.Id, "i1", "Soru", "scale");
        await ctx.SeedAsync(template, section, item);
        return (template, section, item);
    }

    public static TheoryData<string> AssessmentWrites() =>
        ["add-section", "update-section", "delete-section", "add-item", "update-item", "delete-item", "translation"];

    private static Task RunAssessmentWrite(TenantTestContext ctx, string op,
        (AssessmentTemplate Template, AssessmentSection Section, AssessmentItem Item) t) => op switch
    {
        "add-section" => ctx.Handler<AddAssessmentSectionCommandHandler>()
            .Handle(new AddAssessmentSectionCommand(t.Template.Id, "s2", 1, null), default),
        "update-section" => ctx.Handler<UpdateAssessmentSectionCommandHandler>()
            .Handle(new UpdateAssessmentSectionCommand(t.Section.Id, "s1x", 2, null), default),
        "delete-section" => ctx.Handler<DeleteAssessmentSectionCommandHandler>()
            .Handle(new DeleteAssessmentSectionCommand(t.Section.Id), default),
        "add-item" => ctx.Handler<AddAssessmentItemCommandHandler>()
            .Handle(new AddAssessmentItemCommand(t.Section.Id, "i2", "Yeni", "scale", null, 1m, 1), default),
        "update-item" => ctx.Handler<UpdateAssessmentItemCommandHandler>()
            .Handle(new UpdateAssessmentItemCommand(t.Item.Id, "i1x", "Değişti", "scale", null, 1m, 1), default),
        "delete-item" => ctx.Handler<DeleteAssessmentItemCommandHandler>()
            .Handle(new DeleteAssessmentItemCommand(t.Item.Id), default),
        "translation" => ctx.Handler<UpsertTemplateTranslationCommandHandler>()
            .Handle(new UpsertTemplateTranslationCommand(t.Template.Id, "en", "Template", null), default),
        _ => throw new InvalidOperationException(op),
    };

    private static async Task<(string, string, int, int, int)> AssessmentSnapshotAsync(TenantTestContext ctx) =>
        (await ctx.Db.AssessmentSections.AsNoTracking().Select(s => s.Code).SingleAsync(),
         await ctx.Db.AssessmentItems.AsNoTracking().Select(i => i.Prompt).SingleAsync(),
         await ctx.Db.AssessmentSections.CountAsync(),
         await ctx.Db.AssessmentItems.CountAsync(),
         await ctx.Db.AssessmentTemplateTranslations.CountAsync());

    [Theory]
    [MemberData(nameof(AssessmentWrites))]
    public async Task Assessment_template_content_of_a_global_template_cannot_be_changed(string op)
    {
        using var ctx = new TenantTestContext();
        var t = await SeedTemplateAsync(ctx, null);
        var before = await AssessmentSnapshotAsync(ctx);

        await ShouldBePlatformReadOnlyAsync(() => RunAssessmentWrite(ctx, op, t));

        ctx.Db.ChangeTracker.Clear();
        (await AssessmentSnapshotAsync(ctx)).Should().Be(before);
    }

    [Theory]
    [MemberData(nameof(AssessmentWrites))]
    public async Task Assessment_template_content_of_another_tenant_is_not_found(string op)
    {
        using var ctx = new TenantTestContext();
        var t = await SeedTemplateAsync(ctx, TenantB);
        var before = await AssessmentSnapshotAsync(ctx);

        await ShouldBeNotFoundAsync(() => RunAssessmentWrite(ctx, op, t));

        ctx.Db.ChangeTracker.Clear();
        (await AssessmentSnapshotAsync(ctx)).Should().Be(before);
    }

    [Theory]
    [MemberData(nameof(AssessmentWrites))]
    public async Task Assessment_template_content_of_own_template_can_be_changed(string op)
    {
        using var ctx = new TenantTestContext();
        var t = await SeedTemplateAsync(ctx, TenantA);

        await RunAssessmentWrite(ctx, op, t);
    }

    // ── Y4: education.program_translation ─────────────────────────────────────

    [Fact]
    public async Task Program_translation_of_another_tenants_program_is_not_found_and_unchanged()
    {
        using var ctx = new TenantTestContext();
        var program = EducationProgram.Create(TenantB, "p1", "Program B");
        await ctx.SeedAsync(new Locale { Code = "en", EnglishName = "English", NativeName = "English" }, program,
            new ProgramTranslation { ProgramId = program.Id, Locale = "en", Name = "Original" });

        await ShouldBeNotFoundAsync(() => ctx.Handler<SetProgramTranslationCommandHandler>()
            .Handle(new SetProgramTranslationCommand(program.Id, "en", "Hijacked", null), default));

        (await ctx.Db.ProgramTranslations.SingleAsync()).Name.Should().Be("Original");
    }

    [Fact]
    public async Task Program_translation_of_own_program_is_written()
    {
        using var ctx = new TenantTestContext();
        var program = EducationProgram.Create(TenantA, "p1", "Program A");
        await ctx.SeedAsync(new Locale { Code = "en", EnglishName = "English", NativeName = "English" }, program);

        await ctx.Handler<SetProgramTranslationCommandHandler>()
            .Handle(new SetProgramTranslationCommand(program.Id, "en", "Program A (en)", null), default);

        (await ctx.Db.ProgramTranslations.SingleAsync()).Name.Should().Be("Program A (en)");
    }

    // ── Diğer *_translation yazımları (aynı kök neden) ─────────────────────────

    [Fact]
    public async Task Goal_template_translation_of_a_global_template_is_rejected()
    {
        using var ctx = new TenantTestContext();
        var template = GoalTemplate.Create(null, "Platform hedefi");
        await ctx.SeedAsync(new Locale { Code = "en", EnglishName = "English", NativeName = "English" }, template);

        await ShouldBePlatformReadOnlyAsync(() => ctx.Handler<SetGoalTemplateTranslationCommandHandler>()
            .Handle(new SetGoalTemplateTranslationCommand(template.Id, "en", "Changed", null), default));

        (await ctx.Db.GoalTemplateTranslations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Menu_translations_of_a_platform_menu_item_are_rejected()
    {
        using var ctx = new TenantTestContext();
        var item = MenuItem.Create(null, null, "dashboard", "/", "home");
        await ctx.SeedAsync(item, new MenuItemTranslation { MenuItemId = item.Id, Locale = "tr", Label = "Pano" });

        await ShouldBePlatformReadOnlyAsync(() => ctx.Handler<SetMenuItemTranslationsCommandHandler>()
            .Handle(new SetMenuItemTranslationsCommand(item.Id, [new("tr", "Ele geçirildi")]), default));

        (await ctx.Db.MenuItemTranslations.SingleAsync()).Label.Should().Be("Pano");
    }

    [Fact]
    public async Task Platform_menu_item_cannot_be_updated_toggled_or_deleted_by_a_tenant()
    {
        using var ctx = new TenantTestContext();
        var item = MenuItem.Create(null, null, "dashboard", "/", "home");
        await ctx.SeedAsync(item);

        await ShouldBePlatformReadOnlyAsync(() => ctx.Handler<UpdateMenuItemCommandHandler>().Handle(
            new UpdateMenuItemCommand(item.Id, null, "/", "home", 99, null, null, item.RowVersion), default));
        await ShouldBePlatformReadOnlyAsync(() => ctx.Handler<DeactivateMenuItemCommandHandler>()
            .Handle(new DeactivateMenuItemCommand(item.Id), default));
        await ShouldBePlatformReadOnlyAsync(() => ctx.Handler<ActivateMenuItemCommandHandler>()
            .Handle(new ActivateMenuItemCommand(item.Id), default));
        await ShouldBePlatformReadOnlyAsync(() => ctx.Handler<DeleteMenuItemCommandHandler>()
            .Handle(new DeleteMenuItemCommand(item.Id), default));

        var stored = await ctx.Db.MenuItems.SingleAsync();
        stored.SortOrder.Should().NotBe(99);
        stored.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Menu_translations_of_own_menu_item_are_replaced()
    {
        using var ctx = new TenantTestContext();
        var item = MenuItem.Create(TenantA, null, "custom", "/custom", "star");
        await ctx.SeedAsync(item, new MenuItemTranslation { MenuItemId = item.Id, Locale = "tr", Label = "Eski" });

        await ctx.Handler<SetMenuItemTranslationsCommandHandler>()
            .Handle(new SetMenuItemTranslationsCommand(item.Id, [new("tr", "Yeni")]), default);

        (await ctx.Db.MenuItemTranslations.SingleAsync()).Label.Should().Be("Yeni");
    }

    [Fact]
    public async Task Platform_notification_template_cannot_be_updated_or_deleted()
    {
        using var ctx = new TenantTestContext();
        var template = new NotificationTemplate { Code = "session_reminder", CorporationId = null };
        await ctx.SeedAsync(template,
            new NotificationTemplateTranslation { TemplateId = template.Id, Locale = "tr", Body = "Hatırlatma" });

        await ShouldBePlatformReadOnlyAsync(() => ctx.Handler<UpdateNotificationTemplateCommandHandler>().Handle(
            new UpdateNotificationTemplateCommand(template.Id, null, null, true, [new("tr", null, "Değişti")], 1), default));
        await ShouldBePlatformReadOnlyAsync(() => ctx.Handler<DeleteNotificationTemplateCommandHandler>()
            .Handle(new DeleteNotificationTemplateCommand(template.Id), default));

        (await ctx.Db.NotificationTemplates.CountAsync()).Should().Be(1);
        (await ctx.Db.NotificationTemplateTranslations.SingleAsync()).Body.Should().Be("Hatırlatma");
    }

    [Fact]
    public async Task Platform_notification_trigger_config_cannot_be_deleted()
    {
        using var ctx = new TenantTestContext();
        var config = NotificationTriggerConfig.Create("session_reminder", null, 60, corporationId: null);
        await ctx.SeedAsync(config);

        await ShouldBePlatformReadOnlyAsync(() => ctx.Handler<DeleteNotificationTriggerConfigCommandHandler>()
            .Handle(new DeleteNotificationTriggerConfigCommand(config.Id), default));

        (await ctx.Db.NotificationTriggerConfigs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Shared_global_ref_value_label_cannot_be_edited_by_a_tenant()
    {
        using var ctx = new TenantTestContext();
        var type = new RefType { Code = "therapy_type", Name = "Terapi" };
        var value = new RefValue { RefTypeId = type.Id, CorporationId = null, Code = "aba", IsSystem = false };
        await ctx.SeedAsync(type, value, new RefValueTranslation { RefValueId = value.Id, Locale = "tr", Label = "ABA" });

        await ShouldBePlatformReadOnlyAsync(() => ctx.Handler<UpdateRefValueCommandHandler>()
            .Handle(new UpdateRefValueCommand(value.Id, "Ele geçirildi"), default));

        (await ctx.Db.RefValueTranslations.SingleAsync()).Label.Should().Be("ABA");
    }

    [Fact]
    public async Task Contract_template_of_another_tenant_is_not_found()
    {
        using var ctx = new TenantTestContext();
        var template = ContractTemplate.Create(TenantB, "kayit");
        await ctx.SeedAsync(template);

        await ShouldBeNotFoundAsync(() => ctx.Handler<UpdateContractTemplateCommandHandler>().Handle(
            new UpdateContractTemplateCommand(template.Id, null, null, [new("tr", "Başlık", "Metin")], 1), default));
        await ShouldBeNotFoundAsync(() => ctx.Handler<NewVersionContractTemplateCommandHandler>().Handle(
            new NewVersionContractTemplateCommand(template.Id, null, null), default));

        (await ctx.Db.ContractTemplateTranslations.CountAsync()).Should().Be(0);
        (await ctx.Db.ContractTemplates.CountAsync()).Should().Be(1);
    }

    // ── Y6: survey_question_response ──────────────────────────────────────────

    private static async Task<(SurveyDefinition Survey, SurveyQuestion Question, SurveyAnswerOption Option)> SeedSurveyAsync(
        TenantTestContext ctx, Guid corporationId, string title)
    {
        var survey = SurveyDefinition.Create(corporationId, title, target: "staff");
        var question = SurveyQuestion.Create(survey.Id, "Memnun musunuz?", "choice");
        var option = SurveyAnswerOption.Create(question.Id, "Evet");
        await ctx.SeedAsync(survey, question, option);
        return (survey, question, option);
    }

    [Fact]
    public async Task Survey_answer_to_a_question_of_another_survey_is_a_validation_error_and_writes_nothing()
    {
        using var ctx = new TenantTestContext();
        var (survey, _, _) = await SeedSurveyAsync(ctx, TenantA, "A");
        var (_, foreignQuestion, _) = await SeedSurveyAsync(ctx, TenantB, "B");

        var act = () => ctx.Handler<SubmitSurveyResponseCommandHandler>().Handle(new SubmitSurveyResponseCommand(
            survey.Id, null, null, null, [new(foreignQuestion.Id, "x", null, null)]), default);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Answers[0].QuestionId");
        (await ctx.Db.SurveyResponses.CountAsync()).Should().Be(0);
        (await ctx.Db.SurveyQuestionResponses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Survey_answer_option_of_another_question_is_a_validation_error()
    {
        using var ctx = new TenantTestContext();
        var (survey, question, _) = await SeedSurveyAsync(ctx, TenantA, "A");
        var (_, _, foreignOption) = await SeedSurveyAsync(ctx, TenantA, "A2");

        var act = () => ctx.Handler<SubmitSurveyResponseCommandHandler>().Handle(new SubmitSurveyResponseCommand(
            survey.Id, null, null, null, [new(question.Id, null, foreignOption.Id, null)]), default);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Answers[0].AnswerOptionId");
        (await ctx.Db.SurveyQuestionResponses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Survey_answers_that_belong_to_the_survey_are_saved()
    {
        using var ctx = new TenantTestContext();
        var (survey, question, option) = await SeedSurveyAsync(ctx, TenantA, "A");

        await ctx.Handler<SubmitSurveyResponseCommandHandler>().Handle(new SubmitSurveyResponseCommand(
            survey.Id, null, null, null, [new(question.Id, null, option.Id, null)]), default);

        (await ctx.Db.SurveyQuestionResponses.SingleAsync()).AnswerOptionId.Should().Be(option.Id);
    }
}
