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

🔵 PRIORITATE SCĂZUTĂ (⭐) - Implementare în Sprint 3:
