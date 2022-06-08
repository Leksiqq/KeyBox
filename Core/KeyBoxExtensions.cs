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

    public static IHostBuilder AddKeyBox(this IHostBuilder builder, Action<IKeyBoxConfiguration> configure)
    {
        builder.UseServiceProviderFactory(new ServiceProviderFactory()).ConfigureServices((context, services) =>
        {
            services.AddKeyBox(configure);
        });
        return builder;
    }

    public static IKeyBoxConfiguration AddPrimaryKey<Target>(this IKeyBoxConfiguration keyBoxConfiguration, IDictionary<string, Type> definition)
        where Target: class
    {
        return keyBoxConfiguration.AddPrimaryKey(typeof(Target), definition);
    }

    public static IKeyBoxConfiguration AddPrimaryKey<Target>(this IKeyBoxConfiguration keyBoxConfiguration, Type exampleType)
        where Target : class
    {
        return keyBoxConfiguration.AddPrimaryKey(typeof(Target), exampleType);
    }

    public static IKeyBoxConfiguration AddPrimaryKey<Target, Example>(this IKeyBoxConfiguration keyBoxConfiguration)
        where Target : class 
        where Example : class
    {
        return keyBoxConfiguration.AddPrimaryKey(typeof(Target), typeof(Example));
    }

}
