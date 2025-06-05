using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Concurrency.Domain.Repositories
{
    /// <summary>
    /// Defines the interface for a generic repository pattern.
    /// This interface provides common CRUD operations for entities.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity this repository handles</typeparam>
    public interface IRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Gets an entity by its primary key asynchronously.
        /// </summary>
        /// <param name="id">The primary key of the entity</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>The entity if found, null otherwise</returns>
        Task<TEntity> GetByIdAsync(object id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>A list of all entities</returns>
        Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds entities that match the specified predicate asynchronously.
        /// </summary>
        /// <param name="predicate">The predicate to filter entities</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>A list of entities that match the predicate</returns>
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>The added entity</returns>
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>The updated entity</returns>
        Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to remove</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a queryable collection of entities.
        /// </summary>
        /// <returns>A queryable collection of entities</returns>
        IQueryable<TEntity> Query();
    }
} 