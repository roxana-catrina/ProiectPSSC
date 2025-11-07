// ═══════════════════════════════════════════════════════════════════════════════
// 🎯 PAYMENT COMMAND HANDLERS - APPLICATION LAYER
// ═══════════════════════════════════════════════════════════════════════════════

namespace Proiect.Application.Payments.Commands.Handlers;

using Proiect.Domain.Payments;
using Proiect.Domain.Payments.Services;
using Proiect.Infrastructure.Persistence;
using Proiect.Application.Payments.Commands;

// ═══════════════════════════════════════════════════════════════════════════════
// PAYMENT COMMAND HANDLERS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// 🎯 HANDLER 1: Procesează o plată
/// Flow: Validare → Fraud Check → Create Payment → Process via Gateway → Complete/Fail
/// Eveniment generat: PaymentCompleted (sau PaymentFailed)
/// </summary>
public class ProcessPaymentCommandHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGatewayService _paymentGatewayService;
    private readonly IFraudDetectionService _fraudDetectionService;
    private readonly IPaymentDomainService _paymentDomainService;

    public ProcessPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IPaymentGatewayService paymentGatewayService,
        IFraudDetectionService fraudDetectionService,
        IPaymentDomainService paymentDomainService)
    {
        _paymentRepository = paymentRepository;
        _paymentGatewayService = paymentGatewayService;
        _fraudDetectionService = fraudDetectionService;
        _paymentDomainService = paymentDomainService;
    }

    public async Task<ProcessPaymentResult> HandleAsync(ProcessPaymentCommand command)
    {
        try
        {
            // ═══════════════════════════════════════════════════════════════════
            // STEP 1: VALIDARE
            // ═══════════════════════════════════════════════════════════════════
            
            // Verifică dacă există deja un payment completat pentru acest order
            if (await _paymentDomainService.HasCompletedPaymentForOrderAsync(command.OrderId))
            {
                return new ProcessPaymentResult
                {
                    Success = false,
                    ErrorMessage = "Payment already exists for this order"
                };
            }

            // ═══════════════════════════════════════════════════════════════════
            // STEP 2: CREARE VALUE OBJECTS
            // ═══════════════════════════════════════════════════════════════════
            
            Money amount;
            PaymentDetails? paymentDetails = null;

            try
            {
                amount = new Money(command.Amount, command.Currency);
            }
            catch (ArgumentException ex)
            {
                return new ProcessPaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Invalid amount or currency: {ex.Message}"
                };
            }

            // Creare PaymentDetails pentru plăți cu card
            if ((command.PaymentMethod == PaymentMethod.CreditCard || 
                 command.PaymentMethod == PaymentMethod.DebitCard) &&
                !string.IsNullOrWhiteSpace(command.MaskedCardNumber))
            {
                try
                {
                    paymentDetails = new PaymentDetails(
                        command.MaskedCardNumber!,
                        command.CardHolderName ?? "Unknown",
                        command.ExpiryDate ?? "");
                }
                catch (ArgumentException ex)
                {
                    return new ProcessPaymentResult
                    {
                        Success = false,
                        ErrorMessage = $"Invalid payment details: {ex.Message}"
                    };
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            // STEP 3: CREARE PAYMENT AGGREGATE
            // ═══════════════════════════════════════════════════════════════════
            
            Payment payment;
            try
            {
                payment = Payment.Create(
                    command.OrderId,
                    amount,
                    command.PaymentMethod,
                    paymentDetails);
            }
            catch (ArgumentException ex)
            {
                return new ProcessPaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to create payment: {ex.Message}"
                };
            }

            // ═══════════════════════════════════════════════════════════════════
            // STEP 4: FRAUD DETECTION
            // ═══════════════════════════════════════════════════════════════════
            
            var fraudCheck = await _fraudDetectionService.CheckPaymentAsync(
                payment, 
                command.CustomerEmail);

            if (fraudCheck.ShouldBlock)
            {
                payment.Cancel($"Blocked due to fraud detection: {string.Join(", ", fraudCheck.Reasons)}");
                await _paymentRepository.SaveAsync(payment);

                return new ProcessPaymentResult
                {
                    Success = false,
                    PaymentId = payment.PaymentId,
                    Status = PaymentStatus.Cancelled,
                    ErrorMessage = "Payment blocked due to fraud detection"
                };
            }

            // ═══════════════════════════════════════════════════════════════════
            // STEP 5: PROCESARE PRIN GATEWAY
            // ═══════════════════════════════════════════════════════════════════
            
            payment.StartProcessing();
            await _paymentRepository.SaveAsync(payment);

            var gatewayResult = await _paymentGatewayService.ProcessPaymentAsync(payment);

            if (gatewayResult.Success)
            {
                // SUCCESS: Completează payment-ul
                var transactionInfo = new TransactionInfo(
                    gatewayResult.TransactionId!,
                    gatewayResult.AuthorizationCode ?? "",
                    DateTime.UtcNow,
                    gatewayResult.GatewayResponse ?? "");

                payment.Complete(transactionInfo);
                await _paymentRepository.SaveAsync(payment);

                // 🎯 EVENIMENT: PaymentCompleted a fost generat în Payment.Complete()
                
                return new ProcessPaymentResult
                {
                    Success = true,
                    PaymentId = payment.PaymentId,
                    Status = PaymentStatus.Completed,
                    TransactionId = gatewayResult.TransactionId
                };
            }
            else
            {
                // FAILURE: Marchează payment-ul ca eșuat
                payment.Fail(gatewayResult.ErrorMessage ?? "Unknown error");
                await _paymentRepository.SaveAsync(payment);

                return new ProcessPaymentResult
                {
                    Success = false,
                    PaymentId = payment.PaymentId,
                    Status = payment.Status,
                    ErrorMessage = gatewayResult.ErrorMessage
                };
            }
        }
        catch (Exception ex)
        {
            return new ProcessPaymentResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }
}

