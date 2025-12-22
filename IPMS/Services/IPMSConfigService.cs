using System.Text.Json;
using IPMS.Core.Configs;
using Microsoft.Extensions.Hosting;

namespace IPMS.Services
{
    public interface IIPMSConfigService
    {
        AppConfigs GetConfigs();
    }

    public class IPMSConfigService : IIPMSConfigService
    {
        private readonly AppConfigs _configs;

        public IPMSConfigService(IHostEnvironment env)
        {
            var path = Path.Combine(env.ContentRootPath, "Configs", "IPMSconfigs.json");
            var json = File.ReadAllText(path);
            _configs = JsonSerializer.Deserialize<AppConfigs>(json) ?? new AppConfigs();
        }

        public AppConfigs GetConfigs() => _configs;
    }
}