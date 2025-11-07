WORKFLOW-URI PRELUARE COMANDĂ:
Preluare comandă
Anulare comandă
Modificare comandă
Returnare comandă
Procesare comanda
Confirmare comanda
Plata comanda

ACTORI:
👤 1. Clientul
Plasează comenzi.
Poate modifica sau anula comenzi (în anumite condiții).
Confirmă plata.
Solicită returnarea produselor.

🧑‍💼 2. Operatorul/Agentul de vânzări
Verifică și confirmă comenzile.
Poate modifica comenzi la cererea clientului.
Procesează comenzile.
Gestionează anulările și retururile.

🏬 3. Depozit
Pregătește comenzile pentru livrare.
Confirmă procesarea și ambalarea.
Colaborează cu curierii pentru livrare.

💳 4. Sistem de plati
Verifică și autorizează tranzacțiile.
Gestionează rambursările în caz de retur sau anulare.

🛠️ 6. Administratorul sistemului
Configurează și întreține sistemul.
Gestionează permisiunile și fluxurile de lucru.
Monitorizează integritatea datelor și securitatea.

SCENARII:
1. Clientul plasează o comandă prin interfața sistemului
Selectează produse, cantitate, adresa de livrare și metoda de plată.

2. Sistemul confirmă comanda
Verifică disponibilitatea produselor și validează datele introduse.

3. Operatorul poate modifica comanda la cererea clientului
Se pot schimba produse, cantități, adresa sau metoda de plată.

4. Clientul poate anula comanda înainte de procesare
Sistemul actualizează statusul comenzii și eliberează stocul.

5. Sistemul procesează comanda
Pregătirea produselor pentru livrare, generare AWB, notificare curier.

6. Clientul efectuează plata
Sistemul validează tranzacția și confirmă comanda ca fiind plătită.

7. Clientul poate refuza comanda la livrare sau solicita returnarea
Sistemul inițiază procesul de retur și, dacă e cazul, rambursarea.

8. Sistemul realocă produsele returnate sau anulate
Produsele revin în stoc și pot fi comandate de alți clienți.

9. Sistemul gestionează modificările post-livrare
Schimb de produse, actualizare factură, suport post-vânzare.

═══════════════════════════════════════════════════════════════════════════════
EVENT STORMING - DOMAIN EVENTS
═══════════════════════════════════════════════════════════════════════════════

📋 FORMAT: EventName | Trigger | Data | Subsequent Events

═══════════════════════════════════════════════════════════════════════════════
FLUX PRINCIPAL - EVENT CHAIN
═══════════════════════════════════════════════════════════════════════════════

FLUX NORMAL (Happy Path):
OrderPlaced → OrderValidated → StockReserved → OrderConfirmed → 
PaymentRequested → PaymentInitiated → PaymentAuthorized → PaymentCompleted → 
OrderPaid → OrderReadyForProcessing → OrderAssignedToWarehouse → 
OrderPickingStarted → OrderPicked → OrderPackingStarted → OrderPacked → 
ShippingLabelGenerated → OrderReadyForShipment → OrderShipped → 
OrderInTransit → OrderOutForDelivery → OrderDelivered → StockReleased

FLUX ANULARE:
OrderCancellationRequested → OrderCancellationApproved → OrderCancelled → 
StockReleased → RefundInitiated → RefundProcessing → RefundCompleted

FLUX RETURNARE:
ReturnRequested → ReturnApproved → OrderReturnInitiated → ReturnInTransit → 
ReturnReceived → ReturnInspectionStarted → ReturnInspectionCompleted → 
ReturnAccepted → StockRestocked → RefundInitiated → RefundProcessing → 
RefundCompleted

FLUX MODIFICARE:
OrderModificationRequested → OrderModificationApproved → OrderModified → 
OrderRevalidated → (continuă flux normal)


─────────────────────────────────────────────────────────────────────────────
🔵 FLUX PRINCIPAL COMANDĂ (8 evenimente)
─────────────────────────────────────────────────────────────────────────────

1️⃣ OrderPlaced (Comandă Plasată) ⭐⭐⭐
   Trigger: Clientul submitează comanda prin interfața sistemului
   Data: OrderId, CustomerId, CustomerInfo, OrderItems, ShippingAddress, 
         PaymentMethod, TotalAmount, OrderDate
   De ce e important: Punct de intrare în sistem, declanșează întregul proces

