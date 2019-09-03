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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Catalyst.Abstractions.Repository;
using SharpRepository.Repository;
using SharpRepository.Repository.Caching;
using SharpRepository.Repository.FetchStrategies;
using SharpRepository.Repository.Queries;
using SharpRepository.Repository.Specifications;
using SharpRepository.Repository.Transactions;

namespace Catalyst.Core.Repository
{
    public class RepositoryWrapper<T> : IRepositoryWrapper<T> where T : class, IDocument
    {
        public RepositoryWrapper(IRepository<T, string> repository)
        {
            Repository = repository;
        }

        protected readonly IRepository<T, string> Repository;

        public IRepositoryConventions Conventions
        {
            get => Repository.Conventions;
            set => Repository.Conventions = value;
        }

        public Type EntityType => Repository.EntityType;

        public Type KeyType => Repository.KeyType;

        public ICachingStrategy<T, string> CachingStrategy
        {
            get => Repository.CachingStrategy;
            set => Repository.CachingStrategy = value;
        }

        public bool CachingEnabled { get => Repository.CachingEnabled; set => Repository.CachingEnabled = value; }

        public bool CacheUsed => Repository.CacheUsed;

        public string TraceInfo => Repository.TraceInfo;

        public bool GenerateKeyOnAdd { get => Repository.GenerateKeyOnAdd; set => Repository.GenerateKeyOnAdd = value; }

        public void Add(T entity)
        {
            Repository.Add(entity);
        }

        public void Add(IEnumerable<T> entities)
        {
            Repository.Add(entities);
        }

        public IQueryable<T> AsQueryable()
        {
            return Repository.AsQueryable();
        }

        public double Average(Expression<Func<T, int>> selector)
        {
            return Repository.Average(selector);
        }

        public double Average(ISpecification<T> criteria, Expression<Func<T, int>> selector)
        {
            return Repository.Average(criteria, selector);
        }

        public double Average(Expression<Func<T, bool>> predicate, Expression<Func<T, int>> selector)
        {
            return Repository.Average(predicate, selector);
        }

        public double? Average(Expression<Func<T, int?>> selector)
        {
            return Repository.Average(selector);
        }

        public double? Average(ISpecification<T> criteria, Expression<Func<T, int?>> selector)
        {
            return Repository.Average(criteria, selector);
        }

        public double? Average(Expression<Func<T, bool>> predicate, Expression<Func<T, int?>> selector)
        {
            return Repository.Average(predicate, selector);
        }

        public double Average(Expression<Func<T, long>> selector)
        {
            return Repository.Average(selector);
        }

        public double Average(ISpecification<T> criteria, Expression<Func<T, long>> selector)
        {
            return Repository.Average(criteria, selector);
        }

        public double Average(Expression<Func<T, bool>> predicate, Expression<Func<T, long>> selector)
        {
            return Repository.Average(predicate, selector);
        }

        public double? Average(Expression<Func<T, long?>> selector)
        {
            return Repository.Average(selector);
        }

        public double? Average(ISpecification<T> criteria, Expression<Func<T, long?>> selector)
        {
            return Repository.Average(criteria, selector);
        }

        public double? Average(Expression<Func<T, bool>> predicate, Expression<Func<T, long?>> selector)
        {
            return Repository.Average(predicate, selector);
        }

        public decimal Average(Expression<Func<T, decimal>> selector)
        {
            return Repository.Average(selector);
        }

        public decimal Average(ISpecification<T> criteria, Expression<Func<T, decimal>> selector)
        {
            return Repository.Average(criteria, selector);
        }

        public decimal Average(Expression<Func<T, bool>> predicate, Expression<Func<T, decimal>> selector)
        {
            return Repository.Average(predicate, selector);
        }

        public decimal? Average(Expression<Func<T, decimal?>> selector)
        {
            return Repository.Average(selector);
        }

        public decimal? Average(ISpecification<T> criteria, Expression<Func<T, decimal?>> selector)
        {
            return Repository.Average(criteria, selector);
        }

        public decimal? Average(Expression<Func<T, bool>> predicate, Expression<Func<T, decimal?>> selector)
        {
            return Repository.Average(predicate, selector);
        }

        public double Average(Expression<Func<T, double>> selector)
        {
            return Repository.Average(selector);
        }

        public double Average(ISpecification<T> criteria, Expression<Func<T, double>> selector)
        {
            return Repository.Average(criteria, selector);
        }

        public double Average(Expression<Func<T, bool>> predicate, Expression<Func<T, double>> selector)
        {
            return Repository.Average(predicate, selector);
        }

