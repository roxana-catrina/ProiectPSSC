# 📦 BOUNDED CONTEXT: SHIPPING & DELIVERY - DDD DESIGN

## Data: November 7, 2025

---

## 1. COMENZI → EVENIMENTE (Command-Event Mapping)

### 1.1 PrepareForShipment → ShipmentPrepared
**Comandă:** `PrepareForShipment`
- **Input:** ShipmentId, Notes (optional)
- **Trigger:** Când coletul este pregătit în depozit (ambalare, etichetare)
- **Eveniment emis:** `ShipmentPrepared`
- **Scenariu:** Warehouse confirmă că pachetul este gata pentru expediere

### 1.2 ShipOrder → OrderShipped
**Comandă:** `ShipOrder`
- **Input:** ShipmentId, Carrier, TrackingNumber, EstimatedDeliveryDate (optional)
- **Trigger:** Când coletul este preluat de curier pentru transport
- **Eveniment emis:** `OrderShipped` ⭐ (EVENIMENT PRINCIPAL)
- **Scenariu:** Curierul preia coletul → se generează tracking number

### 1.3 UpdateTracking → ShipmentTrackingUpdated
**Comandă:** `UpdateTracking`
- **Input:** ShipmentId, Location, Status, Notes
- **Trigger:** Update-uri intermediare de la curier (in transit)
- **Eveniment emis:** `ShipmentTrackingUpdated`
- **Scenariu:** Coletul trece prin hub-uri de tranzit

### 1.4 DeliverOrder → OrderDelivered
**Comandă:** `DeliverOrder`
- **Input:** ShipmentId, RecipientName, DeliveredBy, Notes
- **Trigger:** Când coletul este livrat destinatarului final
- **Eveniment emis:** `OrderDelivered` ⭐ (EVENIMENT PRINCIPAL)
- **Scenariu:** Curierul confirmă livrarea cu semnătură

### 1.5 MarkShipmentAsLost → ShipmentLost
**Comandă:** `MarkShipmentAsLost`
- **Input:** ShipmentId, Reason
- **Trigger:** Coletul este pierdut în tranzit
- **Eveniment emis:** `ShipmentLost`

### 1.6 MarkShipmentAsReturned → ShipmentReturned
**Comandă:** `MarkShipmentAsReturned`
- **Input:** ShipmentId, Reason
- **Trigger:** Livrare eșuată, destinatar absent
- **Eveniment emis:** `ShipmentReturned`

### 1.7 UpdateDeliveryAddress → DeliveryAddressUpdated
**Comandă:** `UpdateDeliveryAddress`
- **Input:** ShipmentId, NewAddress
- **Trigger:** Client modifică adresa înainte de shipping
- **Eveniment emis:** `DeliveryAddressUpdated`

---

## 2. AGREGĂRI (Aggregates)

### 2.1 AGREGAT ROOT: Shipment

**Identificator:** ShipmentId (Guid)

**Responsabilități:**
- Gestionează ciclul de viață al livrării (Created → Prepared → Shipped → InTransit → Delivered)
- Tracked changes și evenimente de tracking
- Asigură că doar comenzi valide pot progresa prin stări
- Menține adresa de livrare și informații despre curier

**Structură:**
```csharp
public class Shipment
{
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
    
    private List<TrackingEvent> _trackingEvents;
}
```

**Value Objects incluse:**
- `DeliveryAddress`: RecipientName, Street, City, PostalCode, Country, Phone
- `TrackingEvent`: Timestamp, Description, Location

**Granularitate:**
- Un agregat per comandă (1:1 mapping cu Order)
- Alternativă: dacă o comandă poate avea multiple colete, atunci agregatul devine `Shipment` per colet individual

---

## 3. REGULI DE VALIDARE

### 3.1 PrepareForShipment

#### Validări pre-condiții:
✅ `ShipmentId exists`
✅ `Status == Created` (nu poți pregăti ceva deja pregătit/expediat)

#### Validări business:
- Status trebuie să progreseze secvențial
- Nu permite re-prepare după shipping

#### Excepții aruncate:
- `InvalidShippingCommandException` - status incorect

---

### 3.2 ShipOrder

