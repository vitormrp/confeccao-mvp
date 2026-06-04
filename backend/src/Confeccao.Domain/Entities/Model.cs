namespace Confeccao.Domain.Entities;

public class Model
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public int ButtonCount { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ModelStage> Stages { get; set; } = new List<ModelStage>();
}
