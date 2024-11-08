namespace CSASystemAPI.Domain.Entities;

public class Tag
{
    public string Name { get; set; } = default!;
    public string Color { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}