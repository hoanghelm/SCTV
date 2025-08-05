using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Common.ErrorResult
{
	public enum HttpCode
	{
		[Description("Bad request")]
		BadRequest = 400,

		[Description("Unauthorized")]
		Unauthorized = 401,

		[Description("Forbidden")]
		Forbidden = 403,

		[Description("Not found")]
		Notfound = 404,

		[Description("Internal server error")]
		InternalServerError = 500,

		[Description("OK")]
		OK = 200,
	}
}