using Common.ApiResponse;
using Common.Enum;
using Common.ErrorResult;
using Common.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Post.Infrastructure.Middlewares
{
	public class ExceptionMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger _logger;

		public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task Invoke(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (Exception ex)
			{
				await HandleExceptionAsync(context, ex);
			}
		}

		private Task HandleExceptionAsync(HttpContext context, Exception ex)
		{
			_logger.LogError(ex.Message);

			var response = context.Response;
			var statusCode = (int)HttpCode.InternalServerError;
			var result = JsonSerializer.Serialize(new ApiJsonResult<object>(statusCode, ex.Message), new JsonSerializerOptions()
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			});

			response.OnStarting(async () =>
			{
				response.StatusCode = statusCode;
				response.ContentType = HeaderMediaType.JSON.GetDescription();
				await response.WriteAsync(result);
			});

			return Task.CompletedTask;
		}
	}
}
