namespace Aynesil.Application.Features.Leads.Dtos;

/// <summary>
/// Aggregated pipeline dashboard — total lead counts grouped by pipeline stage.
/// Used to render the kanban / funnel summary widget.
/// </summary>
public record PipelineSummaryDto(
    IReadOnlyList<PipelineStageCountDto> Stages,
    int TotalLeads,
    int TotalConverted,
    int TotalLost
);

/// <summary>Lead count for a single pipeline stage.</summary>
public record PipelineStageCountDto(
    Guid StageId,
    string StageCode,
    int Count
);
