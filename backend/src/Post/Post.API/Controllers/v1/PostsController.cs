using Common.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Post.Infrastructure.ApiRoute;
using Post.Service.Post.Commands;
using Post.Service.Post.Queries;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Post.API.Controllers.v1
{
	[ApiVersion("1.0")]
	[Route("api/v{version:apiVersion}/[controller]")]
	[ApiController]
	public class PostsController : ControllerBase
	{
		private readonly IMediator _mediator;

		public PostsController(IMediator mediator)
		{
			_mediator = mediator;
		}

		[HttpPost(ApiRoutes.Post.CREATE)]
		public async Task<IActionResult> CreateAsync(CreatePostRequest request)
		{
			return await _mediator.Send(request);
		}

		[AllowAnonymous]
		[HttpGet(ApiRoutes.Post.GET_LIST)]
		[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "*" })]
		public async Task<IActionResult> GetListAsync([FromQuery] GetPostsRequest request)
		{
			return await _mediator.Send(request);
		}
	}
}
