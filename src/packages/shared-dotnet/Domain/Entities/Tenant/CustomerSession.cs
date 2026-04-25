namespace TabFlow.Shared.Domain.Entities.Tenant;

public sealed class CustomerSession
{
    public Guid Id { get; private set; }
    public Guid TableId { get; private set; }
    public bool IsOpen { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    public IReadOnlyList<CustomerAccessTicket> AccessTickets => _accessTickets.AsReadOnly();
    private readonly List<CustomerAccessTicket> _accessTickets = [];

    public IReadOnlyList<CartItem> CartItems => _cartItems.AsReadOnly();
    private readonly List<CartItem> _cartItems = [];

    private CustomerSession() { }

    public static CustomerSession Open(Guid tableId)
    {
        return new CustomerSession
        {
            Id = Guid.NewGuid(),
            TableId = tableId,
            IsOpen = true,
            OpenedAt = DateTimeOffset.UtcNow
        };
    }

    public CustomerAccessTicket IssueTicket()
    {
        var ticket = CustomerAccessTicket.Create(Id);
        _accessTickets.Add(ticket);
        return ticket;
    }

    public void Close()
    {
        IsOpen = false;
        ClosedAt = DateTimeOffset.UtcNow;
        foreach (var ticket in _accessTickets)
            ticket.Invalidate();
    }
}
