//#region LICENSE

///**
//* Copyright (c) 2019 Catalyst Network
//*
//* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
//*
//* Catalyst.Node is free software: you can redistribute it and/or modify
//* it under the terms of the GNU General Public License as published by
//* the Free Software Foundation, either version 2 of the License, or
//* (at your option) any later version.
//*
//* Catalyst.Node is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//* GNU General Public License for more details.
//*
//* You should have received a copy of the GNU General Public License
//* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
//*/

//#endregion

//using System;
//using System.Linq;
//using RocksDbSharp;
//using SharpRepository.Repository;
//using SharpRepository.Repository.Caching;
//using SharpRepository.Repository.FetchStrategies;

//namespace Catalyst.Modules.Repository.CosmosDb
//{
//    public class RocksDbRepository<T, TKey> : LinqRepositoryBase<T, TKey> where T : class, new()
//    {
//        //private RocksDb _rocksDb;
//        //public RocksDbRepository(ICachingStrategy<T, string> cachingStrategy = null) :
//        //    base()
//        //{
//        //    _rocksDb = RocksDb.Open(new DbOptions(), "");
//        //}

//        //public override void Dispose()
//        //{
//        //    _rocksDb.Dispose();
//        //}

//        //protected override void AddItem(T entity)
//        //{
//        //    _rocksDb.add
//        //}

//        //protected override IQueryable<T> BaseQuery(IFetchStrategy<T> fetchStrategy = null)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //protected override void DeleteItem(T entity)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //protected override void SaveChanges()
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //protected override void UpdateItem(T entity)
//        //{
//        //    throw new NotImplementedException();
//        }
//    }
//}
