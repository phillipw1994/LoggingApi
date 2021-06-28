using LoggingAuthApi.Settings.Models;

namespace LoggingAuthApi.Settings.Interfaces
{
    public interface IConfigurationSettings
    {
        Api Api { get; }
        string ConnectionString(string connectionStringName);
    }
}