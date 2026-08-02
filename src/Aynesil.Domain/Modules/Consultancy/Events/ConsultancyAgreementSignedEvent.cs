namespace Aynesil.Domain.Modules.Consultancy.Events;

public record ConsultancyAgreementSignedEvent(
    Guid AgreementId,
    Guid CorporationId,
    Guid ConsultancyPlanId,
    DateOnly SignedDate) : BaseDomainEvent;
