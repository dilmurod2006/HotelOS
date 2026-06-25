using HotelOS.SharedKernel.Common;
using Housekeeping.Application.DTOs;
using Housekeeping.Domain.Enums;
using MediatR;

namespace Housekeeping.Application.Queries.GetCleaningTasks;

public sealed record GetCleaningTasksQuery(CleaningTaskStatus? Status = null)
    : IRequest<Result<IReadOnlyList<CleaningTaskDto>>>;
