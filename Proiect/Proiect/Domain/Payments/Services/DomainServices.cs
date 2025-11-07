// ═══════════════════════════════════════════════════════════════════════════════
// 🛡️ PAYMENT DOMAIN SERVICES
// ═══════════════════════════════════════════════════════════════════════════════

namespace Proiect.Domain.Payments.Services;

using Proiect.Domain.Payments;
using Proiect.Infrastructure.Persistence;

// ═══════════════════════════════════════════════════════════════════════════════
// EXTERNAL GATEWAY SERVICES (Infrastructure)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Result object pentru procesarea plăților prin gateway
/// </summary>
public record PaymentGatewayResult
{
    public bool Success { get; init; }
    public string? TransactionId { get; init; }
    public string? AuthorizationCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? GatewayResponse { get; init; }
}

/// <summary>
/// Result object pentru procesarea refund-urilor prin gateway
/// </summary>
public record RefundGatewayResult
{
    public bool Success { get; init; }
    public string? TransactionId { get; init; }
    public string? AuthorizationCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? GatewayResponse { get; init; }
}

/// <summary>
/// Interface pentru serviciul de gateway de plată
/// Implementarea va fi în Infrastructure layer
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>
    /// Procesează o plată prin gateway-ul extern (Stripe, PayPal, etc.)
    /// </summary>
    Task<PaymentGatewayResult> ProcessPaymentAsync(Payment payment);
    
    /// <summary>
    /// Procesează o rambursare prin gateway-ul extern
    /// </summary>
    Task<RefundGatewayResult> ProcessRefundAsync(Refund refund);
    
    /// <summary>
    /// Verifică status-ul unei tranzacții
    /// </summary>
    Task<PaymentGatewayResult> CheckTransactionStatusAsync(string transactionId);
}

// ═══════════════════════════════════════════════════════════════════════════════
// FRAUD DETECTION SERVICE
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Result object pentru verificarea fraudelor
/// </summary>
public record FraudCheckResult
{
    public bool IsSuspicious { get; init; }
    public FraudRiskLevel RiskLevel { get; init; }
    public List<string> Reasons { get; init; } = new();
    public bool ShouldBlock { get; init; }
}

public enum FraudRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Interface pentru serviciul de detectare a fraudelor
/// </summary>
public interface IFraudDetectionService
{
    /// <summary>
    /// Verifică dacă o plată este suspectă de fraudă
    /// Verificări:
    /// - Multiple plăți rapide de la același email
    /// - Sume neobișnuit de mari
    /// - Pattern-uri suspecte
    /// </summary>
    Task<FraudCheckResult> CheckPaymentAsync(Payment payment, string customerEmail);
}

/// <summary>
/// Implementare simplă a serviciului de detectare a fraudelor
/// În producție ar trebui integrat cu servicii externe (Stripe Radar, etc.)
/// </summary>
public class FraudDetectionService : IFraudDetectionService
{
    private readonly IPaymentRepository _paymentRepository;
    
    public FraudDetectionService(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }
    