#### Validări pre-condiții:
✅ `ShipmentId exists`
✅ `Status == Prepared` (trebuie pregătit mai întâi)
✅ `Carrier is not empty`
✅ `TrackingNumber is not empty`
✅ `EstimatedDeliveryDate > DateTime.UtcNow` (dacă specificat)

#### Validări business:
- Adresa de livrare trebuie să fie completă
- Nu permite shipping fără prepare
- Tracking number trebuie să fie unic (verificare la nivel repository)

#### Excepții aruncate:
- `InvalidShippingCommandException` - parametri invalizi sau status incorect

---

### 3.3 UpdateTracking

#### Validări pre-condiții:
✅ `ShipmentId exists`
✅ `Status >= Shipped` (trebuie expediat)
✅ `Status != Delivered` (nu mai actualizezi tracking după livrare)

#### Validări business:
- Permite multiple update-uri intermediare
- Tranziție automată de la Shipped → InTransit la primul update

---

### 3.4 DeliverOrder

#### Validări pre-condiții:
✅ `ShipmentId exists`
✅ `Status >= Shipped` (trebuie expediat)
✅ `Status != Delivered` (nu livra de 2 ori)
✅ `RecipientName is not empty`

#### Validări business:
- Nu poți livra ceva ce nu a fost expediat
- DeliveredAt >= ShippedAt (temporal consistency)

#### Excepții aruncate:
- `InvalidShippingCommandException` - nu a fost expediat sau deja livrat

---

### 3.5 UpdateDeliveryAddress

#### Validări pre-condiții:
✅ `Status < Shipped` (doar înainte de expediere)
✅ `NewAddress is complete and valid`

#### Validări business:
- Nu permite schimbarea adresei după shipping
- Adresa trebuie să conțină toate câmpurile obligatorii

---

## 4. INVARIANȚI (Invariants)

### 4.1 Invariant A: Identificatori Non-Empty
```csharp
ShipmentId != Guid.Empty
OrderId != Guid.Empty
```
**Descriere:** Identificatorii trebuie să fie valizi

**Enforcement:** Verificat în constructor și `EnsureInvariants()`

---

### 4.2 Invariant B: Progresie Status Secvențială
```csharp
Created → Prepared → Shipped → InTransit → Delivered
```
**Descriere:** Status-ul poate progresa doar înainte în ciclul de viață (nu backward)

**Enforcement:** Verificat în fiecare metodă care schimbă status-ul

**Excepții:** Lost și Returned pot apărea după Shipped (dar nu după Delivered)

---

### 4.3 Invariant C: Carrier și TrackingNumber Obligatorii După Shipping
```csharp
IF Status >= Shipped THEN Carrier != null AND TrackingNumber != null
```
**Descriere:** După expediere, trebuie să existe informații despre curier

**Enforcement:** Verificat în `EnsureInvariants()` și în metoda `Ship()`

---

### 4.4 Invariant D: DeliveredAt >= ShippedAt
```csharp
IF DeliveredAt != null AND ShippedAt != null THEN DeliveredAt >= ShippedAt
```
**Descriere:** Nu poți livra înainte de a expedia (consistență temporală)

**Enforcement:** Verificat în `EnsureInvariants()`

---

### 4.5 Invariant E: Nu Se Poate Livra Fără Shipping
```csharp
IF Status == Delivered THEN ShippedAt != null
```
**Descriere:** Delivery necesită shipping mai întâi

**Enforcement:** Verificat în `Deliver()` și `EnsureInvariants()`

---

### 4.6 Invariant F: Adresa de Livrare Completă
```csharp
DeliveryAddress != null
DeliveryAddress.RecipientName != empty
DeliveryAddress.Street != empty
DeliveryAddress.City != empty
DeliveryAddress.PostalCode != empty
DeliveryAddress.Country != empty
```
**Descriere:** Adresa trebuie să conțină toate informațiile necesare

**Enforcement:** Verificat în `DeliveryAddress.Validate()`

---

## 5. FLUXURI DE PROCES (Process Flows)

