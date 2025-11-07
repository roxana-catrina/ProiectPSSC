# 🚀 RETURNS MANAGEMENT - QUICK START GUIDE

## 📋 Start Rapid - 5 Minute Setup

### Pas 1: Structura Fișierelor

Toate fișierele au fost create în:
```
Proiect/
├── Domain/Returns/
│   ├── Return.cs                 ✅ Aggregate Root
│   ├── Events/DomainEvents.cs    ✅ 6 evenimente
│   └── Services/ReturnDomainServices.cs ✅ 4 servicii
├── Application/Returns/Commands/
│   ├── ReturnCommands.cs         ✅ Comenzi + DTOs
│   └── Handlers/ReturnCommandHandlers.cs ✅ Handlers
├── Infrastructure/Persistence/
│   └── ReturnRepository.cs       ✅ Repository
├── Controllers/
│   └── ReturnsController.cs      ✅ API Controller
└── Documentație/
    ├── RETURNS_DDD_DESIGN.md
    ├── RETURNS_API_EXAMPLES.http
    ├── RETURNS_IMPLEMENTATION_SUMMARY.md
    └── RETURNS_FINAL_SUMMARY.md
```

### Pas 2: Înregistrare Servicii în Program.cs

Adaugă următorul cod în `Program.cs`:

```csharp
// RETURNS MANAGEMENT SERVICES
builder.Services.AddScoped<ReturnsManagement.Infrastructure.Persistence.IReturnRepository, 
                           ReturnsManagement.Infrastructure.Persistence.ReturnRepository>();
builder.Services.AddScoped<ReturnsManagement.Application.Returns.Commands.Handlers.IOrderService, 
                           ReturnsManagement.Infrastructure.Persistence.MockOrderService>();
builder.Services.AddScoped<ReturnsManagement.Domain.Returns.Services.ReturnEligibilityService>();
builder.Services.AddScoped<ReturnsManagement.Domain.Returns.Services.RefundCalculationService>();
builder.Services.AddScoped<ReturnsManagement.Domain.Returns.Services.ReturnPolicyService>();
builder.Services.AddScoped<ReturnsManagement.Domain.Returns.Services.ReturnAuthorizationService>();

// MediatR (dacă nu e deja înregistrat)
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
```

### Pas 3: Run API

```bash
cd Proiect
dotnet run
```

API va fi disponibil la: `https://localhost:7001` (sau alt port specificat)

---

## 🎯 Testare Rapidă - Scenariul "Happy Path"

### 1️⃣ Creează un Return Request

**POST** `https://localhost:7001/api/returns/request`

```json
{
  "orderId": "11111111-1111-1111-1111-111111111111",
  "customerId": "22222222-2222-2222-2222-222222222222",
  "customerName": "Ion Popescu",
  "customerEmail": "ion.popescu@example.com",
  "items": [
    {
      "productId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "productName": "Laptop Dell XPS 15",
      "quantity": 1,
      "unitPrice": 299.99,
      "productCategory": "electronics"
    }
  ],
  "reason": "DefectiveProduct",
  "detailedDescription": "The laptop does not power on",
  "orderDeliveryDate": "2025-11-02T00:00:00Z",
  "productCategory": "electronics"
}
```

**Răspuns Așteptat:**
```json
{
  "success": true,
  "returnId": "...",
  "rmaCode": "RMA-20251107-...",
  "message": "Return request created successfully...",
  "validationErrors": []
}
```

### 2️⃣ Aprobă Returul

**POST** `https://localhost:7001/api/returns/{returnId}/approve`

```json
{
  "approvedBy": "99999999-9999-9999-9999-999999999999",
  "approverName": "Manager George",
  "approvalNotes": "Approved - defective product confirmed",
  "applyRestockingFee": false,
  "approverRole": "Manager"
}
```

### 3️⃣ Primește Produsul

**POST** `https://localhost:7001/api/returns/{returnId}/receive`

```json
{
  "receivedBy": "88888888-8888-8888-8888-888888888888",
  "receiverName": "Warehouse Staff Andrei",
  "receivedItems": [
    {
      "productId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "quantityReceived": 1,
      "condition": "Defective",
      "conditionNotes": "Product does not power on",
      "acceptableForResale": false
    }
  ],
  "trackingNumber": "TRACK-RET-123456",
  "warehouseLocation": "WAREHOUSE-A",
  "inspectionNotes": "Defect confirmed"
}
```

### 4️⃣ Acceptă Returul și Procesează Rambursarea

**POST** `https://localhost:7001/api/returns/{returnId}/accept`

```json
{
  "acceptedBy": "77777777-7777-7777-7777-777777777777",
  "accepterName": "Finance Manager Ana",
  "refundMethod": "OriginalPaymentMethod",
  "refundReference": "REFUND-2025-11-001",
  "notes": "Full refund approved",
  "inventoryUpdated": true
}
```

### 5️⃣ Verifică Statusul

**GET** `https://localhost:7001/api/returns/{returnId}`

---

## 📊 Date de Test Pre-configurate

### Comenzi Disponibile în MockOrderService:

| OrderId | CustomerId | Status | Delivery Date | Eligibil Retur |
|---------|-----------|--------|---------------|----------------|
| `11111111-1111-1111-1111-111111111111` | `22222222-2222-2222-2222-222222222222` | Delivered | Acum 5 zile | ✅ DA |
| `33333333-3333-3333-3333-333333333333` | `44444444-4444-4444-4444-444444444444` | Delivered | Acum 20 zile | ✅ DA (la limită) |
| `55555555-5555-5555-5555-555555555555` | `66666666-6666-6666-6666-666666666666` | Delivered | Acum 30 zile | ❌ NU (expirat) |

---

## 🔍 Debugging & Troubleshooting

### Verifică că Serviciile Sunt Înregistrate

Adaugă breakpoint în `ReturnsController` constructor și verifică că toate dependencies sunt injectate.

### Verifică Namespaces

Dacă apar erori de compilare, asigură-te că toate namespace-urile sunt:
```csharp
ReturnsManagement.Domain.Returns
ReturnsManagement.Domain.Returns.Events
ReturnsManagement.Domain.Returns.Services
ReturnsManagement.Application.Returns.Commands
ReturnsManagement.Application.Returns.Commands.Handlers
ReturnsManagement.Infrastructure.Persistence
ReturnsManagement.Controllers
```

### Logs

Controller-ul logează toate operațiunile. Verifică output-ul console:
```
Processing return request for order {OrderId}
Return request created: {ReturnId}, RMA: {RmaCode}
Processing approval for return {ReturnId}
...
```

---

## 📖 Documentație Completă

Pentru detalii complete, consultă:

1. **RETURNS_DDD_DESIGN.md** - Design și arhitectură
2. **RETURNS_API_EXAMPLES.http** - 6+ scenarii de testare
3. **RETURNS_IMPLEMENTATION_SUMMARY.md** - Detalii implementare
4. **RETURNS_FINAL_SUMMARY.md** - Rezumat complet

---

## 🎓 Concepte Cheie Implementate

### Domain-Driven Design
- ✅ Aggregate Root (Return)
- ✅ Value Objects (Money, RmaCode, ReturnPolicy, ReturnWindow)
- ✅ Domain Events (6 evenimente)
- ✅ Domain Services (4 servicii)
- ✅ Factory Methods
- ✅ Invariants Protection

### Clean Architecture
- ✅ Domain Layer (business logic)
- ✅ Application Layer (use cases)
- ✅ Infrastructure Layer (persistence)
- ✅ Presentation Layer (API)

### CQRS
- ✅ Commands (RequestReturn, ApproveReturn, etc.)
- ✅ Queries (GetReturnStatus)
- ✅ Handlers (unul per command)

---

## 🚦 Status Flow

```
┌─────────────┐
│  REQUESTED  │ ◄── Client inițiază retur
└──────┬──────┘
       │ Approve
       ▼
┌─────────────┐
│  APPROVED   │ ◄── Manager aprobă
└──────┬──────┘
       │ Receive
       ▼
┌─────────────┐
│  RECEIVED   │ ◄── Warehouse primește produsul
└──────┬──────┘
       │ Accept
       ▼
┌─────────────┐
│  ACCEPTED   │ ◄── Finance procesează rambursarea
└─────────────┘
       │
       ▼
┌─────────────┐
│  COMPLETED  │ ◄── Rambursare finalizată
└─────────────┘

       Rejection Path:
       ┌──────────┐
       │ REJECTED │ ◄── Poate fi respins din REQUESTED sau RECEIVED
       └──────────┘
```

---

## 💡 Tips & Best Practices

1. **Folosește RMA Code** pentru tracking - formatul `RMA-YYYYMMDD-XXXXXXXX` este unic
2. **Verifică Return Window** - `DaysRemaining` îți arată câte zile mai are clientul
3. **Autorizare pe Nivele** - Retururi mari necesită nivel superior de aprobare
4. **Restocking Fee** - Aplicat automat pentru produse deschise (dacă politica permite)
5. **Product Condition** - Documentează întotdeauna starea produsului primit

---

## ❓ FAQ

**Q: Cum adaug o nouă politică de retur?**  
A: În `ReturnPolicyService.InitializeDefaultPolicies()` adaugă categoria și politica.

**Q: Cum schimb perioada de retur?**  
A: Modifică `ReturnPolicy` pentru categoria respectivă (default 14 zile, extended 30 zile).

**Q: Cum adaug un nou motiv de retur?**  
A: Adaugă în enum-ul `ReturnReason` și actualizează `IsReasonValid()` în `ReturnEligibilityService`.

**Q: Cum modific limitele de autorizare?**  
A: În `ReturnAuthorizationService`, modifică constantele `MANAGER_APPROVAL_LIMIT` și `SUPERVISOR_APPROVAL_LIMIT`.

---

## 🎉 Success!

Dacă ai urmat pașii de mai sus, ai acum un **sistem complet funcțional de management al retururilor**!

**Next Steps:**
1. ✅ Testează toate scenariile din `RETURNS_API_EXAMPLES.http`
2. ✅ Adaugă Unit Tests
3. ✅ Integrează cu EF Core pentru persistence reală
4. ✅ Adaugă sistem de notificări (email/SMS)

---

**Happy Coding! 🚀**