2️⃣ OrderValidated (Comandă Validată) ⭐⭐⭐
   Trigger: Sistemul verifică disponibilitatea produselor și validează datele
   Data: OrderId, ValidationDate, ValidatedBy, AvailableStock
   De ce e important: Asigură integritatea datelor și disponibilitatea stocului

3️⃣ OrderRejected (Comandă Respinsă) ⭐⭐
   Trigger: Sistemul detectează erori în validare
   Data: OrderId, RejectionReason, RejectionDate, UnavailableProducts
   De ce e important: Gestionează cazurile de eroare și notifică clientul

4️⃣ StockReserved (Stoc Rezervat) ⭐⭐⭐
   Trigger: Sistemul rezervă produsele pentru comandă după validare
   Data: OrderId, ReservedItems, ReservationDate, ExpirationDate
   De ce e important: Previne vânzarea excesivă (overselling)

5️⃣ OrderConfirmed (Comandă Confirmată) ⭐⭐⭐
   Trigger: Operatorul sau sistemul confirmă comanda
   Data: OrderId, ConfirmationDate, ConfirmedBy, EstimatedDeliveryDate
   De ce e important: Confirmă acceptarea comenzii în procesare

6️⃣ PaymentCompleted (Plată Finalizată) ⭐⭐⭐
   Trigger: Plata este procesată cu succes
   Data: OrderId, PaymentId, TransactionId, PaidAmount, PaymentDate, Receipt
   De ce e important: Confirmă capacitatea financiară pentru procesare

7️⃣ OrderShipped (Comandă Expediată) ⭐⭐⭐
   Trigger: Curierul preia comanda din depozit
   Data: OrderId, AWBNumber, CourierService, ShippedDate, EstimatedDeliveryDate
   De ce e important: Marchează tranziția către faza de livrare

8️⃣ OrderDelivered (Comandă Livrată) ⭐⭐⭐
   Trigger: Clientul primește și acceptă comanda
   Data: OrderId, DeliveredDate, RecipientName, RecipientSignature
   De ce e important: Finalizează cu succes ciclul de viață al comenzii

─────────────────────────────────────────────────────────────────────────────
🔴 FLUX ANULARE (4 evenimente)
─────────────────────────────────────────────────────────────────────────────

9️⃣ OrderCancellationRequested (Anulare Comandă Solicitată) ⭐⭐
   Trigger: Clientul sau operatorul solicită anularea comenzii
   Data: OrderId, RequestedBy, CancellationReason, RequestDate
   De ce e important: Permite clientului să anuleze comenzi nedorite

1️⃣0️⃣ OrderCancelled (Comandă Anulată) ⭐⭐⭐
   Trigger: Comanda este anulată efectiv
   Data: OrderId, CancellationDate, CancelledBy, CancellationReason
   De ce e important: Execută anularea și declanșează eliberarea resurselor

1️⃣1️⃣ RefundInitiated (Rambursare Inițiată) ⭐⭐
   Trigger: Sistemul inițiază procesul de rambursare
   Data: OrderId, RefundId, RefundAmount, RefundReason, InitiatedDate
   De ce e important: Asigură returnarea banilor în caz de anulare

1️⃣2️⃣ RefundCompleted (Rambursare Finalizată) ⭐⭐
   Trigger: Rambursarea este efectuată cu succes
   Data: OrderId, RefundId, RefundedAmount, RefundDate, RefundReceipt
   De ce e important: Confirmă finalizarea procesului financiar

─────────────────────────────────────────────────────────────────────────────
🟣 FLUX RETURNARE (4 evenimente)
─────────────────────────────────────────────────────────────────────────────

1️⃣3️⃣ ReturnRequested (Returnare Solicitată) ⭐⭐⭐
   Trigger: Clientul solicită returnarea produselor după livrare
   Data: OrderId, ReturnRequestId, ReturnedItems, RequestDate, ReturnReason
   De ce e important: Punct de intrare pentru procesul de returnare

1️⃣4️⃣ ReturnApproved (Returnare Aprobată) ⭐⭐
   Trigger: Operatorul aprobă cererea de returnare
   Data: OrderId, ReturnRequestId, ApprovedBy, ApprovedItems, ApprovalDate
   De ce e important: Controlează ce returnări sunt acceptate

