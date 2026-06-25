using HotelOS.SharedKernel.Common;
using Maintenance.Application.DTOs;
using MediatR;

namespace Maintenance.Application.Queries.GetPriorityQueue;

public sealed record GetPriorityQueueQuery : IRequest<Result<IReadOnlyList<MaintenanceIssueDto>>>;
