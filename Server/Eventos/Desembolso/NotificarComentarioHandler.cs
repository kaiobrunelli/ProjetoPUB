using PlataformaNotificacao;
using PlataformaNotificacao.Application.Interface;
using PlataformaNotificacao.Domain;
using PlataformaNotificacao.Domain.DTO;
using PlataformaNotificacao.Domain.Enum;

namespace SipubDesembolsos.Server.Eventos.Desembolso;

// Comentário inserido → notifica individualmente o destinatário (vem do evento)
public class NotificarComentarioHandler(INotificacaoService notificacao)
    : IEventHandler<ComentarioInseridoEvento>
{
    public async Task HandleAsync(ComentarioInseridoEvento e)
        => await notificacao.EnviarPorMatriculasAsync(
            matriculas: [e.MatriculaDestinatario],
            titulo:     $"Novo comentário — {e.DesembolsoId}",
            mensagem:   $"{e.AutorNome}: \"{e.Comentario}\"",
            tipo:       TipoNotificacao.Normal,
            link:       $"/?contratoId={e.DesembolsoId}"
        );
}
