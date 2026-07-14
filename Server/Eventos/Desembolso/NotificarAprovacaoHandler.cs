using PlataformaNotificacao;
using PlataformaNotificacao.Application.Interface;
using PlataformaNotificacao.Domain;
using PlataformaNotificacao.Domain.DTO;
using PlataformaNotificacao.Domain.Enum;

namespace SipubDesembolsos.Server.Eventos.Desembolso;

// Aprovado → notifica todos os usuários do módulo SIPUB
public class NotificarAprovacaoHandler(INotificacaoService notificacao) : IEventHandler<DesembolsoAprovadoEvento>
{
    public async Task HandleAsync(DesembolsoAprovadoEvento e)
    {
        await notificacao.EnviarModuloAsync(
            titulo:    $"Desembolso {e.DesembolsoId} aprovado",
            mensagem: $"Aprovado por {e.UsuarioNome}.",
            tipo:      TipoNotificacao.Normal,
            codigoAplicativo:    CodigoAplicativo.Sipub,           
            link:      $"/?contratoId={e.DesembolsoId}"         
        );
    }
}