        public double? Average(Expression<Func<T, double?>> selector)
        {
            return Repository.Average(selector);
        }

        public double? Average(ISpecification<T> criteria, Expression<Func<T, double?>> selector)
        {
            return Repository.Average(criteria, selector);
        }

        public double? Average(Expression<Func<T, bool>> predicate, Expression<Func<T, double?>> selector)
        {
            return Repository.Average(predicate, selector);
        }

        public float Average(Expression<Func<T, float>> selector)
        {
            return Repository.Average(selector);
        }

        public float Average(ISpecification<T> criteria, Expression<Func<T, float>> selector)
        {
            return Repository.Average(criteria, selector);
        }

        public float Average(Expression<Func<T, bool>> predicate, Expression<Func<T, float>> selector)
        {
            return Repository.Average(predicate, selector);
        }

        public float? Average(Expression<Func<T, float?>> selector)
        {
            return Repository.Average(selector);
        }

        public float? Average(ISpecification<T> criteria, Expression<Func<T, float?>> selector)
        {
            return Repository.Average(criteria, selector);
        }

        public float? Average(Expression<Func<T, bool>> predicate, Expression<Func<T, float?>> selector)
        {
            return Repository.Average(predicate, selector);
        }

        public IBatch<T> BeginBatch()
        {
            return Repository.BeginBatch();
        }

        public void ClearCache()
        {
            Repository.ClearCache();
        }

        public int Count()
        {
            return Repository.Count();
        }

        public int Count(ISpecification<T> criteria)
        {
            return Repository.Count(criteria);
        }

        public int Count(Expression<Func<T, bool>> predicate)
        {
            return Repository.Count(predicate);
        }

        public void Delete(string key)
        {
            Repository.Delete(key);
        }

        public void Delete(IEnumerable<string> keys)
        {
            Repository.Delete(keys);
        }

        public void Delete(params string[] keys)
        {
            Repository.Delete(keys);
        }

        public void Delete(T entity)
        {
            Repository.Delete(entity);
        }

        public void Delete(IEnumerable<T> entities)
        {
            Repository.Delete(entities);
        }

        public void Delete(Expression<Func<T, bool>> predicate)
        {
            Repository.Delete(predicate);
        }

        public void Delete(ISpecification<T> criteria)
        {
            Repository.Delete(criteria);
        }

        public IDisabledCache DisableCaching()
        {
            return Repository.DisableCaching();
        }

        public void Dispose()
        {
            Repository.Dispose();
        }

        public bool Exists(string key)
        {
            return Repository.Exists(key);
        }

        public bool Exists(Expression<Func<T, bool>> predicate)
        {
            return Repository.Exists(predicate);
        }

        public bool Exists(ISpecification<T> criteria)
        {
            return Repository.Exists(criteria);
        }

        public T Find(Expression<Func<T, bool>> predicate, IQueryOptions<T> queryOptions = null)
        {
            return Repository.Find(predicate, queryOptions);
        }

        public TResult Find<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions = null)
        {
            return Repository.Find(predicate, selector, queryOptions);
        }

        public T Find(ISpecification<T> criteria, IQueryOptions<T> queryOptions = null)
        {
            return Repository.Find(criteria, queryOptions);
        }

        public TResult Find<TResult>(ISpecification<T> criteria, Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions = null)
        {
            return Repository.Find(criteria, selector, queryOptions);
        }

        public IEnumerable<T> FindAll(Expression<Func<T, bool>> predicate, IQueryOptions<T> queryOptions = null)
        {
            return Repository.FindAll(predicate, queryOptions);
        }

