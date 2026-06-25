using HotelOS.SharedKernel.Common;
using Maintenance.Application.DTOs;
using Maintenance.Domain.Enums;
using MediatR;

namespace Maintenance.Application.Commands.ReportIssue;

public sealed record ReportIssueCommand(
    Guid RoomId,
    string RoomNumber,
    string Description,
    IssueUrgency Urgency) : IRequest<Result<MaintenanceIssueDto>>;