### 5.1 Flux: Livrare Standard (Happy Path)
```
1. Order BC: OrderConfirmed event
2. Shipping BC: CreateShipment command
3. Warehouse: PrepareForShipment command
4. Shipping BC: emit ShipmentPrepared event
5. Courier pickup: ShipOrder command
6. Shipping BC: emit OrderShipped event ⭐
7. In transit: UpdateTracking command (multiple)
8. Shipping BC: emit ShipmentTrackingUpdated events
9. Final delivery: DeliverOrder command
10. Shipping BC: emit OrderDelivered event ⭐
11. Order BC: receive OrderDelivered → update order status
```

### 5.2 Flux: Livrare Eșuată (Returned)
```
1-6. ... (ca în happy path)
7. Recipient absent / refused
8. Shipping BC: MarkShipmentAsReturned command
9. Shipping BC: emit ShipmentReturned event
10. Order BC: handle return → refund sau reschedulare
```

### 5.3 Flux: Colet Pierdut
```
1-6. ... (shipping normal)
7. Investigation → not found
8. Shipping BC: MarkShipmentAsLost command
9. Shipping BC: emit ShipmentLost event
10. Order BC: compensate → refund
11. Inventory BC: poate înregistra pierdere
```

### 5.4 Flux: Modificare Adresă (Before Shipping)
```
1. Client: UpdateDeliveryAddress command
2. Shipping BC: validate (status < Shipped)
3. Shipping BC: UpdateDeliveryAddress on aggregate
4. Shipping BC: emit DeliveryAddressUpdated event
```

---

## 6. EXEMPLE DE COD

### 6.1 Agregat cu Enforcement de Invarianți

```csharp
public void Ship(string carrier, string trackingNumber, DateTime? estimatedDeliveryDate)
{
    // VALIDĂRI
    if (Status != ShipmentStatus.Prepared)
        throw new InvalidShippingCommandException($"Cannot ship in status {Status}");
    
    if (string.IsNullOrWhiteSpace(carrier))
        throw new InvalidShippingCommandException("Carrier is required");
    
    if (string.IsNullOrWhiteSpace(trackingNumber))
        throw new InvalidShippingCommandException("Tracking number is required");
    
    // OPERAȚIE
    Status = ShipmentStatus.Shipped;
    Carrier = carrier;
    TrackingNumber = trackingNumber;
    ShippedAt = DateTime.UtcNow;
    EstimatedDeliveryDate = estimatedDeliveryDate ?? DateTime.UtcNow.AddDays(3);
    
    AddTrackingEvent($"Shipped via {carrier}", carrier);
    
    // VERIFICARE INVARIANȚI
    EnsureInvariants();
    
    // EVENIMENT
    _uncommittedEvents.Add(new OrderShipped(...));
}
```

### 6.2 Verificare Invarianți

```csharp
private void EnsureInvariants()
{
    // INVARIANT: Carrier și TrackingNumber după shipping
    if (IsShipped && (string.IsNullOrWhiteSpace(Carrier) || 
                      string.IsNullOrWhiteSpace(TrackingNumber)))
        throw new InvariantViolationException("Carrier and TrackingNumber required after shipping");
    
    // INVARIANT: DeliveredAt >= ShippedAt
    if (DeliveredAt.HasValue && ShippedAt.HasValue && DeliveredAt.Value < ShippedAt.Value)
        throw new InvariantViolationException("DeliveredAt cannot be before ShippedAt");
    
    // INVARIANT: Nu se poate livra fără shipping
    if (IsDelivered && !ShippedAt.HasValue)
        throw new InvariantViolationException("Cannot be delivered without being shipped");
}
```

---

## 7. API ENDPOINTS (Quick Reference)

| Method | Endpoint | Command | Event |
|--------|----------|---------|-------|
| POST | `/api/shipping` | CreateShipment | - |
| GET | `/api/shipping/{id}` | - | - |
| GET | `/api/shipping/order/{orderId}` | - | - |
| GET | `/api/shipping/track/{trackingNumber}` | - | (public tracking) |
| POST | `/api/shipping/{id}/prepare` | PrepareForShipment | ShipmentPrepared |
| POST | `/api/shipping/{id}/ship` | ShipOrder | **OrderShipped** ⭐ |
| POST | `/api/shipping/{id}/tracking` | UpdateTracking | ShipmentTrackingUpdated |
| POST | `/api/shipping/{id}/deliver` | DeliverOrder | **OrderDelivered** ⭐ |
| POST | `/api/shipping/{id}/mark-lost` | MarkShipmentAsLost | ShipmentLost |
| POST | `/api/shipping/{id}/mark-returned` | MarkShipmentAsReturned | ShipmentReturned |
| PUT | `/api/shipping/{id}/address` | UpdateDeliveryAddress | DeliveryAddressUpdated |

