// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: INVENTORY MANAGEMENT - DOMAIN EVENTS
// ═══════════════════════════════════════════════════════════════════════════════
// Evenimente de domeniu pentru Inventory
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

namespace Proiect.Domain.Inventory.Events;


/// <summary>
/// EVENIMENT: StockReserved
/// Emis când: o cantitate din stoc este rezervată pentru o comandă
/// Declanșat de: ReserveStock command
/// </summary>
public record StockReserved(
    string Sku,
    Guid ReservationId,
    int Quantity,
    string Reason,
    DateTime ReservedAt,
    DateTime ExpiresAt
);

/// <summary>
/// EVENIMENT: StockReleased
/// Emis când: o rezervare este eliberată (comandă anulată, timeout, etc.)
/// Declanșat de: ReleaseStock command
/// </summary>
public record StockReleased(
    string Sku,
    Guid ReservationId,
    int Quantity,
    string Reason,
    DateTime ReleasedAt
);

/// <summary>
/// EVENIMENT: StockCommitted
/// Emis când: o rezervare este transformată în consum efectiv (shipping)
/// Declanșat de: CommitReservation command
/// </summary>
public record StockCommitted(
    string Sku,
    Guid ReservationId,
    int Quantity,
    string Reason,
    DateTime CommittedAt
);

/// <summary>
/// EVENIMENT: StockIncreased
/// Emis când: stocul este mărit (recepție marfă)
/// Declanșat de: IncreaseStock command
/// </summary>
public record StockIncreased(
    string Sku,
    int Quantity,
    string Reason,
    DateTime Timestamp
);

/// <summary>
/// EVENIMENT: StockDecreased
/// Emis când: stocul este redus direct (pierdere, deteriorare)
/// Declanșat de: DecreaseStock command
/// </summary>
public record StockDecreased(
    string Sku,
    int Quantity,
    string Reason,
    DateTime Timestamp
);

/// <summary>
/// EVENIMENT: LowStockDetected
/// Emis când: stocul atinge nivelul minim
/// </summary>
public record LowStockDetected(
    string Sku,
    int CurrentStock,
    int MinimumStockLevel,
    DateTime DetectedAt
);

/// <summary>
/// EVENIMENT: ReorderPointReached
/// Emis când: stocul atinge punctul de recomandare
/// </summary>
public record ReorderPointReached(
    string Sku,
    int CurrentStock,
    int ReorderPoint,
    DateTime DetectedAt
);
