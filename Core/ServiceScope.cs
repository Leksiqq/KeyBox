using Microsoft.Extensions.DependencyInjection;

namespace Net.Leksi.KeyBox;

internal class ServiceScope: IServiceScope
{
    public IServiceProvider ServiceProvider { get; init; }

    internal ServiceScope(IServiceProvider serviceProvider) => ServiceProvider = serviceProvider;

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
