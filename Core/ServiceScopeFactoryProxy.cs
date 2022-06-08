using Microsoft.Extensions.DependencyInjection;

namespace Net.Leksi.KeyBox;

internal class ServiceScopeFactoryProxy: IServiceScopeFactory
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    internal ServiceScopeFactoryProxy(IServiceScopeFactory serviceScopeFactory) => _serviceScopeFactory = serviceScopeFactory;

    public IServiceScope CreateScope() => new ServiceScope(new ServiceProviderProxy(_serviceScopeFactory.CreateScope().ServiceProvider));
}
