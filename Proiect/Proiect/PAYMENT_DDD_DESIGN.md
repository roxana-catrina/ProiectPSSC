# 💳 PAYMENT BOUNDED CONTEXT - DDD DESIGN

## 📋 OVERVIEW
Acest bounded context gestionează procesarea plăților și a rambursărilor pentru comenzi.

---

## 🎯 COMENZI → EVENIMENTE → AGREGĂRI

### 1️⃣ **ProcessPayment** → **PaymentCompleted**
**Comandă:** `ProcessPaymentCommand`
- **Input:**
  - `OrderId` (Guid) - ID-ul comenzii
  - `Amount` (decimal) - Suma de plătit
  - `Currency` (string) - Moneda (EUR, USD, RON)
  - `PaymentMethod` (enum) - Metodă: CreditCard, DebitCard, PayPal, BankTransfer
  - `CardDetails` (optional) - Detalii card dacă e aplicabil
  
- **Agregare responsabilă:** `Payment`
- **Eveniment generat:** `PaymentCompleted`

**Reguli de validare:**
- ✅ Amount > 0
- ✅ Currency trebuie să fie validă (EUR, USD, RON)
- ✅ OrderId trebuie să existe
- ✅ Payment pentru OrderId nu trebuie să existe deja în status Completed
- ✅ Detalii card necesare pentru CreditCard/DebitCard

**Invarianți (Payment Aggregate):**
- Un payment nu poate fi procesat de două ori
- Status-ul poate trece doar prin tranziții valide: Pending → Processing → Completed/Failed
- Retry count nu poate depăși 3 încercări

---

### 2️⃣ **InitiateRefund** → **RefundInitiated**
**Comandă:** `InitiateRefundCommand`
- **Input:**
  - `PaymentId` (Guid) - ID-ul plății originale
  - `RefundAmount` (decimal) - Suma de rambursat
  - `Reason` (string) - Motivul rambursării
  - `ReasonCategory` (enum) - CustomerRequest, Fraud, Error, OrderCancellation
  - `RequestedBy` (string) - Cine a solicitat (Customer/Admin)

- **Agregare responsabilă:** `Refund`
- **Eveniment generat:** `RefundInitiated`

**Reguli de validare:**
- ✅ Payment trebuie să existe și să fie Completed
- ✅ RefundAmount <= Original Payment Amount
- ✅ Nu există deja un refund Completed pentru acest payment
- ✅ Suma totală a refund-urilor (including pending) <= Original Amount
- ✅ Reason nu poate fi gol

**Invarianți (Refund Aggregate):**
- Un refund nu poate depăși suma plății originale
- Multiple refund-uri parțiale sunt permise dacă suma totală <= original amount
- Status-ul poate trece prin: Initiated → Processing → Completed/Failed

---

### 3️⃣ **CompleteRefund** → **RefundCompleted**
**Comandă:** `CompleteRefundCommand`
- **Input:**
  - `RefundId` (Guid) - ID-ul rambursării
  - `TransactionId` (string) - ID tranzacție de la gateway
  - `AuthorizationCode` (string) - Cod autorizare
  - `GatewayResponse` (string) - Răspuns gateway

- **Agregare responsabilă:** `Refund`
- **Eveniment generat:** `RefundCompleted`

**Reguli de validare:**
- ✅ Refund trebuie să existe
- ✅ Refund status trebuie să fie Initiated sau Processing
- ✅ TransactionId nu poate fi gol
- ✅ Nu s-au depășit numărul maxim de retry-uri

**Invarianți (Refund Aggregate):**
- Un refund poate fi completat o singură dată
- TransactionInfo trebuie setat la completare
- ProcessedAt trebuie să fie după CreatedDate

---

## 🏗️ AGREGĂRI

### **Payment Aggregate**
**Aggregate Root:** `Payment`

**Entități:**
- Payment (root)

**Value Objects:**
- `Money` (Amount, Currency)
- `PaymentDetails` (MaskedCardNumber, CardHolderName, ExpiryDate)
- `TransactionInfo` (TransactionId, AuthorizationCode, ProcessedAt, GatewayResponse)

**Enums:**
- `PaymentStatus`: Pending, Processing, Completed, Failed, Cancelled
- `PaymentMethod`: CreditCard, DebitCard, PayPal, BankTransfer, Cash

