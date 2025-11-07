# 📧 NOTIFICATION MANAGEMENT - QUICK START GUIDE

## 📋 Bounded Context: NOTIFICATIONS

### 🎯 Scop
Gestionează toate notificările către clienți prin multiple canale (Email, SMS, Push, In-App).

---

## 1️⃣ COMENZI ȘI EVENIMENTE - Mapare Completă

### Comandă → Eveniment Principal

| Comandă | Eveniment | Descriere | Exemplu Use Case |
|---------|-----------|-----------|------------------|
| `SendNotificationCommand` | `CustomerNotified` | Trimite notificare imediată | "Comanda ta a fost plasată" |
| `ScheduleNotificationCommand` | `NotificationScheduled` | Programează notificare | "Reminder: comanda livrată mâine" |
| `ResendNotificationCommand` | `CustomerNotified` | Retrimite notificare | Retry după eșec |
| `CancelNotificationCommand` | `NotificationCancelled` | Anulează notificare programată | Anulare reminder |
| `MarkAsReadCommand` | `NotificationRead` | Marchează ca citită | User deschide in-app notification |

### Evenimente BONUS (Tracking)

| Eveniment | Când Se Declanșează | Analytics Use |
|-----------|---------------------|---------------|
| `NotificationDelivered` | Provider confirmă livrare | Delivery rate tracking |
| `NotificationOpened` | User deschide email | Open rate (15-25%) |
| `NotificationClicked` | User dă click pe link | Click-through rate (2-5%) |
| `NotificationBounced` | Email bounce | Email validation, blacklist |
| `NotificationFailed` | Trimitere eșuează | Retry logic, alerting |

---

## 2️⃣ AGREGĂRI

### Agregatul Principal: **Notification**

**Responsabilități:**
- ✅ Gestionează ciclul de viață: Created → Sent → Delivered
- ✅ Validează destinatari (email, telefon, device token)
- ✅ Retry logic cu exponential backoff (5min → 15min → 1h)
- ✅ Tracking status și delivery
- ✅ Respectă preferințele clientului

**Entități:**
- `Notification` (Aggregate Root)
- `NotificationRecipient` (Entity)
- `NotificationAttempt` (Entity) - tracking retrimiteri

**Value Objects:**
- `EmailAddress` - Validare RFC 5322
- `PhoneNumber` - Validare E.164 (+40XXXXXXXXX)
- `NotificationContent` - Subject + Body
- `DeliverySchedule` - Data programării
- `RetryPolicy` - Max attempts + backoff

**Enums:**
```csharp
NotificationStatus: Draft, Scheduled, Pending, Sending, Sent, Delivered, Failed, Cancelled
NotificationType: OrderPlaced, PaymentConfirmed, ShipmentDispatched, ReturnApproved...
NotificationChannel: Email, SMS, Push, InApp
NotificationPriority: Low, Normal, High, Urgent
```

### Agregatul Secundar: **NotificationPreference**

**Responsabilități:**
- Preferințe client (opt-in/opt-out)
- Canale preferate
- Quiet hours (22:00 - 08:00)
- GDPR compliance

---

## 3️⃣ REGULI DE VALIDARE

### SendNotification - 8 Validări

| # | Validare | Descriere | Excepție |
|---|----------|-----------|----------|
| 1 | Destinatar valid | Email SAU Telefon SAU DeviceToken | `InvalidRecipientException` |
| 2 | Email format | RFC 5322 (name@domain.com) | `InvalidEmailFormatException` |
| 3 | Telefon format | E.164 (+40XXXXXXXXX) | `InvalidPhoneFormatException` |
| 4 | Tip suportat | Există în enum NotificationType | `UnsupportedNotificationTypeException` |
| 5 | Template există | Pentru tip + canal + limbă | `TemplateNotFoundException` |
| 6 | Conținut non-gol | Subject.Length > 0 OR Body.Length > 0 | `EmptyContentException` |
| 7 | Nu a optat-out | Client permis notificarea | `OptedOutException` |
| 8 | Limită zilnică | < 50 notificări/client/zi | `DailyLimitExceededException` |

**Reguli Business:**
- ⚡ Priority URGENT bypass quiet hours
- 📧 Marketing necesită consimțământ explicit (GDPR)
- 💰 SMS necesită buget disponibil
- 🔒 Transactional nu pot fi dezactivate

### ScheduleNotification - +4 Validări