/// <summary>
/// Handler: Retry Payment
/// </summary>
public class RetryPaymentCommandHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGatewayService _paymentGatewayService;

    public RetryPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IPaymentGatewayService paymentGatewayService)
    {
        _paymentRepository = paymentRepository;
        _paymentGatewayService = paymentGatewayService;
    }

    public async Task<ProcessPaymentResult> HandleAsync(RetryPaymentCommand command)
    {
        var payment = await _paymentRepository.GetByIdAsync(command.PaymentId);
        
        if (payment == null)
        {
            return new ProcessPaymentResult
            {
                Success = false,
                ErrorMessage = "Payment not found"
            };
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return new ProcessPaymentResult
            {
                Success = false,
                ErrorMessage = "Payment cannot be retried in current status"
            };
        }

        payment.StartProcessing();
        await _paymentRepository.SaveAsync(payment);

        var gatewayResult = await _paymentGatewayService.ProcessPaymentAsync(payment);

        if (gatewayResult.Success)
        {
            var transactionInfo = new TransactionInfo(
                gatewayResult.TransactionId!,
                gatewayResult.AuthorizationCode ?? "",
                DateTime.UtcNow,
                gatewayResult.GatewayResponse ?? "");

            payment.Complete(transactionInfo);
            await _paymentRepository.SaveAsync(payment);

            return new ProcessPaymentResult
            {
                Success = true,
                PaymentId = payment.PaymentId,
                Status = PaymentStatus.Completed,
                TransactionId = gatewayResult.TransactionId
            };
        }
        else
        {
            payment.Fail(gatewayResult.ErrorMessage ?? "Unknown error");
            await _paymentRepository.SaveAsync(payment);

            return new ProcessPaymentResult
            {
                Success = false,
                PaymentId = payment.PaymentId,
                Status = payment.Status,
                ErrorMessage = gatewayResult.ErrorMessage
            };
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// REFUND COMMAND HANDLERS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// 🎯 HANDLER 2: Inițiază o rambursare
/// Flow: Validare → Verificare disponibilitate → Create Refund
/// Eveniment generat: RefundInitiated
/// </summary>
public class InitiateRefundCommandHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRefundRepository _refundRepository;
    private readonly IPaymentDomainService _paymentDomainService;

    public InitiateRefundCommandHandler(
        IPaymentRepository paymentRepository,
        IRefundRepository refundRepository,
        IPaymentDomainService paymentDomainService)
    {
        _paymentRepository = paymentRepository;
        _refundRepository = refundRepository;
        _paymentDomainService = paymentDomainService;
    }

    public async Task<InitiateRefundResult> HandleAsync(InitiateRefundCommand command)
    {
        try
        {
            // ═══════════════════════════════════════════════════════════════════
            // STEP 1: VALIDARE - Payment există și este completat
            // ═══════════════════════════════════════════════════════════════════
            
            var payment = await _paymentRepository.GetByIdAsync(command.PaymentId);
            
            if (payment == null)
            {
                return new InitiateRefundResult
                {
                    Success = false,
                    ErrorMessage = "Payment not found"
                };
            }

            if (!payment.CanBeRefunded())
            {
                return new InitiateRefundResult
                {
                    Success = false,
                    ErrorMessage = $"Payment cannot be refunded. Current status: {payment.Status}"
                };
            }

            // ═══════════════════════════════════════════════════════════════════
            // STEP 2: VERIFICARE - Sumă disponibilă pentru refund
            // ═══════════════════════════════════════════════════════════════════
            
            var canRefund = await _paymentDomainService.CanRefundPaymentAsync(
                command.PaymentId, 
                command.RefundAmount);

            if (!canRefund)
            {
                var totalRefunded = await _paymentDomainService.GetTotalRefundedAmountAsync(command.PaymentId);
                var remaining = payment.Amount.Amount - totalRefunded;
                
                return new InitiateRefundResult
                {
                    Success = false,
                    ErrorMessage = $"Refund amount exceeds available amount. Remaining: {remaining} {payment.Amount.Currency}"
                };
            }

            // ═══════════════════════════════════════════════════════════════════
            // STEP 3: CREARE VALUE OBJECTS
            // ═══════════════════════════════════════════════════════════════════
            
            Money refundAmount;
            RefundReason refundReason;

            try
            {
                refundAmount = new Money(command.RefundAmount, payment.Amount.Currency);
                refundReason = new RefundReason(
                    command.Reason,
                    command.ReasonCategory,
                    command.RequestedBy,
                    DateTime.UtcNow);
            }
            catch (ArgumentException ex)
            {
                return new InitiateRefundResult
                {
                    Success = false,
                    ErrorMessage = $"Invalid refund data: {ex.Message}"
                };
            }

            // ═══════════════════════════════════════════════════════════════════
            // STEP 4: CREARE REFUND AGGREGATE
            // ═══════════════════════════════════════════════════════════════════
            
            Refund refund;
            try
            {
                refund = Refund.Initiate(
                    payment.PaymentId,
                    payment.OrderId,
                    refundAmount,
                    payment.Amount,
                    refundReason);
            }
            catch (ArgumentException ex)
            {
                return new InitiateRefundResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to initiate refund: {ex.Message}"
                };
            }

            await _refundRepository.SaveAsync(refund);

            // 🎯 EVENIMENT: RefundInitiated a fost generat în Refund.Initiate()

            return new InitiateRefundResult
            {
                Success = true,
                RefundId = refund.RefundId
            };
        }
        catch (Exception ex)
        {
            return new InitiateRefundResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }
}

/// <summary>
/// 🎯 HANDLER 3: Completează o rambursare
/// Flow: Validare → Complete Refund
/// Eveniment generat: RefundCompleted
/// </summary>
public class CompleteRefundCommandHandler
{
    private readonly IRefundRepository _refundRepository;

    public CompleteRefundCommandHandler(IRefundRepository refundRepository)
    {
        _refundRepository = refundRepository;
    }

    public async Task<CompleteRefundResult> HandleAsync(CompleteRefundCommand command)
    {
        try
        {
            // ═══════════════════════════════════════════════════════════════════
            // STEP 1: VALIDARE
            // ═══════════════════════════════════════════════════════════════════
            
            var refund = await _refundRepository.GetByIdAsync(command.RefundId);
            
            if (refund == null)
            {
                return new CompleteRefundResult
                {
                    Success = false,
                    ErrorMessage = "Refund not found"
                };
            }

            // ═══════════════════════════════════════════════════════════════════
            // STEP 2: CREARE TRANSACTION INFO
            // ═══════════════════════════════════════════════════════════════════
            
            TransactionInfo transactionInfo;
            try
            {
                transactionInfo = new TransactionInfo(
                    command.TransactionId,
                    command.AuthorizationCode,
                    DateTime.UtcNow,
                    command.GatewayResponse);
            }
            catch (ArgumentException ex)
            {
                return new CompleteRefundResult
                {
                    Success = false,
                    ErrorMessage = $"Invalid transaction info: {ex.Message}"
                };
            }

            // ═══════════════════════════════════════════════════════════════════
            // STEP 3: COMPLETARE REFUND
            // ═══════════════════════════════════════════════════════════════════
            
            try
            {
                refund.Complete(transactionInfo);
                await _refundRepository.SaveAsync(refund);

                // 🎯 EVENIMENT: RefundCompleted a fost generat în Refund.Complete()

                return new CompleteRefundResult
                {
                    Success = true,
                    RefundId = refund.RefundId
                };
            }
            catch (InvalidOperationException ex)
            {
                return new CompleteRefundResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        catch (Exception ex)
        {
            return new CompleteRefundResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }
}

/// <summary>
/// Handler: Procesează un refund complet (start to finish)
/// </summary>
public class ProcessRefundCommandHandler
{
    private readonly IRefundRepository _refundRepository;
    private readonly IPaymentGatewayService _paymentGatewayService;

    public ProcessRefundCommandHandler(
        IRefundRepository refundRepository,
        IPaymentGatewayService paymentGatewayService)
    {
        _refundRepository = refundRepository;
        _paymentGatewayService = paymentGatewayService;
    }

    public async Task<CompleteRefundResult> HandleAsync(ProcessRefundCommand command)
    {
        var refund = await _refundRepository.GetByIdAsync(command.RefundId);
        
        if (refund == null)
        {
            return new CompleteRefundResult
            {
                Success = false,
                ErrorMessage = "Refund not found"
            };
        }

        // Start processing
        try
        {
            refund.StartProcessing();
            await _refundRepository.SaveAsync(refund);
        }
        catch (InvalidOperationException ex)
        {
            return new CompleteRefundResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }

        // Process via gateway
        var gatewayResult = await _paymentGatewayService.ProcessRefundAsync(refund);

        if (gatewayResult.Success)
        {
            var transactionInfo = new TransactionInfo(
                gatewayResult.TransactionId!,
                gatewayResult.AuthorizationCode ?? "",
                DateTime.UtcNow,
                gatewayResult.GatewayResponse ?? "");

            refund.Complete(transactionInfo);
            await _refundRepository.SaveAsync(refund);

            return new CompleteRefundResult
            {
                Success = true,
                RefundId = refund.RefundId
            };
        }
        else
        {
            refund.Fail(gatewayResult.ErrorMessage ?? "Unknown error");
            await _refundRepository.SaveAsync(refund);

            return new CompleteRefundResult
            {
                Success = false,
                RefundId = refund.RefundId,
                ErrorMessage = gatewayResult.ErrorMessage
            };
        }
    }
}
