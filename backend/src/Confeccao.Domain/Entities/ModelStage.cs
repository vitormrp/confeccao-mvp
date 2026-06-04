using Confeccao.Domain.Enums;

namespace Confeccao.Domain.Entities;

/// <summary>
/// One step in the pipeline flow for a given <see cref="Model"/>.
/// Sequence is 0-indexed and contiguous; e.g., {0=Cutting, 1=Sewing, 2=Washing, ...}.
/// </summary>
public class ModelStage
{
    public Guid Id { get; set; }
    public Guid ModelId { get; set; }
    public Model? Model { get; set; }
    public StageCode Stage { get; set; }
    public int Sequence { get; set; }
}
