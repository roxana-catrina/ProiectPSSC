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

─────────────────────────────────────────────────────────────────────────────
🟧 WORKFLOW: PRELUARE COMANDĂ
─────────────────────────────────────────────────────────────────────────────

1️⃣ OrderPlaced (Comandă Plasată)
   Trigger: Clientul submitează comanda prin interfața sistemului
   Data: 
   - OrderId
   - CustomerId
   - CustomerInfo (nume, email, telefon)
   - OrderItems (productId, productName, quantity, price)
   - ShippingAddress (strada, oraș, județ, cod poștal)
   - PaymentMethod
   - TotalAmount
   - OrderDate
   Subsequent Events: 
   - OrderValidated (dacă validarea reușește)
   - OrderRejected (dacă validarea eșuează)

2️⃣ OrderValidated (Comandă Validată)
   Trigger: Sistemul verifică disponibilitatea produselor și validează datele
   Data:
   - OrderId
   - ValidationDate
   - ValidatedBy (System)
   - AvailableStock (pentru fiecare produs)
   Subsequent Events:
   - StockReserved
   - OrderConfirmationSent

3️⃣ OrderRejected (Comandă Respinsă)
   Trigger: Sistemul detectează erori în validare (stoc insuficient, date invalide)
   Data:
   - OrderId
   - RejectionReason
   - RejectionDate
   - UnavailableProducts
   Subsequent Events:
   - CustomerNotified

4️⃣ StockReserved (Stoc Rezervat)
   Trigger: Sistemul rezervă produsele pentru comandă după validare
   Data:
   - OrderId
   - ReservedItems (productId, quantity, warehouseId)
   - ReservationDate
   - ExpirationDate
   Subsequent Events:
   - OrderConfirmed

5️⃣ OrderConfirmed (Comandă Confirmată)
   Trigger: Operatorul sau sistemul confirmă comanda
   Data:
   - OrderId
   - ConfirmationDate
   - ConfirmedBy (operatorId sau System)
   - EstimatedDeliveryDate
   Subsequent Events:
   - PaymentRequested
   - CustomerNotified

─────────────────────────────────────────────────────────────────────────────
🟦 WORKFLOW: PLATĂ COMANDĂ
─────────────────────────────────────────────────────────────────────────────

6️⃣ PaymentRequested (Plată Solicitată)
   Trigger: Sistemul solicită plata de la client
   Data:
   - OrderId
   - Amount
   - PaymentMethod
   - PaymentDueDate
   - PaymentLink/Reference
   Subsequent Events:
   - PaymentInitiated

7️⃣ PaymentInitiated (Plată Inițiată)
   Trigger: Clientul începe procesul de plată
   Data:
   - OrderId
   - PaymentId
   - PaymentMethod
   - InitiatedDate
   Subsequent Events:
   - PaymentAuthorized
   - PaymentFailed

8️⃣ PaymentAuthorized (Plată Autorizată)
   Trigger: Sistemul de plăți autorizează tranzacția
   Data:
   - OrderId
   - PaymentId
   - TransactionId
   - AuthorizationCode
   - AuthorizedAmount
   - AuthorizedDate
   Subsequent Events:
   - PaymentCompleted

9️⃣ PaymentCompleted (Plată Finalizată)
   Trigger: Plata este procesată cu succes
   Data:
   - OrderId
   - PaymentId
   - TransactionId
   - PaidAmount
   - PaymentDate
   - Receipt
   Subsequent Events:
   - OrderPaid
   - CustomerNotified

🔟 PaymentFailed (Plată Eșuată)
   Trigger: Sistemul de plăți respinge tranzacția
   Data:
   - OrderId
   - PaymentId
   - FailureReason
   - FailureDate
   - RetryAttempt
   Subsequent Events:
   - PaymentRetryRequested (dacă se permite retry)
   - OrderCancelled (dacă nu se poate finaliza plata)

