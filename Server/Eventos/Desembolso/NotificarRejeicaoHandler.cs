using PlataformaNotificacao;
using PlataformaNotificacao.Application.Interface;
using PlataformaNotificacao.Domain;
using PlataformaNotificacao.Domain.DTO;
using PlataformaNotificacao.Domain.Enum;

namespace SipubDesembolsos.Server.Eventos.Desembolso;

// Rejeitado → notifica apenas a coordenação responsável pelo desembolso
public class NotificarRejeicaoHandler(INotificacaoService notificacao)
    : IEventHandler<DesembolsoRejeitadoEvento>
{
    public async Task HandleAsync(DesembolsoRejeitadoEvento e)
    {
        await notificacao.EnviarCoordenacaoAsync(
            codigoCoordenacao: e.CoordenaçãoResponsavel,
            titulo:           $"Desembolso {e.DesembolsoId} rejeitado",
            mensagem:         $"Rejeitado por {e.UsuarioNome}. Verifique a pendência.",
            tipo:             TipoNotificacao.Alerta,
            link:             $"/?contratoId={e.DesembolsoId}"
        );
    }
}
