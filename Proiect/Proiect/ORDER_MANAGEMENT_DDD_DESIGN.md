═══════════════════════════════════════════════════════════════════════════════
📦 BOUNDED CONTEXT: ORDER MANAGEMENT - DOMAIN DRIVEN DESIGN
═══════════════════════════════════════════════════════════════════════════════

Data: November 7, 2025
Context: Sistema de Preluare Comenzi - ORDER MANAGEMENT

═══════════════════════════════════════════════════════════════════════════════
🎯 STRUCTURĂ DDD - OVERVIEW
═══════════════════════════════════════════════════════════════════════════════

COMENZI (Commands) → AGREGATE (Aggregates) → EVENIMENTE (Events)

┌─────────────────────────────────────────────────────────────────────────┐
│                          COMENZI (Commands)                             │
│                                                                         │
│  PlaceOrderCommand                                                      │
│  ValidateOrderCommand                                                   │
│  ConfirmOrderCommand                                                    │
│  CancelOrderCommand                                                     │
│  ModifyOrderCommand                                                     │
│                                                                         │
└────────────────────────┬────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        AGREGATE (Aggregates)                            │
│                                                                         │
│  Order (Aggregate Root)                                                 │
│    - OrderId                                                            │
│    - CustomerId                                                         │
│    - OrderItems (Value Objects)                                         │
│    - ShippingAddress (Value Object)                                     │
│    - OrderStatus (Enum)                                                 │
│    - TotalAmount                                                        │
│    - CreatedDate, ModifiedDate                                          │
│                                                                         │
│  OrderItem (Entity inside Aggregate)                                    │
│    - ProductId                                                          │
│    - Quantity                                                           │
│    - UnitPrice                                                          │
│    - LineTotal                                                          │
│                                                                         │
└────────────────────────┬────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      EVENIMENTE (Domain Events)                         │
│                                                                         │
│  OrderPlaced                                                            │
│  OrderValidated                                                         │
│  OrderRejected                                                          │
│  OrderConfirmed                                                         │
│  OrderCancellationRequested                                             │
│  OrderCancelled                                                         │
│  OrderModificationRequested                                             │
│  OrderModified                                                          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

═══════════════════════════════════════════════════════════════════════════════
1️⃣ COMENZI (Commands) → EVENIMENTE (Events) MAPPING
═══════════════════════════════════════════════════════════════════════════════

┌────────────────────────────┬─────────────────────────┬──────────────────┐
│ COMANDĂ                    │ EVENIMENT SUCCES        │ EVENIMENT EȘEC   │
├────────────────────────────┼─────────────────────────┼──────────────────┤
│ PlaceOrderCommand          │ OrderPlaced             │ -                │
│ ValidateOrderCommand       │ OrderValidated          │ OrderRejected    │
│ ConfirmOrderCommand        │ OrderConfirmed          │ -                │
│ RequestCancellationCommand │ OrderCancellationReq.   │ -                │
│ CancelOrderCommand         │ OrderCancelled          │ -                │
│ RequestModificationCommand │ OrderModificationReq.   │ -                │
│ ModifyOrderCommand         │ OrderModified           │ -                │
└────────────────────────────┴─────────────────────────┴──────────────────┘

═══════════════════════════════════════════════════════════════════════════════
2️⃣ AGREGATUL ORDER - RESPONSABILITĂȚI ȘI INVARIANȚI
═══════════════════════════════════════════════════════════════════════════════

🎯 RESPONSABILITĂȚI:
   - Gestionează ciclul de viață al unei comenzi
   - Asigură consistența datelor comenzii
   - Validează modificările înainte de a fi aplicate
   - Menține istoricul stărilor comenzii
   - Calculează totalul comenzii

