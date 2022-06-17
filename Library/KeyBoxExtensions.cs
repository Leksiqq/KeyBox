using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Net.Leksi.KeyBox;

public static class KeyBoxExtensions
{
    public static IServiceCollection AddKeyBox(this IServiceCollection services, Action<IKeyBoxConfiguration> configure)
    {
        KeyBox.AddKeyBox(services, configure);
        return services;
    }

    public static IKeyBoxConfiguration AddPrimaryKey<Target>(this IKeyBoxConfiguration keyBoxConfiguration, IDictionary<string, object> definition)
        where Target: class
    {
        return keyBoxConfiguration.AddPrimaryKey(typeof(Target), definition);
    }

}