1️⃣5️⃣ ReturnReceived (Retur Recepționat) ⭐⭐
   Trigger: Depozitul primește produsele returnate
   Data: OrderId, ReturnId, ReceivedItems, ReceivedDate, ReceivedBy
   De ce e important: Confirmă primirea fizică a produselor

1️⃣6️⃣ ReturnAccepted (Retur Acceptat) ⭐⭐⭐
   Trigger: Toate produsele returnate sunt în stare acceptabilă
   Data: OrderId, ReturnId, AcceptedItems, AcceptedDate, AcceptedBy
   De ce e important: Finalizează procesul de retur și declanșează rambursarea

─────────────────────────────────────────────────────────────────────────────
🟡 FLUX MODIFICARE (2 evenimente)
─────────────────────────────────────────────────────────────────────────────

1️⃣7️⃣ OrderModificationRequested (Modificare Comandă Solicitată) ⭐⭐
   Trigger: Clientul sau operatorul solicită modificarea comenzii
   Data: OrderId, RequestedBy, RequestedChanges, RequestDate, Reason
   De ce e important: Permite flexibilitate în gestionarea comenzilor

1️⃣8️⃣ OrderModified (Comandă Modificată) ⭐⭐
   Trigger: Modificările sunt aplicate în sistem
   Data: OrderId, ModifiedFields, OldValues, NewValues, ModificationDate
   De ce e important: Actualizează comanda conform cerințelor clientului

─────────────────────────────────────────────────────────────────────────────
🟢 EVENIMENTE SISTEM CRITICE (2 evenimente)
─────────────────────────────────────────────────────────────────────────────

1️⃣9️⃣ StockReleased (Stoc Eliberat) ⭐⭐⭐
   Trigger: Stocul rezervat este eliberat (după anulare sau livrare)
   Data: OrderId, ReleasedItems, ReleaseReason, ReleaseDate
   De ce e important: Eliberează produsele pentru alte comenzi

2️⃣0️⃣ CustomerNotified (Client Notificat) ⭐⭐⭐
   Trigger: Sistem trimite notificare către client
   Data: OrderId, NotificationType, NotificationContent, RecipientContact
   De ce e important: Menține clientul informat despre statusul comenzii

═══════════════════════════════════════════════════════════════════════════════
📊 PRIORITIZARE PENTRU IMPLEMENTARE
═══════════════════════════════════════════════════════════════════════════════

🔴 PRIORITATE MAXIMĂ (⭐⭐⭐) - Implementare în Sprint 1:
   1. OrderPlaced
   2. OrderValidated
   3. StockReserved
   4. OrderConfirmed
   5. PaymentCompleted
   6. OrderShipped
   7. OrderDelivered
   8. OrderCancelled
   9. ReturnRequested
   10. ReturnAccepted
   11. StockReleased
   12. CustomerNotified

🟡 PRIORITATE MEDIE (⭐⭐) - Implementare în Sprint 2:
   13. OrderRejected
   14. OrderCancellationRequested
   15. RefundInitiated
   16. RefundCompleted
   17. ReturnApproved
   18. ReturnReceived
   19. OrderModificationRequested
   20. OrderModified

═══════════════════════════════════════════════════════════════════════════════
🎯 BOUNDED CONTEXTS - ORGANIZARE DDD
═══════════════════════════════════════════════════════════════════════════════

Sistemul este structurat în 6 BOUNDED CONTEXTS bazate pe Domain-Driven Design,
fiecare având responsabilități clare și autonomie în gestionarea evenimentelor.

═══════════════════════════════════════════════════════════════════════════════
📦 BOUNDED CONTEXT 1: ORDER MANAGEMENT
═══════════════════════════════════════════════════════════════════════════════

🎯 RESPONSABILITĂȚI:
   - Gestionează ciclul de viață complet al comenzilor
   - Validează și procesează comenzile noi
   - Coordonează modificările și anulările
   - Menține starea și istoricul comenzilor
   - Orchestrează workflow-ul comenzii între contexte

