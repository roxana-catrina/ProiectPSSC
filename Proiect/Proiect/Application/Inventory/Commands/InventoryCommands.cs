// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: INVENTORY MANAGEMENT - APPLICATION LAYER (COMMANDS)
// ═══════════════════════════════════════════════════════════════════════════════
// Comenzi pentru gestionarea inventarului
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

namespace Proiect.Application.Inventory.Commands;

/// <summary>
/// COMANDĂ: ReserveStock
/// Declanșează: StockReserved event
/// Validări:
/// - Quantity > 0
/// - SKU există
/// - ReservationId nu există deja (idempotency)
/// - Cantitate disponibilă >= cantitate cerută
/// </summary>
public record ReserveStock(
    string Sku,
    Guid ReservationId,
    int Quantity,
    string Reason,
    DateTime? ExpiresAt = null
);

/// <summary>
/// COMANDĂ: ReleaseStock
/// Declanșează: StockReleased event
/// Validări:
/// - Quantity > 0
/// - ReservationId există
/// - Cantitatea eliberată <= cantitatea rezervată
/// </summary>
public record ReleaseStock(
    string Sku,
    Guid ReservationId,
    int Quantity,
    string Reason
);

/// <summary>
/// COMANDĂ: CommitReservation
/// Declanșează: StockCommitted event
/// Validări:
/// - ReservationId există
/// - TotalOnHand >= cantitatea rezervată
/// </summary>
public record CommitReservation(
    string Sku,
    Guid ReservationId,
    string Reason
);

/// <summary>
/// COMANDĂ: IncreaseStock
/// Declanșează: StockIncreased event
/// Validări:
/// - Quantity > 0
/// </summary>
public record IncreaseStock(
    string Sku,
    int Quantity,
    string Reason
);

/// <summary>
/// COMANDĂ: DecreaseStock
/// Declanșează: StockDecreased event
/// Validări:
/// - Quantity > 0
/// - TotalOnHand >= Quantity
/// </summary>
public record DecreaseStock(
    string Sku,
    int Quantity,
    string Reason
);

/// <summary>
/// COMANDĂ: CreateInventoryItem
/// Inițializează un nou produs în inventar
/// </summary>
public record CreateInventoryItem(
    string Sku,
    int InitialStock,
    int MinimumStockLevel,
    int ReorderPoint
);

/// <summary>
/// COMANDĂ: ExpireReservations
/// Eliberează automat rezervările expirate
/// </summary>
public record ExpireReservations(string Sku);
