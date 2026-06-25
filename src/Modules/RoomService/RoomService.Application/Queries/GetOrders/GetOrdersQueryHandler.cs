using HotelOS.SharedKernel.Common;
using MediatR;
using RoomService.Application.DTOs;
using RoomService.Application.Interfaces;

namespace RoomService.Application.Queries.GetOrders;

public sealed class GetOrdersQueryHandler
    : IRequestHandler<GetOrdersQuery, Result<IReadOnlyList<ServiceOrderDto>>>
{
    private readonly IServiceOrderRepository _repository;

    public GetOrdersQueryHandler(IServiceOrderRepository repository)
        => _repository = repository;

    public async Task<Result<IReadOnlyList<ServiceOrderDto>>> Handle(
        GetOrdersQuery query, CancellationToken cancellationToken)
    {
        var orders = query.Status.HasValue
            ? await _repository.GetByStatusAsync(query.Status.Value, cancellationToken)
            : await _repository.GetAllAsync(cancellationToken);

        var dtos = orders.Select(ServiceOrderMapper.ToDto).ToList().AsReadOnly();
        return Result<IReadOnlyList<ServiceOrderDto>>.Success(dtos);
    }
}