| # | Validare | Descriere |
|---|----------|-----------|
| 9 | Data viitoare | ScheduledFor > DateTime.UtcNow |
| 10 | Max 90 zile | ScheduledFor <= DateTime.UtcNow.AddDays(90) |
| 11 | Nu duplicate | Nu există identică programată |
| 12 | Toate Send | + toate de la SendNotification |

### ResendNotification - 4 Validări

| # | Validare | Descriere |
|---|----------|-----------|
| 1 | Notificare există | GetById(notificationId) != null |
| 2 | Max 3 încercări | AttemptCount < 3 |
| 3 | Backoff time | LastAttempt + BackoffTime < Now |
| 4 | Nu e delivered | Status != Delivered |

**Backoff exponențial:**
- Attempt 1 → Wait 5 minutes
- Attempt 2 → Wait 15 minutes
- Attempt 3 → Wait 1 hour
- After 3 → Permanent FAILED

### CancelNotification - 3 Validări

| # | Validare | Descriere |
|---|----------|-----------|
| 1 | Notificare există | GetById != null |
| 2 | Status Scheduled | Status == Scheduled |
| 3 | Nu e prea târziu | ScheduledFor > Now.AddMinutes(5) |

### MarkAsRead - 3 Validări

| # | Validare | Descriere |
|---|----------|-----------|
| 1 | Notificare există | GetById != null |
| 2 | User = destinatar | User.Id == Notification.RecipientId |
| 3 | Nu e deja citită | IsRead == false |

---

## 4️⃣ INVARIANȚI

### Invariant 1: Status Progression ✅

```
Created → Scheduled → Pending → Sending → Sent → Delivered ✅
                           ↓
                        Failed → Retrying (max 3×)
```

**Cod:**
```csharp
public void Send() {
    if (Status != NotificationStatus.Pending)
        throw new InvalidStatusException($"Cannot send from status {Status}");
    
    Status = NotificationStatus.Sending;
}
```

### Invariant 2: Destinatar Valid ✅

```csharp
∀ recipient: (Email != null) OR (Phone != null) OR (DeviceToken != null)
```

**Cod:**
```csharp
public void ValidateRecipient() {
    if (Recipient.Email == null && 
        Recipient.Phone == null && 
        Recipient.DeviceToken == null)
        throw new InvalidRecipientException("At least one contact method required");
}
```

### Invariant 3: Retry Limit ✅

```csharp
AttemptCount ≤ 3
```

**Cod:**
```csharp
public void Retry() {
    if (AttemptCount >= 3)
        throw new MaxRetriesExceededException("Cannot retry more than 3 times");
    
    AttemptCount++;
}
```

### Invariant 4: Scheduled Time ✅

```csharp
IF Status == Scheduled THEN ScheduledFor > DateTime.UtcNow
```

**Cod:**
```csharp
public void Schedule(DateTime scheduledFor) {
    if (scheduledFor <= DateTime.UtcNow)
        throw new InvalidScheduleException("Cannot schedule in the past");
    
    ScheduledFor = scheduledFor;
    Status = NotificationStatus.Scheduled;
}
```

### Invariant 5: Content Not Empty ✅

```csharp
Subject.Length > 0 OR Body.Length > 0
```

**Cod:**
```csharp
public void ValidateContent() {
    if (string.IsNullOrWhiteSpace(Subject) && 
        string.IsNullOrWhiteSpace(Body))
        throw new EmptyContentException("Notification must have content");
}
```

### Invariant 6: Daily Limit ✅

```csharp
COUNT(Notifications per Customer today) ≤ 50
```

**Cod:**
```csharp
public async Task ValidateDailyLimit(Guid customerId) {
    var count = await repository.CountTodayNotificationsAsync(customerId);
    if (count >= 50)
        throw new DailyLimitExceededException($"Customer has {count}/50 notifications today");
}
```

### Invariant 7: Channel Consistency ✅

```csharp
IF Channel == Email THEN Recipient.Email != null
IF Channel == SMS THEN Recipient.Phone != null
IF Channel == Push THEN Recipient.DeviceToken != null
```

**Cod:**
```csharp
public void ValidateChannelConsistency() {
    switch (Channel) {
        case NotificationChannel.Email:
            if (Recipient.Email == null)
                throw new ChannelMismatchException("Email channel requires email address");
            break;
        case NotificationChannel.SMS:
            if (Recipient.Phone == null)
                throw new ChannelMismatchException("SMS channel requires phone number");
            break;
        case NotificationChannel.Push:
            if (Recipient.DeviceToken == null)
                throw new ChannelMismatchException("Push channel requires device token");
            break;
    }
}
```

