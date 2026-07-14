using Microsoft.Extensions.DependencyInjection;

namespace PlataformaNotificacao;

public class Publicador(IServiceProvider provider) : IPublicador
{
    public async Task PublicarAsync<TEvento>(TEvento evento) where TEvento : class
    {
        var handlers = provider.GetServices<IEventHandler<TEvento>>();
        foreach (var handler in handlers)
            await handler.HandleAsync(evento);
    }
}