📋 EVENIMENTE PROPRII (7 evenimente):
   1. OrderPlaced ⭐⭐⭐
      → Comandă nouă plasată de client
      → Declanșează: OrderValidated
      
   2. OrderValidated ⭐⭐⭐
      → Comanda a fost validată cu succes
      → Declanșează: StockReserved (INVENTORY context)
      
   3. OrderRejected ⭐⭐
      → Comanda a fost respinsă din cauza validării
      → Declanșează: CustomerNotified
      
   4. OrderConfirmed ⭐⭐⭐
      → Comanda confirmată pentru procesare
      → Declanșează: PaymentCompleted (PAYMENT context)
      
   5. OrderCancellationRequested ⭐⭐
      → Solicitare de anulare primită
      → Declanșează: OrderCancelled
      
   6. OrderCancelled ⭐⭐⭐
      → Comanda a fost anulată
      → Declanșează: StockReleased, RefundInitiated
      
   7. OrderModificationRequested ⭐⭐
      → Solicitare de modificare primită
      → Declanșează: OrderModified
      
   8. OrderModified ⭐⭐
      → Comanda a fost modificată
      → Declanșează: StockReserved (re-validare), PaymentCompleted (ajustare)

🔗 COMENZI PRIMITE (Commands):
   - PlaceOrderCommand (de la Client)
   - ValidateOrderCommand (intern)
   - ConfirmOrderCommand (de la Operator)
   - CancelOrderCommand (de la Client/Operator)
   - ModifyOrderCommand (de la Client/Operator)

📤 EVENIMENTE PUBLICATE (Domain Events):
   - OrderPlaced → către toate contextele (informational)
   - OrderValidated → către INVENTORY
   - OrderConfirmed → către PAYMENT
   - OrderCancelled → către INVENTORY, PAYMENT
   - OrderModified → către INVENTORY, PAYMENT

📥 EVENIMENTE CONSUMATE (din alte contexte):
   - StockReserved (din INVENTORY) → pentru confirmare
   - PaymentCompleted (din PAYMENT) → pentru procesare
   - OrderShipped (din SHIPPING) → pentru tracking
   - OrderDelivered (din SHIPPING) → pentru finalizare

🔄 COMUNICARE CU ALTE CONTEXTE:
   → INVENTORY: Solicită rezervare/eliberare stoc
   → PAYMENT: Solicită procesare plăți/rambursări
   → SHIPPING: Trimite comenzi pentru livrare
   → RETURNS: Primește cereri de returnare
   → NOTIFICATION: Trimite evenimente pentru notificări

═══════════════════════════════════════════════════════════════════════════════
💰 BOUNDED CONTEXT 2: PAYMENT
═══════════════════════════════════════════════════════════════════════════════

🎯 RESPONSABILITĂȚI:
   - Procesează plățile pentru comenzi
   - Gestionează tranzacțiile financiare
   - Execută rambursări pentru anulări/returnări
   - Integrare cu gateway-uri de plată externe
   - Menține istoricul tranzacțiilor

📋 EVENIMENTE PROPRII (3 evenimente):
   5. PaymentCompleted ⭐⭐⭐
      → Plata procesată cu succes
      → Declanșează: OrderShipped (SHIPPING context)
      
   15. RefundInitiated ⭐⭐
      → Rambursare inițiată
      → Declanșează: RefundCompleted
      
   16. RefundCompleted ⭐⭐
      → Rambursare finalizată
      → Declanșează: CustomerNotified

🔗 COMENZI PRIMITE (Commands):
   - ProcessPaymentCommand (de la ORDER MANAGEMENT)
   - InitiateRefundCommand (de la ORDER MANAGEMENT/RETURNS)
   - VerifyPaymentStatusCommand (intern)

📤 EVENIMENTE PUBLICATE (Domain Events):
   - PaymentCompleted → către ORDER MANAGEMENT, SHIPPING
   - RefundCompleted → către ORDER MANAGEMENT, NOTIFICATION

📥 EVENIMENTE CONSUMATE (din alte contexte):
   - OrderConfirmed (din ORDER MANAGEMENT) → pentru procesare plată
   - OrderCancelled (din ORDER MANAGEMENT) → pentru rambursare
   - ReturnAccepted (din RETURNS) → pentru rambursare

🔄 COMUNICARE CU ALTE CONTEXTE:
   → ORDER MANAGEMENT: Confirmă plăți procesate
   → SHIPPING: Declanșează livrare după plată
   → RETURNS: Procesează rambursări pentru returnări
   → NOTIFICATION: Notifică despre statusul plăților

💳 INTEGRĂRI EXTERNE:
   - Payment Gateway (Stripe, PayPal, etc.)
   - Banking APIs
   - Fraud Detection Services

