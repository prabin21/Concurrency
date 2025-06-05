using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Concurrency.Domain.Repositories;
using Concurrency.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Concurrency.EntityFrameworkCore.UnitOfWork
{
    /// <summary>
    /// Implementation of the Unit of Work pattern using Entity Framework Core.
    /// This class manages transactions and coordinates changes across multiple repositories.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ConcurrencyDbContext _dbContext;
        private IDbContextTransaction _currentTransaction;
        private readonly Dictionary<Type, object> _repositories;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the UnitOfWork class.
        /// </summary>
        /// <param name="dbContext">The database context to use</param>
        public UnitOfWork(ConcurrencyDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _repositories = new Dictionary<Type, object>();
        }

        /// <summary>
        /// Gets a repository for the specified entity type.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity</typeparam>
        /// <returns>A repository for the specified entity type</returns>
        public IRepository<TEntity> _productRepository<TEntity>() where TEntity : class
        {
            var type = typeof(TEntity);

            if (!_repositories.ContainsKey(type))
            {
                _repositories[type] = new _productRepository<TEntity>(_dbContext);
            }

            return (IRepository<TEntity>)_repositories[type];
        }

        /// <summary>
        /// Begins a new transaction asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null)
            {
                return;
            }

            _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        /// <summary>
        /// Commits all changes made in this unit of work asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);

                if (_currentTransaction != null)
                {
                    await _currentTransaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                await RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
            }
        }

        /// <summary>
        /// Rolls back all changes made in this unit of work asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.RollbackAsync(cancellationToken);
                }
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
            }
        }

        /// <summary>
        /// Disposes of the resources used by the UnitOfWork.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the resources used by the UnitOfWork.
        /// </summary>
        /// <param name="disposing">True if called from Dispose, false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _currentTransaction?.Dispose();
                _disposed = true;
            }
        }
    }
} 