---

## 8. INTEGRARE CU ALTE BOUNDED CONTEXTS

### 8.1 Order BC → Shipping BC
```
Order: OrderConfirmed
    ↓
Shipping: CreateShipment (with delivery address from order)
    ↓
Shipping: ShipmentCreated (internal)
```

### 8.2 Shipping BC → Order BC
```
Shipping: OrderShipped
    ↓
Order: UpdateOrderStatus(Shipped)

Shipping: OrderDelivered
    ↓
Order: UpdateOrderStatus(Delivered) + CompleteOrder
```

### 8.3 Shipping BC → Inventory BC
```
Shipping: OrderShipped
    ↓
Inventory: CommitReservation (transformă rezervarea în consum efectiv)
```

### 8.4 Shipping BC → Payment BC (edge case)
```
Shipping: ShipmentLost
    ↓
Payment: ProcessRefund (dacă payment a fost deja făcut)
```

---

## 9. CONSIDERAȚII TEHNICE

### 9.1 Status Transitions (State Machine)
```
Created ──PrepareForShipment──> Prepared
Prepared ──Ship──> Shipped
Shipped ──UpdateTracking──> InTransit
InTransit ──Deliver──> Delivered

Shipped/InTransit ──MarkAsLost──> Lost
Shipped/InTransit ──MarkAsReturned──> Returned
```

### 9.2 Tracking Visibility
- **Internal tracking:** toate evenimentele în `_trackingEvents`
- **Public tracking:** expus prin API `/track/{trackingNumber}` fără informații sensibile
- **Real-time updates:** poate fi integrat cu webhook-uri de la curieri

### 9.3 Integrare Curieri Externi
- Adapter pattern pentru integrare cu API-uri externe (DHL, FedEx, etc.)
- Sincronizare tracking status prin polling sau webhooks
- Mapping între statusuri externe și ShipmentStatus intern

### 9.4 Delayed Shipments
- Computed property `IsDelayed` verifică dacă `EstimatedDeliveryDate` a trecut
- Background job poate emite `ShipmentDelayed` events
- Notificări automate către clienți

---

## 10. REZUMAT DDD

### Comenzi identificate:
1. ✅ CreateShipment
2. ✅ PrepareForShipment
3. ✅ ShipOrder
4. ✅ UpdateTracking
5. ✅ DeliverOrder
6. ✅ MarkShipmentAsLost
7. ✅ MarkShipmentAsReturned
8. ✅ UpdateDeliveryAddress

### Agregate:
1. ✅ **Shipment** (Root) - per comandă/colet

### Value Objects:
1. ✅ **DeliveryAddress** - adresă completă de livrare
2. ✅ **TrackingEvent** - evenimente de tracking

### Evenimente:
1. ✅ ShipmentPrepared
2. ✅ **OrderShipped** ⭐
3. ✅ ShipmentTrackingUpdated
4. ✅ **OrderDelivered** ⭐
5. ✅ ShipmentLost
6. ✅ ShipmentReturned
7. ✅ DeliveryAddressUpdated

### Invarianți:
1. ✅ ShipmentId și OrderId non-empty
2. ✅ Status progresie secvențială
3. ✅ Carrier și TrackingNumber obligatorii după shipping
4. ✅ DeliveredAt >= ShippedAt
5. ✅ Nu se poate livra fără shipping
6. ✅ Adresa de livrare completă

---

**Implementat cu principiile DDD:**
- Ubiquitous Language ✅
- Bounded Context ✅
- Aggregate Root ✅
- Domain Events ✅
- Invariant Enforcement ✅
- State Machine Pattern ✅
- Value Objects ✅