═══════════════════════════════════════════════════════════════════════════════
📦 BOUNDED CONTEXT 3: INVENTORY
═══════════════════════════════════════════════════════════════════════════════

🎯 RESPONSABILITĂȚI:
   - Gestionează stocul de produse
   - Rezervă produse pentru comenzi
   - Eliberează stoc pentru comenzi anulate/livrate
   - Reintroduce produse returnate în stoc
   - Monitorizează disponibilitatea produselor

📋 EVENIMENTE PROPRII (2 evenimente):
   3. StockReserved ⭐⭐⭐
      → Stoc rezervat pentru comandă
      → Declanșează: OrderConfirmed (ORDER MANAGEMENT)
      
   11. StockReleased ⭐⭐⭐
      → Stoc eliberat (livrare/anulare/returnare)
      → Declanșează: StockAvailabilityUpdated (intern)

🔗 COMENZI PRIMITE (Commands):
   - ReserveStockCommand (de la ORDER MANAGEMENT)
   - ReleaseStockCommand (de la ORDER MANAGEMENT/SHIPPING/RETURNS)
   - RestockReturnedItemsCommand (de la RETURNS)
   - CheckStockAvailabilityCommand (de la ORDER MANAGEMENT)

📤 EVENIMENTE PUBLICATE (Domain Events):
   - StockReserved → către ORDER MANAGEMENT
   - StockReleased → către ORDER MANAGEMENT, NOTIFICATION
   - StockAvailabilityUpdated → către CATALOG (dacă există)

📥 EVENIMENTE CONSUMATE (din alte contexte):
   - OrderValidated (din ORDER MANAGEMENT) → pentru rezervare
   - OrderCancelled (din ORDER MANAGEMENT) → pentru eliberare
   - OrderDelivered (din SHIPPING) → pentru eliberare definitivă
   - ReturnAccepted (din RETURNS) → pentru reintroducere în stoc

🔄 COMUNICARE CU ALTE CONTEXTE:
   → ORDER MANAGEMENT: Confirmă rezervări de stoc
   → SHIPPING: Primește confirmări de livrare
   → RETURNS: Primește produse returnate
   → NOTIFICATION: Alertează pentru stoc scăzut

📊 AGREGAT PRINCIPAL:
   - Product (ProductId, Name, SKU, Quantity, ReservedQuantity)
   - StockReservation (ReservationId, OrderId, ProductId, Quantity, ExpirationDate)

═══════════════════════════════════════════════════════════════════════════════
🚚 BOUNDED CONTEXT 4: SHIPPING & DELIVERY
═══════════════════════════════════════════════════════════════════════════════

🎯 RESPONSABILITĂȚI:
   - Gestionează procesul de expediere
   - Coordonează cu serviciile de curierat
   - Urmărește statusul livrărilor
   - Confirmă livrările reușite
   - Gestionează încercările de livrare eșuate

📋 EVENIMENTE PROPRII (2 evenimente):
   6. OrderShipped ⭐⭐⭐
      → Comandă expediată către client
      → Declanșează: OrderDelivered
      
   7. OrderDelivered ⭐⭐⭐
      → Comandă livrată cu succes
      → Declanșează: StockReleased (INVENTORY)

🔗 COMENZI PRIMITE (Commands):
   - ShipOrderCommand (de la PAYMENT/ORDER MANAGEMENT)
   - ConfirmDeliveryCommand (de la Curier)
   - RescheduleDeliveryCommand (de la Client/Curier)
   - GenerateShippingLabelCommand (intern)

📤 EVENIMENTE PUBLICATE (Domain Events):
   - OrderShipped → către ORDER MANAGEMENT, NOTIFICATION
   - OrderDelivered → către ORDER MANAGEMENT, INVENTORY, NOTIFICATION
   - DeliveryAttemptFailed → către ORDER MANAGEMENT (dacă există)

📥 EVENIMENTE CONSUMATE (din alte contexte):
   - PaymentCompleted (din PAYMENT) → pentru expediere
   - OrderCancelled (din ORDER MANAGEMENT) → pentru oprire expediere

🔄 COMUNICARE CU ALTE CONTEXTE:
   → ORDER MANAGEMENT: Confirmă expedieri și livrări
   → INVENTORY: Confirmă livrarea pentru eliberare stoc
   → PAYMENT: Primește confirmări de plată
   → NOTIFICATION: Trimite update-uri de tracking
   → RETURNS: Primește cereri de retur după livrare

