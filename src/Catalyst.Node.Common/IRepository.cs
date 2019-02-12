using System;
using System.Collections.Generic;

namespace Catalyst.Node.Common
{
    public interface IRepository<T> where T : EntityBase
    {
        T Find(long key);
        void Add(T entity);
        void Update(T item);
        void Remove(long key);
        T GetById(Int64 id);
        IEnumerable<T> GetAll();
    }
}