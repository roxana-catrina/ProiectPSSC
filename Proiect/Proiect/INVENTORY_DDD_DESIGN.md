# 📦 BOUNDED CONTEXT: INVENTORY MANAGEMENT - DDD DESIGN

## Data: November 7, 2025

---

## 1. COMENZI → EVENIMENTE (Command-Event Mapping)

### 1.1 ReserveStock → StockReserved
**Comandă:** `ReserveStock`
- **Input:** SKU, ReservationId, Quantity, Reason, ExpiresAt (optional)
- **Trigger:** Când o comandă este plasată și trebuie rezervat stoc
- **Eveniment emis:** `StockReserved`
- **Scenariu:** Order BC trimite cerere de rezervare → Inventory BC rezervă stocul

### 1.2 ReleaseStock → StockReleased
**Comandă:** `ReleaseStock`
- **Input:** SKU, ReservationId, Quantity, Reason
- **Trigger:** Când o comandă este anulată, timeout rezervare, sau payment failed
- **Eveniment emis:** `StockReleased`
- **Scenariu:** Order cancelled / Payment failed → eliberează rezervarea

### 1.3 CommitReservation → StockCommitted
**Comandă:** `CommitReservation`
- **Input:** SKU, ReservationId, Reason
- **Trigger:** Când comanda este expediată (shipping confirmed)
- **Eveniment emis:** `StockCommitted`
- **Scenariu:** Order shipped → transformă rezervarea în consum efectiv

### 1.4 IncreaseStock → StockIncreased
**Comandă:** `IncreaseStock`
- **Input:** SKU, Quantity, Reason
- **Trigger:** Recepție marfă, ajustare inventar
- **Eveniment emis:** `StockIncreased`

### 1.5 DecreaseStock → StockDecreased
**Comandă:** `DecreaseStock`
- **Input:** SKU, Quantity, Reason
- **Trigger:** Deteriorare, pierdere, ajustare inventar
- **Eveniment emis:** `StockDecreased`

---

## 2. AGREGĂRI (Aggregates)

### 2.1 AGREGAT ROOT: InventoryItem

**Identificator:** SKU (String)

**Responsabilități:**
- Gestionează cantitatea totală de stoc (TotalOnHand)
- Gestionează rezervările active pentru comenzi
- Asigură că suma rezervărilor nu depășește stocul disponibil
- Emite evenimente de domeniu pentru schimbări de stare

**Structură:**
```csharp
public class InventoryItem
{
    public string Sku { get; private set; }
    public int TotalOnHand { get; private set; }
    public int MinimumStockLevel { get; private set; }
    public int ReorderPoint { get; private set; }
    private Dictionary<Guid, Reservation> _reservations;
    
    // Computed
    public int Available => TotalOnHand - Sum(Reservations)
}
```

**Value Objects incluse:**
- `Reservation`: ReservationId, Quantity, Reason, ReservedAt, ExpiresAt

**Granularitate:**
- Un agregat per SKU (Product)
- Alternativă pentru scalabilitate mare: separare Reservation ca agregat independent

---

## 3. REGULI DE VALIDARE

### 3.1 ReserveStock

#### Validări pre-condiții:
✅ `Quantity > 0`
✅ `SKU exists` (InventoryItem există în sistem)
✅ `ReservationId is unique` (idempotency - nu există deja)
✅ `Available >= Quantity` (stoc disponibil suficient)
✅ `ExpiresAt > DateTime.UtcNow` (dacă este specificat)

#### Validări business:
- Nu permite rezervări negative
- Nu permite dublă rezervare cu același ReservationId
- Verifică că nu depășim stocul fizic disponibil

#### Excepții aruncate:
- `InvalidInventoryCommandException` - parametri invalizi
- `InsufficientStockException` - stoc insuficient
- `InvalidOperationException` - SKU nu există

---

### 3.2 ReleaseStock

#### Validări pre-condiții:
✅ `Quantity > 0`
✅ `ReservationId exists` (rezervarea trebuie să existe)
✅ `Quantity <= ReservedQuantity` (nu elibera mai mult decât e rezervat)

#### Validări business:
- Permite release parțial (quantity < reserved)
- Permite release total (quantity = reserved) → șterge rezervarea
- Idempotent: dacă rezervarea nu mai există, nu aruncă eroare (opțional)

#### Excepții aruncate:
- `InvalidInventoryCommandException` - parametri invalizi
- `ReservationNotFoundException` - rezervarea nu există

---

### 3.3 CommitReservation

