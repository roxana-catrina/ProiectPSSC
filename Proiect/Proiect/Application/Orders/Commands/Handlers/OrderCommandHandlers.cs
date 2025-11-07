// ═══════════════════════════════════════════════════════════════════════════════
// 📋 COMMAND HANDLERS - ORDER MANAGEMENT CONTEXT
// ═══════════════════════════════════════════════════════════════════════════════

namespace OrderManagement.Application.Orders.Commands.Handlers;

using MediatR;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.Services;
using OrderManagement.Infrastructure.Persistence;

// ═══════════════════════════════════════════════════════════════════════════════
// 1️⃣ PlaceOrderCommandHandler
// ═══════════════════════════════════════════════════════════════════════════════

public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, PlaceOrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;
    private readonly PlaceOrderCommandValidator _validator;

    public PlaceOrderCommandHandler(
        IOrderRepository orderRepository,
        IMediator mediator)
    {
        _orderRepository = orderRepository;
        _mediator = mediator;
        _validator = new PlaceOrderCommandValidator();
    }

    public async Task<PlaceOrderResult> Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        // 1. Validare comandă
        var validationErrors = _validator.Validate(command);
        if (validationErrors.Any())
        {
            return new PlaceOrderResult(
                Success: false,
                OrderId: null,
                Message: "Validation failed",
                ValidationErrors: validationErrors
            );
        }

        try
        {
            // 2. Creează Value Objects
            var customerInfo = new CustomerInfo(
                command.CustomerInfo.Name,
                command.CustomerInfo.Email,
                command.CustomerInfo.PhoneNumber
            );

            var shippingAddress = new ShippingAddress(
                command.ShippingAddress.Street,
                command.ShippingAddress.City,
                command.ShippingAddress.County,
                command.ShippingAddress.PostalCode,
                command.ShippingAddress.Country
            );

            var orderItems = command.OrderItems.Select(dto => 
                new OrderItem(
                    dto.ProductId,
                    dto.ProductName,
                    dto.Quantity,
                    new Money(dto.UnitPrice)
                )
            ).ToList();

            var paymentMethod = Enum.Parse<PaymentMethod>(command.PaymentMethod);

            // 3. Creează agregatul Order folosind Factory Method
            var order = Order.Create(
                command.CustomerId,
                customerInfo,
                orderItems,
                shippingAddress,
                paymentMethod
            );

            // 4. Salvează comanda în repository
            await _orderRepository.SaveAsync(order);

            // 5. Publică domain events
            foreach (var domainEvent in order.DomainEvents)
            {
                await _mediator.Publish(domainEvent, cancellationToken);
            }

            order.ClearDomainEvents();

            // 6. Returnează rezultat
            return new PlaceOrderResult(
                Success: true,
                OrderId: order.OrderId,
                Message: "Order placed successfully",
                ValidationErrors: new List<string>()
            );
        }
        catch (Exception ex)
        {
            return new PlaceOrderResult(
                Success: false,
                OrderId: null,
                Message: $"Error placing order: {ex.Message}",
                ValidationErrors: new List<string> { ex.Message }
            );
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 2️⃣ ValidateOrderCommandHandler
// ═══════════════════════════════════════════════════════════════════════════════

public class ValidateOrderCommandHandler : IRequestHandler<ValidateOrderCommand, ValidateOrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderValidationService _validationService;
    private readonly IMediator _mediator;
    private readonly ValidateOrderCommandValidator _validator;

    public ValidateOrderCommandHandler(
        IOrderRepository orderRepository,
        IOrderValidationService validationService,
        IMediator mediator)
    {
        _orderRepository = orderRepository;
        _validationService = validationService;
        _mediator = mediator;
        _validator = new ValidateOrderCommandValidator();
    }

    public async Task<ValidateOrderResult> Handle(ValidateOrderCommand command, CancellationToken cancellationToken)
    {
        // 1. Validare comandă
        var validationErrors = _validator.Validate(command);
        if (validationErrors.Any())
        {
            return new ValidateOrderResult(
                Success: false,
                IsValid: false,
                Message: "Validation failed",
                ValidationErrors: validationErrors
            );
        }

        try
        {
            // 2. Încarcă comanda din repository
            var order = await _orderRepository.GetByIdAsync(command.OrderId);
            if (order == null)
            {
                return new ValidateOrderResult(
                    Success: false,
                    IsValid: false,
                    Message: "Order not found",
                    ValidationErrors: new List<string> { "Order not found" }
                );
            }

            // 3. Validare de business prin Domain Service
            var validationResult = await _validationService.ValidateOrderAsync(order);

            if (validationResult.IsValid)
            {
                // 4. Marchează comanda ca validată
                order.Validate();

                // 5. Salvează modificările
                await _orderRepository.SaveAsync(order);

                // 6. Publică domain events
                foreach (var domainEvent in order.DomainEvents)
                {
                    await _mediator.Publish(domainEvent, cancellationToken);
                }

                order.ClearDomainEvents();

                return new ValidateOrderResult(
                    Success: true,
                    IsValid: true,
                    Message: "Order validated successfully",
                    ValidationErrors: new List<string>()
                );
            }
            else
            {
                // Comanda este respinsă
                order.Reject(validationResult.RejectionReason ?? "Validation failed");

                await _orderRepository.SaveAsync(order);

                foreach (var domainEvent in order.DomainEvents)
                {
                    await _mediator.Publish(domainEvent, cancellationToken);
                }

                order.ClearDomainEvents();

                return new ValidateOrderResult(
                    Success: true,
                    IsValid: false,
                    Message: "Order rejected",
                    ValidationErrors: validationResult.Errors
                );
            }
        }
        catch (Exception ex)
        {
            return new ValidateOrderResult(
                Success: false,
                IsValid: false,
                Message: $"Error validating order: {ex.Message}",
                ValidationErrors: new List<string> { ex.Message }
            );
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 3️⃣ ConfirmOrderCommandHandler
// ═══════════════════════════════════════════════════════════════════════════════

public class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand, ConfirmOrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;
    private readonly ConfirmOrderCommandValidator _validator;

    public ConfirmOrderCommandHandler(
        IOrderRepository orderRepository,
        IMediator mediator)
    {
        _orderRepository = orderRepository;
        _mediator = mediator;
        _validator = new ConfirmOrderCommandValidator();
    }

    public async Task<ConfirmOrderResult> Handle(ConfirmOrderCommand command, CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(command);
        if (validationErrors.Any())
        {
            return new ConfirmOrderResult(
                Success: false,
                Message: "Validation failed",
                ValidationErrors: validationErrors
            );
        }

        try
        {
            var order = await _orderRepository.GetByIdAsync(command.OrderId);
            if (order == null)
            {
                return new ConfirmOrderResult(
                    Success: false,
                    Message: "Order not found",
                    ValidationErrors: new List<string> { "Order not found" }
                );
            }

            // Confirmă comanda
            order.Confirm(command.ConfirmedBy, command.EstimatedDeliveryDate);

            await _orderRepository.SaveAsync(order);

            foreach (var domainEvent in order.DomainEvents)
            {
                await _mediator.Publish(domainEvent, cancellationToken);
            }

            order.ClearDomainEvents();

            return new ConfirmOrderResult(
                Success: true,
                Message: "Order confirmed successfully",
                ValidationErrors: new List<string>()
            );
        }
        catch (InvalidOperationException ex)
        {
            return new ConfirmOrderResult(
                Success: false,
                Message: ex.Message,
                ValidationErrors: new List<string> { ex.Message }
            );
        }
        catch (Exception ex)
        {
            return new ConfirmOrderResult(
                Success: false,
                Message: $"Error confirming order: {ex.Message}",
                ValidationErrors: new List<string> { ex.Message }
            );
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 4️⃣ CancelOrderCommandHandler
// ═══════════════════════════════════════════════════════════════════════════════

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, CancelOrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;
    private readonly CancelOrderCommandValidator _validator;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IMediator mediator)
    {
        _orderRepository = orderRepository;
        _mediator = mediator;
        _validator = new CancelOrderCommandValidator();
    }

    public async Task<CancelOrderResult> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(command);
        if (validationErrors.Any())
        {
            return new CancelOrderResult(
                Success: false,
                Message: "Validation failed",
                ValidationErrors: validationErrors
            );
        }

        try
        {
            var order = await _orderRepository.GetByIdAsync(command.OrderId);
            if (order == null)
            {
                return new CancelOrderResult(
                    Success: false,
                    Message: "Order not found",
                    ValidationErrors: new List<string> { "Order not found" }
                );
            }

            // Creează CancellationReason
            var cancellationReason = new CancellationReason(command.Reason, command.CancelledBy);

            // Anulează comanda
            order.Cancel(cancellationReason);

            await _orderRepository.SaveAsync(order);

            foreach (var domainEvent in order.DomainEvents)
            {
                await _mediator.Publish(domainEvent, cancellationToken);
            }

            order.ClearDomainEvents();

            return new CancelOrderResult(
                Success: true,
                Message: "Order cancelled successfully",
                ValidationErrors: new List<string>()
            );
        }
        catch (InvalidOperationException ex)
        {
            return new CancelOrderResult(
                Success: false,
                Message: ex.Message,
                ValidationErrors: new List<string> { ex.Message }
            );
        }
        catch (Exception ex)
        {
            return new CancelOrderResult(
                Success: false,
                Message: $"Error cancelling order: {ex.Message}",
                ValidationErrors: new List<string> { ex.Message }
            );
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 5️⃣ ModifyOrderCommandHandler
// ═══════════════════════════════════════════════════════════════════════════════

public class ModifyOrderCommandHandler : IRequestHandler<ModifyOrderCommand, ModifyOrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;
    private readonly ModifyOrderCommandValidator _validator;

    public ModifyOrderCommandHandler(
        IOrderRepository orderRepository,
        IMediator mediator)
    {
        _orderRepository = orderRepository;
        _mediator = mediator;
        _validator = new ModifyOrderCommandValidator();
    }

    public async Task<ModifyOrderResult> Handle(ModifyOrderCommand command, CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(command);
        if (validationErrors.Any())
        {
            return new ModifyOrderResult(
                Success: false,
                Message: "Validation failed",
                ValidationErrors: validationErrors
            );
        }

        try
        {
            var order = await _orderRepository.GetByIdAsync(command.OrderId);
            if (order == null)
            {
                return new ModifyOrderResult(
                    Success: false,
                    Message: "Order not found",
                    ValidationErrors: new List<string> { "Order not found" }
                );
            }

            // Construiește parametrii pentru modificare
            List<OrderItem>? newOrderItems = null;
            if (command.NewOrderItems != null)
            {
                newOrderItems = command.NewOrderItems.Select(dto =>
                    new OrderItem(
                        dto.ProductId,
                        dto.ProductName,
                        dto.Quantity,
                        new Money(dto.UnitPrice)
                    )
                ).ToList();
            }

            ShippingAddress? newShippingAddress = null;
            if (command.NewShippingAddress != null)
            {
                newShippingAddress = new ShippingAddress(
                    command.NewShippingAddress.Street,
                    command.NewShippingAddress.City,
                    command.NewShippingAddress.County,
                    command.NewShippingAddress.PostalCode,
                    command.NewShippingAddress.Country
                );
            }

            PaymentMethod? newPaymentMethod = null;
            if (!string.IsNullOrWhiteSpace(command.NewPaymentMethod))
            {
                newPaymentMethod = Enum.Parse<PaymentMethod>(command.NewPaymentMethod);
            }

            // Modifică comanda
            order.Modify(newOrderItems, newShippingAddress, newPaymentMethod);

            await _orderRepository.SaveAsync(order);

            foreach (var domainEvent in order.DomainEvents)
            {
                await _mediator.Publish(domainEvent, cancellationToken);
            }

            order.ClearDomainEvents();

            return new ModifyOrderResult(
                Success: true,
                Message: "Order modified successfully",
                ValidationErrors: new List<string>()
            );
        }
        catch (InvalidOperationException ex)
        {
            return new ModifyOrderResult(
                Success: false,
                Message: ex.Message,
                ValidationErrors: new List<string> { ex.Message }
            );
        }
        catch (Exception ex)
        {
            return new ModifyOrderResult(
                Success: false,
                Message: $"Error modifying order: {ex.Message}",
                ValidationErrors: new List<string> { ex.Message }
            );
        }
    }
}
