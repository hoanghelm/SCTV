using Common.ApiResponse;
using MediatR;

namespace PersonDetections.Service.Queries;

public class GetPersonDetectionNotificationsRequest : IRequest<ApiResult>
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
    public Guid? CameraId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}