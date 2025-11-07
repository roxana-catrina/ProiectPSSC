// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: INVENTORY MANAGEMENT - DOMAIN LAYER
// ═══════════════════════════════════════════════════════════════════════════════
// Agregat: InventoryItem - gestionează stocul și rezervările
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

using Proiect.Domain.Inventory.Events;

namespace Proiect.Domain.Inventory;

/// <summary>
/// AGREGAT ROOT: InventoryItem
/// Gestionează stocul pentru un SKU și rezervările asociate
/// 
/// INVARIANȚI:
/// 1. TotalOnHand >= 0 (nu există stoc negativ)
/// 2. Suma rezervărilor &lt;= TotalOnHand
/// 3. Fiecare rezervare are cantitate &gt; 0
/// 4. ReservationId este unic în cadrul agregatului
/// 5. Cantitatea disponibilă (Available) >= 0
/// </summary>
public class InventoryItem
{
    // ═══════════════════════════════════════════════════════════════════════════
    // STATE (Encapsulated)
    // ═══════════════════════════════════════════════════════════════════════════
    
    public string Sku { get; private set; }
    public int TotalOnHand { get; private set; }
    public int MinimumStockLevel { get; private set; }
    public int ReorderPoint { get; private set; }
    
    private readonly Dictionary<Guid, Reservation> _reservations = new();
    
    // Pentru event sourcing - evenimente necomise
    private readonly List<object> _uncommittedEvents = new();
    
    // ═══════════════════════════════════════════════════════════════════════════
    // COMPUTED PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Cantitatea disponibilă pentru rezervări = TotalOnHand - Suma rezervărilor
    /// </summary>
    public int Available => TotalOnHand - _reservations.Values.Sum(r => r.Quantity);
    
    public IReadOnlyCollection<Reservation> Reservations => _reservations.Values.ToList().AsReadOnly();
    
    public IReadOnlyList<object> UncommittedEvents => _uncommittedEvents.AsReadOnly();
    
    public bool IsLowStock => TotalOnHand <= MinimumStockLevel;
    
    public bool NeedsReorder => TotalOnHand <= ReorderPoint;
    
    // ═══════════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════════
    
    public InventoryItem(string sku, int initialOnHand = 0, int minimumStockLevel = 0, int reorderPoint = 0)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new InvalidInventoryCommandException("SKU cannot be empty");
        
        if (initialOnHand < 0)
            throw new InvalidInventoryCommandException("Initial stock cannot be negative");
        
        if (minimumStockLevel < 0)
            throw new InvalidInventoryCommandException("Minimum stock level cannot be negative");
        
        if (reorderPoint < 0)
            throw new InvalidInventoryCommandException("Reorder point cannot be negative");
        
        Sku = sku;
        TotalOnHand = initialOnHand;
        MinimumStockLevel = minimumStockLevel;
        ReorderPoint = reorderPoint;
        
        EnsureInvariants();
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // BUSINESS OPERATIONS (Commands → Events)
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// COMMAND: Reserve
    /// Rezervă o cantitate din stoc pentru o comandă
    /// EMITE: StockReserved
    /// </summary>
    public void Reserve(Guid reservationId, int quantity, string reason, DateTime? expiresAt = null)
    {
        // VALIDĂRI (Reguli de business)
        if (quantity <= 0)
            throw new InvalidInventoryCommandException("Quantity must be greater than 0");
        
        if (_reservations.ContainsKey(reservationId))
            throw new InvalidInventoryCommandException($"Reservation {reservationId} already exists (idempotency check)");
        
        if (quantity > Available)
            throw new InsufficientStockException(
                $"Insufficient stock for SKU {Sku}. Available: {Available}, Requested: {quantity}");
        
        if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
            throw new InvalidInventoryCommandException("Expiration date must be in the future");
        
        // OPERAȚIE
        var reservation = new Reservation(reservationId, quantity, reason, expiresAt ?? DateTime.UtcNow.AddHours(24));
        _reservations.Add(reservationId, reservation);
        
        // VERIFICARE INVARIANȚI
        EnsureInvariants();
        
        // EVENIMENT
        var @event = new StockReserved(
            Sku,
            reservationId,
            quantity,
            reason,
            reservation.ReservedAt,
            reservation.ExpiresAt);
        
        _uncommittedEvents.Add(@event);
    }
    
    /// <summary>
    /// COMMAND: Release
    /// Eliberează o rezervare (total sau parțial)
    /// EMITE: StockReleased
    /// </summary>
    public void Release(Guid reservationId, int quantity, string reason)
    {
        // VALIDĂRI
        if (quantity <= 0)
            throw new InvalidInventoryCommandException("Quantity must be greater than 0");
        
        if (!_reservations.TryGetValue(reservationId, out var reservation))
            throw new ReservationNotFoundException($"Reservation {reservationId} not found for SKU {Sku}");
        
        if (quantity > reservation.Quantity)
            throw new InvalidInventoryCommandException(
                $"Cannot release more than reserved. Reserved: {reservation.Quantity}, Requested: {quantity}");
        
        // OPERAȚIE
        int releasedQuantity = quantity;
        reservation.Quantity -= quantity;
        
        if (reservation.Quantity == 0)
        {
            _reservations.Remove(reservationId);
        }
        
        // VERIFICARE INVARIANȚI
        EnsureInvariants();
        
        // EVENIMENT
        var @event = new StockReleased(
            Sku,
            reservationId,
            releasedQuantity,
            reason,
            DateTime.UtcNow);
        
        _uncommittedEvents.Add(@event);
    }
    
