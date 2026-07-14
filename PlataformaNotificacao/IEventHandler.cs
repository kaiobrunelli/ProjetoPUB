namespace PlataformaNotificacao;

public interface IEventHandler<TEvento> where TEvento : class
{
    Task HandleAsync(TEvento evento);
}
