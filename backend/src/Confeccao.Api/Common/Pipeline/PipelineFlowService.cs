using Confeccao.Api.Infrastructure;
using Confeccao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Common.Pipeline;

/// <summary>
/// Resolves stage progression for a model based on its configured flow
/// (<see cref="Domain.Entities.ModelStage"/> rows). All stage logic should funnel
/// through this service so flows can change in data without touching the rest of the app.
/// </summary>
public class PipelineFlowService
{
    private readonly ConfeccaoDbContext _db;

    public PipelineFlowService(ConfeccaoDbContext db) => _db = db;

    /// <summary>
    /// Returns the stage that comes after <paramref name="currentStage"/> in
    /// model <paramref name="modelId"/>'s flow, or null if it's the final stage.
    /// </summary>
    public async Task<StageCode?> GetNextStageAsync(Guid modelId, StageCode currentStage, CancellationToken ct = default)
    {
        var stages = await _db.ModelStages
            .Where(s => s.ModelId == modelId)
            .OrderBy(s => s.Sequence)
            .Select(s => s.Stage)
            .ToListAsync(ct);

        var idx = stages.IndexOf(currentStage);
        if (idx < 0) throw new InvalidOperationException(
            $"Stage {currentStage} is not part of model {modelId}'s flow.");

        return idx + 1 < stages.Count ? stages[idx + 1] : null;
    }

    /// <summary>
    /// Returns the first stage of a model's flow. Used when an order is created
    /// to know which stage the initial PipelineItem should go to (always Cutting today,
    /// but driven by data so a model could one day skip cutting).
    /// </summary>
    public async Task<StageCode> GetFirstStageAsync(Guid modelId, CancellationToken ct = default)
    {
        var first = await _db.ModelStages
            .Where(s => s.ModelId == modelId)
            .OrderBy(s => s.Sequence)
            .Select(s => (StageCode?)s.Stage)
            .FirstOrDefaultAsync(ct);

        return first ?? throw new InvalidOperationException(
            $"Model {modelId} has no stages configured.");
    }
}
