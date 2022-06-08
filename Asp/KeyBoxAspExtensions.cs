using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Net.Leksi.KeyBox;

public static class KeyBoxAspExtensions
{
    public static IApplicationBuilder UseKeyBox(this IApplicationBuilder app)
    {
        IKeyBox? keyBox = app.ApplicationServices.GetService(typeof(IKeyBox)) as IKeyBox;
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
