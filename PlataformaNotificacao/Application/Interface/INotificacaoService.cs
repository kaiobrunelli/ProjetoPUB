using PlataformaNotificacao.Domain.DTO;
using PlataformaNotificacao.Domain.Enum;

namespace PlataformaNotificacao.Application.Interface;

public interface INotificacaoService
{
    event EventHandler<NotificacaoEventArgs>? OnNotificacao;

    Task EnviarGeralAsync(
        string titulo, string mensagem, TipoNotificacao tipo = TipoNotificacao.Normal,
        string? link = null, int? dias = null, int? horas = null);

    Task EnviarModuloAsync(
        string titulo, string mensagem, TipoNotificacao tipo,
        CodigoAplicativo codigoAplicativo, string? link = null,
        int? dias = null, int? horas = null);

    Task EnviarCoordenacaoAsync(
        string codigoCoordenacao,
        string titulo, string mensagem, TipoNotificacao tipo = TipoNotificacao.Normal,
        string? link = null, int? dias = null, int? horas = null);

    Task EnviarPorMatriculasAsync(
        IEnumerable<string> matriculas,
        string titulo, string mensagem, TipoNotificacao tipo = TipoNotificacao.Normal,
        CodigoAplicativo? codigoAplicativo = null, string? link = null,
        int? dias = null, int? horas = null);

    Task EnviarIndividualAsync(
        string titulo, string mensagem,
        List<string> matriculas, CodigoAplicativo? codigoAplicativo = null,
        string? link = null, int? dias = null, int? horas = null,
        TipoNotificacao tipo = TipoNotificacao.Normal);

    Task<List<NotificacaoDto>> ObterMinhasAsync(string matriculaUsuario, int limite = 50);

    Task<int> ContarNaoLidasAsync(string matriculaUsuario);

    Task<bool> MarcarLidaAsync(int codigoNotificacao, string matriculaUsuario);

    Task MarcarTodasLidasAsync(string matriculaUsuario);
}
