- Order confirmation
- Payment confirmation
- Shipping updates
- Password reset
- Account verification

**Caracteristici:**
- Prioritate HIGH
- Cannot opt-out (legal requirement)
- Immediate delivery
- Guaranteed delivery (retry until success)

### Marketing Notifications (Opt-in Required)
- Promotional offers
- New products
- Newsletter
- Recommendations

**Caracteristici:**
- Prioritate NORMAL
- Can opt-out
- Respect quiet hours
- Include unsubscribe link

### System Notifications
- Account security alerts
- Terms of service updates
- Planned maintenance

**Caracteristici:**
- Prioritate HIGH
- Limited opt-out
- Multi-channel delivery

---

## 9️⃣ TEMPLATE SYSTEM

### Template Structure
```csharp
public class NotificationTemplate
{
    string TemplateId;           // "order_placed_email_ro"
    NotificationType Type;       // OrderPlaced
    NotificationChannel Channel; // Email
    string Language;             // "ro"
    string Subject;              // "Comanda #{{orderNumber}} confirmată"
    string Body;                 // HTML/Text with placeholders
    Dictionary<string, string> Placeholders; // {{orderNumber}}, {{customerName}}
}
```

### Template Example: Order Placed Email (Romanian)
```html
Subject: Comanda #{{orderNumber}} a fost plasată cu succes!

Body:
<h1>Bună {{customerName}},</h1>
<p>Comanda ta <strong>#{{orderNumber}}</strong> a fost plasată cu succes!</p>

<h2>Detalii comandă:</h2>
<ul>
  <li>Data: {{orderDate}}</li>
  <li>Total: {{orderTotal}} RON</li>
  <li>Livrare estimată: {{estimatedDelivery}}</li>
</ul>

<p>Vei primi o notificare când comanda va fi expediată.</p>

<a href="{{trackingUrl}}">Vezi status comandă</a>
```

---

## 🔟 GDPR & COMPLIANCE

### Data Protection
- **Right to be forgotten**: Delete all notifications for a customer
- **Data portability**: Export notification history
- **Consent management**: Explicit opt-in for marketing
- **Unsubscribe**: One-click unsubscribe in all marketing emails

### Audit Trail
- Log all notification send attempts
- Track consent changes
- Record opt-out requests
- Retention policy: 2 years

### Privacy
- Do not include sensitive data in SMS (truncate order details)
- Secure storage of device tokens
- Encrypted email content for sensitive notifications

---

## 1️⃣1️⃣ PERFORMANCE & SCALABILITY

### Rate Limiting
- Per customer: 50 notifications/day
- Per API client: 1000 notifications/minute
- Email provider: Respect rate limits (SendGrid: 100 emails/second)

### Batch Processing
- Queue notifications in batches
- Process in background workers
- Prioritize urgent notifications

### Caching
- Cache templates
- Cache customer preferences
- Cache contact information

### Monitoring
- Track delivery rates
- Monitor failed notifications
- Alert on high failure rates (> 5%)
- Track costs (SMS, email)

---

## 1️⃣2️⃣ ERROR HANDLING

### Failure Scenarios

1. **Invalid Recipient**
   - Action: Mark as failed, don't retry
   - Notify: Support team

2. **Temporary Provider Error (5xx)**
   - Action: Retry with exponential backoff
   - Max retries: 3

3. **Invalid Template**
   - Action: Log error, use fallback template
   - Notify: Development team

4. **Rate Limit Exceeded**
   - Action: Queue and delay
   - Resume after cooldown

5. **Customer Opted Out**
   - Action: Skip, log
   - No retry

### Fallback Strategy
```
Primary Channel (Email) FAILED
  ↓
Fallback to Secondary (SMS)
  ↓
If both fail: In-App Notification
  ↓
If all fail: Store in Dead Letter Queue
```

---

## 1️⃣3️⃣ UBIQUITOUS LANGUAGE

### Termeni Cheie
- **Notification**: Mesaj trimis către client
- **Channel**: Metodă de livrare (Email, SMS, Push, In-App)
- **Template**: Șablon predefinit pentru notificare
- **Placeholder**: Variabilă în template ({{customerName}})
- **Recipient**: Destinatarul notificării
- **Delivery Status**: Status livrare (Sent, Delivered, Failed, Bounced)
- **Opt-in/Opt-out**: Consimțământ pentru primirea notificărilor
- **Quiet Hours**: Interval orar când nu se trimit notificări (22:00-08:00)
- **Retry**: Reîncercare de trimitere
- **Backoff**: Creștere interval între reîncercări
- **Dead Letter Queue**: Coadă pentru notificări eșuate definitiv
- **Transactional**: Notificare legată de o tranzacție (nu poate fi dezactivată)
- **Marketing**: Notificare promoțională (poate fi dezactivată)

