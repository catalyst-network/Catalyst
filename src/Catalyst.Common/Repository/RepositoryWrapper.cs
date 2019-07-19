#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using Catalyst.Common.Interfaces.Repository;
using SharpRepository.Repository;
using SharpRepository.Repository.Caching;
using SharpRepository.Repository.FetchStrategies;
using SharpRepository.Repository.Queries;
using SharpRepository.Repository.Specifications;
using SharpRepository.Repository.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Catalyst.Common.Repository
{
    public class RepositoryWrapper<T> : IRepositoryWrapper<T> where T : class, IDocument
    {
        public RepositoryWrapper(IRepository<T, string> repository)
        {
            _repository = repository;
        }

        private readonly IRepository<T, string> _repository;

        public IRepositoryConventions Conventions
        {
            get => _repository.Conventions;
            set => _repository.Conventions = value;
        }

        public Type EntityType => _repository.EntityType;

        public Type KeyType => _repository.KeyType;

        public ICachingStrategy<T, string> CachingStrategy
        {
            get => _repository.CachingStrategy;
            set => _repository.CachingStrategy = value;
        }

        public bool CachingEnabled { get => _repository.CachingEnabled; set => _repository.CachingEnabled = value; }

        public bool CacheUsed => _repository.CacheUsed;

        public string TraceInfo => _repository.TraceInfo;

        public bool GenerateKeyOnAdd { get => _repository.GenerateKeyOnAdd; set => _repository.GenerateKeyOnAdd = value; }

        public void Add(T entity)
        {
            _repository.Add(entity);
        }

        public void Add(IEnumerable<T> entities)
        {
            _repository.Add(entities);
        }

        public IQueryable<T> AsQueryable()
        {
            return _repository.AsQueryable();
        }

        public double Average(Expression<Func<T, int>> selector)
        {
            return _repository.Average(selector);
        }

        public double Average(ISpecification<T> criteria, Expression<Func<T, int>> selector)
        {
            return _repository.Average(criteria, selector);
        }

        public double Average(Expression<Func<T, bool>> predicate, Expression<Func<T, int>> selector)
        {
            return _repository.Average(predicate, selector);
        }

        public double? Average(Expression<Func<T, int?>> selector)
        {
            return _repository.Average(selector);
        }

        public double? Average(ISpecification<T> criteria, Expression<Func<T, int?>> selector)
        {
            return _repository.Average(criteria, selector);
        }

        public double? Average(Expression<Func<T, bool>> predicate, Expression<Func<T, int?>> selector)
        {
            return _repository.Average(predicate, selector);
        }

        public double Average(Expression<Func<T, long>> selector)
        {
            return _repository.Average(selector);
        }

        public double Average(ISpecification<T> criteria, Expression<Func<T, long>> selector)
        {
            return _repository.Average(criteria, selector);
        }

        public double Average(Expression<Func<T, bool>> predicate, Expression<Func<T, long>> selector)
        {
            return _repository.Average(predicate, selector);
        }

        public double? Average(Expression<Func<T, long?>> selector)
        {
            return _repository.Average(selector);
        }

        public double? Average(ISpecification<T> criteria, Expression<Func<T, long?>> selector)
        {
            return _repository.Average(criteria, selector);
        }

        public double? Average(Expression<Func<T, bool>> predicate, Expression<Func<T, long?>> selector)
        {
            return _repository.Average(predicate, selector);
        }

        public decimal Average(Expression<Func<T, decimal>> selector)
        {
            return _repository.Average(selector);
        }

        public decimal Average(ISpecification<T> criteria, Expression<Func<T, decimal>> selector)
        {
            return _repository.Average(criteria, selector);
        }

        public decimal Average(Expression<Func<T, bool>> predicate, Expression<Func<T, decimal>> selector)
        {
            return _repository.Average(predicate, selector);
        }

        public decimal? Average(Expression<Func<T, decimal?>> selector)
        {
            return _repository.Average(selector);
        }

        public decimal? Average(ISpecification<T> criteria, Expression<Func<T, decimal?>> selector)
        {
            return _repository.Average(criteria, selector);
        }

        public decimal? Average(Expression<Func<T, bool>> predicate, Expression<Func<T, decimal?>> selector)
        {
            return _repository.Average(predicate, selector);
        }

        public double Average(Expression<Func<T, double>> selector)
        {
            return _repository.Average(selector);
        }

        public double Average(ISpecification<T> criteria, Expression<Func<T, double>> selector)
        {
            return _repository.Average(criteria, selector);
        }

        public double Average(Expression<Func<T, bool>> predicate, Expression<Func<T, double>> selector)
        {
            return _repository.Average(predicate, selector);
        }

        public double? Average(Expression<Func<T, double?>> selector)
        {
            return _repository.Average(selector);
        }

        public double? Average(ISpecification<T> criteria, Expression<Func<T, double?>> selector)
        {
            return _repository.Average(criteria, selector);
        }

        public double? Average(Expression<Func<T, bool>> predicate, Expression<Func<T, double?>> selector)
        {
            return _repository.Average(predicate, selector);
        }

        public float Average(Expression<Func<T, float>> selector)
        {
            return _repository.Average(selector);
        }

        public float Average(ISpecification<T> criteria, Expression<Func<T, float>> selector)
        {
            return _repository.Average(criteria, selector);
        }

        public float Average(Expression<Func<T, bool>> predicate, Expression<Func<T, float>> selector)
        {
            return _repository.Average(predicate, selector);
        }

        public float? Average(Expression<Func<T, float?>> selector)
        {
            return _repository.Average(selector);
        }

        public float? Average(ISpecification<T> criteria, Expression<Func<T, float?>> selector)
        {
            return _repository.Average(criteria, selector);
        }

        public float? Average(Expression<Func<T, bool>> predicate, Expression<Func<T, float?>> selector)
        {
            return _repository.Average(predicate, selector);
        }

        public IBatch<T> BeginBatch()
        {
            return _repository.BeginBatch();
        }

        public void ClearCache()
        {
            _repository.ClearCache();
        }

        public int Count()
        {
            return _repository.Count();
        }

        public int Count(ISpecification<T> criteria)
        {
            return _repository.Count(criteria);
        }

        public int Count(Expression<Func<T, bool>> predicate)
        {
            return _repository.Count(predicate);
        }

        public void Delete(string key)
        {
            _repository.Delete(key);
        }

        public void Delete(IEnumerable<string> keys)
        {
            _repository.Delete(keys);
        }

        public void Delete(params string[] keys)
        {
            _repository.Delete(keys);
        }

        public void Delete(T entity)
        {
            _repository.Delete(entity);
        }

        public void Delete(IEnumerable<T> entities)
        {
            _repository.Delete(entities);
        }

        public void Delete(Expression<Func<T, bool>> predicate)
        {
            _repository.Delete(predicate);
        }

        public void Delete(ISpecification<T> criteria)
        {
            _repository.Delete(criteria);
        }

        public IDisabledCache DisableCaching()
        {
            return _repository.DisableCaching();
        }

        public void Dispose()
        {
            _repository.Dispose();
        }

        public bool Exists(string key)
        {
            return _repository.Exists(key);
        }

        public bool Exists(Expression<Func<T, bool>> predicate)
        {
            return _repository.Exists(predicate);
        }

        public bool Exists(ISpecification<T> criteria)
        {
            return _repository.Exists(criteria);
        }

        public T Find(Expression<Func<T, bool>> predicate, IQueryOptions<T> queryOptions = null)
        {
            return _repository.Find(predicate, queryOptions);
        }

        public TResult Find<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions = null)
        {
            return _repository.Find(predicate, selector, queryOptions);
        }

        public T Find(ISpecification<T> criteria, IQueryOptions<T> queryOptions = null)
        {
            return _repository.Find(criteria, queryOptions);
        }

        public TResult Find<TResult>(ISpecification<T> criteria, Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions = null)
        {
            return _repository.Find(criteria, selector, queryOptions);
        }

        public IEnumerable<T> FindAll(Expression<Func<T, bool>> predicate, IQueryOptions<T> queryOptions = null)
        {
            return _repository.FindAll(predicate, queryOptions);
        }

        public IEnumerable<TResult> FindAll<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions = null)
        {
            return _repository.FindAll(predicate, selector, queryOptions);
        }

        public IEnumerable<T> FindAll(ISpecification<T> criteria, IQueryOptions<T> queryOptions = null)
        {
            return _repository.FindAll(criteria, queryOptions);
        }

        public IEnumerable<TResult> FindAll<TResult>(ISpecification<T> criteria, Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions = null)
        {
            return _repository.FindAll(criteria, selector, queryOptions);
        }

        public T Get(string key)
        {
            return _repository.Get(key);
        }

        public T Get(string key, IFetchStrategy<T> fetchStrategy)
        {
            return _repository.Get(key, fetchStrategy);
        }

        public T Get(string key, params string[] includePaths)
        {
            return _repository.Get(key, includePaths);
        }

        public T Get(string key, params Expression<Func<T, object>>[] includePaths)
        {
            return _repository.Get(key, includePaths);
        }

        public TResult Get<TResult>(string key, Expression<Func<T, TResult>> selector)
        {
            return _repository.Get(key, selector);
        }

        public TResult Get<TResult>(string key, Expression<Func<T, TResult>> selector, IFetchStrategy<T> fetchStrategy)
        {
            return _repository.Get(key, selector, fetchStrategy);
        }

        public TResult Get<TResult>(string key, Expression<Func<T, TResult>> selector, params Expression<Func<T, object>>[] includePaths)
        {
            return _repository.Get(key, selector, includePaths);
        }

        public TResult Get<TResult>(string key, Expression<Func<T, TResult>> selector, params string[] includePaths)
        {
            return _repository.Get(key, selector, includePaths);
        }

        public IEnumerable<T> GetAll()
        {
            return _repository.GetAll();
        }

        public IEnumerable<T> GetAll(IFetchStrategy<T> fetchStrategy)
        {
            return _repository.GetAll(fetchStrategy);
        }

        public IEnumerable<T> GetAll(params string[] includePaths)
        {
            return _repository.GetAll(includePaths);
        }

        public IEnumerable<T> GetAll(params Expression<Func<T, object>>[] includePaths)
        {
            return _repository.GetAll(includePaths);
        }

        public IEnumerable<T> GetAll(IQueryOptions<T> queryOptions)
        {
            return _repository.GetAll(queryOptions);
        }

        public IEnumerable<T> GetAll(IQueryOptions<T> queryOptions, IFetchStrategy<T> fetchStrategy)
        {
            return _repository.GetAll(queryOptions, fetchStrategy);
        }

        public IEnumerable<T> GetAll(IQueryOptions<T> queryOptions, params string[] includePaths)
        {
            return _repository.GetAll(queryOptions, includePaths);
        }

        public IEnumerable<T> GetAll(IQueryOptions<T> queryOptions, params Expression<Func<T, object>>[] includePaths)
        {
            return _repository.GetAll(queryOptions, includePaths);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector)
        {
            return _repository.GetAll(selector);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions)
        {
            return _repository.GetAll(selector, queryOptions);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector, IFetchStrategy<T> fetchStrategy)
        {
            return _repository.GetAll(selector, fetchStrategy);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector, params string[] includePaths)
        {
            return _repository.GetAll(selector, includePaths);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector, params Expression<Func<T, object>>[] includePaths)
        {
            return _repository.GetAll(selector, includePaths);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions, IFetchStrategy<T> fetchStrategy)
        {
            return _repository.GetAll(selector, queryOptions, fetchStrategy);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions, params string[] includePaths)
        {
            return _repository.GetAll(selector, queryOptions, includePaths);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions, params Expression<Func<T, object>>[] includePaths)
        {
            return _repository.GetAll(selector, queryOptions, includePaths);
        }

        public IEnumerable<T> GetMany(params string[] keys)
        {
            return _repository.GetMany(keys);
        }

        public IEnumerable<T> GetMany(IEnumerable<string> keys)
        {
            return _repository.GetMany(keys);
        }

        public IEnumerable<T> GetMany(IEnumerable<string> keys, IFetchStrategy<T> fetchStrategy)
        {
            return _repository.GetMany(keys, fetchStrategy);
        }

        public IEnumerable<TResult> GetMany<TResult>(Expression<Func<T, TResult>> selector, params string[] keys)
        {
            return _repository.GetMany(selector, keys);
        }

        public IEnumerable<TResult> GetMany<TResult>(IEnumerable<string> keys, Expression<Func<T, TResult>> selector)
        {
            return _repository.GetMany(keys, selector);
        }

        public IDictionary<string, T> GetManyAsDictionary(params string[] keys)
        {
            return _repository.GetManyAsDictionary(keys);
        }

        public IDictionary<string, T> GetManyAsDictionary(IEnumerable<string> keys)
        {
            return _repository.GetManyAsDictionary(keys);
        }

        public IDictionary<string, T> GetManyAsDictionary(IEnumerable<string> keys, IFetchStrategy<T> fetchStrategy)
        {
            return _repository.GetManyAsDictionary(keys, fetchStrategy);
        }

        public string GetPrimaryKey(T entity)
        {
            return _repository.GetPrimaryKey(entity);
        }

        public IEnumerable<TResult> GroupBy<TGroupKey, TResult>(Expression<Func<T, TGroupKey>> keySelector, Expression<Func<IGrouping<TGroupKey, T>, TResult>> resultSelector)
        {
            return _repository.GroupBy(keySelector, resultSelector);
        }

        public IEnumerable<TResult> GroupBy<TGroupKey, TResult>(ISpecification<T> criteria, 
            Expression<Func<T, TGroupKey>> keySelector,
            Expression<Func<IGrouping<TGroupKey, T>, TResult>> resultSelector)
        {
            return _repository.GroupBy(criteria, keySelector, resultSelector);
        }

        public IEnumerable<TResult> GroupBy<TGroupKey, TResult>(Expression<Func<T, bool>> predicate, 
            Expression<Func<T, TGroupKey>> keySelector, 
            Expression<Func<IGrouping<TGroupKey, T>, TResult>> resultSelector)
        {
            return _repository.GroupBy(predicate, keySelector, resultSelector);
        }

        public IDictionary<TGroupKey, int> GroupCount<TGroupKey>(Expression<Func<T, TGroupKey>> selector)
        {
            return _repository.GroupCount(selector);
        }

        public IDictionary<TGroupKey, int> GroupCount<TGroupKey>(ISpecification<T> criteria, Expression<Func<T, TGroupKey>> selector)
        {
            return _repository.GroupCount(criteria, selector);
        }

        public IDictionary<TGroupKey, int> GroupCount<TGroupKey>(Expression<Func<T, bool>> predicate, Expression<Func<T, TGroupKey>> selector)
        {
            return _repository.GroupCount(predicate, selector);
        }

        public IDictionary<TGroupKey, long> GroupLongCount<TGroupKey>(Expression<Func<T, TGroupKey>> selector)
        {
            return _repository.GroupLongCount(selector);
        }

        public IDictionary<TGroupKey, long> GroupLongCount<TGroupKey>(ISpecification<T> criteria, Expression<Func<T, TGroupKey>> selector)
        {
            return _repository.GroupLongCount(criteria, selector);
        }

        public IDictionary<TGroupKey, long> GroupLongCount<TGroupKey>(Expression<Func<T, bool>> predicate, 
            Expression<Func<T, TGroupKey>> 
                selector)
        {
            return _repository.GroupLongCount(predicate, selector);
        }

        public IRepositoryQueryable<TResult> Join<TJoinKey, TInner, TResult>(IRepositoryQueryable<TInner> innerRepository, 
            Expression<Func<T, TJoinKey>> outerKeySelector, 
            Expression<Func<TInner, TJoinKey>> innerKeySelector, 
            Expression<Func<T, TInner, TResult>> resultSelector) where TInner : class where TResult : class
        {
            return _repository.Join(innerRepository, outerKeySelector, innerKeySelector, resultSelector);
        }

        public long LongCount()
        {
            return _repository.LongCount();
        }

        public long LongCount(ISpecification<T> criteria)
        {
            return _repository.LongCount(criteria);
        }

        public long LongCount(Expression<Func<T, bool>> predicate)
        {
            return _repository.LongCount(predicate);
        }

        public TResult Max<TResult>(Expression<Func<T, TResult>> selector)
        {
            return _repository.Max(selector);
        }

        public TResult Max<TResult>(ISpecification<T> criteria, Expression<Func<T, TResult>> selector)
        {
            return _repository.Max(criteria, selector);
        }

        public TResult Max<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector)
        {
            return _repository.Max(predicate, selector);
        }

        public TResult Min<TResult>(Expression<Func<T, TResult>> selector)
        {
            return _repository.Min(selector);
        }

        public TResult Min<TResult>(ISpecification<T> criteria, Expression<Func<T, TResult>> selector)
        {
            return _repository.Min(criteria, selector);
        }

        public TResult Min<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector)
        {
            return _repository.Min(predicate, selector);
        }

        public int Sum(Expression<Func<T, int>> selector)
        {
            return _repository.Sum(selector);
        }

        public int Sum(ISpecification<T> criteria, Expression<Func<T, int>> selector)
        {
            return _repository.Sum(criteria, selector);
        }

        public int Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, int>> selector)
        {
            return _repository.Sum(predicate, selector);
        }

        public int? Sum(Expression<Func<T, int?>> selector)
        {
            return _repository.Sum(selector);
        }

        public int? Sum(ISpecification<T> criteria, Expression<Func<T, int?>> selector)
        {
            return _repository.Sum(criteria, selector);
        }

        public int? Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, int?>> selector)
        {
            return _repository.Sum(predicate, selector);
        }

        public long Sum(Expression<Func<T, long>> selector)
        {
            return _repository.Sum(selector);
        }

        public long Sum(ISpecification<T> criteria, Expression<Func<T, long>> selector)
        {
            return _repository.Sum(criteria, selector);
        }

        public long Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, long>> selector)
        {
            return _repository.Sum(predicate, selector);
        }

        public long? Sum(Expression<Func<T, long?>> selector)
        {
            return _repository.Sum(selector);
        }

        public long? Sum(ISpecification<T> criteria, Expression<Func<T, long?>> selector)
        {
            return _repository.Sum(criteria, selector);
        }

        public long? Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, long?>> selector)
        {
            return _repository.Sum(predicate, selector);
        }

        public decimal Sum(Expression<Func<T, decimal>> selector)
        {
            return _repository.Sum(selector);
        }

        public decimal Sum(ISpecification<T> criteria, Expression<Func<T, decimal>> selector)
        {
            return _repository.Sum(criteria, selector);
        }

        public decimal Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, decimal>> selector)
        {
            return _repository.Sum(predicate, selector);
        }

        public decimal? Sum(Expression<Func<T, decimal?>> selector)
        {
            return _repository.Sum(selector);
        }

        public decimal? Sum(ISpecification<T> criteria, Expression<Func<T, decimal?>> selector)
        {
            return _repository.Sum(criteria, selector);
        }

        public decimal? Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, decimal?>> selector)
        {
            return _repository.Sum(predicate, selector);
        }

        public double Sum(Expression<Func<T, double>> selector)
        {
            return _repository.Sum(selector);
        }

        public double Sum(ISpecification<T> criteria, Expression<Func<T, double>> selector)
        {
            return _repository.Sum(criteria, selector);
        }

        public double Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, double>> selector)
        {
            return _repository.Sum(predicate, selector);
        }

        public double? Sum(Expression<Func<T, double?>> selector)
        {
            return _repository.Sum(selector);
        }

        public double? Sum(ISpecification<T> criteria, Expression<Func<T, double?>> selector)
        {
            return _repository.Sum(criteria, selector);
        }

        public double? Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, double?>> selector)
        {
            return _repository.Sum(predicate, selector);
        }

        public float Sum(Expression<Func<T, float>> selector)
        {
            return _repository.Sum(selector);
        }

        public float Sum(ISpecification<T> criteria, Expression<Func<T, float>> selector)
        {
            return _repository.Sum(criteria, selector);
        }

        public float Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, float>> selector)
        {
            return _repository.Sum(predicate, selector);
        }

        public float? Sum(Expression<Func<T, float?>> selector)
        {
            return _repository.Sum(selector);
        }

        public float? Sum(ISpecification<T> criteria, Expression<Func<T, float?>> selector)
        {
            return _repository.Sum(criteria, selector);
        }

        public float? Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, float?>> selector)
        {
            return _repository.Sum(predicate, selector);
        }

        public bool TryFind(Expression<Func<T, bool>> predicate, out T entity)
        {
            return _repository.TryFind(predicate, out entity);
        }

        public bool TryFind(Expression<Func<T, bool>> predicate, IQueryOptions<T> queryOptions, out T entity)
        {
            return _repository.TryFind(predicate, queryOptions, out entity);
        }

        public bool TryFind<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector, out TResult entity)
        {
            return _repository.TryFind(predicate, selector, out entity);
        }

        public bool TryFind<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions, out TResult entity)
        {
            return _repository.TryFind(predicate, selector, queryOptions, out entity);
        }

        public bool TryFind(ISpecification<T> criteria, out T entity)
        {
            return _repository.TryFind(criteria, out entity);
        }

        public bool TryFind(ISpecification<T> criteria, IQueryOptions<T> queryOptions, out T entity)
        {
            return _repository.TryFind(criteria, queryOptions, out entity);
        }

        public bool TryFind<TResult>(ISpecification<T> criteria, Expression<Func<T, TResult>> selector, out TResult entity)
        {
            return _repository.TryFind(criteria, selector, out entity);
        }

        public bool TryFind<TResult>(ISpecification<T> criteria, Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions, out TResult entity)
        {
            return _repository.TryFind(criteria, selector, queryOptions, out entity);
        }

        public bool TryGet(string key, out T entity)
        {
            return _repository.TryGet(key, out entity);
        }

        public bool TryGet<TResult>(string key, Expression<Func<T, TResult>> selector, out TResult entity)
        {
            return _repository.TryGet(key, selector, out entity);
        }

        public void Update(T entity)
        {
            _repository.Update(entity);
        }

        public void Update(IEnumerable<T> entities)
        {
            _repository.Update(entities);
        }
    }
}
