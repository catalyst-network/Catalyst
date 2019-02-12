using System;
using System.Collections.Generic;
using Catalyst.Node.Common;

namespace Catalyst.Node.Core.P2P
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PeerRepository<T> : IRepository<T> where T : EntityBase
    {
        private readonly T _storageProvider;

        public PeerRepository(T storageProvider)
        {
            _storageProvider = storageProvider;
        }
        
        public T Find(long key)
        {
            throw new NotImplementedException();
        }

        public void Add(T entity)
        {
            throw new NotImplementedException();
        }

        public void Update(T item)
        {
            throw new NotImplementedException();
        }

        public void Remove(long key)
        {
            throw new NotImplementedException();
        }

        public T GetById(long id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> GetAll()
        {
            throw new NotImplementedException();
        }
    }
}
