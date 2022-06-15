using Microsoft.Extensions.DependencyInjection;

namespace Net.Leksi.KeyBox;

internal class ServiceProviderFactory : IServiceProviderFactory<ServiceProviderFactory.Builder>
{
    public Builder CreateBuilder(IServiceCollection services) => new Builder(services);

    public IServiceProvider CreateServiceProvider(Builder containerBuilder) => containerBuilder.CreateServiceProvider();

    internal class Builder
    {
        private readonly IServiceCollection _services;

        internal Builder(IServiceCollection services) => _services = services;

        internal IServiceProvider CreateServiceProvider() => new ServiceProviderProxy(_services.BuildServiceProvider());
    }
}
