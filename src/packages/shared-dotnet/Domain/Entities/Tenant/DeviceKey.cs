namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class DeviceKey
{
    public Guid TableId { get; private set; }
    public string DeviceKeyHash { get; private set; } = default!;
    public DateTimeOffset? LastSeenAt { get; private set; }

    private DeviceKey() { }

    internal static DeviceKey Create(Guid tableId, string deviceKeyHash)
    {
        return new DeviceKey
        {
            TableId = tableId,
            DeviceKeyHash = deviceKeyHash
        };
    }

    public void RecordSeen() => LastSeenAt = DateTimeOffset.UtcNow;
}
