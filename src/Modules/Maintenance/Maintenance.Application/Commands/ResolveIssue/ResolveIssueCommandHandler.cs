using HotelOS.SharedKernel.Common;
using Maintenance.Application.Commands.ReportIssue;
using Maintenance.Application.DTOs;
using Maintenance.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maintenance.Application.Commands.ResolveIssue;

public sealed class ResolveIssueCommandHandler
    : IRequestHandler<ResolveIssueCommand, Result<MaintenanceIssueDto>>
{
    private readonly IMaintenanceIssueRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResolveIssueCommandHandler> _logger;

    public ResolveIssueCommandHandler(
        IMaintenanceIssueRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ResolveIssueCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<MaintenanceIssueDto>> Handle(
        ResolveIssueCommand command, CancellationToken cancellationToken)
    {
        var issue = await _repository.GetByIdAsync(command.IssueId, cancellationToken);
        if (issue is null)
            return Result<MaintenanceIssueDto>.Failure(
                new Error("Maintenance.NotFound", $"Maintenance issue {command.IssueId} not found."));

        issue.Resolve(command.ResolutionNotes);

        await _repository.UpdateAsync(issue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Maintenance issue {IssueId} resolved. Room: {RoomNumber}.",
            issue.Id, issue.RoomNumber);

        return Result<MaintenanceIssueDto>.Success(ReportIssueCommandHandler.MapToDto(issue, priorityRank: 0));
    }
}
