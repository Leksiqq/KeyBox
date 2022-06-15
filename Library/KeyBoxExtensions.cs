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

    public static IHostBuilder AddKeyBox(this IHostBuilder builder, Action<IKeyBoxConfiguration> configure)
    {
        KeyBox.AddKeyBox(builder, configure);
        return builder;
    }

    public static IKeyBoxConfiguration AddPrimaryKey<Target>(this IKeyBoxConfiguration keyBoxConfiguration, IDictionary<string, object> definition)
        where Target: class
    {
        return keyBoxConfiguration.AddPrimaryKey(typeof(Target), definition);
    }

    public static IApplicationBuilder UseKeyBox(this IApplicationBuilder app)
    {
        IKeyBox keyBox = app.ApplicationServices.GetRequiredService<IKeyBox>();

        if (keyBox is { } && keyBox.HasMappedPrimaryKeys)
        {
            app.Use(async (HttpContext context, Func<Task>? next) =>
            {
                context.RequestServices = new ServiceProviderProxy(context.RequestServices);
                await (next?.Invoke() ?? Task.CompletedTask);
            });
        }
        return app;
    }

}
