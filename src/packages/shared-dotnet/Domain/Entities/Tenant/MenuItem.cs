namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class MenuItem
{
    public Guid Id { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public bool IsAvailable { get; private set; }
    public Guid? StationId { get; private set; }

    private MenuItem() { }

    public static MenuItem Create(Guid categoryId, string name, decimal price, string? description = null)
    {
        return new MenuItem
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            Name = name,
            Description = description,
            Price = price,
            IsAvailable = true
        };
    }

    public void Update(string name, string? description, decimal price, bool isAvailable)
    {
        Name = name;
        Description = description;
        Price = price;
        IsAvailable = isAvailable;
    }

    public void AssignStation(Guid? stationId) => StationId = stationId;
}