1️⃣1️⃣ OrderPaid (Comandă Plătită)
   Trigger: Confirmarea finalizării plății
   Data:
   - OrderId
   - PaymentId
   - PaidAmount
   - PaidDate
   Subsequent Events:
   - OrderReadyForProcessing

─────────────────────────────────────────────────────────────────────────────
🟩 WORKFLOW: PROCESARE COMANDĂ
─────────────────────────────────────────────────────────────────────────────

1️⃣2️⃣ OrderReadyForProcessing (Comandă Pregătită pentru Procesare)
   Trigger: Comanda este plătită și validată
   Data:
   - OrderId
   - ProcessingPriority
   - ReadyDate
   Subsequent Events:
   - OrderAssignedToWarehouse

1️⃣3️⃣ OrderAssignedToWarehouse (Comandă Alocată Depozitului)
   Trigger: Sistemul alocă comanda unui depozit
   Data:
   - OrderId
   - WarehouseId
   - AssignedDate
   - AssignedBy
   Subsequent Events:
   - OrderPickingStarted

1️⃣4️⃣ OrderPickingStarted (Pregătire Comandă Inițiată)
   Trigger: Depozitul începe să pregătească produsele
   Data:
   - OrderId
   - WarehouseId
   - PickerId
   - StartDate
   Subsequent Events:
   - OrderPicked

1️⃣5️⃣ OrderPicked (Produse Colectate)
   Trigger: Produsele au fost colectate din depozit
   Data:
   - OrderId
   - PickedItems (productId, quantity, pickerId)
   - PickedDate
   Subsequent Events:
   - OrderPackingStarted

1️⃣6️⃣ OrderPackingStarted (Ambalare Inițiată)
   Trigger: Procesul de ambalare începe
   Data:
   - OrderId
   - PackerId
   - PackingStartDate
   Subsequent Events:
   - OrderPacked

1️⃣7️⃣ OrderPacked (Comandă Ambalată)
   Trigger: Comanda a fost ambalată complet
   Data:
   - OrderId
   - PackageDetails (weight, dimensions, packageId)
   - PackedBy
   - PackedDate
   Subsequent Events:
   - ShippingLabelGenerated

1️⃣8️⃣ ShippingLabelGenerated (AWB Generat)
   Trigger: Sistemul generează AWB pentru curier
   Data:
   - OrderId
   - AWBNumber
   - CourierService
   - GeneratedDate
   - TrackingUrl
   Subsequent Events:
   - OrderReadyForShipment

1️⃣9️⃣ OrderReadyForShipment (Comandă Pregătită pentru Livrare)
   Trigger: Comanda este pregătită să fie preluată de curier
   Data:
   - OrderId
   - AWBNumber
   - ReadyDate
   - WarehouseLocation
   Subsequent Events:
   - OrderShipped
   - CustomerNotified

2️⃣0️⃣ OrderShipped (Comandă Expediată)
   Trigger: Curierul preia comanda din depozit
   Data:
   - OrderId
   - AWBNumber
   - CourierService
   - CourierId
   - ShippedDate
   - EstimatedDeliveryDate
   Subsequent Events:
   - OrderInTransit
   - CustomerNotified

2️⃣1️⃣ OrderInTransit (Comandă în Tranzit)
   Trigger: Comanda este în curs de livrare
   Data:
   - OrderId
   - AWBNumber
   - CurrentLocation
   - LastUpdateDate
   - EstimatedDeliveryDate
   Subsequent Events:
   - OrderOutForDelivery

2️⃣2️⃣ OrderOutForDelivery (Comandă în Curs de Livrare)
   Trigger: Curierul este în drum spre adresa de livrare
   Data:
   - OrderId
   - AWBNumber
   - CourierId
   - OutForDeliveryDate
   - EstimatedArrivalTime
   Subsequent Events:
   - OrderDelivered
   - DeliveryAttemptFailed

