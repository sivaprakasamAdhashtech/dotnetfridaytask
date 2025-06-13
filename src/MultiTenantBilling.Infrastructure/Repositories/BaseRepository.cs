using MongoDB.Driver;
using MultiTenantBilling.Core.Interfaces;
using System.Linq.Expressions;

namespace MultiTenantBilling.Infrastructure.Repositories;

public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly IMongoCollection<T> _collection;

    protected BaseRepository(IMongoCollection<T> collection)
    {
        _collection = collection;
    }

    public virtual async Task<T?> GetByIdAsync(string id)
    {
        var filter = Builders<T>.Filter.Eq("_id", id);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _collection.Find(predicate).ToListAsync();
    }

    public virtual async Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate)
    {
        return await _collection.Find(predicate).FirstOrDefaultAsync();
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        await _collection.InsertOneAsync(entity);
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty == null)
            throw new InvalidOperationException("Entity must have an Id property");

        var id = idProperty.GetValue(entity)?.ToString();
        if (string.IsNullOrEmpty(id))
            throw new InvalidOperationException("Entity Id cannot be null or empty");

        var filter = Builders<T>.Filter.Eq("_id", id);
        await _collection.ReplaceOneAsync(filter, entity);
        return entity;
    }

    public virtual async Task<bool> DeleteAsync(string id)
    {
        var filter = Builders<T>.Filter.Eq("_id", id);
        var result = await _collection.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }

    public virtual async Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        if (predicate == null)
            return await _collection.CountDocumentsAsync(_ => true);
        
        return await _collection.CountDocumentsAsync(predicate);
    }

    public virtual async Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? predicate = null)
    {
        var skip = (page - 1) * pageSize;
        
        if (predicate == null)
        {
            return await _collection
                .Find(_ => true)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();
        }
        
        return await _collection
            .Find(predicate)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();
    }
}