🛡️ INVARIANȚI (Business Rules care TREBUIE respectate ÎNTOTDEAUNA):

   I1. O comandă TREBUIE să aibă cel puțin un produs
       → OrderItems.Count >= 1

   I2. Totalul comenzii TREBUIE să fie suma tuturor liniilor
       → TotalAmount == OrderItems.Sum(item => item.LineTotal)

   I3. O comandă poate fi modificată DOAR în stările: Placed, Validated
       → OrderStatus IN [Placed, Validated] pentru modificare

   I4. O comandă poate fi anulată DOAR înainte de expediere
       → OrderStatus NOT IN [Shipped, Delivered] pentru anulare

   I5. Cantitatea fiecărui produs TREBUIE să fie > 0
       → OrderItem.Quantity > 0

   I6. Prețul fiecărui produs TREBUIE să fie > 0
       → OrderItem.UnitPrice > 0

   I7. O comandă anulată NU poate fi reactivată
       → OrderStatus != Cancelled (imuabil după anulare)

   I8. Adresa de livrare TREBUIE să fie validă
       → ShippingAddress != null && ShippingAddress.IsValid()

   I9. OrderId TREBUIE să fie unic și generat o singură dată
       → OrderId != Guid.Empty && immutable

   I10. CustomerId TREBUIE să existe și să fie valid
        → CustomerId != Guid.Empty

═══════════════════════════════════════════════════════════════════════════════
3️⃣ REGULI DE VALIDARE PENTRU FIECARE COMANDĂ
═══════════════════════════════════════════════════════════════════════════════

