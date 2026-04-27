namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class Station
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string Color { get; private set; } = default!;
    public string Type { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public bool IsFallback { get; private set; }
    public int SortOrder { get; private set; }

    private Station() { }

    public static Station Create(string name, string code, string color, string type, int sortOrder)
    {
        return new Station
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            Color = color,
            Type = type,
            IsActive = true,
            IsFallback = false,
            SortOrder = sortOrder
        };
    }

    public void Update(string name, string code, string color, string type, bool isActive, int sortOrder)
    {
        Name = name;
        Code = code;
        Color = color;
        Type = type;
        IsActive = isActive;
        SortOrder = sortOrder;
    }

    public void SetFallback(bool isFallback) => IsFallback = isFallback;
}