---

## 1️⃣4️⃣ SCENARII DE BUSINESS

### Scenario 1: Order Placed - Notificare Imediată
1. Client plasează comandă
2. ORDER context publică `OrderPlaced` event
3. NOTIFICATION context primește event
4. Verifică preferințe client (nu a optat-out)
5. Încarcă template "order_placed_email_ro"
6. Procesează placeholders cu date din comandă
7. Trimite email prin SendGrid
8. Primește confirmare livrare
9. Marchează notificare ca "Delivered"
10. Publică `CustomerNotified` event

### Scenario 2: Shipping Update - Notificare Multiplă
1. Colet expediat
2. SHIPPING publică `ShipmentDispatched`
3. NOTIFICATION trimite Email + SMS + Push
4. Email: Success
5. SMS: Failed (număr invalid)
6. Push: Success
7. Înregistrează rezultate
8. Nu reîncearcă SMS (invalid recipient)

### Scenario 3: Payment Failed - Retry Logic
1. Plata eșuează
2. PAYMENT publică `PaymentFailed`
3. NOTIFICATION încearcă trimitere email
4. SendGrid returnează 503 (server busy)
5. Se programează retry după 5 minute
6. A doua încercare: Success
7. Marchează ca "Delivered"

### Scenario 4: Marketing - Respectare Opt-out
1. Campanie marketing programată
2. Se verifică preferințe pentru fiecare client
3. Client A: Opted-in → Trimite
4. Client B: Opted-out → Skip
5. Client C: Quiet hours (23:00) → Amână pentru 08:00
6. Raportează statistici campanie

---

## 1️⃣5️⃣ METRICS & ANALYTICS

### Key Performance Indicators (KPIs)

1. **Delivery Rate**: % notificări livrate cu succes
   - Target: > 99% pentru transactional
   - Target: > 95% pentru marketing

2. **Open Rate** (Email): % email-uri deschise
   - Industry average: 15-25%

3. **Click-Through Rate** (Email): % click-uri pe link-uri
   - Industry average: 2-5%

4. **Bounce Rate**: % email-uri returnate
   - Target: < 2%

5. **Unsubscribe Rate**: % dezabonări
   - Target: < 0.5%

6. **Cost per Notification**:
   - Email: ~$0.001
   - SMS: ~$0.05
   - Push: Free

7. **Average Delivery Time**: Timp mediu până la livrare
   - Target: < 5 secunde

---

Această documentație oferă o bază solidă pentru implementarea bounded context-ului NOTIFICATION folosind principiile DDD.
# 📧 BOUNDED CONTEXT: NOTIFICATION MANAGEMENT

## 📋 Domain-Driven Design - Analiză Completă

Data: November 7, 2025

---

## 1️⃣ COMENZI ȘI EVENIMENTE

### Mapare Comenzi → Evenimente

| Comandă | Eveniment Rezultat | Descriere |
|---------|-------------------|-----------|
| `SendNotification` | `CustomerNotified` | Trimite notificare către client |
| `ScheduleNotification` | `NotificationScheduled` | Programează notificare pentru mai târziu |
| `ResendNotification` | `CustomerNotified` | Retrimite notificare |
| `CancelNotification` | `NotificationCancelled` | Anulează notificare programată |
| `MarkAsRead` | `NotificationRead` | Marchează notificare ca citită |

### Evenimente Suplimentare Generate de Alte Bounded Contexts

| Context Sursă | Eveniment | Declanșează Notificare |
|---------------|-----------|------------------------|
| ORDER | `OrderPlaced` | "Comanda ta a fost plasată" |
| ORDER | `OrderConfirmed` | "Comanda ta a fost confirmată" |
| ORDER | `OrderShipped` | "Comanda ta a fost expediată" |
| ORDER | `OrderDelivered` | "Comanda ta a fost livrată" |
| ORDER | `OrderCancelled` | "Comanda ta a fost anulată" |
| PAYMENT | `PaymentCompleted` | "Plata a fost procesată cu succes" |
| PAYMENT | `PaymentFailed` | "Plata a eșuat" |
| PAYMENT | `RefundProcessed` | "Rambursarea ta a fost procesată" |
| SHIPPING | `ShipmentDispatched` | "Coletul tău a fost expediat" |
| SHIPPING | `ShipmentInTransit` | "Coletul este în tranzit" |
| SHIPPING | `ShipmentDelivered` | "Coletul a fost livrat" |
| RETURNS | `ReturnRequested` | "Cererea de retur a fost primită" |
| RETURNS | `ReturnApproved` | "Returul tău a fost aprobat" |
| RETURNS | `ReturnRejected` | "Returul tău a fost respins" |
| RETURNS | `ReturnAccepted` | "Rambursarea ta va fi procesată" |

