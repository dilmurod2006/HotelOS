using HotelOS.SharedKernel.Common;
using Housekeeping.Application.DTOs;
using MediatR;

namespace Housekeeping.Application.Commands.StartCleaning;

public sealed record StartCleaningCommand(Guid TaskId, Guid CleanerId)
    : IRequest<Result<CleaningTaskDto>>;