🚛 INTEGRĂRI EXTERNE:
   - Courier APIs (FAN Courier, DHL, etc.)
   - Address Validation Services
   - GPS Tracking Systems

📦 AGREGAT PRINCIPAL:
   - Shipment (ShipmentId, OrderId, AWBNumber, CourierService, Status)
   - DeliveryTracking (TrackingEvents, Location, EstimatedDelivery)

═══════════════════════════════════════════════════════════════════════════════
🔄 BOUNDED CONTEXT 5: RETURNS
═══════════════════════════════════════════════════════════════════════════════

🎯 RESPONSABILITĂȚI:
   - Procesează cereri de returnare
   - Aprobă/respinge returnări
   - Primește și inspectează produse returnate
   - Declanșează rambursări pentru returnări acceptate
   - Coordonează cu INVENTORY pentru reintroducere în stoc

📋 EVENIMENTE PROPRII (4 evenimente):
   9. ReturnRequested ⭐⭐⭐
      → Cerere de returnare primită
      → Declanșează: ReturnApproved
      
   17. ReturnApproved ⭐⭐
      → Returnare aprobată
      → Declanșează: ReturnReceived
      
   18. ReturnReceived ⭐⭐
      → Produse returnate primite în depozit
      → Declanșează: ReturnAccepted
      
   10. ReturnAccepted ⭐⭐⭐
      → Returnare acceptată după inspecție
      → Declanșează: StockReleased (INVENTORY), RefundInitiated (PAYMENT)

🔗 COMENZI PRIMITE (Commands):
   - RequestReturnCommand (de la Client)
   - ApproveReturnCommand (de la Operator)
   - ReceiveReturnCommand (de la Warehouse)
   - InspectReturnCommand (de la Warehouse)
   - RejectReturnCommand (de la Operator)

📤 EVENIMENTE PUBLICATE (Domain Events):
   - ReturnRequested → către ORDER MANAGEMENT, NOTIFICATION
   - ReturnApproved → către NOTIFICATION
   - ReturnAccepted → către INVENTORY, PAYMENT, NOTIFICATION

📥 EVENIMENTE CONSUMATE (din alte contexte):
   - OrderDelivered (din SHIPPING) → activează eligibilitatea pentru retur
   - OrderCancelled (din ORDER MANAGEMENT) → anulează cereri de retur

🔄 COMUNICARE CU ALTE CONTEXTE:
   → ORDER MANAGEMENT: Raportează returnări procesate
   → PAYMENT: Solicită rambursări
   → INVENTORY: Reintroduce produse în stoc
   → SHIPPING: Coordonează transportul produselor returnate
   → NOTIFICATION: Notifică clienții despre statusul returului

📋 AGREGAT PRINCIPAL:
   - Return (ReturnId, OrderId, ReturnedItems, Reason, Status)
   - ReturnItem (ProductId, Quantity, Condition, InspectionResult)

═══════════════════════════════════════════════════════════════════════════════
📢 BOUNDED CONTEXT 6: NOTIFICATION
═══════════════════════════════════════════════════════════════════════════════

🎯 RESPONSABILITĂȚI:
   - Trimite notificări către clienți
   - Gestionează preferințele de notificare
   - Suportă multiple canale (email, SMS, push)
   - Menține istoricul notificărilor
   - Retry logic pentru notificări eșuate

📋 EVENIMENTE PROPRII (1 eveniment):
   12. CustomerNotified ⭐⭐⭐
      → Client notificat despre eveniment
      → End event (nu declanșează alte evenimente)

🔗 COMENZI PRIMITE (Commands):
   - SendNotificationCommand (de la toate contextele)
   - UpdateNotificationPreferencesCommand (de la Client)
   - ResendNotificationCommand (manual)

📤 EVENIMENTE PUBLICATE (Domain Events):
   - CustomerNotified → către ORDER MANAGEMENT (pentru tracking)
   - NotificationFailed → către ADMIN (dacă există)

📥 EVENIMENTE CONSUMATE (din TOATE contextele):
   - OrderPlaced, OrderValidated, OrderRejected
   - OrderConfirmed, OrderCancelled, OrderModified
   - PaymentCompleted, RefundCompleted
   - StockReleased (low stock alerts)
   - OrderShipped, OrderDelivered
   - ReturnRequested, ReturnApproved, ReturnAccepted

