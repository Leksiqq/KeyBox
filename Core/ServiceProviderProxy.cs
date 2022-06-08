using Microsoft.Extensions.DependencyInjection;

namespace Net.Leksi.KeyBox;

public class ServiceProviderProxy : IServiceProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KeyBox _keyBox;

    public ServiceProviderProxy(IServiceProvider serviceProvider) => 
        (_serviceProvider, _keyBox) = (serviceProvider, (serviceProvider.GetRequiredService<IKeyBox>() as KeyBox)!);

    public object? GetService(Type serviceType)
    {
        object? result = _serviceProvider?.GetService(serviceType);
        if (result is IServiceScopeFactory serviceScopeFactory && result is not ServiceScopeFactoryProxy)
        {
            result = new ServiceScopeFactoryProxy(serviceScopeFactory);
        } 
        else if (result is { } && result is not IKeyBox && result is not IKeyRing)
        {
            _keyBox.CreateKeyRing(result);
        }
        return result;
    }
}
