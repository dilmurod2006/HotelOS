using HotelOS.SharedKernel.Common;
using Maintenance.Application.DTOs;
using MediatR;

namespace Maintenance.Application.Commands.ResolveIssue;

public sealed record ResolveIssueCommand(
    Guid IssueId,
    string ResolutionNotes) : IRequest<Result<MaintenanceIssueDto>>;