┌─────────────────────────────────────────────────────────────────────────┐
│ 1️⃣ PlaceOrderCommand                                                    │
├─────────────────────────────────────────────────────────────────────────┤
│ REGULI DE VALIDARE:                                                     │
│ ✓ CustomerId nu este gol                                                │
│ ✓ OrderItems nu este gol (minim 1 produs)                               │
│ ✓ Fiecare OrderItem are Quantity > 0                                    │
│ ✓ Fiecare OrderItem are UnitPrice > 0                                   │
│ ✓ ShippingAddress este completă și validă                               │
│ ✓ PaymentMethod este valid (Card, Cash, etc.)                           │
│ ✓ TotalAmount > 0                                                       │
│                                                                          │
│ BUSINESS RULES:                                                          │
│ • Clientul trebuie să existe în sistem                                  │
│ • Adresa de livrare trebuie să fie în zona acoperită                    │
│ • Suma minimă pentru comandă: 50 RON                                    │
│                                                                          │
│ REZULTAT:                                                                │
│ Succes → OrderPlaced event                                              │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ 2️⃣ ValidateOrderCommand                                                 │
├─────────────────────────────────────────────────────────────────────────┤
│ REGULI DE VALIDARE:                                                     │
│ ✓ Comanda există și are status "Placed"                                 │
│ ✓ Produsele sunt disponibile în stoc                                    │
│ ✓ Prețurile produselor nu s-au modificat                                │
│ ✓ Adresa de livrare este validă și acoperită                            │
│                                                                          │
│ BUSINESS RULES:                                                          │
│ • Verifică disponibilitatea fiecărui produs în INVENTORY context        │
│ • Verifică dacă prețurile sunt încă valide                              │
│ • Validează zona de livrare                                             │
│ • Verifică dacă clientul nu are comenzi frauduloase                     │
│                                                                          │
│ REZULTAT:                                                                │
│ Succes → OrderValidated event                                           │
│ Eșec → OrderRejected event (cu motiv: stoc insuficient, preț invalid)  │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ 3️⃣ ConfirmOrderCommand                                                  │
├─────────────────────────────────────────────────────────────────────────┤
│ REGULI DE VALIDARE:                                                     │
│ ✓ Comanda există și are status "Validated"                              │
│ ✓ Stocul a fost rezervat cu succes (StockReserved event primit)        │
│ ✓ Operatorul are dreptul să confirme comenzi                            │
│                                                                          │
│ BUSINESS RULES:                                                          │
│ • Doar operatori autorizați pot confirma comenzi                        │
│ • Confirmarea se face doar după rezervarea stocului                     │
│ • Estimează data de livrare bazat pe stoc și locație                    │
│                                                                          │
│ REZULTAT:                                                                │
│ Succes → OrderConfirmed event (cu estimatedDeliveryDate)               │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ 4️⃣ RequestCancellationCommand                                           │
├─────────────────────────────────────────────────────────────────────────┤
│ REGULI DE VALIDARE:                                                     │
│ ✓ Comanda există                                                         │
│ ✓ Solicitantul este clientul comenzii sau un operator                   │
│ ✓ Motivul anulării este specificat                                      │
│                                                                          │
│ BUSINESS RULES:                                                          │
│ • Clientul poate solicita anulare oricând înainte de livrare           │
│ • Operatorul poate solicita anulare din motive administrative          │
│ • Motivul trebuie să fie valid (schimbare de părere, preț, etc.)       │
│                                                                          │
│ REZULTAT:                                                                │
│ Succes → OrderCancellationRequested event                               │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ 5️⃣ CancelOrderCommand                                                   │
├─────────────────────────────────────────────────────────────────────────┤
│ REGULI DE VALIDARE:                                                     │
│ ✓ Comanda există                                                         │
│ ✓ Comanda NU este în status "Shipped" sau "Delivered"                   │
│ ✓ Există o cerere de anulare aprobată                                   │
│                                                                          │
│ BUSINESS RULES:                                                          │
│ • Comenzile expediate NU pot fi anulate (doar returnate)               │
│ • Anularea eliberează stocul rezervat                                   │
│ • Dacă s-a plătit, se declanșează rambursare automată                   │
│ • Clientul primește confirmare de anulare                               │
│                                                                          │
│ REZULTAT:                                                                │
│ Succes → OrderCancelled event (declanșează StockReleased, RefundInit.)  │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ 6️⃣ RequestModificationCommand                                           │
├─────────────────────────────────────────────────────────────────────────┤
│ REGULI DE VALIDARE:                                                     │
│ ✓ Comanda există                                                         │
│ ✓ Comanda este în status "Placed" sau "Validated"                       │
│ ✓ Modificările sunt specificate (produse, cantități, adresă)            │
│ ✓ Solicitantul este clientul sau operatorul                             │
│                                                                          │
│ BUSINESS RULES:                                                          │
│ • Modificări permise doar înainte de confirmare                         │
│ • Clientul poate modifica: produse, cantități, adresă                   │
│ • Operatorul poate modifica: orice câmp                                 │
│ • Modificările trebuie să respecte toate invarianții                    │
│                                                                          │
│ REZULTAT:                                                                │
│ Succes → OrderModificationRequested event                               │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│ 7️⃣ ModifyOrderCommand                                                   │
├─────────────────────────────────────────────────────────────────────────┤
│ REGULI DE VALIDARE:                                                     │
│ ✓ Comanda există și este în status modificabil                          │
│ ✓ Există o cerere de modificare aprobată                                │
│ ✓ Noile valori respectă toate invarianții                               │
│ ✓ Produsele noi sunt disponibile                                        │
│                                                                          │
│ BUSINESS RULES:                                                          │
│ • Re-validează tot comanda după modificare                              │
│ • Ajustează rezervarea de stoc pentru produse modificate               │
│ • Recalculează totalul comenzii                                         │
│ • Dacă prețul se modifică, poate necesita re-aprobare plată            │
│                                                                          │
│ REZULTAT:                                                                │
│ Succes → OrderModified event (declanșează re-validare și stock adjust.) │
└─────────────────────────────────────────────────────────────────────────┘

═══════════════════════════════════════════════════════════════════════════════
4️⃣ STATE MACHINE - TRANZIȚII DE STARE PERMISE
═══════════════════════════════════════════════════════════════════════════════

┌─────────────┐
│   PLACED    │ ← PlaceOrderCommand
└──────┬──────┘
       │
       ├─→ VALIDATED ← ValidateOrderCommand (succes)
       │
       └─→ REJECTED ← ValidateOrderCommand (eșec)
       
┌─────────────┐
│  VALIDATED  │
└──────┬──────┘
       │
       ├─→ CONFIRMED ← ConfirmOrderCommand (după StockReserved)
       │
       └─→ MODIFIED ← ModifyOrderCommand → revine la PLACED
       
┌─────────────┐
│  CONFIRMED  │
└──────┬──────┘
       │
       ├─→ PAID ← după PaymentCompleted event
       │
       └─→ CANCELLED ← CancelOrderCommand (înainte de SHIPPED)
       
┌─────────────┐
│    PAID     │
└──────┬──────┘
       │
       ├─→ SHIPPED ← după OrderShipped event
       │
       └─→ CANCELLED ← CancelOrderCommand (cu penalizare)
       