🔄 COMUNICARE CU ALTE CONTEXTE:
   → TOATE CONTEXTELE: Primește evenimente pentru notificare
   → ORDER MANAGEMENT: Confirmă livrarea notificărilor

📧 INTEGRĂRI EXTERNE:
   - Email Service (SendGrid, AWS SES)
   - SMS Gateway (Twilio)
   - Push Notification Services (Firebase)

📨 TIPURI DE NOTIFICĂRI:
   - Order Confirmation
   - Payment Confirmation
   - Shipping Notification
   - Delivery Confirmation
   - Cancellation Notice
   - Refund Confirmation
   - Return Status Updates

═══════════════════════════════════════════════════════════════════════════════
🔗 CONTEXT MAP - RELAȚII ÎNTRE BOUNDED CONTEXTS
═══════════════════════════════════════════════════════════════════════════════

┌─────────────────────────────────────────────────────────────────────────┐
│                        ORDER MANAGEMENT (CORE)                          │
│                              [Orchestrator]                             │
└──────────┬────────────┬────────────┬───────────┬────────────┬───────────┘
           │            │            │           │            │
           ▼            ▼            ▼           ▼            ▼
    ┌──────────┐  ┌──────────┐  ┌──────────┐ ┌─────────┐ ┌──────────────┐
    │ PAYMENT  │  │INVENTORY │  │ SHIPPING │ │ RETURNS │ │ NOTIFICATION │
    │[Partner] │  │[Partner] │  │[Customer]│ │[Partner]│ │  [Supplier]  │
    └──────────┘  └──────────┘  └──────────┘ └─────────┘ └──────────────┘

TIPURI DE RELAȚII:

1. ORDER MANAGEMENT ←→ INVENTORY [Partnership]
   - Bi-directional communication
   - Shared responsibility pentru stoc
   - Events: OrderValidated ↔ StockReserved

2. ORDER MANAGEMENT ←→ PAYMENT [Partnership]
   - Bi-directional communication
   - Shared responsibility pentru tranzacții
   - Events: OrderConfirmed ↔ PaymentCompleted

3. ORDER MANAGEMENT → SHIPPING [Customer-Supplier]
   - ORDER MANAGEMENT = Customer (consumă servicii)
   - SHIPPING = Supplier (furnizează servicii livrare)
   - Events: PaymentCompleted → OrderShipped

4. ORDER MANAGEMENT ←→ RETURNS [Partnership]
   - Bi-directional communication
   - Shared responsibility pentru returnări
   - Events: OrderDelivered ↔ ReturnRequested

5. ALL CONTEXTS → NOTIFICATION [Published Language]
   - Notification = Shared Kernel pentru comunicare
   - Toate contextele publică evenimente
   - One-way communication

═══════════════════════════════════════════════════════════════════════════════
📊 MATRICEA EVENIMENTELOR PE CONTEXTE
═══════════════════════════════════════════════════════════════════════════════

┌────────────────────────┬─────┬─────┬─────┬─────┬─────┬─────┐
│ EVENT                  │ OM  │ PAY │ INV │ SHP │ RET │ NOT │
├────────────────────────┼─────┼─────┼─────┼─────┼─────┼─────┤
│ 1. OrderPlaced         │  P  │     │     │     │     │  C  │
│ 2. OrderValidated      │  P  │     │  T  │     │     │  C  │
│ 3. StockReserved       │  C  │     │  P  │     │     │  C  │
│ 4. OrderConfirmed      │  P  │  T  │     │     │     │  C  │
│ 5. PaymentCompleted    │  C  │  P  │     │  T  │     │  C  │
│ 6. OrderShipped        │  C  │     │     │  P  │     │  C  │
│ 7. OrderDelivered      │  C  │     │  T  │  P  │  T  │  C  │
│ 8. OrderCancelled      │  P  │  T  │  T  │  T  │     │  C  │
│ 9. ReturnRequested     │  C  │     │     │     │  P  │  C  │
│10. ReturnAccepted      │  C  │  T  │  T  │     │  P  │  C  │
│11. StockReleased       │  C  │     │  P  │     │     │  C  │
│12. CustomerNotified    │  C  │     │     │     │     │  P  │
│13. OrderRejected       │  P  │     │     │     │     │  C  │
│14. CancellationReq.    │  P  │     │     │     │     │  C  │
│15. RefundInitiated     │  C  │  P  │     │     │  C  │  C  │
│16. RefundCompleted     │  C  │  P  │     │     │     │  C  │
│17. ReturnApproved      │  C  │     │     │     │  P  │  C  │
│18. ReturnReceived      │  C  │     │     │     │  P  │  C  │
│19. ModificationReq.    │  P  │     │     │     │     │  C  │
│20. OrderModified       │  P  │  T  │  T  │     │     │  C  │
└────────────────────────┴─────┴─────┴─────┴─────┴─────┴─────┘