2️⃣3️⃣ OrderDelivered (Comandă Livrată)
   Trigger: Clientul primește și acceptă comanda
   Data:
   - OrderId
   - AWBNumber
   - DeliveredDate
   - RecipientName
   - RecipientSignature
   - DeliveredBy (courierId)
   Subsequent Events:
   - StockReleased
   - CustomerNotified

2️⃣4️⃣ DeliveryAttemptFailed (Tentativă de Livrare Eșuată)
   Trigger: Curierul nu poate livra comanda
   Data:
   - OrderId
   - AWBNumber
   - FailureReason
   - AttemptDate
   - NextAttemptDate
   Subsequent Events:
   - DeliveryRescheduled
   - OrderReturnedToSender (după X tentative)

─────────────────────────────────────────────────────────────────────────────
🟨 WORKFLOW: MODIFICARE COMANDĂ
─────────────────────────────────────────────────────────────────────────────

2️⃣5️⃣ OrderModificationRequested (Modificare Comandă Solicitată)
   Trigger: Clientul sau operatorul solicită modificarea comenzii
   Data:
   - OrderId
   - RequestedBy (customerId sau operatorId)
   - RequestedChanges (products, quantities, address, payment method)
   - RequestDate
   - Reason
   Subsequent Events:
   - OrderModificationApproved
   - OrderModificationRejected

2️⃣6️⃣ OrderModificationApproved (Modificare Aprobată)
   Trigger: Sistemul sau operatorul aprobă modificarea
   Data:
   - OrderId
   - ApprovedBy
   - ApprovedChanges
   - ApprovalDate
   Subsequent Events:
   - OrderModified

2️⃣7️⃣ OrderModificationRejected (Modificare Respinsă)
   Trigger: Modificarea nu poate fi efectuată (ex: comanda deja expediată)
   Data:
   - OrderId
   - RejectionReason
   - RejectedBy
   - RejectionDate
   Subsequent Events:
   - CustomerNotified

2️⃣8️⃣ OrderModified (Comandă Modificată)
   Trigger: Modificările sunt aplicate în sistem
   Data:
   - OrderId
   - ModifiedFields
   - OldValues
   - NewValues
   - ModifiedBy
   - ModificationDate
   Subsequent Events:
   - StockAdjusted (dacă s-au modificat produse)
   - PaymentAdjusted (dacă s-a modificat valoarea)
   - OrderRevalidated

─────────────────────────────────────────────────────────────────────────────
🟥 WORKFLOW: ANULARE COMANDĂ
─────────────────────────────────────────────────────────────────────────────

2️⃣9️⃣ OrderCancellationRequested (Anulare Comandă Solicitată)
   Trigger: Clientul sau operatorul solicită anularea comenzii
   Data:
   - OrderId
   - RequestedBy (customerId sau operatorId)
   - CancellationReason
   - RequestDate
   Subsequent Events:
   - OrderCancellationApproved
   - OrderCancellationRejected

3️⃣0️⃣ OrderCancellationApproved (Anulare Aprobată)
   Trigger: Sistemul verifică că anularea este posibilă
   Data:
   - OrderId
   - ApprovedBy
   - ApprovalDate
   - CancellationReason
   Subsequent Events:
   - OrderCancelled

3️⃣1️⃣ OrderCancellationRejected (Anulare Respinsă)
   Trigger: Anularea nu este permisă (ex: comandă deja expediată)
   Data:
   - OrderId
   - RejectionReason
   - RejectedBy
   - RejectionDate
   Subsequent Events:
   - CustomerNotified

3️⃣2️⃣ OrderCancelled (Comandă Anulată)
   Trigger: Comanda este anulată efectiv
   Data:
   - OrderId
   - CancellationDate
   - CancelledBy
   - CancellationReason
   Subsequent Events:
   - StockReleased
   - RefundInitiated (dacă s-a plătit)
   - CustomerNotified

