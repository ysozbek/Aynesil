namespace Aynesil.Domain.Modules.Consultancy.Events;

public record ConsultancyReportCreatedEvent(
    Guid ReportId,
    Guid CorporationId,
    Guid? ConsultancyPlanId,
    Guid? SchoolVisitId) : BaseDomainEvent;
