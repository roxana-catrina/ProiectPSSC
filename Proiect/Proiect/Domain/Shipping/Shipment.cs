// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: SHIPPING & DELIVERY - DOMAIN LAYER
// ═══════════════════════════════════════════════════════════════════════════════
// Agregat: Shipment - gestionează procesul de livrare
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

using Proiect.Domain.Shipping.Events;

namespace Proiect.Domain.Shipping;

/// <summary>
/// AGREGAT ROOT: Shipment
/// Gestionează procesul de shipping și delivery pentru o comandă
/// 
/// INVARIANȚI:
/// 1. ShipmentId este unic și non-null
/// 2. OrderId trebuie să existe și să fie valid
/// 3. Status poate progresa doar în ordine: Created → Prepared → Shipped → InTransit → Delivered
/// 4. Nu se poate livra (Delivered) fără să fie expediată (Shipped)
/// 5. DeliveredAt &gt;= ShippedAt (dacă ambele sunt setate)
/// 6. Adresa de livrare trebuie să fie completă înainte de shipping
/// 7. Carrier și TrackingNumber sunt obligatorii după shipping
/// </summary>
public class Shipment
{
    // ═══════════════════════════════════════════════════════════════════════════
    // STATE (Encapsulated)
    // ═══════════════════════════════════════════════════════════════════════════
    
    public Guid ShipmentId { get; private set; }
    public Guid OrderId { get; private set; }
    public ShipmentStatus Status { get; private set; }
    
    public DeliveryAddress DeliveryAddress { get; private set; }
    
    public string? Carrier { get; private set; }
    public string? TrackingNumber { get; private set; }
    
