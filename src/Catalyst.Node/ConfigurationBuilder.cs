using System;
using System.Collections.Generic;
using Catalyst.Helpers.Util;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Node
{
    public class ConfigurationBuilder
    {
        public IConfigurationRoot Configuration; 
        private readonly IConfigurationBuilder _configurationBuilder;

        /// <summary>
        /// 
        /// </summary>
        public ConfigurationBuilder()
        {
            _configurationBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public ConfigurationBuilder AddJsonFile(string fullPath)
        {
            _configurationBuilder.AddJsonFile(fullPath);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IConfigurationRoot Build()
        {
            return _configurationBuilder.Build();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public IConfigurationSection GetSection(string section)
        {
            return Configuration.GetSection(section);
        }
    }
}
