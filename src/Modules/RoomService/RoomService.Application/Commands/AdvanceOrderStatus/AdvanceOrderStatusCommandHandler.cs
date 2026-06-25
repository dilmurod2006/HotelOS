using HotelOS.SharedKernel.Common;
using MediatR;
using RoomService.Application;
using Microsoft.Extensions.Logging;
using RoomService.Application.DTOs;
using RoomService.Application.Interfaces;
using RoomService.Domain.Enums;

namespace RoomService.Application.Commands.AdvanceOrderStatus;

/// <summary>
/// STATE MACHINE HANDLER — Received → Preparing → Delivering → Delivered
/// ─────────────────────────────────────────────────────────────────────────
/// The domain entity owns all transition logic via EnsureTransition().
/// The handler simply loads the aggregate, calls the next-step method,
/// and commits. Invalid transitions surface as InvalidOperationException
/// from the domain and are converted to a failure Result here.
/// ─────────────────────────────────────────────────────────────────────────
/// Why one-step advancement per command: each HTTP call corresponds to a
/// real-world kitchen action (e.g., "start preparing"), making the audit
/// trail unambiguous and preventing accidental multi-step skips.
/// </summary>
public sealed class AdvanceOrderStatusCommandHandler
    : IRequestHandler<AdvanceOrderStatusCommand, Result<ServiceOrderDto>>
{
    private readonly IServiceOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdvanceOrderStatusCommandHandler> _logger;

    public AdvanceOrderStatusCommandHandler(
        IServiceOrderRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AdvanceOrderStatusCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ServiceOrderDto>> Handle(
        AdvanceOrderStatusCommand command, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
            return Result<ServiceOrderDto>.Failure(
                new Error("Order.NotFound", $"Service order {command.OrderId} not found."));

        // Advance one step; domain enforces valid transitions via EnsureTransition()
        try
        {
            switch (order.Status)
            {
                case OrderStatus.Received:
                    order.StartPreparing();
                    break;

                case OrderStatus.Preparing:
                    order.StartDelivery();
                    break;

                case OrderStatus.Delivering:
                    order.MarkDelivered();
                    break;

                case OrderStatus.Delivered:
                    return Result<ServiceOrderDto>.Failure(new Error(
                        "Order.AlreadyDelivered",
                        $"Order {command.OrderId} is already in the terminal state Delivered."));

                default:
                    return Result<ServiceOrderDto>.Failure(new Error(
                        "Order.UnknownStatus",
                        $"Unrecognised order status '{order.Status}' on order {command.OrderId}."));
            }
        }
        catch (InvalidOperationException ex)
        {
            // Domain rejected the transition (guard for concurrent modification)
            return Result<ServiceOrderDto>.Failure(new Error("Order.InvalidTransition", ex.Message));
        }

        await _repository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Service order {OrderId} advanced to {Status}.",
            order.Id, order.Status);

        return Result<ServiceOrderDto>.Success(ServiceOrderMapper.ToDto(order));
    }
}
