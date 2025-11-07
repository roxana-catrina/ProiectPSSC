// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: SHIPPING & DELIVERY - DOMAIN EVENTS
// ═══════════════════════════════════════════════════════════════════════════════
// Evenimente de domeniu pentru Shipping
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

namespace Proiect.Domain.Shipping.Events;

/// <summary>
/// EVENIMENT: ShipmentPrepared
/// Emis când: coletul este pregătit pentru expediere
/// Declanșat de: PrepareForShipment command
/// </summary>
public record ShipmentPrepared(
    Guid ShipmentId,
    Guid OrderId,
    DateTime PreparedAt,
    string Notes
);

/// <summary>
/// EVENIMENT: OrderShipped
/// Emis când: coletul este expediat către client
/// Declanșat de: Ship command
/// </summary>
public record OrderShipped(
    Guid ShipmentId,
    Guid OrderId,
    string Carrier,
    string TrackingNumber,
    DateTime ShippedAt,
    DateTime EstimatedDeliveryDate,
    DeliveryAddress DeliveryAddress
);

/// <summary>
/// EVENIMENT: ShipmentTrackingUpdated
/// Emis când: se actualizează statusul de tracking
/// Declanșat de: UpdateTrackingStatus command
/// </summary>
public record ShipmentTrackingUpdated(
    Guid ShipmentId,
    Guid OrderId,
    string Location,
    string Status,
    DateTime Timestamp
);

/// <summary>
/// EVENIMENT: OrderDelivered
/// Emis când: coletul este livrat cu succes
/// Declanșat de: Deliver command
/// </summary>
public record OrderDelivered(
    Guid ShipmentId,
    Guid OrderId,
    DateTime DeliveredAt,
    string RecipientName,
    string DeliveredBy,
    string Notes
);

/// <summary>
/// EVENIMENT: ShipmentLost
/// Emis când: coletul este pierdut în tranzit
/// Declanșat de: MarkAsLost command
/// </summary>
public record ShipmentLost(
    Guid ShipmentId,
    Guid OrderId,
    string Reason,
    DateTime LostAt
);

/// <summary>
/// EVENIMENT: ShipmentReturned
/// Emis când: coletul este returnat (delivery failed)
/// Declanșat de: MarkAsReturned command
/// </summary>
public record ShipmentReturned(
    Guid ShipmentId,
    Guid OrderId,
    string Reason,
    DateTime ReturnedAt
);

/// <summary>
/// EVENIMENT: DeliveryAddressUpdated
/// Emis când: adresa de livrare este actualizată
/// Declanșat de: UpdateDeliveryAddress command
/// </summary>
public record DeliveryAddressUpdated(
    Guid ShipmentId,
    Guid OrderId,
    DeliveryAddress NewAddress,
    DateTime UpdatedAt
);

/// <summary>
/// EVENIMENT: ShipmentDelayed
/// Emis când: livrarea întârzie față de estimare
/// </summary>
public record ShipmentDelayed(
    Guid ShipmentId,
    Guid OrderId,
    DateTime OriginalEstimate,
    DateTime NewEstimate,
    string Reason
);

