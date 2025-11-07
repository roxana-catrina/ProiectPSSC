// ═══════════════════════════════════════════════════════════════════════════════
// 📦 BOUNDED CONTEXT: RETURNS MANAGEMENT - DOMAIN SERVICES
// ═══════════════════════════════════════════════════════════════════════════════
// Domain Services pentru logica de business complexă
// Data: November 7, 2025
// ═══════════════════════════════════════════════════════════════════════════════

using ReturnsManagement.Domain.Returns.Events;

namespace ReturnsManagement.Domain.Returns.Services;

// ═══════════════════════════════════════════════════════════════════════════════
// DOMAIN SERVICE: ReturnEligibilityService
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Service pentru verificarea eligibilității returului
/// Responsabilități:
/// - Verifică dacă o comandă poate fi returnată
/// - Calculează perioada de retur rămasă
/// - Aplică reguli speciale pentru diferite categorii de produse
/// </summary>
public class ReturnEligibilityService
{
    public record EligibilityResult(
        bool IsEligible,
        string Reason,
        int DaysRemaining,
        ReturnPolicy ApplicablePolicy
    );

    /// <summary>
    /// Verifică dacă o comandă este eligibilă pentru retur
    /// </summary>
    public EligibilityResult CheckEligibility(
        DateTime orderDeliveryDate,
        string productCategory,
        decimal orderAmount,
        bool isCustomProduct = false)
    {
        // Regula 1: Produsele personalizate nu pot fi returnate
        if (isCustomProduct)
        {
            return new EligibilityResult(
                false,
                "Custom-made products are not eligible for return",
                0,
                ReturnPolicy.NonReturnable()
            );
        }

        // Regula 2: Determinare politică bazată pe categorie
        var policy = DeterminePolicyByCategory(productCategory);

        // Regula 3: Verifică perioada de retur
        var returnWindow = new ReturnWindow(orderDeliveryDate, policy.ReturnPeriodDays);
        
        if (returnWindow.IsExpired)
        {
            return new EligibilityResult(
                false,
                $"Return window has expired. Last eligible date was {returnWindow.ExpirationDate:yyyy-MM-dd}",
                0,
                policy
            );
        }

        // Regula 4: Verifică valoarea minimă/maximă
        if (orderAmount < 10)
        {
            return new EligibilityResult(
                false,
                "Order amount is below minimum returnable value (10 RON)",
                returnWindow.DaysRemaining,
                policy
            );
        }

        return new EligibilityResult(
            true,
            "Order is eligible for return",
            returnWindow.DaysRemaining,
            policy
        );
    }

    /// <summary>
    /// Determină politica de retur bazată pe categoria produsului
    /// </summary>
    private ReturnPolicy DeterminePolicyByCategory(string category)
    {
        return category?.ToLower() switch
        {
            "electronics" => new ReturnPolicy(
                returnPeriodDays: 30,
                isReturnable: true,
                restockingFeePercentage: 10,
                requiresOriginalPackaging: true,
                policyDescription: "Extended return period for electronics with 10% restocking fee if opened"
            ),
            "clothing" => new ReturnPolicy(
                returnPeriodDays: 30,
                isReturnable: true,
                restockingFeePercentage: 0,
                requiresOriginalPackaging: false,
                policyDescription: "Extended return period for clothing items"
            ),
            "books" => new ReturnPolicy(
                returnPeriodDays: 14,
                isReturnable: true,
                restockingFeePercentage: 0,
                requiresOriginalPackaging: false,
                policyDescription: "Standard return period for books"
            ),
            "food" or "perishables" => ReturnPolicy.NonReturnable(),
            "digital" => ReturnPolicy.NonReturnable(),
            "custom" => ReturnPolicy.NonReturnable(),
            _ => ReturnPolicy.StandardPolicy()
        };
    }