┌─────────────┐
│   SHIPPED   │
└──────┬──────┘
       │
       └─→ DELIVERED ← după OrderDelivered event
       
┌─────────────┐
│  DELIVERED  │ ← STARE FINALĂ (poate doar RETURN)
└─────────────┘

┌─────────────┐
│  CANCELLED  │ ← STARE FINALĂ (imuabilă)
└─────────────┘

┌─────────────┐
│  REJECTED   │ ← STARE FINALĂ (imuabilă)
└─────────────┘

REGULI TRANZIȚII:
✓ PLACED → VALIDATED, REJECTED, CANCELLED, MODIFIED
✓ VALIDATED → CONFIRMED, MODIFIED, CANCELLED
✓ CONFIRMED → PAID, CANCELLED
✓ PAID → SHIPPED, CANCELLED
✓ SHIPPED → DELIVERED (NU CANCELLED)
✓ DELIVERED, CANCELLED, REJECTED → IMUABILE

═══════════════════════════════════════════════════════════════════════════════
5️⃣ VALUE OBJECTS - ENCAPSULARE CONCEPTE DE BUSINESS
═══════════════════════════════════════════════════════════════════════════════

1. OrderItem (Entity în agregat)
   - ProductId (Guid)
   - ProductName (string)
   - Quantity (PositiveInteger)
   - UnitPrice (Money)
   - LineTotal (Money) - calculat: Quantity * UnitPrice

2. ShippingAddress (Value Object)
   - Street (string)
   - City (string)
   - County (string)
   - PostalCode (string)
   - Country (string)
   - Invarianți: toate câmpurile completate, format valid cod poștal

3. CustomerInfo (Value Object)
   - Name (string)
   - Email (Email - cu validare)
   - PhoneNumber (PhoneNumber - cu validare format)

4. Money (Value Object)
   - Amount (decimal)
   - Currency (string - default "RON")
   - Operații: Add, Subtract, Multiply

5. OrderStatus (Enum)
   - Placed, Validated, Rejected, Confirmed, Paid, Shipped, Delivered, Cancelled

6. CancellationReason (Value Object)
   - Reason (string)
   - RequestedBy (CustomerId sau OperatorId)
   - RequestedAt (DateTime)

═══════════════════════════════════════════════════════════════════════════════
6️⃣ DOMAIN SERVICES
═══════════════════════════════════════════════════════════════════════════════

1. OrderValidationService
   - Responsabilitate: Validări complexe care implică multiple agregate
   - Metode:
     • ValidateProductAvailability(OrderItems) → verifică cu INVENTORY
     • ValidateShippingAddress(Address) → verifică acoperire zonă
     • ValidateCustomer(CustomerId) → verifică istoric comenzi

2. OrderPricingService
   - Responsabilitate: Calculează prețuri și reduceri
   - Metode:
     • CalculateOrderTotal(OrderItems)
     • ApplyDiscounts(Order, Customer)
     • ValidatePricing(Order) → verifică dacă prețurile sunt corecte

3. OrderCancellationService
   - Responsabilitate: Logică complexă de anulare
   - Metode:
     • CanBeCancelled(Order) → verifică dacă poate fi anulată
     • CalculateCancellationFee(Order) → calcul penalizare
     • ProcessCancellation(Order) → coordonează cu PAYMENT și INVENTORY

═══════════════════════════════════════════════════════════════════════════════
7️⃣ REPOSITORY INTERFACE
═══════════════════════════════════════════════════════════════════════════════

IOrderRepository (Aggregate Repository Pattern)
   - GetByIdAsync(OrderId) → Order
   - SaveAsync(Order) → void
   - GetByCustomerIdAsync(CustomerId) → List<Order>
   - GetOrdersByStatusAsync(OrderStatus) → List<Order>
   - ExistsAsync(OrderId) → bool

Note:
• Repository lucrează DOAR cu aggregate root (Order)
• NU expune OrderItems separat (sunt parte din agregat)
• Repository asigură persistența și reconstitirea agregatului

═══════════════════════════════════════════════════════════════════════════════

