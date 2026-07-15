using PlataformaNotificacao.Domain;
using PlataformaNotificacao.Domain.Enum;

namespace PlataformaNotificacao.UI.Servicos;

/// <summary>
/// Camada de DOMÍNIO das notificações em tempo real.
///
/// Consome o SignalRService (transporte) via EscutarEvento&lt;T&gt; e expõe para a UI
/// apenas o que ela precisa: o evento tipado e o estado da conexão. Regras de
/// domínio de notificação (ex.: descarte de expiradas) moram aqui.
///
/// Registrar como Singleton e iniciar uma única vez no bootstrap, assim que a
/// matrícula for resolvida (o componente SinoNotificacoes já faz isso).
/// </summary>
public class ServicoNotificacao
{
    private const string NomeEventoHub = "ReceberNotificacao";

    private readonly SignalRService _signalR;
    private bool _escutaRegistrada;

    /// <summary>Disparado a cada notificação recebida em tempo real (já filtrada).</summary>
    public event Action<MensagemNotificacao>? AoReceberNotificacao;

    /// <summary>Estado da conexão ("Conectado em tempo real" / "Offline").</summary>
    public bool Conectado => _signalR.Conectado;

    public string? UsuarioAtual => _signalR.UsuarioAtual;

    public ServicoNotificacao(SignalRService signalR)
    {
        _signalR = signalR;
    }

    /// <summary>
    /// Conecta com a identidade informada — ou reconecta, se o usuário mudou
    /// (seletor de teste da barra). Idempotente para o mesmo usuário.
    /// </summary>
    public async Task IniciarOuReconectarAsync(string matriculaUsuario)
    {
        // 1º define a identidade: a URL do hub é montada com ela.
        // (Se o usuário mudou, o transporte reconstrói a conexão sozinho.)
        await _signalR.DefinirUsuarioAsync(matriculaUsuario);

        // Escuta registrada uma única vez — sobrevive às reconstruções via fábricas
        if (!_escutaRegistrada)
        {
            _escutaRegistrada = true;
            await _signalR.EscutarEvento<MensagemNotificacao>(NomeEventoHub, TratarMensagemAsync);
        }

        try
        {
            await _signalR.IniciarHubConnection();
        }
        catch
        {
            // Servidor fora do ar no boot: o transporte já agendou nova tentativa e a
            // escuta continua registrada. Não propagar — o componente que chamou ainda
            // precisa carregar a contagem/histórico do banco via HTTP.
        }
    }

    private Task TratarMensagemAsync(MensagemNotificacao msg)
    {
        Console.WriteLine($"[SignalR] Notificação recebida: {msg.Titulo} (escopo: {msg.Escopo})");

        // Notificação que chegou já vencida não deve aparecer para o usuário
        if (msg.DataValidade != default && msg.DataValidade.ToLocalTime() < DateTime.Now)
            return Task.CompletedTask;

        AoReceberNotificacao?.Invoke(msg);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Alias para compatibilidade com componentes que utilizam
    /// IniciarAsync().
    /// </summary>
    public Task IniciarAsync(string matriculaUsuario)
    {
        return IniciarOuReconectarAsync(matriculaUsuario);
    }
}
