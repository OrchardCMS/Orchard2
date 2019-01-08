using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using OrchardCore.Abstractions.Shell;

namespace OrchardCore.Environment.Shell
{
    /// <summary>
    /// Holds the tenant <see cref="IConfiguration"/> which is lazily built
    /// from the application configuration appsettings.json, the 'App_Data/appsettings.json'
    /// file and then the 'App_Data/Sites/{tenant}/appsettings.json' file.
    /// </summary>
    public class ShellConfiguration : IShellConfiguration
    {
        private IConfiguration _configuration;
        private IConfiguration _updatableData;

        private readonly string _name;
        private Func<string, IConfigurationBuilder> _configBuilderFactory;
        private IConfigurationBuilder _configurationBuilder;

        public ShellConfiguration()
        {
        }

        public ShellConfiguration(string name, Func<string, IConfigurationBuilder> factory)
        {
            _name = name;
            _configBuilderFactory = factory;
        }

        public ShellConfiguration(string name, ShellConfiguration configuration)
        {
            _name = name;
            if (configuration._configuration == null)
            {
                _configBuilderFactory = configuration._configBuilderFactory;
                return;
            }

            _configurationBuilder = new ConfigurationBuilder()
                .AddConfiguration(configuration._configuration);
        }

        /// <summary>
        /// The tenant lazily built <see cref="IConfiguration"/>.
        /// </summary>
        public IConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    lock (this)
                    {
                        if (_configuration == null)
                        {
                            var configurationBuilder = _configurationBuilder ??
                                _configBuilderFactory?.Invoke(_name) ??
                                new ConfigurationBuilder();

                            _updatableData = new ConfigurationBuilder()
                                .AddInMemoryCollection()
                                .Build();

                            _configuration = configurationBuilder
                                .AddConfiguration(_updatableData)
                                .Build();
                        }
                    }
                }

                return _configuration;
            }
        }

        public string this[string key]
        {
            get => Configuration[key];

            set
            {
                if (Configuration[key] != value)
                {
                    lock (this)
                    {
                        _updatableData[key] = value;
                    }
                }
            }
        }

        public IConfigurationSection GetSection(string key)
        {
            return Configuration.GetSection(key);
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return Configuration.GetChildren();
        }

        public IChangeToken GetReloadToken()
        {
            return Configuration.GetReloadToken();
        }
    }
}