LEGENDĂ:
P = Produce (contextul generează evenimentul)
C = Consume (contextul ascultă evenimentul)
T = Trigger (evenimentul declanșează o acțiune în context)

OM  = ORDER MANAGEMENT
PAY = PAYMENT
INV = INVENTORY
SHP = SHIPPING
RET = RETURNS
NOT = NOTIFICATION

═══════════════════════════════════════════════════════════════════════════════
🎯 FLUX DE EVENIMENTE ÎNTRE CONTEXTE - HAPPY PATH
═══════════════════════════════════════════════════════════════════════════════

[CLIENT] → PlaceOrderCommand
           ↓
    ┌──────────────────┐
    │ ORDER MANAGEMENT │ ① OrderPlaced
    └────────┬─────────┘
             │ ValidateOrderCommand
             ▼
    ┌──────────────────┐
    │ ORDER MANAGEMENT │ ② OrderValidated
    └────────┬─────────┘
             │ Domain Event
             ▼
    ┌──────────────────┐
    │    INVENTORY     │ ③ StockReserved
    └────────┬─────────┘
             │ Domain Event
             ▼
    ┌──────────────────┐
    │ ORDER MANAGEMENT │ ④ OrderConfirmed
    └────────┬─────────┘
             │ ProcessPaymentCommand
             ▼
    ┌──────────────────┐
    │     PAYMENT      │ ⑤ PaymentCompleted
    └────────┬─────────┘
             │ Domain Event
             ▼
    ┌──────────────────┐
    │     SHIPPING     │ ⑥ OrderShipped
    └────────┬─────────┘
             │ ConfirmDeliveryCommand
             ▼
    ┌──────────────────┐
    │     SHIPPING     │ ⑦ OrderDelivered
    └────────┬─────────┘
             │ Domain Event
             ▼
    ┌──────────────────┐
    │    INVENTORY     │ ⑧ StockReleased
    └──────────────────┘
             │
             ▼
    ┌──────────────────┐
    │   NOTIFICATION   │ ⑨ CustomerNotified (la fiecare pas)
    └──────────────────┘

═══════════════════════════════════════════════════════════════════════════════
💡 RECOMANDĂRI DE IMPLEMENTARE
═══════════════════════════════════════════════════════════════════════════════

1️⃣ COMUNICARE ÎNTRE CONTEXTE:
   ✅ Folosește Domain Events pentru comunicare asincronă
   ✅ Implementează Event Bus (RabbitMQ, Azure Service Bus, Kafka)
   ✅ Fiecare context își publică propriile evenimente
   ✅ Contextele se abonează la evenimentele relevante

2️⃣ CONSISTENȚĂ DATE:
   ✅ Eventual Consistency între contexte
   ✅ Strong Consistency în interiorul contextului
   ✅ Saga Pattern pentru tranzacții distribuite
   ✅ Compensation logic pentru rollback

3️⃣ AUTONOMIE CONTEXTE:
   ✅ Fiecare context are propria bază de date
   ✅ Nu există shared database între contexte
   ✅ Fiecare context își definește propriile modele
   ✅ Anti-Corruption Layer pentru integrări

4️⃣ PRIORITIZARE IMPLEMENTARE:
   Sprint 1: ORDER MANAGEMENT + INVENTORY + NOTIFICATION
   Sprint 2: PAYMENT + SHIPPING
   Sprint 3: RETURNS + optimizări

5️⃣ TEHNOLOGII RECOMANDATE:
   - .NET 9 pentru fiecare microserviciu
   - MediatR pentru comenzi și evenimente interne
   - RabbitMQ/Azure Service Bus pentru evenimente externe
   - Entity Framework Core pentru persistență
   - Redis pentru caching
   - Serilog pentru logging centralizat
