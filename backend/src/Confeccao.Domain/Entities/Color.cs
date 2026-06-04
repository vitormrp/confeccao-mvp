namespace Confeccao.Domain.Entities;

public class Color
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string HexCode { get; set; }
    public bool HasLining { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}
