using PlataformaOperacional.Model.Plataforma;

namespace PlataformaOperacional.Service.Middleware
{
    /// <summary>
    /// Camada de DOMÍNIO das notificações em tempo real.
    ///
    /// Consome o SignalRService (transporte) via EscutarEvento&lt;T&gt; e expõe para a UI
    /// apenas o que ela precisa: o evento tipado e o estado da conexão. Toda regra de
    /// negócio de notificação (descarte de expiradas, dedup) mora aqui — nem no
    /// transporte, nem nos componentes.
    ///
    /// Registrar como Singleton e iniciar uma única vez no bootstrap da aplicação,
    /// assim que o serviço de usuário resolver a matrícula (ex.: no MainLayout).
    /// </summary>
    public class ServicoNotificacao
    {
        private const string NomeEventoHub = "ReceberNotificacao";

        private readonly SignalRService _signalR;
        private bool _iniciado;

        /// <summary>Disparado a cada notificação recebida em tempo real (já filtrada).</summary>
        public event Action<MensagemNotificacao> AoReceberNotificacao;

        /// <summary>Estado da conexão, para a UI exibir "Conectado em tempo real" / "Offline".</summary>
        public bool Conectado => _signalR.Conectado;

        /// <summary>Repasse dos eventos de estado do transporte (a UI decide o que exibir).</summary>
        public event Action<string> AoReconectar;
        public event Action AoDesconectar;

        public ServicoNotificacao(SignalRService signalR)
        {
            _signalR = signalR;
            _signalR.AoReconectar += id => AoReconectar?.Invoke(id);
            _signalR.AoDesconectar += () => AoDesconectar?.Invoke();
        }

        /// <summary>
        /// Define a identidade da conexão e registra a escuta do evento de notificação.
        /// Idempotente. Se o servidor estiver fora do ar neste momento, a escuta fica
        /// registrada e passa a receber assim que o transporte reconectar.
        /// </summary>
        public async Task IniciarAsync(string matricula)
        {
            if (_iniciado)
                return;
            _iniciado = true;

            await _signalR.DefinirUsuarioAsync(matricula);

            try
            {
                await _signalR.EscutarEvento<MensagemNotificacao>(NomeEventoHub, TratarMensagemAsync);
            }
            catch (Exception ex)
            {
                // Start falhou (servidor fora do ar), mas a escuta já ficou registrada e o
                // SignalRService agendou o retry — não propagamos para não quebrar o bootstrap.
                Console.WriteLine($"[Notificacao] Hub indisponível no início, retry agendado: {ex.Message}");
            }
        }

        private Task TratarMensagemAsync(MensagemNotificacao msg)
        {
            // Notificação que chegou já vencida não deve aparecer para o usuário
            if (msg.DataValidade != default && msg.DataValidade.ToLocalTime() < DateTime.Now)
                return Task.CompletedTask;

            AoReceberNotificacao?.Invoke(msg);
            return Task.CompletedTask;
        }
    }
}