#### Validări pre-condiții:
✅ `ReservationId exists`
✅ `TotalOnHand >= ReservedQuantity` (pentru a scădea din stoc)

#### Excepții aruncate:
- `ReservationNotFoundException`
- `InvalidInventoryCommandException`

---

### 3.4 IncreaseStock

#### Validări:
✅ `Quantity > 0`

---

### 3.5 DecreaseStock

#### Validări:
✅ `Quantity > 0`
✅ `TotalOnHand >= Quantity` (nu permite stoc negativ)

---

## 4. INVARIANȚI (Invariants)

### 4.1 Invariant A: Stoc Non-Negativ
```csharp
TotalOnHand >= 0
```
**Descriere:** Stocul fizic nu poate fi niciodată negativ

**Enforcement:** Verificat în `EnsureInvariants()` și în metodele `DecreaseStock()`, `CommitReservation()`

**Excepție:** `InvariantViolationException`

---

### 4.2 Invariant B: Rezervări Pozitive
```csharp
∀ Reservation r: r.Quantity > 0
```
**Descriere:** Fiecare rezervare trebuie să aibă cantitate strict pozitivă

**Enforcement:** Verificat la crearea rezervării și după release

---

### 4.3 Invariant C: Suma Rezervărilor ≤ Stoc Total
```csharp
Sum(Reservations.Quantity) <= TotalOnHand
```
**Descriere:** Nu poți rezerva mai mult decât ai în stoc fizic (CRUCIAL pentru consistență)

**Enforcement:** Verificat în `Reserve()` prin `Available >= Quantity`

**Relaxare:** Dacă se permite backorder, acest invariant se modifică și se gestionează separat

---

### 4.4 Invariant D: Disponibil Non-Negativ
```csharp
Available = TotalOnHand - Sum(Reservations) >= 0
```
**Descriere:** Cantitatea disponibilă pentru noi rezervări este întotdeauna >= 0

**Enforcement:** Derivat automat din Invariant C

---

### 4.5 Invariant E: Unicitate ReservationId
```csharp
∀ r1, r2 ∈ Reservations: r1.Id ≠ r2.Id
```
**Descriere:** Fiecare rezervare are un ID unic în cadrul agregatului

**Enforcement:** Verificat în `Reserve()` prin `_reservations.ContainsKey()`

---

## 5. FLUXURI DE PROCES (Process Flows)

### 5.1 Flux: Plasare Comandă (Happy Path)
```
1. Order BC: PlaceOrder command
2. Order BC: emit OrderPlaced event
3. Inventory BC: receive OrderPlaced (saga/orchestrator)
4. Inventory BC: ReserveStock command
5. Inventory BC: validate (Available >= Quantity)
6. Inventory BC: emit StockReserved event
7. Payment BC: ProcessPayment command
8. Payment BC: emit PaymentSucceeded event
9. Shipping BC: ShipOrder command
10. Inventory BC: CommitReservation command
11. Inventory BC: emit StockCommitted event
```

### 5.2 Flux: Anulare Comandă
```
1. Order BC: CancelOrder command
2. Order BC: emit OrderCancelled event
3. Inventory BC: receive OrderCancelled
4. Inventory BC: ReleaseStock command
5. Inventory BC: emit StockReleased event
```

### 5.3 Flux: Payment Failed
```
1. Payment BC: emit PaymentFailed event
2. Inventory BC: receive PaymentFailed (compensation)
3. Inventory BC: ReleaseStock command
4. Inventory BC: emit StockReleased event
5. Order BC: receive compensations → update order status
```

### 5.4 Flux: Expirare Rezervare (Background Job)
```
1. Scheduler: trigger ExpireReservations job
2. Inventory BC: load all InventoryItems
3. Inventory BC: for each item → ExpireReservations()
4. Inventory BC: emit StockReleased pentru fiecare rezervare expirată
```

---

## 6. EXEMPLE DE COD

### 6.1 Agregat cu Enforcement de Invarianți

```csharp
public void Reserve(Guid reservationId, int quantity, string reason, DateTime? expiresAt)
{
    // VALIDĂRI
    if (quantity <= 0)
        throw new InvalidInventoryCommandException("Quantity > 0");
    
    if (_reservations.ContainsKey(reservationId))
        throw new InvalidInventoryCommandException("Already reserved (idempotency)");
    
    if (quantity > Available)
        throw new InsufficientStockException($"Available: {Available}, Requested: {quantity}");
    
    // OPERAȚIE
    var reservation = new Reservation(reservationId, quantity, reason, expiresAt);
    _reservations.Add(reservationId, reservation);
    
    // VERIFICARE INVARIANȚI
    EnsureInvariants();
    
    // EVENIMENT
    _uncommittedEvents.Add(new StockReserved(...));
}
```

