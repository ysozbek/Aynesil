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
    int ResponseCount,
    IReadOnlyList<SurveyQuestionDto> Questions);

public record SurveyDefinitionListItemDto(
    Guid Id,
    Guid? TypeId,
    string? TypeCode,
    string Title,
    string Target,
    bool IsActive,
    int QuestionCount,
    int ResponseCount,
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
    string? GuardianName,
    Guid? StudentId,
    string? StudentName,
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
    string? GuardianName,
    Guid? EducatorId,
    string? EducatorName,
    Guid? SessionId,
    short? Rating,
    string? Comment,
    DateTimeOffset CreatedAt);

// ── Results (per-question summary) ────────────────────────────────────────────

public record SurveyResultsDto(
    Guid SurveyId,
    string Title,
    string Target,
    int ResponseCount,
    DateTimeOffset? LastSubmittedAt,
    IReadOnlyList<SurveyQuestionResultDto> Questions);

public record SurveyQuestionResultDto(
    Guid QuestionId,
    string QuestionText,
    string QuestionType,
    bool IsRequired,
    int AnsweredCount,
    decimal? Average,
    IReadOnlyList<SurveyResultBucketDto> Distribution,
    IReadOnlyList<string> TextAnswers);

/// <summary>One answer bucket: option text / "Evet"-"Hayır" code / rating value, with its count.</summary>
public record SurveyResultBucketDto(string Label, string? Value, int Count);

// ── Pending surveys (parent portal) ───────────────────────────────────────────

public record PendingSurveyDto(
    Guid SurveyId,
    string Title,
    string? Description,
    string? TypeCode,
    IReadOnlyList<SurveyQuestionDto> Questions,
    IReadOnlyList<PendingSurveyStudentDto> Students);

public record PendingSurveyStudentDto(Guid StudentId, string StudentName);