    public DateTime? PreparedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? EstimatedDeliveryDate { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    
    public string? DeliveredBy { get; private set; }
    public string? RecipientName { get; private set; }
    public string? DeliveryNotes { get; private set; }
    
    // Tracking events pentru vizibilitate
    private readonly List<TrackingEvent> _trackingEvents = new();
    public IReadOnlyList<TrackingEvent> TrackingEvents => _trackingEvents.AsReadOnly();
    
    // Pentru event sourcing
    private readonly List<object> _uncommittedEvents = new();
    public IReadOnlyList<object> UncommittedEvents => _uncommittedEvents.AsReadOnly();
    
    // ═══════════════════════════════════════════════════════════════════════════
    // COMPUTED PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════════
    
    public bool IsDelivered => Status == ShipmentStatus.Delivered;
    public bool IsShipped => Status >= ShipmentStatus.Shipped;
    public bool CanBeCancelled => Status < ShipmentStatus.Shipped;
    public bool IsDelayed => EstimatedDeliveryDate.HasValue 
        && EstimatedDeliveryDate.Value < DateTime.UtcNow 
        && !IsDelivered;
    
    // ═══════════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════════
    
    public Shipment(Guid orderId, DeliveryAddress deliveryAddress)
    {
        if (orderId == Guid.Empty)
            throw new InvalidShippingCommandException("OrderId cannot be empty");
        
        if (deliveryAddress == null)
            throw new InvalidShippingCommandException("Delivery address is required");
        
        deliveryAddress.Validate();
        
        ShipmentId = Guid.NewGuid();
        OrderId = orderId;
        DeliveryAddress = deliveryAddress;
        Status = ShipmentStatus.Created;
        
        EnsureInvariants();
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // BUSINESS OPERATIONS (Commands → Events)
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// COMMAND: PrepareForShipment
    /// Pregătește coletul pentru expediere (ambalare, etichetare)
    /// EMITE: ShipmentPrepared
    /// </summary>
    public void PrepareForShipment(string notes = "")
    {
        if (Status != ShipmentStatus.Created)
            throw new InvalidShippingCommandException($"Cannot prepare shipment in status {Status}");
        
        Status = ShipmentStatus.Prepared;
        PreparedAt = DateTime.UtcNow;
        
        AddTrackingEvent("Shipment prepared for dispatch", "Warehouse");
        
        EnsureInvariants();
        
        var @event = new ShipmentPrepared(ShipmentId, OrderId, PreparedAt.Value, notes);
        _uncommittedEvents.Add(@event);
    }
    
    /// <summary>
    /// COMMAND: Ship
    /// Expediază coletul către client
    /// EMITE: OrderShipped
    /// </summary>
    public void Ship(string carrier, string trackingNumber, DateTime? estimatedDeliveryDate = null)
    {
        // VALIDĂRI
        if (Status != ShipmentStatus.Prepared)
            throw new InvalidShippingCommandException(
                $"Cannot ship in status {Status}. Must be Prepared first.");
        
        if (string.IsNullOrWhiteSpace(carrier))
            throw new InvalidShippingCommandException("Carrier is required");
        
        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new InvalidShippingCommandException("Tracking number is required");
        
        if (estimatedDeliveryDate.HasValue && estimatedDeliveryDate.Value <= DateTime.UtcNow)
            throw new InvalidShippingCommandException("Estimated delivery date must be in the future");
        
        // OPERAȚIE
        Status = ShipmentStatus.Shipped;
        Carrier = carrier;
        TrackingNumber = trackingNumber;
        ShippedAt = DateTime.UtcNow;
        EstimatedDeliveryDate = estimatedDeliveryDate ?? DateTime.UtcNow.AddDays(3);
        
        AddTrackingEvent($"Shipped via {carrier}", carrier);
        
        EnsureInvariants();
        
        // EVENIMENT PRINCIPAL
        var @event = new OrderShipped(
            ShipmentId,
            OrderId,
            Carrier,
            TrackingNumber,
            ShippedAt.Value,
            EstimatedDeliveryDate.Value,
            DeliveryAddress);
        
        _uncommittedEvents.Add(@event);
    }
    
    /// <summary>
    /// COMMAND: UpdateTrackingStatus
    /// Actualizează statusul în tranzit (opțional, pentru tracking intermediar)
    /// </summary>
    public void UpdateTrackingStatus(string location, string status, string notes = "")
    {
        if (!IsShipped)
            throw new InvalidShippingCommandException("Cannot update tracking for unshipped order");
        
        if (IsDelivered)
            throw new InvalidShippingCommandException("Cannot update tracking for delivered order");
        
        AddTrackingEvent($"{status} - {notes}", location);
        
        if (Status == ShipmentStatus.Shipped)
        {
            Status = ShipmentStatus.InTransit;
        }
        
        var @event = new ShipmentTrackingUpdated(ShipmentId, OrderId, location, status, DateTime.UtcNow);
        _uncommittedEvents.Add(@event);
    }
    
    /// <summary>
    /// COMMAND: Deliver
    /// Marchează coletul ca livrat
    /// EMITE: OrderDelivered
    /// </summary>
    public void Deliver(string recipientName, string deliveredBy, string notes = "")
    {
        // VALIDĂRI
        if (!IsShipped)
            throw new InvalidShippingCommandException(
                "Cannot deliver an order that hasn't been shipped");
        
        if (IsDelivered)
            throw new InvalidShippingCommandException("Order already delivered");
        
        if (string.IsNullOrWhiteSpace(recipientName))
            throw new InvalidShippingCommandException("Recipient name is required");
        
        // OPERAȚIE
        Status = ShipmentStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        RecipientName = recipientName;
        DeliveredBy = deliveredBy ?? "Unknown";
        DeliveryNotes = notes;
        
        AddTrackingEvent($"Delivered to {recipientName}", "Destination");
        
        EnsureInvariants();
        
        // EVENIMENT PRINCIPAL
        var @event = new OrderDelivered(
            ShipmentId,
            OrderId,
            DeliveredAt.Value,
            RecipientName,
            DeliveredBy,
            notes);
        
        _uncommittedEvents.Add(@event);
    }
    
    /// <summary>
    /// COMMAND: MarkAsLost
    /// Marchează coletul ca pierdut în tranzit
    /// </summary>
    public void MarkAsLost(string reason)
    {
        if (!IsShipped || IsDelivered)
            throw new InvalidShippingCommandException("Can only mark shipped (non-delivered) items as lost");
        
        Status = ShipmentStatus.Lost;
        AddTrackingEvent($"Shipment lost: {reason}", "System");
        
        var @event = new ShipmentLost(ShipmentId, OrderId, reason, DateTime.UtcNow);
        _uncommittedEvents.Add(@event);
    }
    
    /// <summary>
    /// COMMAND: MarkAsReturned
    /// Marchează coletul ca returnat (delivery failed)
    /// </summary>
    public void MarkAsReturned(string reason)
    {
        if (IsDelivered)
            throw new InvalidShippingCommandException("Cannot return a delivered shipment");
        
        if (!IsShipped)
            throw new InvalidShippingCommandException("Can only return shipped items");
        
        Status = ShipmentStatus.Returned;
        AddTrackingEvent($"Shipment returned: {reason}", "Carrier");
        
        var @event = new ShipmentReturned(ShipmentId, OrderId, reason, DateTime.UtcNow);
        _uncommittedEvents.Add(@event);
    }
    
    /// <summary>
    /// COMMAND: UpdateDeliveryAddress
    /// Actualizează adresa de livrare (doar înainte de shipping)
    /// </summary>
    public void UpdateDeliveryAddress(DeliveryAddress newAddress)
    {
        if (IsShipped)
            throw new InvalidShippingCommandException("Cannot update address after shipping");
        
        if (newAddress == null)
            throw new InvalidShippingCommandException("Address cannot be null");
        
        newAddress.Validate();
        
        DeliveryAddress = newAddress;
        
        var @event = new DeliveryAddressUpdated(ShipmentId, OrderId, newAddress, DateTime.UtcNow);
        _uncommittedEvents.Add(@event);
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════════════════
    
    private void AddTrackingEvent(string description, string location)
    {
        var trackingEvent = new TrackingEvent(DateTime.UtcNow, description, location);
        _trackingEvents.Add(trackingEvent);
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // INVARIANT ENFORCEMENT
    // ═══════════════════════════════════════════════════════════════════════════
    
    private void EnsureInvariants()
    {
        // INVARIANT 1: ShipmentId non-empty
        if (ShipmentId == Guid.Empty)
            throw new InvariantViolationException("ShipmentId cannot be empty");
        
        // INVARIANT 2: OrderId non-empty
        if (OrderId == Guid.Empty)
            throw new InvariantViolationException("OrderId cannot be empty");
        
        // INVARIANT 3: DeliveryAddress required
        if (DeliveryAddress == null)
            throw new InvariantViolationException("DeliveryAddress is required");
        
        // INVARIANT 4: Carrier și TrackingNumber după shipping
        if (IsShipped && (string.IsNullOrWhiteSpace(Carrier) || string.IsNullOrWhiteSpace(TrackingNumber)))
            throw new InvariantViolationException("Carrier and TrackingNumber required after shipping");
        
        // INVARIANT 5: DeliveredAt >= ShippedAt
        if (DeliveredAt.HasValue && ShippedAt.HasValue && DeliveredAt.Value < ShippedAt.Value)
            throw new InvariantViolationException("DeliveredAt cannot be before ShippedAt");
        
        // INVARIANT 6: Nu se poate livra fără shipping
        if (IsDelivered && !ShippedAt.HasValue)
            throw new InvariantViolationException("Cannot be delivered without being shipped");
    }
    
    public void ClearUncommittedEvents()
    {
        _uncommittedEvents.Clear();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ENUMS
// ═══════════════════════════════════════════════════════════════════════════════

public enum ShipmentStatus
{
    Created = 0,
    Prepared = 1,
    Shipped = 2,
    InTransit = 3,
    Delivered = 4,
    Returned = 5,
    Lost = 6
}

// ═══════════════════════════════════════════════════════════════════════════════
// VALUE OBJECTS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Value Object: DeliveryAddress
/// Adresa completă de livrare
/// </summary>
public record DeliveryAddress(
    string RecipientName,
    string Street,
    string City,
    string PostalCode,
    string Country,
    string? Phone = null,
    string? AdditionalInfo = null)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RecipientName))
            throw new InvalidShippingCommandException("Recipient name is required");
        
        if (string.IsNullOrWhiteSpace(Street))
            throw new InvalidShippingCommandException("Street is required");
        
        if (string.IsNullOrWhiteSpace(City))
            throw new InvalidShippingCommandException("City is required");
        
        if (string.IsNullOrWhiteSpace(PostalCode))
            throw new InvalidShippingCommandException("Postal code is required");
        
        if (string.IsNullOrWhiteSpace(Country))
            throw new InvalidShippingCommandException("Country is required");
    }
    
    public string ToFormattedString()
    {
        return $"{RecipientName}\n{Street}\n{City}, {PostalCode}\n{Country}";
    }
}

/// <summary>
/// Value Object: TrackingEvent
/// Eveniment de tracking pentru vizibilitate
/// </summary>
public record TrackingEvent(
    DateTime Timestamp,
    string Description,
    string Location);

// ═══════════════════════════════════════════════════════════════════════════════
// DOMAIN EXCEPTIONS
// ═══════════════════════════════════════════════════════════════════════════════

public class InvalidShippingCommandException : Exception
{
    public InvalidShippingCommandException(string message) : base(message) { }
}

public class InvariantViolationException : Exception
{
    public InvariantViolationException(string message) : base(message) { }
}

