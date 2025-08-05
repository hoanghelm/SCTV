using Common.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Post.Infrastructure.ApiRoute;
using Post.Service.Post.Commands;
using System.Threading.Tasks;

namespace Post.API.Controllers.v1
{
	[ApiVersion("1.0")]
	[Route("api/v{version:apiVersion}/[controller]")]
	[ApiController]
	public class CommentsController : ControllerBase
	{
		private readonly IMediator _mediator;

        public CommentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost(ApiRoutes.Post.COMMENT)]
		public async Task<IActionResult> CreateAsync(CreateCommentRequest request)
		{
			var userId = Request.Headers[HeaderInfo.USER_ID].ToString();
			var userName = Request.Headers[HeaderInfo.USER_NAME].ToString();
			request.Author = userId;
			request.AuthorName = userName;
			return await _mediator.Send(request);
		}
	}
}
