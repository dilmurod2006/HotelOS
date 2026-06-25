using HotelOS.SharedKernel.Common;
using Housekeeping.Application.DTOs;
using Housekeeping.Application.Interfaces;
using Housekeeping.Domain.Enums;
using MediatR;

namespace Housekeeping.Application.Queries.GetCleaningTasks;

public sealed class GetCleaningTasksQueryHandler
    : IRequestHandler<GetCleaningTasksQuery, Result<IReadOnlyList<CleaningTaskDto>>>
{
    private readonly ICleaningTaskRepository _repository;

    public GetCleaningTasksQueryHandler(ICleaningTaskRepository repository)
        => _repository = repository;

    public async Task<Result<IReadOnlyList<CleaningTaskDto>>> Handle(
        GetCleaningTasksQuery query, CancellationToken cancellationToken)
    {
        var tasks = query.Status.HasValue
            ? await _repository.GetByStatusAsync(query.Status.Value, cancellationToken)
            : await _repository.GetAllAsync(cancellationToken);

        var dtos = tasks.Select(MapToDto).ToList().AsReadOnly();
        return Result<IReadOnlyList<CleaningTaskDto>>.Success(dtos);
    }

    internal static CleaningTaskDto MapToDto(Domain.Entities.CleaningTask t) => new(
        t.Id, t.RoomId, t.RoomNumber, t.Floor,
        t.Status, t.Priority,
        t.AssignedCleanerId, t.StartedAt, t.CompletedAt, t.CreatedAt);
}
