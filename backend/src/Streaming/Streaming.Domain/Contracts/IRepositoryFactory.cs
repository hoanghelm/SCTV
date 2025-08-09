using System;
using System.Collections.Generic;
using System.Text;

namespace Streaming.Domain.Contracts
{
	public interface IRepositoryFactory
	{
		IRepository<TEntity> GetRepository<TEntity>() where TEntity : class;
	}
}