    /// <summary>
    /// COMMAND: CommitReservation
    /// Transformă o rezervare în consum efectiv (ex: la shipping)
    /// </summary>
    public void CommitReservation(Guid reservationId, string reason)
    {
        if (!_reservations.TryGetValue(reservationId, out var reservation))
            throw new ReservationNotFoundException($"Reservation {reservationId} not found");
        
        // Scadem din stoc și eliminăm rezervarea
        TotalOnHand -= reservation.Quantity;
        _reservations.Remove(reservationId);
        
        EnsureInvariants();
        
        var @event = new StockCommitted(Sku, reservationId, reservation.Quantity, reason, DateTime.UtcNow);
        _uncommittedEvents.Add(@event);
    }
    
    /// <summary>
    /// COMMAND: IncreaseStock
    /// Adaugă stoc (ex: recepție marfă)
    /// </summary>
    public void IncreaseStock(int quantity, string reason)
    {
        if (quantity <= 0)
            throw new InvalidInventoryCommandException("Quantity must be greater than 0");
        
        TotalOnHand += quantity;
        
        EnsureInvariants();
        
        var @event = new StockIncreased(Sku, quantity, reason, DateTime.UtcNow);
        _uncommittedEvents.Add(@event);
    }
    
    /// <summary>
    /// COMMAND: DecreaseStock
    /// Scade stoc direct (ex: deteriorare, pierdere)
    /// </summary>
    public void DecreaseStock(int quantity, string reason)
    {
        if (quantity <= 0)
            throw new InvalidInventoryCommandException("Quantity must be greater than 0");
        
        if (quantity > TotalOnHand)
            throw new InvalidInventoryCommandException(
                $"Cannot decrease more than on-hand. On-hand: {TotalOnHand}, Requested: {quantity}");
        
        TotalOnHand -= quantity;
        
        EnsureInvariants();
        
        var @event = new StockDecreased(Sku, quantity, reason, DateTime.UtcNow);
        _uncommittedEvents.Add(@event);
    }
    
    /// <summary>
    /// COMMAND: ExpireReservation
    /// Eliberează automat rezervările expirate
    /// </summary>
    public void ExpireReservations()
    {
        var now = DateTime.UtcNow;
        var expiredReservations = _reservations.Values
            .Where(r => r.ExpiresAt <= now)
            .ToList();
        
        foreach (var reservation in expiredReservations)
        {
            Release(reservation.ReservationId, reservation.Quantity, "Reservation expired");
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // INVARIANT ENFORCEMENT
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Verifică și asigură că toți invarianții agregatului sunt respectați
    /// </summary>
    private void EnsureInvariants()
    {
        // INVARIANT 1: TotalOnHand >= 0
        if (TotalOnHand < 0)
            throw new InvariantViolationException(
                $"INVARIANT VIOLATION: TotalOnHand cannot be negative. Current: {TotalOnHand}");
        
        // INVARIANT 2: Fiecare rezervare are cantitate > 0
        var invalidReservation = _reservations.Values.FirstOrDefault(r => r.Quantity <= 0);
        if (invalidReservation != null)
            throw new InvariantViolationException(
                $"INVARIANT VIOLATION: Reservation {invalidReservation.ReservationId} has invalid quantity: {invalidReservation.Quantity}");
        
        // INVARIANT 3: Suma rezervărilor <= TotalOnHand
        var sumReserved = _reservations.Values.Sum(r => r.Quantity);
        if (sumReserved > TotalOnHand)
            throw new InvariantViolationException(
                $"INVARIANT VIOLATION: Total reserved ({sumReserved}) exceeds on-hand ({TotalOnHand})");
        
        // INVARIANT 4: Available >= 0
        if (Available < 0)
            throw new InvariantViolationException(
                $"INVARIANT VIOLATION: Available quantity cannot be negative. Current: {Available}");
    }
    
    /// <summary>
    /// Șterge evenimentele necomise (după persistare)
    /// </summary>
    public void ClearUncommittedEvents()
    {
        _uncommittedEvents.Clear();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// VALUE OBJECT: Reservation
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Value Object pentru o rezervare de stoc
/// </summary>
public class Reservation
{
    public Guid ReservationId { get; }
    public int Quantity { get; set; } // Mutable pentru release parțial
    public string Reason { get; }
    public DateTime ReservedAt { get; }
    public DateTime ExpiresAt { get; }
    
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    
    public Reservation(Guid reservationId, int quantity, string reason, DateTime expiresAt)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));
        
        ReservationId = reservationId;
        Quantity = quantity;
        Reason = reason ?? "Unknown";
        ReservedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DOMAIN EXCEPTIONS
// ═══════════════════════════════════════════════════════════════════════════════

public class InvalidInventoryCommandException : Exception
{
    public InvalidInventoryCommandException(string message) : base(message) { }
}

public class InsufficientStockException : Exception
{
    public InsufficientStockException(string message) : base(message) { }
}

public class ReservationNotFoundException : Exception
{
    public ReservationNotFoundException(string message) : base(message) { }
}

public class InvariantViolationException : Exception
{
    public InvariantViolationException(string message) : base(message) { }
}
