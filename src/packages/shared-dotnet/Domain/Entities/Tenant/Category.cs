namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? DefaultStationId { get; private set; }

    public IReadOnlyList<MenuItem> Items => _items.AsReadOnly();
    private readonly List<MenuItem> _items = [];

    private Category() { }

    public static Category Create(string name, int sortOrder)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            SortOrder = sortOrder,
            IsActive = true
        };
    }

    public void Update(string name, int sortOrder, bool isActive) { Name = name; SortOrder = sortOrder; IsActive = isActive; }
    public void SetDefaultStation(Guid? stationId) => DefaultStationId = stationId;
}
