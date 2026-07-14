using PlataformaNotificacao;
using PlataformaNotificacao.Application.Interface;
using PlataformaNotificacao.Domain;
using PlataformaNotificacao.Domain.DTO;
using PlataformaNotificacao.Domain.Enum;

namespace SipubDesembolsos.Server.Eventos.Desembolso;

// Desembolso(s) inserido(s) → notifica módulo SIPUB inteiro
public class NotificarInsercaoDesembolsosHandler(INotificacaoService notificacao)
    : IEventHandler<DesembolsosInseridosEvento>
{
    public async Task HandleAsync(DesembolsosInseridosEvento e)
    {
        var (titulo, mensagem) = e.DesembolsoIds.Count == 1
            ? (
                "Novo desembolso inserido",
                $"{e.OperadorNome} inseriu o desembolso {e.DesembolsoIds[0]}."
              )
            : (
                "Novos desembolsos inseridos",
                $"{e.OperadorNome} inseriu {e.DesembolsoIds.Count} desembolsos."
              );

        var link = e.DesembolsoIds.Count == 1
            ? $"/?contratoId={e.DesembolsoIds[0]}"
            : null;

        await notificacao.EnviarModuloAsync(
            titulo:           titulo,
            mensagem:         mensagem,
            tipo:             TipoNotificacao.Normal,
            codigoAplicativo: CodigoAplicativo.Sipub,
            link:             link
        );
    }
}
