// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: SHIPPING & DELIVERY - APPLICATION LAYER (COMMANDS)
// ═══════════════════════════════════════════════════════════════════════════════
// Comenzi pentru gestionarea shipping-ului
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

using Proiect.Domain.Shipping;

namespace Proiect.Application.Shipping.Commands;

/// <summary>
/// COMANDĂ: CreateShipment
/// Creează un shipment pentru o comandă
/// </summary>
public record CreateShipment(
    Guid OrderId,
    DeliveryAddress DeliveryAddress
);

/// <summary>
/// COMANDĂ: PrepareForShipment
/// Pregătește coletul pentru expediere
/// Declanșează: ShipmentPrepared event
/// Validări:
/// - ShipmentId există
/// - Status = Created
/// </summary>
public record PrepareForShipment(
    Guid ShipmentId,
    string Notes = ""
);

/// <summary>
/// COMANDĂ: ShipOrder
/// Expediază coletul către client
/// Declanșează: OrderShipped event
/// Validări:
/// - ShipmentId există
/// - Status = Prepared
/// - Carrier nu este gol
/// - TrackingNumber nu este gol
/// - EstimatedDeliveryDate în viitor (opțional)
/// </summary>
public record ShipOrder(
    Guid ShipmentId,
    string Carrier,
    string TrackingNumber,
    DateTime? EstimatedDeliveryDate = null
);

/// <summary>
/// COMANDĂ: UpdateTracking
/// Actualizează statusul de tracking
/// Declanșează: ShipmentTrackingUpdated event
/// Validări:
/// - ShipmentId există
/// - Status >= Shipped
/// - Status != Delivered
/// </summary>
public record UpdateTracking(
    Guid ShipmentId,
    string Location,
    string Status,
    string Notes = ""
);

/// <summary>
/// COMANDĂ: DeliverOrder
/// Marchează comanda ca livrată
/// Declanșează: OrderDelivered event
/// Validări:
/// - ShipmentId există
/// - Status >= Shipped
/// - Status != Delivered
/// - RecipientName nu este gol
/// </summary>
public record DeliverOrder(
    Guid ShipmentId,
    string RecipientName,
    string DeliveredBy,
    string Notes = ""
);

/// <summary>
/// COMANDĂ: MarkShipmentAsLost
/// Marchează coletul ca pierdut
/// Declanșează: ShipmentLost event
/// </summary>
public record MarkShipmentAsLost(
    Guid ShipmentId,
    string Reason
);

/// <summary>
/// COMANDĂ: MarkShipmentAsReturned
/// Marchează coletul ca returnat
/// Declanșează: ShipmentReturned event
/// </summary>
public record MarkShipmentAsReturned(
    Guid ShipmentId,
    string Reason
);

/// <summary>
/// COMANDĂ: UpdateDeliveryAddress
/// Actualizează adresa de livrare (doar înainte de shipping)
/// Declanșează: DeliveryAddressUpdated event
/// </summary>
public record UpdateDeliveryAddress(
    Guid ShipmentId,
    DeliveryAddress NewAddress
);

