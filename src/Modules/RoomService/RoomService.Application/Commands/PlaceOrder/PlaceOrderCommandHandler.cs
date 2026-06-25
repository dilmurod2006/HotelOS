using HotelOS.SharedKernel.Common;
using MediatR;
using RoomService.Application;
using Microsoft.Extensions.Logging;
using RoomService.Application.DTOs;
using RoomService.Application.Interfaces;
using RoomService.Domain.Entities;

namespace RoomService.Application.Commands.PlaceOrder;

public sealed class PlaceOrderCommandHandler
    : IRequestHandler<PlaceOrderCommand, Result<ServiceOrderDto>>
{
    private readonly IServiceOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PlaceOrderCommandHandler> _logger;

    public PlaceOrderCommandHandler(
        IServiceOrderRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<PlaceOrderCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ServiceOrderDto>> Handle(
        PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        if (command.Items is null || command.Items.Count == 0)
            return Result<ServiceOrderDto>.Failure(
                new Error("Order.Empty", "Order must contain at least one item."));

        var lineItems = command.Items
            .Select(i => new OrderLineItem(i.ItemName, i.Quantity, i.UnitPrice))
            .ToList();

        var order = ServiceOrder.Create(command.BookingId, command.RoomNumber, lineItems);

        await _repository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Service order {OrderId} placed for room {RoomNumber}. Items: {Count}. Total: {Total:C}.",
            order.Id, order.RoomNumber, lineItems.Count, order.TotalPrice);

        return Result<ServiceOrderDto>.Success(ServiceOrderMapper.ToDto(order));
    }
}
