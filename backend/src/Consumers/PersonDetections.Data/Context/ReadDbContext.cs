using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace PersonDetections.Data.Context
{
	public class ReadDbContext : ApplicationContext
	{
		public ReadDbContext(DbContextOptions<ReadDbContext> options) : base(options)
		{
		}
	}
}
