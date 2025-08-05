using System;
using System.Collections.Generic;
using System.Text;

namespace Paginate
{
	public interface IPaginate<TResult>
	{
		int Size { get; }
		int Page { get; }
		int Total { get; }
		IList<TResult> Items { get; }
	}
}