using HotelOS.SharedKernel.Common;
using Housekeeping.Application.DTOs;
using Housekeeping.Application.Interfaces;
using Housekeeping.Application.Queries.GetCleaningTasks;
using MediatR;

namespace Housekeeping.Application.Commands.StartCleaning;

public sealed class StartCleaningCommandHandler
    : IRequestHandler<StartCleaningCommand, Result<CleaningTaskDto>>
{
    private readonly ICleaningTaskRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public StartCleaningCommandHandler(ICleaningTaskRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CleaningTaskDto>> Handle(
        StartCleaningCommand command, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(command.TaskId, cancellationToken);
        if (task is null)
            return Result<CleaningTaskDto>.Failure(
                new Error("CleaningTask.NotFound", $"Task {command.TaskId} not found."));

        try
        {
            task.StartCleaning(command.CleanerId);
        }
        catch (InvalidOperationException ex)
        {
            return Result<CleaningTaskDto>.Failure(
                new Error("CleaningTask.InvalidTransition", ex.Message));
        }

        await _repository.UpdateAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CleaningTaskDto>.Success(GetCleaningTasksQueryHandler.MapToDto(task));
    }
}
