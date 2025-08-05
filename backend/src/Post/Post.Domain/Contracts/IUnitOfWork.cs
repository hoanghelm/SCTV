using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Post.Domain.Contracts
{
	public interface IUnitOfWork : IRepositoryFactory, IDisposable
	{
		int Commit();
		Task<int> CommitAsync();
	}

	public interface IUnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
	{
		TContext Context { get; }
	}
}