─────────────────────────────────────────────────────────────────────────────
🟪 WORKFLOW: RETURNARE COMANDĂ
─────────────────────────────────────────────────────────────────────────────

3️⃣3️⃣ OrderRefusedAtDelivery (Comandă Refuzată la Livrare)
   Trigger: Clientul refuză să primească comanda de la curier
   Data:
   - OrderId
   - AWBNumber
   - RefusalDate
   - RefusalReason
   - CourierId
   Subsequent Events:
   - OrderReturnInitiated

3️⃣4️⃣ ReturnRequested (Returnare Solicitată)
   Trigger: Clientul solicită returnarea produselor după livrare
   Data:
   - OrderId
   - ReturnRequestId
   - ReturnedItems (productId, quantity, reason)
   - RequestDate
   - ReturnReason
   Subsequent Events:
   - ReturnApproved
   - ReturnRejected

3️⃣5️⃣ ReturnApproved (Returnare Aprobată)
   Trigger: Operatorul aprobă cererea de returnare
   Data:
   - OrderId
   - ReturnRequestId
   - ApprovedBy
   - ApprovedItems
   - ApprovalDate
   - ReturnInstructions
   Subsequent Events:
   - ReturnShippingLabelGenerated
   - CustomerNotified

3️⃣6️⃣ ReturnRejected (Returnare Respinsă)
   Trigger: Cererea de returnare este respinsă
   Data:
   - OrderId
   - ReturnRequestId
   - RejectionReason
   - RejectedBy
   - RejectionDate
   Subsequent Events:
   - CustomerNotified

3️⃣7️⃣ OrderReturnInitiated (Retur Inițiat)
   Trigger: Procesul de returnare începe
   Data:
   - OrderId
   - ReturnId
   - ReturnedItems
   - InitiatedDate
   - ReturnMethod
   Subsequent Events:
   - ReturnInTransit

3️⃣8️⃣ ReturnInTransit (Retur în Tranzit)
   Trigger: Produsele returnate sunt în curs de transport
   Data:
   - OrderId
   - ReturnId
   - ReturnAWB
   - CurrentLocation
   - EstimatedArrivalDate
   Subsequent Events:
   - ReturnReceived

3️⃣9️⃣ ReturnReceived (Retur Recepționat)
   Trigger: Depozitul primește produsele returnate
   Data:
   - OrderId
   - ReturnId
   - ReceivedItems
   - ReceivedDate
   - ReceivedBy
   - WarehouseId
   Subsequent Events:
   - ReturnInspectionStarted

4️⃣0️⃣ ReturnInspectionStarted (Inspecție Retur Inițiată)
   Trigger: Depozitul verifică starea produselor returnate
   Data:
   - OrderId
   - ReturnId
   - InspectorId
   - InspectionStartDate
   Subsequent Events:
   - ReturnInspectionCompleted

4️⃣1️⃣ ReturnInspectionCompleted (Inspecție Retur Finalizată)
   Trigger: Verificarea produselor returnate este completă
   Data:
   - OrderId
   - ReturnId
   - InspectionResults (per item: condition, acceptability)
   - InspectedBy
   - InspectionDate
   Subsequent Events:
   - ReturnAccepted
   - ReturnPartiallyAccepted
   - ReturnRejectedAfterInspection

4️⃣2️⃣ ReturnAccepted (Retur Acceptat)
   Trigger: Toate produsele returnate sunt în stare acceptabilă
   Data:
   - OrderId
   - ReturnId
   - AcceptedItems
   - AcceptedDate
   - AcceptedBy
   Subsequent Events:
   - StockRestocked
   - RefundInitiated
   - CustomerNotified

4️⃣3️⃣ ReturnPartiallyAccepted (Retur Acceptat Parțial)
   Trigger: Doar o parte din produsele returnate sunt acceptate
   Data:
   - OrderId
   - ReturnId
   - AcceptedItems
   - RejectedItems (cu motiv)
   - PartialAcceptanceDate
   Subsequent Events:
   - StockRestocked (pentru items acceptate)
   - PartialRefundInitiated
   - CustomerNotified

