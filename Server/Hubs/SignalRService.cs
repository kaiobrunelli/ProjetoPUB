using Microsoft.AspNetCore.SignalR;
using PlataformaNotificacao;
using PlataformaNotificacao.Domain;
using PlataformaNotificacao.Domain.Enum;

namespace SipubDesembolsos.Server.Hubs;

public class SignalRService(IHubContext<HubNotificacao> hubContext)
{
    public async Task HandlerObserver(object? sender, ObservadorAutomacao e)
    {
        await hubContext.Clients.All.SendAsync(e.ChaveConexao, e.PercentualProcessado, e);
    }

    // Roteamento por Escopo (intenção), não por tamanho de lista:
    //   Geral                → sempre Clients.All
    //   Modulo/Individual/...→ grupos por matrícula, só se houver destinatário real
    // Uma lista vazia em escopo não-Geral NÃO vira broadcast — evita notificação
    // restrita "vazar" para todo mundo por causa de um filtro que deu zero.
    public async Task HandlerObserver(object? sender, NotificacaoEventArgs e)
    {
        IClientProxy? destino = e.Escopo switch
        {
            EscopoNotificacao.Geral => hubContext.Clients.All,
            _ when e.Destinatarios.Count > 0 => hubContext.Clients.Groups(e.Destinatarios),
            _ => null
        };

        if (destino is not null)
            await destino.SendAsync(e.ChaveConexao, e.Mensagem);
    }
}
