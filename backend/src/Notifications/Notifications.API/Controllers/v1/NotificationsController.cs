using Asp.Versioning;
using Common.ApiResponse;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PersonDetections.Service.Queries;

namespace Notifications.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ApiResult> GetNotifications([FromQuery] GetPersonDetectionNotificationsRequest request)
    {
        return await _mediator.Send(request);
    }

    [HttpGet("{id:guid}")]
    public async Task<ApiResult> GetNotification(Guid id)
    {
        var query = new GetPersonDetectionNotificationByIdQuery { Id = id };
        return await _mediator.Send(query);
    }
}