using PlataformaOperacional.Model.Plataforma;

namespace PlataformaOperacional.Service.Middleware
{
    /// <summary>
    /// Camada de DOMÍNIO das notificações — versão para o SignalRService MÍNIMO
    /// (aquele em que a progress bar ficou 100% intocada).
    ///
    /// Como o transporte mínimo não tem retry próprio de start nem reconstrução,
    /// este serviço assume essas responsabilidades para a notificação:
    ///   • laço suave de conexão (a cada 10s) enquanto estiver offline;
    ///   • rearme quando o retry automático do SignalR desiste (AoDesconectar).
    /// Nada disso toca o fluxo da progress bar.
    ///
    /// Registrar como Singleton e iniciar uma única vez no bootstrap, assim que a
    /// matrícula for resolvida (o componente SinoNotificacoes já faz isso).
    /// </summary>
    public class ServicoNotificacao
    {
        private const string NomeEventoHub = "ReceberNotificacao";

        private readonly SignalRService _signalR;
        private bool _iniciado;
        private bool _mantendoConexao;

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
            _signalR.AoDesconectar += () =>
            {
                AoDesconectar?.Invoke();
                _ = ManterConexaoAsync();   // retry automático desistiu → rearma por fora
            };
        }

        /// <summary>
        /// Define a identidade (?userId=) e registra a escuta do evento de notificação.
        /// Idempotente. IMPORTANTE: chamar antes de qualquer página conectar o hub —
        /// no fluxo normal, o MainLayout garante isso.
        /// </summary>
        public async Task IniciarAsync(string matricula)
        {
            if (_iniciado)
                return;
            _iniciado = true;

            _signalR.DefinirUsuario(matricula);   // precisa vir ANTES da primeira conexão

            // Registra a escuta (fica valendo mesmo se o servidor estiver fora do ar)
            await _signalR.EscutarEvento<MensagemNotificacao>(NomeEventoHub, TratarMensagemAsync);

            // Garante que a conexão suba (com retry suave se o servidor demorar)
            if (!_signalR.Conectado)
                _ = ManterConexaoAsync();
        }

        /// <summary>
        /// Laço suave de conexão: tenta subir o hub a cada 10s enquanto offline.
        /// Cobre o start inicial com servidor fora do ar e o pós-desistência do
        /// retry automático — casos que o transporte mínimo não trata sozinho.
        /// </summary>
        private async Task ManterConexaoAsync()
        {
            if (_mantendoConexao) return;   // evita laços paralelos
            _mantendoConexao = true;
            try
            {
                while (!_signalR.Conectado)
                {
                    try
                    {
                        await _signalR.IniciarHubConnection();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Notificacao] Hub indisponível, nova tentativa em 10s: {ex.Message}");
                    }

                    if (_signalR.Conectado) break;
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
            }
            finally
            {
                _mantendoConexao = false;
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
