namespace Aynesil.Domain.Modules.Consultancy.Events;

public record ConsultancyAgreementStatusChangedEvent(
    Guid AgreementId,
    Guid CorporationId,
    string OldStatus,
    string NewStatus) : BaseDomainEvent;