        public IEnumerable<TResult> FindAll<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions = null)
        {
            return Repository.FindAll(predicate, selector, queryOptions);
        }

        public IEnumerable<T> FindAll(ISpecification<T> criteria, IQueryOptions<T> queryOptions = null)
        {
            return Repository.FindAll(criteria, queryOptions);
        }

        public IEnumerable<TResult> FindAll<TResult>(ISpecification<T> criteria, Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions = null)
        {
            return Repository.FindAll(criteria, selector, queryOptions);
        }

        public T Get(string key)
        {
            return Repository.Get(key);
        }

        public T Get(string key, IFetchStrategy<T> fetchStrategy)
        {
            return Repository.Get(key, fetchStrategy);
        }

        public T Get(string key, params string[] includePaths)
        {
            return Repository.Get(key, includePaths);
        }

        public T Get(string key, params Expression<Func<T, object>>[] includePaths)
        {
            return Repository.Get(key, includePaths);
        }

        public TResult Get<TResult>(string key, Expression<Func<T, TResult>> selector)
        {
            return Repository.Get(key, selector);
        }

        public TResult Get<TResult>(string key, Expression<Func<T, TResult>> selector, IFetchStrategy<T> fetchStrategy)
        {
            return Repository.Get(key, selector, fetchStrategy);
        }

        public TResult Get<TResult>(string key, Expression<Func<T, TResult>> selector, params Expression<Func<T, object>>[] includePaths)
        {
            return Repository.Get(key, selector, includePaths);
        }

        public TResult Get<TResult>(string key, Expression<Func<T, TResult>> selector, params string[] includePaths)
        {
            return Repository.Get(key, selector, includePaths);
        }

        public IEnumerable<T> GetAll()
        {
            return Repository.GetAll();
        }

        public IEnumerable<T> GetAll(IFetchStrategy<T> fetchStrategy)
        {
            return Repository.GetAll(fetchStrategy);
        }

        public IEnumerable<T> GetAll(params string[] includePaths)
        {
            return Repository.GetAll(includePaths);
        }

        public IEnumerable<T> GetAll(params Expression<Func<T, object>>[] includePaths)
        {
            return Repository.GetAll(includePaths);
        }

        public IEnumerable<T> GetAll(IQueryOptions<T> queryOptions)
        {
            return Repository.GetAll(queryOptions);
        }

        public IEnumerable<T> GetAll(IQueryOptions<T> queryOptions, IFetchStrategy<T> fetchStrategy)
        {
            return Repository.GetAll(queryOptions, fetchStrategy);
        }

        public IEnumerable<T> GetAll(IQueryOptions<T> queryOptions, params string[] includePaths)
        {
            return Repository.GetAll(queryOptions, includePaths);
        }

        public IEnumerable<T> GetAll(IQueryOptions<T> queryOptions, params Expression<Func<T, object>>[] includePaths)
        {
            return Repository.GetAll(queryOptions, includePaths);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector)
        {
            return Repository.GetAll(selector);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions)
        {
            return Repository.GetAll(selector, queryOptions);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector, IFetchStrategy<T> fetchStrategy)
        {
            return Repository.GetAll(selector, fetchStrategy);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector, params string[] includePaths)
        {
            return Repository.GetAll(selector, includePaths);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector, params Expression<Func<T, object>>[] includePaths)
        {
            return Repository.GetAll(selector, includePaths);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions, IFetchStrategy<T> fetchStrategy)
        {
            return Repository.GetAll(selector, queryOptions, fetchStrategy);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions, params string[] includePaths)
        {
            return Repository.GetAll(selector, queryOptions, includePaths);
        }

        public IEnumerable<TResult> GetAll<TResult>(Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions, params Expression<Func<T, object>>[] includePaths)
        {
            return Repository.GetAll(selector, queryOptions, includePaths);
        }

        public IEnumerable<T> GetMany(params string[] keys)
        {
            return Repository.GetMany(keys);
        }

        public IEnumerable<T> GetMany(IEnumerable<string> keys)
        {
            return Repository.GetMany(keys);
        }

        public IEnumerable<T> GetMany(IEnumerable<string> keys, IFetchStrategy<T> fetchStrategy)
        {
            return Repository.GetMany(keys, fetchStrategy);
        }

        public IEnumerable<TResult> GetMany<TResult>(Expression<Func<T, TResult>> selector, params string[] keys)
        {
            return Repository.GetMany(selector, keys);
        }

        public IEnumerable<TResult> GetMany<TResult>(IEnumerable<string> keys, Expression<Func<T, TResult>> selector)
        {
            return Repository.GetMany(keys, selector);
        }

        public IDictionary<string, T> GetManyAsDictionary(params string[] keys)
        {
            return Repository.GetManyAsDictionary(keys);
        }

        public IDictionary<string, T> GetManyAsDictionary(IEnumerable<string> keys)
        {
            return Repository.GetManyAsDictionary(keys);
        }

        public IDictionary<string, T> GetManyAsDictionary(IEnumerable<string> keys, IFetchStrategy<T> fetchStrategy)
        {
            return Repository.GetManyAsDictionary(keys, fetchStrategy);
        }

        public string GetPrimaryKey(T entity)
        {
            return Repository.GetPrimaryKey(entity);
        }

        public IEnumerable<TResult> GroupBy<TGroupKey, TResult>(Expression<Func<T, TGroupKey>> keySelector, Expression<Func<IGrouping<TGroupKey, T>, TResult>> resultSelector)
        {
            return Repository.GroupBy(keySelector, resultSelector);
        }

        public IEnumerable<TResult> GroupBy<TGroupKey, TResult>(ISpecification<T> criteria, 
            Expression<Func<T, TGroupKey>> keySelector,
            Expression<Func<IGrouping<TGroupKey, T>, TResult>> resultSelector)
        {
            return Repository.GroupBy(criteria, keySelector, resultSelector);
        }

        public IEnumerable<TResult> GroupBy<TGroupKey, TResult>(Expression<Func<T, bool>> predicate, 
            Expression<Func<T, TGroupKey>> keySelector, 
            Expression<Func<IGrouping<TGroupKey, T>, TResult>> resultSelector)
        {
            return Repository.GroupBy(predicate, keySelector, resultSelector);
        }

        public IDictionary<TGroupKey, int> GroupCount<TGroupKey>(Expression<Func<T, TGroupKey>> selector)
        {
            return Repository.GroupCount(selector);
        }

        public IDictionary<TGroupKey, int> GroupCount<TGroupKey>(ISpecification<T> criteria, Expression<Func<T, TGroupKey>> selector)
        {
            return Repository.GroupCount(criteria, selector);
        }

        public IDictionary<TGroupKey, int> GroupCount<TGroupKey>(Expression<Func<T, bool>> predicate, Expression<Func<T, TGroupKey>> selector)
        {
            return Repository.GroupCount(predicate, selector);
        }

        public IDictionary<TGroupKey, long> GroupLongCount<TGroupKey>(Expression<Func<T, TGroupKey>> selector)
        {
            return Repository.GroupLongCount(selector);
        }

        public IDictionary<TGroupKey, long> GroupLongCount<TGroupKey>(ISpecification<T> criteria, Expression<Func<T, TGroupKey>> selector)
        {
            return Repository.GroupLongCount(criteria, selector);
        }

        public IDictionary<TGroupKey, long> GroupLongCount<TGroupKey>(Expression<Func<T, bool>> predicate, 
            Expression<Func<T, TGroupKey>> 
                selector)
        {
            return Repository.GroupLongCount(predicate, selector);
        }

        public IRepositoryQueryable<TResult> Join<TJoinKey, TInner, TResult>(IRepositoryQueryable<TInner> innerRepository, 
            Expression<Func<T, TJoinKey>> outerKeySelector, 
            Expression<Func<TInner, TJoinKey>> innerKeySelector, 
            Expression<Func<T, TInner, TResult>> resultSelector) where TInner : class where TResult : class
        {
            return Repository.Join(innerRepository, outerKeySelector, innerKeySelector, resultSelector);
        }

        public long LongCount()
        {
            return Repository.LongCount();
        }

        public long LongCount(ISpecification<T> criteria)
        {
            return Repository.LongCount(criteria);
        }

        public long LongCount(Expression<Func<T, bool>> predicate)
        {
            return Repository.LongCount(predicate);
        }

        public TResult Max<TResult>(Expression<Func<T, TResult>> selector)
        {
            return Repository.Max(selector);
        }

        public TResult Max<TResult>(ISpecification<T> criteria, Expression<Func<T, TResult>> selector)
        {
            return Repository.Max(criteria, selector);
        }

        public TResult Max<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector)
        {
            return Repository.Max(predicate, selector);
        }

        public TResult Min<TResult>(Expression<Func<T, TResult>> selector)
        {
            return Repository.Min(selector);
        }

        public TResult Min<TResult>(ISpecification<T> criteria, Expression<Func<T, TResult>> selector)
        {
            return Repository.Min(criteria, selector);
        }

        public TResult Min<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector)
        {
            return Repository.Min(predicate, selector);
        }

        public int Sum(Expression<Func<T, int>> selector)
        {
            return Repository.Sum(selector);
        }

        public int Sum(ISpecification<T> criteria, Expression<Func<T, int>> selector)
        {
            return Repository.Sum(criteria, selector);
        }

        public int Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, int>> selector)
        {
            return Repository.Sum(predicate, selector);
        }

        public int? Sum(Expression<Func<T, int?>> selector)
        {
            return Repository.Sum(selector);
        }

        public int? Sum(ISpecification<T> criteria, Expression<Func<T, int?>> selector)
        {
            return Repository.Sum(criteria, selector);
        }

        public int? Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, int?>> selector)
        {
            return Repository.Sum(predicate, selector);
        }

        public long Sum(Expression<Func<T, long>> selector)
        {
            return Repository.Sum(selector);
        }

        public long Sum(ISpecification<T> criteria, Expression<Func<T, long>> selector)
        {
            return Repository.Sum(criteria, selector);
        }

        public long Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, long>> selector)
        {
            return Repository.Sum(predicate, selector);
        }

        public long? Sum(Expression<Func<T, long?>> selector)
        {
            return Repository.Sum(selector);
        }

        public long? Sum(ISpecification<T> criteria, Expression<Func<T, long?>> selector)
        {
            return Repository.Sum(criteria, selector);
        }

        public long? Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, long?>> selector)
        {
            return Repository.Sum(predicate, selector);
        }

        public decimal Sum(Expression<Func<T, decimal>> selector)
        {
            return Repository.Sum(selector);
        }

        public decimal Sum(ISpecification<T> criteria, Expression<Func<T, decimal>> selector)
        {
            return Repository.Sum(criteria, selector);
        }

        public decimal Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, decimal>> selector)
        {
            return Repository.Sum(predicate, selector);
        }

        public decimal? Sum(Expression<Func<T, decimal?>> selector)
        {
            return Repository.Sum(selector);
        }

        public decimal? Sum(ISpecification<T> criteria, Expression<Func<T, decimal?>> selector)
        {
            return Repository.Sum(criteria, selector);
        }

        public decimal? Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, decimal?>> selector)
        {
            return Repository.Sum(predicate, selector);
        }

        public double Sum(Expression<Func<T, double>> selector)
        {
            return Repository.Sum(selector);
        }

        public double Sum(ISpecification<T> criteria, Expression<Func<T, double>> selector)
        {
            return Repository.Sum(criteria, selector);
        }

        public double Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, double>> selector)
        {
            return Repository.Sum(predicate, selector);
        }

        public double? Sum(Expression<Func<T, double?>> selector)
        {
            return Repository.Sum(selector);
        }

        public double? Sum(ISpecification<T> criteria, Expression<Func<T, double?>> selector)
        {
            return Repository.Sum(criteria, selector);
        }

        public double? Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, double?>> selector)
        {
            return Repository.Sum(predicate, selector);
        }

        public float Sum(Expression<Func<T, float>> selector)
        {
            return Repository.Sum(selector);
        }

        public float Sum(ISpecification<T> criteria, Expression<Func<T, float>> selector)
        {
            return Repository.Sum(criteria, selector);
        }

        public float Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, float>> selector)
        {
            return Repository.Sum(predicate, selector);
        }

        public float? Sum(Expression<Func<T, float?>> selector)
        {
            return Repository.Sum(selector);
        }

        public float? Sum(ISpecification<T> criteria, Expression<Func<T, float?>> selector)
        {
            return Repository.Sum(criteria, selector);
        }

        public float? Sum(Expression<Func<T, bool>> predicate, Expression<Func<T, float?>> selector)
        {
            return Repository.Sum(predicate, selector);
        }

        public bool TryFind(Expression<Func<T, bool>> predicate, out T entity)
        {
            return Repository.TryFind(predicate, out entity);
        }

        public bool TryFind(Expression<Func<T, bool>> predicate, IQueryOptions<T> queryOptions, out T entity)
        {
            return Repository.TryFind(predicate, queryOptions, out entity);
        }

        public bool TryFind<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector, out TResult entity)
        {
            return Repository.TryFind(predicate, selector, out entity);
        }

        public bool TryFind<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions, out TResult entity)
        {
            return Repository.TryFind(predicate, selector, queryOptions, out entity);
        }

        public bool TryFind(ISpecification<T> criteria, out T entity)
        {
            return Repository.TryFind(criteria, out entity);
        }

        public bool TryFind(ISpecification<T> criteria, IQueryOptions<T> queryOptions, out T entity)
        {
            return Repository.TryFind(criteria, queryOptions, out entity);
        }

        public bool TryFind<TResult>(ISpecification<T> criteria, Expression<Func<T, TResult>> selector, out TResult entity)
        {
            return Repository.TryFind(criteria, selector, out entity);
        }

        public bool TryFind<TResult>(ISpecification<T> criteria, Expression<Func<T, TResult>> selector, IQueryOptions<T> queryOptions, out TResult entity)
        {
            return Repository.TryFind(criteria, selector, queryOptions, out entity);
        }

        public bool TryGet(string key, out T entity)
        {
            return Repository.TryGet(key, out entity);
        }

        public bool TryGet<TResult>(string key, Expression<Func<T, TResult>> selector, out TResult entity)
        {
            return Repository.TryGet(key, selector, out entity);
        }

        public void Update(T entity)
        {
            Repository.Update(entity);
        }

        public void Update(IEnumerable<T> entities)
        {
            Repository.Update(entities);
        }
    }
}