**Invarianți:**
1. Payment.Amount trebuie > 0
2. Payment nu poate fi procesat de două ori (idempotency)
3. Tranziții status valide:
   - Pending → Processing
   - Processing → Completed/Failed
   - Pending → Cancelled
4. RetryCount <= 3
5. TransactionInfo trebuie setat când status = Completed

---

### **Refund Aggregate**
**Aggregate Root:** `Refund`

**Entități:**
- Refund (root)

**Value Objects:**
- `Money` (RefundAmount, OriginalPaymentAmount)
- `RefundReason` (Reason, Category, RequestedBy, RequestedAt)
- `TransactionInfo` (TransactionId, AuthorizationCode, ProcessedAt, GatewayResponse)

**Enums:**
- `RefundStatus`: Initiated, Processing, Completed, Failed, Cancelled
- `RefundReasonCategory`: CustomerRequest, Fraud, Error, OrderCancellation, Duplicate

**Invarianți:**
1. RefundAmount > 0 și <= OriginalPaymentAmount
2. Payment asociat trebuie să fie Completed
3. Suma totală a refund-urilor pentru un payment <= Original Amount
4. Tranziții status valide:
   - Initiated → Processing
   - Processing → Completed/Failed
   - Initiated → Cancelled
5. RetryCount <= 3
6. TransactionInfo trebuie setat când status = Completed

---

## 🛡️ DOMAIN SERVICES

### **IPaymentGatewayService**
Serviciu pentru integrare cu gateway-uri de plată externe (Stripe, PayPal, etc.)
- `Task<PaymentGatewayResult> ProcessPaymentAsync(Payment payment)`
- `Task<RefundGatewayResult> ProcessRefundAsync(Refund refund)`

### **IFraudDetectionService**
Serviciu pentru detectarea fraudelor
- `Task<FraudCheckResult> CheckPaymentAsync(Payment payment, string customerEmail)`
- Verifică: multiple payments rapid, sume mari neobișnuite, pattern-uri suspecte

### **PaymentDomainService**
Logică de business complexă care coordonează multiple agregări
- `Task<bool> CanRefundPaymentAsync(Guid paymentId, decimal amount)`
- `Task<decimal> GetTotalRefundedAmountAsync(Guid paymentId)`

---

## 🔄 WORKFLOW COMPLET

### Payment Flow:
```
1. Command: ProcessPaymentCommand
   ↓
2. Validation: Amount, Currency, PaymentMethod
   ↓
3. Domain Service: FraudDetectionService.CheckPayment()
   ↓
4. Aggregate: Payment.Process()
   ↓
5. Domain Service: PaymentGatewayService.ProcessPayment()
   ↓
6. Aggregate: Payment.Complete() sau Payment.Fail()
   ↓
7. Event: PaymentCompleted sau PaymentFailed
```

### Refund Flow:
```
1. Command: InitiateRefundCommand
   ↓
2. Validation: Payment exists, Amount valid
   ↓
3. Domain Service: PaymentDomainService.CanRefundPayment()
   ↓
4. Aggregate: Refund.Create()
   ↓
5. Event: RefundInitiated
   ↓
6. Command: CompleteRefundCommand
   ↓
7. Domain Service: PaymentGatewayService.ProcessRefund()
   ↓
8. Aggregate: Refund.Complete() sau Refund.Fail()
   ↓
9. Event: RefundCompleted sau RefundFailed
```

---

## 📊 INTEGRARE CU ORDER CONTEXT

**Events consumate din Order Context:**
- `OrderPlaced` → Trigger pentru ProcessPaymentCommand
- `OrderCancelled` → Trigger pentru InitiateRefundCommand (dacă payment completed)

**Events publicate către Order Context:**
- `PaymentCompleted` → Order poate trece în status "Paid"
- `PaymentFailed` → Order poate trece în status "PaymentFailed"
- `RefundCompleted` → Order poate trece în status "Refunded"

---

## 🎯 BEST PRACTICES DDD APLICATE

✅ **Aggregate Boundaries:** Payment și Refund sunt agregări separate  
✅ **Invariants:** Fiecare agregare își menține propriile invarianți  
✅ **Domain Events:** Comunicare asincronă între bounded contexts  
✅ **Value Objects:** Immutabile pentru Money, TransactionInfo, etc.  
✅ **Domain Services:** Logică ce implică multiple agregări sau servicii externe  
✅ **Repository Pattern:** Persistență abstractizată  
✅ **Ubiquitous Language:** Terminologie consistentă (Payment, Refund, nu Transaction)  