4️⃣4️⃣ ReturnRejectedAfterInspection (Retur Respins după Inspecție)
   Trigger: Produsele nu sunt în stare acceptabilă pentru retur
   Data:
   - OrderId
   - ReturnId
   - RejectionReason
   - RejectedDate
   Subsequent Events:
   - ProductsReturnedToCustomer
   - CustomerNotified

─────────────────────────────────────────────────────────────────────────────
🟫 WORKFLOW: RAMBURSĂRI
─────────────────────────────────────────────────────────────────────────────

4️⃣5️⃣ RefundInitiated (Rambursare Inițiată)
   Trigger: Sistemul inițiază procesul de rambursare
   Data:
   - OrderId
   - RefundId
   - RefundAmount
   - RefundReason (cancellation, return, etc.)
   - InitiatedDate
   - RefundMethod
   Subsequent Events:
   - RefundProcessing

4️⃣6️⃣ RefundProcessing (Rambursare în Procesare)
   Trigger: Sistemul de plăți procesează rambursarea
   Data:
   - OrderId
   - RefundId
   - TransactionId
   - ProcessingDate
   Subsequent Events:
   - RefundCompleted
   - RefundFailed

4️⃣7️⃣ RefundCompleted (Rambursare Finalizată)
   Trigger: Rambursarea este efectuată cu succes
   Data:
   - OrderId
   - RefundId
   - RefundedAmount
   - RefundDate
   - RefundReceipt
   Subsequent Events:
   - CustomerNotified

4️⃣8️⃣ RefundFailed (Rambursare Eșuată)
   Trigger: Rambursarea nu poate fi procesată
   Data:
   - OrderId
   - RefundId
   - FailureReason
   - FailureDate
   Subsequent Events:
   - RefundRetryScheduled
   - ManualInterventionRequired

─────────────────────────────────────────────────────────────────────────────
⚙️ EVENIMENTE SISTEM & NOTIFICĂRI
─────────────────────────────────────────────────────────────────────────────

4️⃣9️⃣ StockReleased (Stoc Eliberat)
   Trigger: Stocul rezervat este eliberat (după anulare sau livrare)
   Data:
   - OrderId
   - ReleasedItems (productId, quantity)
   - ReleaseReason
   - ReleaseDate
   Subsequent Events:
   - StockAvailabilityUpdated

5️⃣0️⃣ StockRestocked (Stoc Realocat)
   Trigger: Produsele returnate/anulate revin în stoc
   Data:
   - OrderId
   - ReturnId (dacă aplicabil)
   - RestockedItems (productId, quantity, condition)
   - RestockDate
   - WarehouseId
   Subsequent Events:
   - StockAvailabilityUpdated

5️⃣1️⃣ StockAvailabilityUpdated (Disponibilitate Stoc Actualizată)
   Trigger: Modificări în stocul disponibil
   Data:
   - ProductId
   - OldQuantity
   - NewQuantity
   - UpdateReason
   - UpdateDate
   Subsequent Events:
   - ProductCatalogUpdated

5️⃣2️⃣ CustomerNotified (Client Notificat)
   Trigger: Sistem trimite notificare către client
   Data:
   - OrderId
   - NotificationType (email, SMS, push)
   - NotificationContent
   - RecipientContact
   - SentDate
   Subsequent Events:
   - None (end event)

5️⃣3️⃣ OperatorNotified (Operator Notificat)
   Trigger: Sistem alertează operatorul pentru acțiune necesară
   Data:
   - OrderId
   - OperatorId
   - NotificationType
   - AlertLevel (info, warning, urgent)
   - NotificationContent
   - SentDate
   Subsequent Events:
   - None (poate declanșa acțiuni manuale)

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
