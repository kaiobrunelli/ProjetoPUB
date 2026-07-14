namespace PlataformaNotificacao;

public interface IPublicador
{
    Task PublicarAsync<TEvento>(TEvento evento) where TEvento : class;
}