    /// <summary>
    /// Verifică dacă un motiv de retur este valid pentru o anumită situație
    /// </summary>
    public bool IsReasonValid(ReturnReason reason, ReturnPolicy policy, ProductCondition? productCondition)
    {
        // Verifică dacă motivul este permis de politică
        if (!policy.AllowedReasons.Contains(reason))
            return false;

        // Reguli speciale pentru anumite motive
        return reason switch
        {
            ReturnReason.ChangedMind when productCondition == ProductCondition.Opened 
                => policy.RestockingFeePercentage > 0, // Permite doar dacă există taxă de restocking
            
            ReturnReason.DefectiveProduct or ReturnReason.DamagedInTransit 
                => true, // Întotdeauna valide
            
            ReturnReason.WrongItemReceived 
                => true, // Întotdeauna valid
            
            _ => true
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DOMAIN SERVICE: RefundCalculationService
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Service pentru calcularea rambursărilor
/// Responsabilități:
/// - Calculează suma exactă de rambursat
/// - Aplică taxe de restocking
/// - Determină metoda de rambursare
/// - Gestionează cazuri speciale
/// </summary>
public class RefundCalculationService
{
    public record RefundCalculation(
        Money OriginalAmount,
        Money RestockingFee,
        Money ShippingFeeDeduction,
        Money FinalRefundAmount,
        RefundMethod RecommendedMethod,
        string CalculationNotes
    );

    /// <summary>
    /// Calculează suma de rambursat pentru un retur
    /// </summary>
    public RefundCalculation CalculateRefund(
        Money totalAmount,
        ReturnPolicy policy,
        List<(ProductCondition Condition, int Quantity, Money UnitPrice)> items,
        ReturnReason reason,
        Money originalShippingCost,
        string originalPaymentMethod)
    {
        var restockingFee = Money.Zero(totalAmount.Currency);
        var shippingFeeDeduction = Money.Zero(totalAmount.Currency);
        var notes = new List<string>();

        // Regula 1: Taxa de restocking
        bool shouldApplyRestockingFee = ShouldApplyRestockingFee(items, reason, policy);
        
        if (shouldApplyRestockingFee)
        {
            restockingFee = totalAmount.ApplyPercentage(policy.RestockingFeePercentage);
            notes.Add($"Restocking fee ({policy.RestockingFeePercentage}%) applied: {restockingFee}");
        }

        // Regula 2: Deducere costuri de transport
        // Dacă clientul și-a schimbat decizia, nu rambursăm transportul
        if (reason == ReturnReason.ChangedMind)
        {
            shippingFeeDeduction = originalShippingCost;
            notes.Add($"Shipping cost not refunded for 'Changed Mind' returns: {originalShippingCost}");
        }

        // Regula 3: Ajustări pentru produse deteriorate
        var damageDeduction = CalculateDamageDeduction(items, totalAmount.Currency);
        if (damageDeduction.Amount > 0)
        {
            notes.Add($"Deduction for damaged items: {damageDeduction}");
        }

        // Calcul final
        var finalAmount = totalAmount
            .Subtract(restockingFee)
            .Subtract(shippingFeeDeduction)
            .Subtract(damageDeduction);

        // Determină metoda de rambursare
        var refundMethod = DetermineRefundMethod(originalPaymentMethod, finalAmount);

        return new RefundCalculation(
            totalAmount,
            restockingFee,
            shippingFeeDeduction.Add(damageDeduction),
            finalAmount,
            refundMethod,
            string.Join("; ", notes)
        );
    }

    /// <summary>
    /// Determină dacă se aplică taxa de restocking
    /// </summary>
    private bool ShouldApplyRestockingFee(
        List<(ProductCondition Condition, int Quantity, Money UnitPrice)> items,
        ReturnReason reason,
        ReturnPolicy policy)
    {
        // Nu se aplică taxă pentru produse defecte sau deteriorate în transport
        if (reason == ReturnReason.DefectiveProduct || 
            reason == ReturnReason.DamagedInTransit ||
            reason == ReturnReason.WrongItemReceived)
        {
            return false;
        }

        // Aplică taxă dacă produsul a fost deschis și politica o cere
        if (policy.RestockingFeePercentage > 0 && 
            items.Any(i => i.Condition == ProductCondition.Opened || i.Condition == ProductCondition.Used))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculează deducerea pentru produse deteriorate
    /// Produsele foarte deteriorate pot avea o deducere suplimentară
    /// </summary>
    private Money CalculateDamageDeduction(
        List<(ProductCondition Condition, int Quantity, Money UnitPrice)> items,
        string currency)
    {
        var deduction = Money.Zero(currency);

        foreach (var (condition, quantity, unitPrice) in items)
        {
            var itemValue = new Money(unitPrice.Amount * quantity, currency);
            
            var damagePercentage = condition switch
            {
                ProductCondition.Damaged => 50m,  // 50% deducere pentru produse deteriorate
                ProductCondition.Used => 20m,     // 20% deducere pentru produse folosite
                _ => 0m
            };

            if (damagePercentage > 0)
            {
                deduction = deduction.Add(itemValue.ApplyPercentage(damagePercentage));
            }
        }

        return deduction;
    }

    /// <summary>
    /// Determină metoda optimă de rambursare
    /// </summary>
    private RefundMethod DetermineRefundMethod(string originalPaymentMethod, Money refundAmount)
    {
        // Regula: Rambursare prin aceeași metodă ca plata originală
        return originalPaymentMethod?.ToLower() switch
        {
            "card" or "credit_card" or "debit_card" => RefundMethod.OriginalPaymentMethod,
            "bank_transfer" => RefundMethod.BankTransfer,
            "cash" when refundAmount.Amount <= 500 => RefundMethod.Cash,
            "cash" => RefundMethod.BankTransfer, // Sume mari nu se rambursează cash
            _ => RefundMethod.OriginalPaymentMethod
        };
    }

    /// <summary>
    /// Validează că suma de rambursat este corectă
    /// </summary>
    public bool ValidateRefundAmount(Money refundAmount, Money originalAmount)
    {
        if (refundAmount.Currency != originalAmount.Currency)
            return false;

        if (refundAmount.Amount < 0)
            return false;

        if (refundAmount.Amount > originalAmount.Amount)
            return false;

        return true;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DOMAIN SERVICE: ReturnPolicyService
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Service pentru gestionarea politicilor de retur
/// Responsabilități:
/// - Încarcă politici de retur
/// - Aplică excepții și cazuri speciale
/// - Gestionează politici personalizate pentru clienți VIP
/// </summary>
public class ReturnPolicyService
{
    private readonly Dictionary<string, ReturnPolicy> _categoryPolicies;
    private readonly Dictionary<Guid, ReturnPolicy> _customerSpecificPolicies;

    public ReturnPolicyService()
    {
        _categoryPolicies = InitializeDefaultPolicies();
        _customerSpecificPolicies = new Dictionary<Guid, ReturnPolicy>();
    }

    /// <summary>
    /// Obține politica de retur aplicabilă pentru un produs
    /// </summary>
    public ReturnPolicy GetPolicyForProduct(
        string productCategory,
        Guid? customerId = null,
        bool isVipCustomer = false)
    {
        // Prioritate 1: Politică specifică pentru client
        if (customerId.HasValue && _customerSpecificPolicies.TryGetValue(customerId.Value, out var customerPolicy))
        {
            return customerPolicy;
        }

        // Prioritate 2: Politică pentru clienți VIP
        if (isVipCustomer)
        {
            return GetVipPolicy();
        }

        // Prioritate 3: Politică pentru categorie
        if (_categoryPolicies.TryGetValue(productCategory?.ToLower() ?? "default", out var categoryPolicy))
        {
            return categoryPolicy;
        }

        // Default: Politică standard
        return ReturnPolicy.StandardPolicy();
    }

    /// <summary>
    /// Politică extinsă pentru clienți VIP
    /// </summary>
    private ReturnPolicy GetVipPolicy()
    {
        return new ReturnPolicy(
            returnPeriodDays: 60,
            isReturnable: true,
            restockingFeePercentage: 0,
            requiresOriginalPackaging: false,
            policyDescription: "Extended VIP return policy - 60 days, no restocking fee"
        );
    }

    /// <summary>
    /// Verifică dacă există excepții pentru o anumită comandă
    /// </summary>
    public bool HasException(Guid orderId, out string exceptionReason)
    {
        // Aici ar putea fi logică pentru verificarea excepțiilor
        // Ex: comenzi în perioada de sărbători, promoții speciale, etc.
        exceptionReason = string.Empty;
        return false;
    }

    /// <summary>
    /// Aplică o excepție pentru o comandă specifică
    /// </summary>
    public ReturnPolicy ApplyException(ReturnPolicy basePolicy, string exceptionType)
    {
        return exceptionType?.ToLower() switch
        {
            "holiday_extended" => new ReturnPolicy(
                basePolicy.ReturnPeriodDays + 14,
                basePolicy.IsReturnable,
                basePolicy.RestockingFeePercentage,
                basePolicy.AllowedReasons,
                basePolicy.RequiresOriginalPackaging,
                "Holiday season extended return period"
            ),
            "defect_reported" => new ReturnPolicy(
                90, // 90 zile pentru produse cu defecte raportate
                true,
                0,
                basePolicy.AllowedReasons,
                false,
                "Extended period for reported defects"
            ),
            _ => basePolicy
        };
    }

    /// <summary>
    /// Inițializează politicile default pentru categorii
    /// </summary>
    private Dictionary<string, ReturnPolicy> InitializeDefaultPolicies()
    {
        return new Dictionary<string, ReturnPolicy>
        {
            ["electronics"] = new ReturnPolicy(30, true, 10, requiresOriginalPackaging: true),
            ["clothing"] = new ReturnPolicy(30, true, 0),
            ["shoes"] = new ReturnPolicy(30, true, 0),
            ["books"] = new ReturnPolicy(14, true, 0),
            ["toys"] = new ReturnPolicy(30, true, 0),
            ["home_appliances"] = new ReturnPolicy(30, true, 15, requiresOriginalPackaging: true),
            ["beauty"] = new ReturnPolicy(14, true, 0),
            ["food"] = ReturnPolicy.NonReturnable(),
            ["digital"] = ReturnPolicy.NonReturnable(),
            ["custom"] = ReturnPolicy.NonReturnable(),
            ["default"] = ReturnPolicy.StandardPolicy()
        };
    }

    /// <summary>
    /// Setează o politică personalizată pentru un client
    /// </summary>
    public void SetCustomerSpecificPolicy(Guid customerId, ReturnPolicy policy)
    {
        _customerSpecificPolicies[customerId] = policy;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DOMAIN SERVICE: ReturnAuthorizationService
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Service pentru autorizarea retururilor
/// Responsabilități:
/// - Verifică dacă un utilizator poate aproba/respinge retururi
/// - Aplică limite de autorizare bazate pe valoare
/// - Gestionează escaladări pentru retururi de valoare mare
/// </summary>
public class ReturnAuthorizationService
{
    private const decimal MANAGER_APPROVAL_LIMIT = 5000m;
    private const decimal SUPERVISOR_APPROVAL_LIMIT = 10000m;

    public record AuthorizationResult(
        bool IsAuthorized,
        string Reason,
        bool RequiresEscalation,
        string EscalationLevel
    );

    public enum UserRole
    {
        CustomerService,
        Manager,
        Supervisor,
        Administrator
    }

    /// <summary>
    /// Verifică dacă un utilizator poate aproba un retur
    /// </summary>
    public AuthorizationResult CanApproveReturn(
        UserRole userRole,
        Money returnAmount,
        ReturnReason reason)
    {
        // Administratorii pot aproba orice
        if (userRole == UserRole.Administrator)
        {
            return new AuthorizationResult(true, "Administrator has unlimited approval rights", false, null);
        }

        // Retururi pentru produse defecte pot fi aprobate de orice rol
        if (reason == ReturnReason.DefectiveProduct || reason == ReturnReason.DamagedInTransit)
        {
            return new AuthorizationResult(true, "Defective products can be approved by any authorized personnel", false, null);
        }

        // Verifică limite bazate pe rol
        return userRole switch
        {
            UserRole.CustomerService when returnAmount.Amount <= 1000m 
                => new AuthorizationResult(true, "Within customer service approval limit", false, null),
            
            UserRole.CustomerService 
                => new AuthorizationResult(false, "Amount exceeds customer service limit", true, "Manager"),
            
            UserRole.Manager when returnAmount.Amount <= MANAGER_APPROVAL_LIMIT 
                => new AuthorizationResult(true, "Within manager approval limit", false, null),
            
            UserRole.Manager 
                => new AuthorizationResult(false, "Amount exceeds manager limit", true, "Supervisor"),
            
            UserRole.Supervisor when returnAmount.Amount <= SUPERVISOR_APPROVAL_LIMIT 
                => new AuthorizationResult(true, "Within supervisor approval limit", false, null),
            
            UserRole.Supervisor 
                => new AuthorizationResult(false, "Amount exceeds supervisor limit", true, "Administrator"),
            
            _ => new AuthorizationResult(false, "Invalid role or authorization not found", false, null)
        };
    }

    /// <summary>
    /// Verifică dacă un utilizator poate respinge un retur
    /// </summary>
    public bool CanRejectReturn(UserRole userRole)
    {
        // Doar manageri și superiori pot respinge retururi
        return userRole >= UserRole.Manager;
    }
}

