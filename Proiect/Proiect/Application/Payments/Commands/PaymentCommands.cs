// ═══════════════════════════════════════════════════════════════════════════════
// 📨 PAYMENT COMMANDS - APPLICATION LAYER
// ═══════════════════════════════════════════════════════════════════════════════

namespace Proiect.Application.Payments.Commands;

using Proiect.Domain.Payments;

// ═══════════════════════════════════════════════════════════════════════════════
// PAYMENT COMMANDS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// 🎯 COMANDĂ 1: Procesează o plată pentru o comandă
/// Declanșează evenimentul: PaymentCompleted
/// </summary>
public record ProcessPaymentCommand(
    Guid OrderId,
    decimal Amount,
    string Currency,
    PaymentMethod PaymentMethod,
    string? MaskedCardNumber = null,
    string? CardHolderName = null,
    string? ExpiryDate = null,
    string CustomerEmail = "");

/// <summary>
/// Result pentru ProcessPaymentCommand
/// </summary>
public record ProcessPaymentResult
{
    public bool Success { get; init; }
    public Guid? PaymentId { get; init; }
    public string? ErrorMessage { get; init; }
    public PaymentStatus? Status { get; init; }
    public string? TransactionId { get; init; }
}

/// <summary>
/// Comandă: Reîncearcă o plată eșuată
/// </summary>
public record RetryPaymentCommand(
    Guid PaymentId);

/// <summary>
/// Comandă: Anulează o plată
/// </summary>
public record CancelPaymentCommand(
    Guid PaymentId,
    string Reason);

// ═══════════════════════════════════════════════════════════════════════════════
// REFUND COMMANDS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// 🎯 COMANDĂ 2: Inițiază o rambursare
/// Declanșează evenimentul: RefundInitiated
/// </summary>
public record InitiateRefundCommand(
    Guid PaymentId,
    decimal RefundAmount,
    string Reason,
    RefundReasonCategory ReasonCategory,
    string RequestedBy);

/// <summary>
/// Result pentru InitiateRefundCommand
/// </summary>
public record InitiateRefundResult
{
    public bool Success { get; init; }
    public Guid? RefundId { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 🎯 COMANDĂ 3: Completează o rambursare
/// Declanșează evenimentul: RefundCompleted
/// </summary>
public record CompleteRefundCommand(
    Guid RefundId,
    string TransactionId,
    string AuthorizationCode,
    string GatewayResponse);

/// <summary>
/// Result pentru CompleteRefundCommand
/// </summary>
public record CompleteRefundResult
{
    public bool Success { get; init; }
    public Guid? RefundId { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Comandă: Procesează un refund (start to finish)
/// Combinație de StartProcessing + Complete
/// </summary>
public record ProcessRefundCommand(
    Guid RefundId);

/// <summary>
/// Comandă: Anulează un refund
/// </summary>
public record CancelRefundCommand(
    Guid RefundId,
    string Reason);

// ═══════════════════════════════════════════════════════════════════════════════
// QUERY COMMANDS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Query: Obține detalii despre o plată
/// </summary>
public record GetPaymentByIdQuery(
    Guid PaymentId);

/// <summary>
/// Query: Obține plata pentru o comandă
/// </summary>
public record GetPaymentByOrderIdQuery(
    Guid OrderId);

/// <summary>
/// Query: Obține refund-urile pentru o plată
/// </summary>
public record GetRefundsByPaymentIdQuery(
    Guid PaymentId);

/// <summary>
/// Query: Verifică dacă o plată poate fi rambursată
/// </summary>
public record CanRefundPaymentQuery(
    Guid PaymentId,
    decimal Amount);
