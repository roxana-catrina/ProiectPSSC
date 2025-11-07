// ═══════════════════════════════════════════════════════════════════════════════
// 📋 DOMAIN EVENTS - ORDER MANAGEMENT CONTEXT
// ═══════════════════════════════════════════════════════════════════════════════

namespace OrderManagement.Domain.Orders.Events;

/// <summary>
/// Base interface pentru toate Domain Events
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}

/// <summary>
/// Base class pentru Domain Events
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

// ═══════════════════════════════════════════════════════════════════════════════
// DOMAIN EVENTS
// ═══════════════════════════════════════════════════════════════════════════════

public record OrderPlacedEvent(
    Guid OrderId,
    Guid CustomerId,
    CustomerInfo CustomerInfo,
    List<OrderItem> OrderItems,
    ShippingAddress ShippingAddress,
    PaymentMethod PaymentMethod,
    Money TotalAmount,
    DateTime OrderDate
) : DomainEvent;

public record OrderValidatedEvent(
    Guid OrderId,
    DateTime ValidationDate,
    string ValidatedBy
) : DomainEvent;

public record OrderRejectedEvent(
    Guid OrderId,
    string RejectionReason,
    DateTime RejectionDate
) : DomainEvent;

public record OrderConfirmedEvent(
    Guid OrderId,
    DateTime ConfirmationDate,
    Guid ConfirmedBy,
    DateTime EstimatedDeliveryDate
) : DomainEvent;

public record OrderCancellationRequestedEvent(
    Guid OrderId,
    Guid RequestedBy,
    string CancellationReason,
    DateTime RequestDate
) : DomainEvent;

public record OrderCancelledEvent(
    Guid OrderId,
    DateTime CancellationDate,
    Guid CancelledBy,
    string CancellationReason,
    OrderStatus PreviousStatus
) : DomainEvent;

public record OrderModificationRequestedEvent(
    Guid OrderId,
    Guid RequestedBy,
    Dictionary<string, object> RequestedChanges,
    DateTime RequestDate,
    string Reason
) : DomainEvent;

public record OrderModifiedEvent(
    Guid OrderId,
    Dictionary<string, object> OldValues,
    Dictionary<string, object> NewValues,
    DateTime ModificationDate
) : DomainEvent;

