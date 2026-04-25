namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class TableEntity
{
    public Guid Id { get; private set; }
    public string Label { get; private set; } = default!;
    public bool IsActive { get; private set; }

    public DeviceKey? DeviceKey { get; private set; }

    private TableEntity() { }

    public static TableEntity Create(string label)
    {
        return new TableEntity
        {
            Id = Guid.NewGuid(),
            Label = label,
            IsActive = true
        };
    }

    public void Update(string label, bool isActive)
    {
        Label = label;
        IsActive = isActive;
    }

    public void PairDevice(string deviceKeyHash)
    {
        DeviceKey = DeviceKey.Create(Id, deviceKeyHash);
    }

    public void RevokeDevice()
    {
        DeviceKey = null;
    }
}
