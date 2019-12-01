﻿#region LICENSE

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

using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Catalyst.Modules.Server.Blazor
{
    /// <summary>
    /// A factory for creating a <see cref="ContainerBuilder"/> and an <see cref="IServiceProvider" />.
    /// </summary>
    public class AutofacServiceProviderFactory : IServiceProviderFactory<ContainerBuilder>
    {
        private readonly ContainerBuilder _containerBuilder;
        private AutofacServiceProvider _autofacServiceProvider;

        public AutofacServiceProviderFactory(ContainerBuilder containerBuilder)
        {
            _containerBuilder = containerBuilder;
        }

        /// <summary>
        /// Creates a container builder from an <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The collection of services.</param>
        /// <returns>A container builder that can be used to create an <see cref="IServiceProvider" />.</returns>
        public ContainerBuilder CreateBuilder(IServiceCollection services)
        {
            _containerBuilder.Populate(services);
            return _containerBuilder;
        }

        public void SetContainer(IContainer container) { _autofacServiceProvider.LifetimeScope = container; }

        /// <summary>
        /// Creates an <see cref="IServiceProvider" /> from the container builder.
        /// </summary>
        /// <param name="containerBuilder">The container builder.</param>
        /// <returns>An <see cref="IServiceProvider" />.</returns>
        public IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));

            return _autofacServiceProvider = new AutofacServiceProvider();
        }
    }

    /// <summary>
    /// Autofac implementation of the ASP.NET Core <see cref="IServiceProvider"/>.
    /// </summary>
    /// <seealso cref="System.IServiceProvider" />
    /// <seealso cref="Microsoft.Extensions.DependencyInjection.ISupportRequiredService" />
    internal class AutofacServiceProvider : IServiceProvider, ISupportRequiredService, IDisposable
    {
        public ILifetimeScope LifetimeScope { get; set; }

        private bool _disposed = false;

        /// <summary>
        /// Gets service of type <paramref name="serviceType" /> from the
        /// <see cref="AutofacServiceProvider" /> and requires it be present.
        /// </summary>
        /// <param name="serviceType">
        /// An object that specifies the type of service object to get.
        /// </param>
        /// <returns>
        /// A service object of type <paramref name="serviceType" />.
        /// </returns>
        /// <exception cref="Autofac.Core.Registration.ComponentNotRegisteredException">
        /// Thrown if the <paramref name="serviceType" /> isn't registered with the container.
        /// </exception>
        /// <exception cref="Autofac.Core.DependencyResolutionException">
        /// Thrown if the object can't be resolved from the container.
        /// </exception>
        public object GetRequiredService(Type serviceType) { return LifetimeScope.Resolve(serviceType); }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">
        /// An object that specifies the type of service object to get.
        /// </param>
        /// <returns>
        /// A service object of type <paramref name="serviceType" />; or <see langword="null" />
        /// if there is no service object of type <paramref name="serviceType" />.
        /// </returns>
        public object GetService(Type serviceType) { return LifetimeScope.ResolveOptional(serviceType); }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources;
        /// <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing) LifetimeScope.Dispose();

                _disposed = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
