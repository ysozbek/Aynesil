namespace Aynesil.Domain.Modules.Consultancy.Events;

public record ConsultancyAgreementCreatedEvent(
    Guid AgreementId,
    Guid CorporationId,
    Guid ConsultancyPlanId,
    string Title) : BaseDomainEvent;