    public async Task<FraudCheckResult> CheckPaymentAsync(Payment payment, string customerEmail)
    {
        var reasons = new List<string>();
        var riskLevel = FraudRiskLevel.Low;
        
        // Verificare 1: Sumă suspicioasă (> 10,000)
        if (payment.Amount.Amount > 10000)
        {
            reasons.Add("Large payment amount detected");
            riskLevel = FraudRiskLevel.Medium;
        }
        
        // Verificare 2: Multiple plăți rapide (> 5 plăți în ultimele 10 minute)
        var recentPayments = await _paymentRepository.GetRecentPaymentsByEmailAsync(
            customerEmail, 
            TimeSpan.FromMinutes(10));
        
        if (recentPayments.Count > 5)
        {
            reasons.Add("Multiple rapid payments detected");
            riskLevel = FraudRiskLevel.High;
        }
        
        // Verificare 3: Sumă foarte mare (> 50,000) → blocare automată
        if (payment.Amount.Amount > 50000)
        {
            reasons.Add("Critical amount threshold exceeded");
            riskLevel = FraudRiskLevel.Critical;
            
            return new FraudCheckResult
            {
                IsSuspicious = true,
                RiskLevel = riskLevel,
                Reasons = reasons,
                ShouldBlock = true
            };
        }
        
        return new FraudCheckResult
        {
            IsSuspicious = reasons.Any(),
            RiskLevel = riskLevel,
            Reasons = reasons,
            ShouldBlock = false
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// PAYMENT DOMAIN SERVICE
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Domain Service pentru logică de business complexă care implică multiple agregări
/// </summary>
public interface IPaymentDomainService
{
    /// <summary>
    /// Verifică dacă o plată poate fi rambursată
    /// </summary>
    Task<bool> CanRefundPaymentAsync(Guid paymentId, decimal requestedAmount);
    
    /// <summary>
    /// Calculează suma totală rambursată pentru o plată
    /// </summary>
    Task<decimal> GetTotalRefundedAmountAsync(Guid paymentId);
    
    /// <summary>
    /// Verifică dacă există deja un payment completat pentru un order
    /// </summary>
    Task<bool> HasCompletedPaymentForOrderAsync(Guid orderId);
}

/// <summary>
/// Implementare Payment Domain Service
/// </summary>
public class PaymentDomainService : IPaymentDomainService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRefundRepository _refundRepository;
    
    public PaymentDomainService(
        IPaymentRepository paymentRepository,
        IRefundRepository refundRepository)
    {
        _paymentRepository = paymentRepository;
        _refundRepository = refundRepository;
    }
    
    public async Task<bool> CanRefundPaymentAsync(Guid paymentId, decimal requestedAmount)
    {
        // 1. Verifică dacă payment-ul există și este completat
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null || !payment.CanBeRefunded())
        {
            return false;
        }
        
        // 2. Calculează suma totală rambursată deja
        var totalRefunded = await GetTotalRefundedAmountAsync(paymentId);
        
        // 3. Verifică dacă suma solicitată + suma rambursată <= suma originală
        var remainingAmount = payment.Amount.Amount - totalRefunded;
        
        return requestedAmount > 0 && requestedAmount <= remainingAmount;
    }
    
    public async Task<decimal> GetTotalRefundedAmountAsync(Guid paymentId)
    {
        var refunds = await _refundRepository.GetByPaymentIdAsync(paymentId);
        
        // Sumează doar refund-urile completate și în curs de procesare
        return refunds
            .Where(r => r.Status == RefundStatus.Completed || r.Status == RefundStatus.Processing)
            .Sum(r => r.RefundAmount.Amount);
    }
    
    public async Task<bool> HasCompletedPaymentForOrderAsync(Guid orderId)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
        return payment != null && payment.Status == PaymentStatus.Completed;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// MOCK IMPLEMENTATION - PAYMENT GATEWAY SERVICE
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Mock implementation pentru testing/development
/// În producție ar trebui înlocuit cu integrare reală (Stripe, PayPal, etc.)
/// </summary>
public class MockPaymentGatewayService : IPaymentGatewayService
{
    private readonly Random _random = new();
    
    public async Task<PaymentGatewayResult> ProcessPaymentAsync(Payment payment)
    {
        // Simulăm latență de rețea
        await Task.Delay(500);
        
        // Simulăm 10% rată de eșec
        var success = _random.Next(100) >= 10;
        
        if (success)
        {
            return new PaymentGatewayResult
            {
                Success = true,
                TransactionId = $"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                AuthorizationCode = $"AUTH-{_random.Next(100000, 999999)}",
                GatewayResponse = "Payment processed successfully"
            };
        }
        else
        {
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Insufficient funds",
                GatewayResponse = "Payment declined by issuing bank"
            };
        }
    }
    
    public async Task<RefundGatewayResult> ProcessRefundAsync(Refund refund)
    {
        // Simulăm latență de rețea
        await Task.Delay(500);
        
        // Refund-urile au rată de succes mai mare (95%)
        var success = _random.Next(100) >= 5;
        
        if (success)
        {
            return new RefundGatewayResult
            {
                Success = true,
                TransactionId = $"RFD-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                AuthorizationCode = $"AUTH-{_random.Next(100000, 999999)}",
                GatewayResponse = "Refund processed successfully"
            };
        }
        else
        {
            return new RefundGatewayResult
            {
                Success = false,
                ErrorMessage = "Original transaction not found",
                GatewayResponse = "Refund failed - contact support"
            };
        }
    }
    
    public async Task<PaymentGatewayResult> CheckTransactionStatusAsync(string transactionId)
    {
        await Task.Delay(200);
        
        return new PaymentGatewayResult
        {
            Success = true,
            TransactionId = transactionId,
            GatewayResponse = "Transaction status: Completed"
        };
    }
}
