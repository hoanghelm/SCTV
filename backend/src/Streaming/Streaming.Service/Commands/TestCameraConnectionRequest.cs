using Common.ApiResponse;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Streaming.Service.Commands
{
	public class TestCameraConnectionRequest : IRequest<ApiResult>
	{
		[Required]
		[MaxLength(500)]
		public string StreamUrl { get; set; }

		public int TimeoutSeconds { get; set; } = 10;
	}
}