---

## 2️⃣ AGREGĂRI (AGGREGATES)

### Agregatul Principal: **Notification**

**Responsabilități:**
- Gestionează ciclul de viață al unei notificări
- Validează destinatarii și conținutul
- Gestionează încercările de trimitere (retry logic)
- Tracking status și delivery
- Gestionează preferințele de notificare

**Entități Componente:**
- `Notification` (Aggregate Root)
- `NotificationRecipient` (Entity)
- `NotificationAttempt` (Entity - tracking încercări)
- `NotificationTemplate` (Value Object)

### Agregatul Secundar: **NotificationPreference**

**Responsabilități:**
- Gestionează preferințele clientului pentru notificări
- Opt-in/Opt-out pentru diferite tipuri de notificări
- Canale preferate (Email, SMS, Push)

---

## 3️⃣ REGULI DE VALIDARE

### 3.1 SendNotification Command

**Validări:**
1. ✅ Destinatarul există și este valid
2. ✅ Email/telefon este valid (format)
3. ✅ Tipul de notificare este suportat
4. ✅ Template-ul există pentru tipul de notificare
5. ✅ Conținutul notificării nu este gol
6. ✅ Clientul nu a optat-out pentru acest tip de notificare
7. ✅ Nu s-a depășit limita de notificări per zi pentru client
8. ✅ Prioritatea este validă (Low, Normal, High, Urgent)

**Reguli de Business:**
- Max 50 notificări per client per zi (anti-spam)
- Notificări urgente au prioritate peste cele normale
- Email-urile marketing necesită consimțământ explicit (GDPR)
- SMS-urile au cost și necesită verificare buget

### 3.2 ScheduleNotification Command

**Validări:**
1. ✅ Data programării este în viitor
2. ✅ Data nu depășește 90 zile în viitor
3. ✅ Toate validările de la SendNotification
4. ✅ Nu există deja o notificare programată identică

**Reguli de Business:**
- Notificări programate pot fi anulate până la 5 minute înainte
- Se verifică preferințele la momentul trimiterii, nu la programare

### 3.3 ResendNotification Command

**Validări:**
1. ✅ Notificarea originală există
2. ✅ Nu s-au depășit încercările maxime (default: 3)
3. ✅ A trecut timpul minim între retrimiteri (default: 5 minute)
4. ✅ Notificarea nu a fost deja livrată cu succes

**Reguli de Business:**
- Backoff exponențial: 5min, 15min, 1h
- După 3 încercări eșuate, se marchează ca "Failed"

### 3.4 CancelNotification Command

**Validări:**
1. ✅ Notificarea există și este în status "Scheduled"
2. ✅ Utilizatorul are permisiune să anuleze
3. ✅ Nu a trecut data programării

**Reguli de Business:**
- Notificări deja trimise nu pot fi anulate
- Notificări în "Sending" nu pot fi anulate

### 3.5 MarkAsRead Command

**Validări:**
1. ✅ Notificarea există
2. ✅ Utilizatorul este destinatarul
3. ✅ Notificarea nu este deja marcată ca citită

---

## 4️⃣ INVARIANȚI

### Notification Aggregate Invariants

#### Invariant 1: Status Progression
```
Scheduled → Pending → Sending → Sent → Delivered
                           ↓
                        Failed → Retrying
```
**Regula:** Statusul poate avansa doar în ordine, cu excepția retrimiterii.

#### Invariant 2: Destinatar Valid
```
∀ recipient ∈ Recipients:
    (recipient.Email != null AND IsValidEmail(recipient.Email)) OR
    (recipient.PhoneNumber != null AND IsValidPhone(recipient.PhoneNumber)) OR
    (recipient.DeviceToken != null)
```
**Regula:** Fiecare destinatar trebuie să aibă cel puțin un canal de comunicare valid.