---

## 5️⃣ DOMAIN SERVICES

### NotificationDeliveryService

**Responsabilitate:** Trimitere efectivă prin provider extern

**Metode:**
```csharp
Task<DeliveryResult> SendEmailAsync(EmailAddress to, EmailContent content);
Task<DeliveryResult> SendSmsAsync(PhoneNumber to, string message);
Task<DeliveryResult> SendPushAsync(DeviceToken token, PushContent content);
```

**Provideri:**
- Email: SendGrid, AWS SES, SMTP
- SMS: Twilio, Nexmo
- Push: Firebase Cloud Messaging, Apple Push

### NotificationPreferenceService

**Responsabilitate:** Verifică preferințe client

**Metode:**
```csharp
Task<bool> CanSendAsync(Guid customerId, NotificationType type);
Task<List<NotificationChannel>> GetPreferredChannelsAsync(Guid customerId);
bool IsQuietHours(DateTime time); // 22:00 - 08:00
Task OptOutAsync(Guid customerId, NotificationType type);
Task OptInAsync(Guid customerId, NotificationType type);
```

**Reguli:**
- Transactional: Întotdeauna trimite (legal requirement)
- Marketing: Verifică opt-in explicit
- Quiet Hours: Skip dacă Priority != Urgent

### NotificationTemplateService

**Responsabilitate:** Gestionare template-uri

**Metode:**
```csharp
Task<NotificationTemplate> GetTemplateAsync(NotificationType type, NotificationChannel channel, string language);
string ProcessTemplate(NotificationTemplate template, Dictionary<string, string> data);
Task ValidatePlaceholdersAsync(NotificationTemplate template, Dictionary<string, string> data);
```

**Template Example:**
```csharp
Subject: "Comanda #{{orderNumber}} confirmată!"
Body: "Bună {{customerName}}, comanda ta de {{orderTotal}} RON..."
Placeholders: { orderNumber, customerName, orderTotal, trackingUrl }
```

### NotificationRoutingService

**Responsabilitate:** Determină best channel

**Metode:**
```csharp
Task<NotificationChannel> DetermineBestChannelAsync(Guid customerId, NotificationType type, NotificationPriority priority);
Task<List<NotificationChannel>> GetFallbackChannelsAsync(NotificationChannel failed);
```

**Fallback Strategy:**
```
Email FAILED → Try SMS → Try Push → Try InApp → Dead Letter Queue
```

---

## 6️⃣ INTEGRATION EVENTS

### Subscribe La (Input)

```csharp
// ORDER Context
OrderPlaced → SendNotification(type: OrderPlaced, channel: Email)
OrderShipped → SendNotification(type: OrderShipped, channel: Email + SMS)
OrderDelivered → SendNotification(type: OrderDelivered, channel: Email)

// PAYMENT Context
PaymentCompleted → SendNotification(type: PaymentSuccess, channel: Email)
PaymentFailed → SendNotification(type: PaymentFailed, channel: Email + SMS)
RefundProcessed → SendNotification(type: RefundProcessed, channel: Email)

// SHIPPING Context
ShipmentInTransit → SendNotification(type: ShipmentUpdate, channel: Push)

// RETURNS Context
ReturnApproved → SendNotification(type: ReturnApproved, channel: Email)
ReturnAccepted → SendNotification(type: RefundProcessing, channel: Email)
```

### Publish (Output)

```csharp
CustomerNotified → Analytics, Customer Engagement Tracking
NotificationFailed → Alerting System, Support Dashboard
NotificationDelivered → Analytics (Delivery Rate = 99.2%)
NotificationOpened → Analytics (Open Rate = 22.5%)
NotificationClicked → Analytics (CTR = 3.8%)
NotificationBounced → Email Validation, Blacklist Service
```

---

## 7️⃣ EXAMPLE USE CASES

### Use Case 1: Order Placed - Email Notification

