using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Entites
{
	public interface IUpdatedEntity
	{
		DateTime? UpdatedAt { get; set; }
		string UpdatedBy { get; set; }
	}
}