#### Invariant 3: Retry Limit
```
Notification.AttemptCount ≤ MaxRetryAttempts (default: 3)
```
**Regula:** Nu se pot face mai mult de 3 încercări de trimitere.

#### Invariant 4: Scheduled Time
```
IF Notification.Status == Scheduled THEN
    Notification.ScheduledFor > DateTime.UtcNow
```
**Regula:** Notificările programate trebuie să aibă data în viitor.

#### Invariant 5: Content Not Empty
```
Notification.Subject.Length > 0 OR Notification.Body.Length > 0
```
**Regula:** Notificarea trebuie să aibă conținut.

#### Invariant 6: Daily Limit
```
COUNT(Notifications per Customer per Day) ≤ DailyLimit (default: 50)
```
**Regula:** Max 50 notificări per client per zi.

#### Invariant 7: Channel Consistency
```
IF Notification.Channel == Email THEN Recipient.Email != null
IF Notification.Channel == SMS THEN Recipient.PhoneNumber != null
IF Notification.Channel == Push THEN Recipient.DeviceToken != null
```
**Regula:** Canalul de notificare trebuie să fie compatibil cu datele destinatarului.

---

## 5️⃣ DOMAIN SERVICES

### NotificationDeliveryService
- Trimite efectiv notificarea prin canalul specificat
- Integrare cu provideri externi (SendGrid, Twilio, Firebase)
- Gestionează retry logic cu backoff exponențial
- Tracking delivery status

### NotificationPreferenceService
- Verifică preferințele clientului
- Gestionează opt-in/opt-out
- Conformitate GDPR
- Quiet hours (nu trimite între 22:00 - 08:00)

### NotificationTemplateService
- Încarcă template-uri predefinite
- Procesează placeholder-e ({{customerName}}, {{orderNumber}})
- Localizare (RO, EN)
- Versioning template-uri

### NotificationRoutingService
- Determină cel mai bun canal bazat pe preferințe
- Fallback logic (dacă email eșuează, încearcă SMS)
- Prioritizare bazată pe urgență

---

## 6️⃣ INTEGRARE CU ALTE BOUNDED CONTEXTS

### Dependencies (Upstream)
- **CUSTOMER MANAGEMENT**: Verifică date contact, preferințe
- **ORDER MANAGEMENT**: Obține detalii comandă pentru notificări
- **PAYMENT**: Obține detalii plată pentru notificări
- **RETURNS**: Obține detalii retur pentru notificări

### Events Subscribed (Integration Events)
Notification context se abonează la evenimente din alte contexte:

```csharp
// ORDER Context
- OrderPlaced → SendOrderConfirmationNotification
- OrderShipped → SendShippingNotification
- OrderDelivered → SendDeliveryConfirmation

// PAYMENT Context
- PaymentCompleted → SendPaymentConfirmation
- PaymentFailed → SendPaymentFailureNotification
- RefundProcessed → SendRefundNotification

// RETURNS Context
- ReturnApproved → SendReturnApprovalNotification
- ReturnAccepted → SendRefundProcessingNotification

// SHIPPING Context
- ShipmentInTransit → SendShipmentTrackingUpdate
```

### Events Published (Downstream)
```csharp
- CustomerNotified → Analytics, Audit Log
- NotificationFailed → Alerting System, Support Team
- NotificationDelivered → Analytics, Customer Engagement Metrics
```

---

## 7️⃣ NOTIFICATION CHANNELS

### Email
**Provider:** SendGrid / AWS SES / SMTP  
**Features:**
- HTML templates
- Attachments (PDF invoices, etc.)
- Open tracking
- Click tracking
- Unsubscribe links

**Validări:**
- Valid email format (RFC 5322)
- Domain exists (DNS check)
- Not in blacklist

### SMS
**Provider:** Twilio / Nexmo  
**Features:**
- Unicode support (română cu diacritice)
- Delivery receipts
- Cost tracking

**Validări:**
- Valid phone format (E.164)
- Supported country code
- Budget available

**Limitări:**
- Max 160 caractere (single SMS)
- Max 1530 caractere (concatenated, 10 SMS)

### Push Notification
**Provider:** Firebase Cloud Messaging / Apple Push  
**Features:**
- Rich notifications (images, actions)
- Deep linking
- Badge counts

**Validări:**
- Valid device token
- App installed
- Notifications enabled

### In-App Notification
**Storage:** Database  
**Features:**
- Notification center
- Read/unread status
- Persistence

---

## 8️⃣ NOTIFICATION TYPES

### Transactional Notifications (Always Send)

