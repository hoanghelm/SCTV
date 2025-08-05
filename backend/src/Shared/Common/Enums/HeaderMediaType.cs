using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Common.Enum
{
	public enum HeaderMediaType
	{
		[Description("application/json")]
		JSON,
		[Description("application/x-www-form-urlencoded")]
		FORM
	}
}
