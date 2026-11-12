using static Aynesil.Application.Common.Localization.Msg;

namespace Aynesil.Application.Common.Validation;

/// <summary>
/// Doğrulama mesajlarında kullanılan izin verilen kod kümeleri. Etiketler EnumLabels.resx'ten (tr/en) gelir;
/// her kodun her iki dilde etiketi olduğunu bir birim test doğrular.
/// </summary>
public static class EnumSets
{
    public static readonly EnumSet Horizon = EnumList("Horizon", "long_term", "short_term");
    public static readonly EnumSet GoalStatus = EnumList("GoalStatus", "active", "achieved", "discontinued", "on_hold");
    public static readonly EnumSet Trend = EnumList("Trend", "improving", "stable", "declining");
    public static readonly EnumSet Outcome = EnumList("Outcome", "on_track", "needs_revision", "met");
    public static readonly EnumSet AttendanceStatus = EnumList("AttendanceStatus", "present", "absent", "late", "excused", "left_early");
    public static readonly EnumSet CampAttendanceStatus = EnumList("CampAttendanceStatus", "present", "absent", "late", "excused");
    public static readonly EnumSet CampParticipationStatus = EnumList("CampParticipationStatus", "registered", "attended", "absent", "excused");
    public static readonly EnumSet CampEnrollmentStatus = EnumList("CampEnrollmentStatus", "enrolled", "waitlist");
    public static readonly EnumSet Frequency = EnumList("Frequency", "weekly", "biweekly", "monthly");
    public static readonly EnumSet EducatorRole = EnumList("EducatorRole", "lead", "assistant", "observer", "supervisor");
    public static readonly EnumSet RecurrenceAction = EnumList("RecurrenceAction", "skip", "reschedule", "cancel");
    public static readonly EnumSet EntryType = EnumList("EntryType", "holiday", "closure", "event", "term_break");
    public static readonly EnumSet EmploymentType = EnumList("EmploymentType", "full_time", "part_time", "contractor");
    public static readonly EnumSet LeaveUnit = EnumList("LeaveUnit", "day", "hour");
    public static readonly EnumSet Direction = EnumList("Direction", "inbound", "outbound");
    public static readonly EnumSet ScoringModel = EnumList("ScoringModel", "sum", "average", "rubric", "none");
    public static readonly EnumSet ResponseType = EnumList("ResponseType", "numeric", "scale", "boolean", "text", "choice");
    public static readonly EnumSet ProgramStatus = EnumList("ProgramStatus", "active", "paused", "completed", "cancelled");
    public static readonly EnumSet ParticipantType = EnumList("ParticipantType", "user", "guardian", "lead", "external");
    public static readonly EnumSet MeetingAttendance = EnumList("MeetingAttendance", "invited", "attended", "absent", "tentative");
    public static readonly EnumSet ActionItemStatus = EnumList("ActionItemStatus", "open", "in_progress", "done", "cancelled");
    public static readonly EnumSet SurveyTarget = EnumList("SurveyTarget", "guardian", "educator", "student");
    public static readonly EnumSet SignatureMethod = EnumList("SignatureMethod", "wet", "e_sign", "click_wrap");
}
