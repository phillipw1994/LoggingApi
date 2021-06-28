using LoggingAuthApi.Settings.Interfaces;
using LoggingAuthApi.Settings.Models;
using Microsoft.Extensions.Configuration;

namespace LoggingAuthApi.Settings
{
    public class JsonConfigurationSettings : IConfigurationSettings
    {
        #region constructors
        public JsonConfigurationSettings(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        #endregion

        #region private members
        private IConfiguration Configuration { get; }

        private Api _api;
        #endregion

        public Api Api => _api ?? (_api = Configuration.GetSection("Api").Get<Api>());

        public string ConnectionString(string connectionStringName)
        {
            return Configuration.GetConnectionString(connectionStringName);
        }
    }
}