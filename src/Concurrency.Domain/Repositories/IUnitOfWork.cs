using System;
using System.Threading;
using System.Threading.Tasks;

namespace Concurrency.Domain.Repositories
{
    /// <summary>
    /// Defines the interface for the Unit of Work pattern.
    /// The Unit of Work pattern maintains a list of objects affected by a business transaction
    /// and coordinates the writing out of changes and the resolution of concurrency problems.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Begins a new transaction asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits all changes made in this unit of work asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back all changes made in this unit of work asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a repository for the specified entity type.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity</typeparam>
        /// <returns>A repository for the specified entity type</returns>
        IRepository<TEntity> _productRepository<TEntity>() where TEntity : class;
    }
} 