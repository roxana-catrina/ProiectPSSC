// ═══════════════════════════════════════════════════════════════════════════════
// 🎯 PAYMENT DOMAIN EVENTS
// ═══════════════════════════════════════════════════════════════════════════════

namespace Proiect.Domain.Payments;

// ═══════════════════════════════════════════════════════════════════════════════
// PAYMENT EVENTS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eveniment: Payment a fost creat
/// </summary>
public record PaymentCreated(
    Guid PaymentId,
    Guid OrderId,
    Money Amount);

/// <summary>
/// Eveniment: Procesarea payment-ului a început
/// </summary>
public record PaymentProcessingStarted(
    Guid PaymentId,
    Guid OrderId);

/// <summary>
/// 🎯 EVENIMENT PRINCIPAL: Payment a fost completat cu succes
/// Acest eveniment declanșează actualizarea Order-ului în status "Paid"
/// </summary>
public record PaymentCompleted(
    Guid PaymentId,
    Guid OrderId,
    Money Amount,
    string TransactionId,
    DateTime CompletedAt);

/// <summary>
/// Eveniment: Payment a eșuat
/// </summary>
public record PaymentFailed(
    Guid PaymentId,
    Guid OrderId,
    string FailureReason);

/// <summary>
/// Eveniment: Payment se reîncearcă după un eșec
/// </summary>
public record PaymentRetrying(
    Guid PaymentId,
    Guid OrderId,
    int RetryCount,
    string FailureReason);

/// <summary>
/// Eveniment: Payment a fost anulat
/// </summary>
public record PaymentCancelled(
    Guid PaymentId,
    Guid OrderId,
    string Reason);

// ═══════════════════════════════════════════════════════════════════════════════
// REFUND EVENTS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// 🎯 EVENIMENT PRINCIPAL: Refund a fost inițiat
/// </summary>
public record RefundInitiated(
    Guid RefundId,
    Guid PaymentId,
    Guid OrderId,
    Money RefundAmount,
    string Reason,
    RefundReasonCategory Category);

/// <summary>
/// Eveniment: Procesarea refund-ului a început
/// </summary>
public record RefundProcessingStarted(
    Guid RefundId,
    Guid PaymentId,
    Guid OrderId);

/// <summary>
/// 🎯 EVENIMENT PRINCIPAL: Refund a fost completat cu succes
/// Acest eveniment poate declanșa actualizarea Order-ului în status "Refunded"
/// </summary>
public record RefundCompleted(
    Guid RefundId,
    Guid PaymentId,
    Guid OrderId,
    Money RefundAmount,
    string TransactionId,
    DateTime CompletedAt);

/// <summary>
/// Eveniment: Refund a eșuat
/// </summary>
public record RefundFailed(
    Guid RefundId,
    Guid PaymentId,
    Guid OrderId,
    string FailureReason);

/// <summary>
/// Eveniment: Refund se reîncearcă după un eșec
/// </summary>
public record RefundRetrying(
    Guid RefundId,
    Guid PaymentId,
    Guid OrderId,
    int RetryCount,
    string FailureReason);

/// <summary>
/// Eveniment: Refund a fost anulat
/// </summary>
public record RefundCancelled(
    Guid RefundId,
    Guid PaymentId,
    Guid OrderId,
    string Reason);
