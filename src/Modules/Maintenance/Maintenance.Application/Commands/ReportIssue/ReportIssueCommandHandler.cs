using HotelOS.SharedKernel.Common;
using Maintenance.Application.DTOs;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Maintenance.Application.Commands.ReportIssue;

public sealed class ReportIssueCommandHandler
    : IRequestHandler<ReportIssueCommand, Result<MaintenanceIssueDto>>
{
    private readonly IMaintenanceIssueRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReportIssueCommandHandler> _logger;

    public ReportIssueCommandHandler(
        IMaintenanceIssueRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ReportIssueCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<MaintenanceIssueDto>> Handle(
        ReportIssueCommand command, CancellationToken cancellationToken)
    {
        var issue = MaintenanceIssue.Report(
            command.RoomId,
            command.RoomNumber,
            command.Description,
            command.Urgency);

        await _repository.AddAsync(issue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Maintenance issue {IssueId} reported for room {RoomNumber}. " +
            "Urgency: {Urgency}. Status: {Status}.",
            issue.Id, issue.RoomNumber, issue.Urgency, issue.Status);

        return Result<MaintenanceIssueDto>.Success(MapToDto(issue, priorityRank: 0));
    }

    internal static MaintenanceIssueDto MapToDto(MaintenanceIssue issue, int priorityRank)
        => new(
            Id:                    issue.Id,
            RoomId:                issue.RoomId,
            RoomNumber:            issue.RoomNumber,
            Description:           issue.Description,
            Urgency:               issue.Urgency.ToString(),
            UrgencyLevel:          (int)issue.Urgency,
            Status:                issue.Status.ToString(),
            PriorityRank:          priorityRank,
            AssignedTechnicianId:  issue.AssignedTechnicianId,
            ResolutionNotes:       issue.ResolutionNotes,
            CreatedAt:             issue.CreatedAt,
            UpdatedAt:             issue.UpdatedAt,
            ResolvedAt:            issue.ResolvedAt);
}
