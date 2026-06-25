using HotelOS.SharedKernel.Common;
using Housekeeping.Application.DTOs;
using MediatR;

namespace Housekeeping.Application.Commands.CompleteCleaning;

public sealed record CompleteCleaningCommand(Guid TaskId)
    : IRequest<Result<CleaningTaskDto>>;
