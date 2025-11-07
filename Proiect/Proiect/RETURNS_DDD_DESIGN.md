# 🔄 BOUNDED CONTEXT: RETURNS MANAGEMENT

## 📋 Domain-Driven Design - Analiză Completă

Data: November 7, 2025

---

## 1️⃣ COMENZI ȘI EVENIMENTE

### Mapare Comenzi → Evenimente

| Comandă | Eveniment Rezultat | Descriere |
|---------|-------------------|-----------|
| `RequestReturn` | `ReturnRequested` | Clientul inițiază cererea de retur |
| `ApproveReturn` | `ReturnApproved` | Managerul aprobă cererea de retur |
| `ReceiveReturn` | `ReturnReceived` | Produsul returnat este primit în depozit |
| `AcceptReturn` | `ReturnAccepted` | Returul este acceptat final și se procesează rambursarea |
| `RejectReturn` | `ReturnRejected` | Returul este respins (opțional) |

---

## 2️⃣ AGREGĂRI (AGGREGATES)

### Agregatul Principal: **Return**

**Responsabilități:**
- Gestionează ciclul de viață al unui retur
- Aplică reguli de business pentru retururi
- Menține starea consistentă a returului
- Generează evenimente de domeniu

**Entități Componente:**
- `Return` (Aggregate Root)
- `ReturnItem` (Entity)
- `ReturnPolicy` (Value Object)

---

## 3️⃣ REGULI DE VALIDARE

### 3.1 RequestReturn Command

**Validări:**
1. ✅ Comanda originală trebuie să existe
2. ✅ Comanda trebuie să fie în status "Delivered" sau "Paid"
3. ✅ Perioada de retur nu a expirat (ex: max 14 zile de la livrare)
4. ✅ Cantitatea returnată ≤ cantitatea comandată
5. ✅ Motivul returului este valid și specificat
6. ✅ Clientul este proprietarul comenzii
7. ✅ Produsele pot fi returnate (nu sunt produse de tip "non-returnable")

**Reguli de Business:**
- Period de retur standard: 14 zile
- Period extins pentru anumite categorii: 30 zile
- Produsele personalizate nu pot fi returnate

### 3.2 ApproveReturn Command

**Validări:**
1. ✅ Returul există și este în status "Requested"
2. ✅ Utilizatorul are permisiuni de aprobare
3. ✅ Politica de retur permite aprobarea
4. ✅ Motivul este considerat valid

**Reguli de Business:**
- Doar managerii pot aproba retururi
- Retururi peste o anumită valoare necesită aprobare suplimentară

### 3.3 ReceiveReturn Command

**Validări:**
1. ✅ Returul este în status "Approved"
2. ✅ Cantitatea primită ≤ cantitatea aprobată
3. ✅ Starea produsului este documentată (Intact/Damaged/Opened)
4. ✅ Cod de tracking valid (dacă există)

**Reguli de Business:**
- Se verifică starea fizică a produselor
- Produsele deteriorate pot afecta rambursarea

### 3.4 AcceptReturn Command

**Validări:**
1. ✅ Returul este în status "Received"
2. ✅ Toate produsele au fost inspectate
3. ✅ Metoda de rambursare este validă
4. ✅ Contul/cardul pentru rambursare există

**Reguli de Business:**
- Rambursarea se face prin aceeași metodă ca plata originală
- Taxa de restocking poate fi aplicată (ex: 10% pentru produse deschise)

---

## 4️⃣ INVARIANȚI

### Return Aggregate Invariants

#### Invariant 1: Status Progression
```
Requested → Approved → Received → Accepted
                    ↘ Rejected ↙
```
**Regula:** Statusul poate avansa doar în ordine, nu poate sări pași sau reveni.

#### Invariant 2: Cantitate
```
∀ item ∈ ReturnItems: 
    0 < item.Quantity ≤ item.OriginalOrderQuantity
```
**Regula:** Cantitatea returnată trebuie să fie pozitivă și nu poate depăși cantitatea comandată.

#### Invariant 3: Perioada de Retur
```
CurrentDate ≤ OrderDeliveryDate + ReturnPeriodDays
```
**Regula:** Cererea de retur trebuie făcută în perioada permisă.

#### Invariant 4: Valoare Totală
```
Return.TotalAmount = Σ(ReturnItem.Quantity × ReturnItem.UnitPrice) - RestockingFee
```
**Regula:** Valoarea totală a returului trebuie calculată corect.

#### Invariant 5: Unicitate
```
Un Order poate avea maxim un Return activ per produs
```
**Regula:** Nu se pot crea retururi duplicate pentru același produs din aceeași comandă.

#### Invariant 6: Refund Calculation
```
RefundAmount ≤ Return.TotalAmount ≤ OriginalOrder.TotalAmount
```
**Regula:** Rambursarea nu poate depăși valoarea returului sau a comenzii originale.

---

## 5️⃣ DOMAIN SERVICES

### ReturnEligibilityService
- Verifică dacă o comandă este eligibilă pentru retur
- Calculează perioada de retur rămasă
- Verifică politica de retur pentru categoria de produs

### RefundCalculationService
- Calculează suma de rambursat
- Aplică taxe de restocking
- Determină metoda de rambursare

### ReturnPolicyService
- Încarcă și aplică politici de retur
- Verifică excepții și cazuri speciale

---

## 6️⃣ INTEGRARE CU ALTE BOUNDED CONTEXTS

### Dependencies (Upstream)
- **ORDER MANAGEMENT**: Verifică status comandă, obține detalii comandă
- **PAYMENT**: Procesează rambursarea
- **INVENTORY**: Actualizează stocul cu produsele returnate

### Events Published (Downstream)
- `ReturnRequested` → Notificare către Customer Service
- `ReturnApproved` → Notificare către Warehouse
- `ReturnReceived` → Update Inventory
- `ReturnAccepted` → Trigger Refund în Payment Context

---

## 7️⃣ POLICY & CONFIGURATION

### Return Policy Parameters
```csharp
- StandardReturnPeriodDays: 14
- ExtendedReturnPeriodDays: 30
- RestockingFeePercentage: 10%
- MinimumReturnValue: 10 RON
- MaximumReturnValue: 10,000 RON (fără aprobare specială)
- NonReturnableCategories: ["Digital Products", "Perishables", "Custom Made"]
```

---

## 8️⃣ UBIQUITOUS LANGUAGE

### Termeni Cheie
- **Return Request**: Cerere de retur inițiată de client
- **Return Authorization**: Aprobare pentru returnarea produselor
- **RMA (Return Merchandise Authorization)**: Cod unic pentru tracking-ul returului
- **Restocking Fee**: Taxă aplicată pentru reprocesarea produselor
- **Refund**: Rambursare financiară către client
- **Return Window**: Perioada în care se poate face returul
- **Product Condition**: Starea produsului returnat (Intact/Opened/Damaged)

---

## 9️⃣ SCENARII DE BUSINESS

### Scenario 1: Happy Path
1. Client solicită retur pentru produs livrat acum 5 zile
2. Manager aprobă returul
3. Client trimite produsul înapoi
4. Warehouse primește și inspectează produsul (stare intactă)
5. Returul este acceptat
6. Se procesează rambursarea completă

### Scenario 2: Partial Refund
1. Client returnează produs deschis
2. Se aplică taxă de restocking 10%
3. Rambursare parțială

### Scenario 3: Rejection
1. Client solicită retur după 20 zile (peste perioada standard)
2. Returul este respins automat
3. Client este notificat

---

Această documentație oferă o bază solidă pentru implementarea bounded context-ului RETURNS folosind principiile DDD.

