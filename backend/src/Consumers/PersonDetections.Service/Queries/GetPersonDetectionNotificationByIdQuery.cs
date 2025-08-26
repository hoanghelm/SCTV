using Common.ApiResponse;
using MediatR;

namespace PersonDetections.Service.Queries;

public class GetPersonDetectionNotificationByIdQuery : IRequest<ApiResult>
{
    public Guid Id { get; set; }
}