```csharp
// 1. ORDER publică OrderPlaced event
var orderPlacedEvent = new OrderPlaced(orderId, customerId, orderTotal);
await eventBus.PublishAsync(orderPlacedEvent);

// 2. NOTIFICATION primește event și creează notificare
var notification = Notification.Create(
    customerId: customerId,
    type: NotificationType.OrderPlaced,
    channel: NotificationChannel.Email,
    recipient: new EmailAddress(customer.Email),
    template: await templateService.GetTemplateAsync(
        NotificationType.OrderPlaced, 
        NotificationChannel.Email, 
        "ro"
    ),
    data: new Dictionary<string, string> {
        { "customerName", customer.Name },
        { "orderNumber", order.Number },
        { "orderTotal", order.Total.ToString() },
        { "trackingUrl", $"https://example.com/orders/{orderId}" }
    }
);

// 3. Validare
notification.ValidateInvariants();
await preferenceService.CanSendAsync(customerId, NotificationType.OrderPlaced);

// 4. Trimitere
notification.Send();
var result = await deliveryService.SendEmailAsync(notification.Recipient.Email, notification.Content);

// 5. Update status
if (result.IsSuccess) {
    notification.MarkAsDelivered(result.TrackingId);
} else {
    notification.MarkAsFailed(result.ErrorMessage);
}

// 6. Publicare CustomerNotified event
await eventBus.PublishAsync(new CustomerNotified(notification.Id, customerId, ...));
```

### Use Case 2: Payment Failed - Multi-Channel

```csharp
// Email + SMS pentru notificări critice
var channels = new[] { NotificationChannel.Email, NotificationChannel.SMS };

foreach (var channel in channels) {
    var notification = Notification.Create(..., channel: channel);
    await SendAsync(notification);
}
```

### Use Case 3: Retry After Failure

```csharp
// Primul eșec
notification.MarkAsFailed("SendGrid timeout");
await repository.SaveAsync(notification);

// Retry după 5 minute (job scheduler)
await Task.Delay(TimeSpan.FromMinutes(5));

// A doua încercare
notification.Retry(); // AttemptCount = 2
var result = await deliveryService.SendEmailAsync(...);

if (result.IsSuccess) {
    notification.MarkAsDelivered(result.TrackingId);
} else {
    // Următorul retry după 15 minute
    notification.MarkAsFailed(result.ErrorMessage);
}
```

---

## 8️⃣ METRICS & KPIs

| Metric | Formula | Target | Industry Avg |
|--------|---------|--------|--------------|
| **Delivery Rate** | (Delivered / Sent) × 100 | > 99% | 98-99% |
| **Open Rate** | (Opened / Delivered) × 100 | 20-30% | 15-25% |
| **Click Rate** | (Clicked / Opened) × 100 | 3-5% | 2-5% |
| **Bounce Rate** | (Bounced / Sent) × 100 | < 2% | 2-5% |
| **Unsubscribe Rate** | (Unsubscribed / Delivered) × 100 | < 0.5% | 0.2-0.5% |
| **Avg Delivery Time** | Median(DeliveredAt - SentAt) | < 5s | 3-10s |
| **Cost per Email** | Total Cost / Emails Sent | $0.001 | $0.0005-$0.002 |
| **Cost per SMS** | Total Cost / SMS Sent | $0.05 | $0.03-$0.10 |

---

## 9️⃣ FIȘIERE CREATE

✅ **NOTIFICATION_DDD_DESIGN.md** - Design complet DDD (700+ linii)  
✅ **Domain/Notifications/Events/DomainEvents.cs** - 9 evenimente (400+ linii)  
✅ **NOTIFICATION_IMPLEMENTATION_SUMMARY.md** - Rezumat implementare  
✅ **NOTIFICATION_QUICK_START.md** - Acest ghid  

---

## 🎯 NEXT STEPS

Pentru implementare completă, trebuie create:

1. **Domain Layer:**
   - [ ] Notification.cs (Aggregate Root)
   - [ ] NotificationPreference.cs (Aggregate)
   - [ ] Domain Services (4 fișiere)

2. **Application Layer:**
   - [ ] NotificationCommands.cs (7 comenzi)
   - [ ] NotificationCommandHandlers.cs (7 handlers)

3. **Infrastructure:**
   - [ ] NotificationRepository.cs
   - [ ] SendGridEmailService.cs
   - [ ] TwilioSmsService.cs
   - [ ] FirebasePushService.cs

4. **API:**
   - [ ] NotificationsController.cs (7 endpoints)

5. **Testing:**
   - [ ] Unit Tests
   - [ ] Integration Tests

---

**Design Status:** ✅ **COMPLET**  
**Implementation Status:** 🔨 **READY TO START**  
**Documentație:** ✅ **100% COMPLETE**

Happy Coding! 📧🚀

