namespace Confeccao.Domain.Enums;

/// <summary>
/// Production-pipeline stages an order item flows through. The exact set of stages
/// depends on the model — see <see cref="Entities.ModelStage"/>.
/// </summary>
public enum StageCode
{
    Cutting = 1,
    Interfacing = 2,
    Sewing = 3,
    Washing = 4,
    Buttoning = 5,
    Labeling = 6,
    Pressing = 7,
}
