using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Concurrency.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Concurrency.EntityFrameworkCore.Repositories
{
    /// <summary>
    /// Implementation of the generic repository pattern using Entity Framework Core.
    /// This class provides common CRUD operations for entities.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity this repository handles</typeparam>
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        /// <summary>
        /// Initializes a new instance of the Repository class.
        /// </summary>
        /// <param name="context">The database context to use</param>
        public Repository(DbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<TEntity>();
        }

        /// <summary>
        /// Gets an entity by its primary key asynchronously.
        /// </summary>
        /// <param name="id">The primary key of the entity</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>The entity if found, null otherwise</returns>
        public virtual async Task<TEntity> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new[] { id }, cancellationToken);
        }

        /// <summary>
        /// Gets all entities asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>A list of all entities</returns>
        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Finds entities that match the specified predicate asynchronously.
        /// </summary>
        /// <param name="predicate">The predicate to filter entities</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>A list of entities that match the predicate</returns>
        public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Adds a new entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>The added entity</returns>
        public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var entry = await _dbSet.AddAsync(entity, cancellationToken);
            return entry.Entity;
        }

        /// <summary>
        /// Updates an existing entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>The updated entity</returns>
        public virtual Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var entry = _context.Entry(entity);
            entry.State = EntityState.Modified;
            return Task.FromResult(entry.Entity);
        }

        /// <summary>
        /// Removes an entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to remove</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        public virtual Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets a queryable collection of entities.
        /// </summary>
        /// <returns>A queryable collection of entities</returns>
        public virtual IQueryable<TEntity> Query()
        {
            return _dbSet;
        }
    }
} 