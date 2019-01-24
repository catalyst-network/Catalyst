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
        public ConfigurationBuilder Build()
        {
            Configuration = _configurationBuilder.Build();
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public IConfigurationRoot GetInstance()
        {
            Build();
            if (Configuration != null)
            {
                return Configuration;
            }

            throw new ArgumentNullException();
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