### 6.2 Verificare Invarianți

```csharp
private void EnsureInvariants()
{
    // INVARIANT 1: TotalOnHand >= 0
    if (TotalOnHand < 0)
        throw new InvariantViolationException("TotalOnHand < 0");
    
    // INVARIANT 2: Rezervări pozitive
    if (_reservations.Values.Any(r => r.Quantity <= 0))
        throw new InvariantViolationException("Invalid reservation quantity");
    
    // INVARIANT 3: Sum(Reservations) <= TotalOnHand
    var sumReserved = _reservations.Values.Sum(r => r.Quantity);
    if (sumReserved > TotalOnHand)
        throw new InvariantViolationException("Reserved > OnHand");
}
```

---

## 7. API ENDPOINTS (Quick Reference)

| Method | Endpoint | Command | Event |
|--------|----------|---------|-------|
| POST | `/api/inventory` | CreateInventoryItem | - |
| GET | `/api/inventory/{sku}` | - | - |
| POST | `/api/inventory/{sku}/reserve` | ReserveStock | StockReserved |
| POST | `/api/inventory/{sku}/release` | ReleaseStock | StockReleased |
| POST | `/api/inventory/{sku}/commit` | CommitReservation | StockCommitted |
| POST | `/api/inventory/{sku}/increase` | IncreaseStock | StockIncreased |
| POST | `/api/inventory/{sku}/decrease` | DecreaseStock | StockDecreased |

---

## 8. CONSIDERAȚII TEHNICE

### 8.1 Concurență
- **Optimistic Concurrency:** Folosește version field pentru EF Core
- **Pessimistic Locking:** Lock la nivel repository (dacă necesar)
- **Retry Policy:** Implementează retry cu exponential backoff pentru conflicte

### 8.2 Idempotență
- ReservationId asigură idempotență pentru Reserve
- Duplicate commands nu vor genera erori (check `ContainsKey`)

### 8.3 Event Sourcing (opțional)
- Stocarea evenimentelor `StockReserved`, `StockReleased` în event store
- Reconstituirea agregatului din evenimente
- Avantaje: audit trail complet, replay, debugging

### 8.4 Scalabilitate
- Pentru volume mari: separare `Reservation` ca agregat distinct
- Sharding per SKU
- Cache pentru queries (read model)

---

## 9. TESTE RECOMANDATE

### Unit Tests
- ✅ Reserve cu stoc suficient → success
- ✅ Reserve cu stoc insuficient → InsufficientStockException
- ✅ Dublă rezervare cu același ID → InvalidInventoryCommandException
- ✅ Release rezervare inexistentă → ReservationNotFoundException
- ✅ Release cantitate mai mare decât rezervat → InvalidInventoryCommandException
- ✅ CommitReservation scade TotalOnHand corect
- ✅ Invarianți menținuți după fiecare operație

### Integration Tests
- ✅ Flow complet: Reserve → Commit
- ✅ Flow compensare: Reserve → Release
- ✅ Expirare automată rezervări
- ✅ Concurență: 2 rezervări simultane pentru ultimul produs

---

## 10. REZUMAT DDD

### Comenzi identificate:
1. ✅ ReserveStock
2. ✅ ReleaseStock
3. ✅ CommitReservation
4. ✅ IncreaseStock
5. ✅ DecreaseStock
6. ✅ CreateInventoryItem
7. ✅ ExpireReservations

### Agregate:
1. ✅ **InventoryItem** (Root) - per SKU

### Value Objects:
1. ✅ **Reservation** - (ReservationId, Quantity, Reason, Dates)

### Evenimente:
1. ✅ StockReserved
2. ✅ StockReleased
3. ✅ StockCommitted
4. ✅ StockIncreased
5. ✅ StockDecreased

### Invarianți:
1. ✅ TotalOnHand >= 0
2. ✅ Reservation.Quantity > 0
3. ✅ Sum(Reservations) <= TotalOnHand
4. ✅ Available >= 0
5. ✅ Unique ReservationId per agregat

---

**Implementat cu principiile DDD:**
- Ubiquitous Language ✅
- Bounded Context ✅
- Aggregate Root ✅
- Domain Events ✅
- Invariant Enforcement ✅
- Command-Query Separation ✅

