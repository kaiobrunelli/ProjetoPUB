using Microsoft.AspNetCore.Mvc;
using PlataformaNotificacao;
using SipubDesembolsos.Server.Hubs;
using SipubDesembolsos.Server.Servicos;
using PlataformaNotificacao.Application.Interface;
using PlataformaNotificacao.Domain;
using PlataformaNotificacao.Domain.DTO;
using PlataformaNotificacao.Domain.Enum;

namespace SipubDesembolsos.Server.Controllers;

[ApiController]
[Route("api/processamento")]
public class ControladorProcessamento(SignalRService signalR, IServiceScopeFactory scopeFactory) : ControllerBase
{
    private const string ChaveProgresso = "ProgressoProcessamento";

    /// <summary>Inicia o processamento em background. O progresso chega via SignalR.</summary>
    [HttpPost("executar")]
    public IActionResult Executar()
    {
        _ = SimularProcessamentoAsync();
        return Ok(new { mensagem = "Processamento iniciado." });
    }

    private async Task SimularProcessamentoAsync()
    {
        var fases = new[]
        {
            "Validando contratos...",
            "Verificando documentação...",
            "Calculando desembolsos...",
            "Gerando relatório...",
            "Concluindo..."
        };

        for (int i = 1; i <= 10; i++)
        {
            await Task.Delay(500);

            var faseIdx = (i - 1) / 2;

            await signalR.HandlerObserver(this, new ObservadorAutomacao
            {
                ChaveConexao         = ChaveProgresso,
                NomeProcesso         = "Processamento SIPUB",
                NumeroFaseAtual      = faseIdx + 1,
                TotalAProcessar      = 10,
                TotalProcessado      = i,
                PercentualProcessado = i * 10,
                Mensagem             = fases[Math.Min(faseIdx, fases.Length - 1)],
                Severity             = "Normal"
            });
        }

        using var scope = scopeFactory.CreateScope();
        var notificacao = scope.ServiceProvider.GetRequiredService<INotificacaoService>();

        await notificacao.EnviarGeralAsync(
            titulo:   "Processamento concluído",
            mensagem: "Todos os desembolsos foram processados com sucesso.",
            tipo:     TipoNotificacao.Normal
        );
    }
}
