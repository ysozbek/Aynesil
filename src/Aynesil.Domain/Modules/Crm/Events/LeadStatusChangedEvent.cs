using Aynesil.Domain.Events;

namespace Aynesil.Domain.Modules.Crm.Events;

/// <summary>
/// Raised when a lead's status or pipeline stage changes.
/// Consumers use this event to append a LeadStatusHistory record
/// and to trigger any automated follow-up workflows.
/// </summary>
public record LeadStatusChangedEvent(
    Guid LeadId,
    Guid CorporationId,
    Guid? PreviousStatusId,
    Guid? NewStatusId,
    Guid? PreviousPipelineStageId,
    Guid? NewPipelineStageId,
    Guid? ChangedBy) : BaseDomainEvent;
