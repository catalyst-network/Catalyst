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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Catalyst.Common.Extensions
{
    public static class ObserverExtensions
    {
        public static IObservable<T> MergeWithCompleteOnEither<T>(this IObservable<T> source, IObservable<T> right)
        {
            return Observable.Create<T>(obs =>
            {
                var compositeDisposable = new CompositeDisposable();
                var subject = new Subject<T>();

                compositeDisposable.Add(subject.Subscribe(obs));
                compositeDisposable.Add(source.Subscribe(subject));
                compositeDisposable.Add(right.Subscribe(subject));
                
                return compositeDisposable;
            });     
        }   
    }
}
