namespace Aynesil.Application.Features.Surveys.Dtos;

// ── Survey Definition DTOs ────────────────────────────────────────────────────

public record SurveyDefinitionDto(
    Guid Id,
    Guid CorporationId,
    Guid? TypeId,
    string? TypeCode,
    string Title,
    string? Description,
    string Target,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion,
    IReadOnlyList<SurveyQuestionDto> Questions);

public record SurveyDefinitionListItemDto(
    Guid Id,
    Guid? TypeId,
    string? TypeCode,
    string Title,
    string Target,
    bool IsActive,
    int QuestionCount,
    DateTimeOffset UpdatedAt);

public record SurveyQuestionDto(
    Guid Id,
    string QuestionText,
    string QuestionType,
    bool IsRequired,
    int SortOrder,
    IReadOnlyList<SurveyAnswerOptionDto> AnswerOptions);

public record SurveyAnswerOptionDto(
    Guid Id,
    string OptionText,
    string? OptionValue,
    int SortOrder);

// ── Survey Response DTOs ──────────────────────────────────────────────────────

public record SurveyResponseDto(
    Guid Id,
    Guid SurveyId,
    string SurveyTitle,
    Guid? RespondentUserId,
    Guid? GuardianId,
    Guid? StudentId,
    Guid? SessionId,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<SurveyQuestionResponseDto> QuestionResponses);

public record SurveyResponseListItemDto(
    Guid Id,
    Guid SurveyId,
    string SurveyTitle,
    Guid? GuardianId,
    Guid? StudentId,
    DateTimeOffset? SubmittedAt,
    bool IsComplete);

public record SurveyQuestionResponseDto(
    Guid QuestionId,
    string QuestionText,
    string QuestionType,
    string? AnswerText,
    Guid? AnswerOptionId,
    string? AnswerOptionText,
    decimal? NumericValue);

// ── Parent Feedback DTOs ──────────────────────────────────────────────────────

public record ParentFeedbackDto(
    Guid Id,
    Guid CorporationId,
    Guid? GuardianId,
    Guid? EducatorId,
    Guid? SessionId,
    short? Rating,
    string? Comment,
    DateTimeOffset CreatedAt);

public record ParentFeedbackListItemDto(
    Guid Id,
    Guid? GuardianId,
    Guid? EducatorId,
    Guid? SessionId,
    short? Rating,
    string? Comment,
    DateTimeOffset CreatedAt);
