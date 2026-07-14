using Microsoft.AspNetCore.SignalR.Client;
using PlataformaNotificacao.Domain;
using PlataformaNotificacao.Domain.Enum;
using System.Collections.Concurrent;

namespace SipubDesembolsos.Client.Servicos;

/// <summary>
/// Camada de TRANSPORTE SignalR — dona única da HubConnection.
///
/// Responsabilidades:
///   1. Ciclo de vida da conexão (start concorrente seguro, reconexão infinita,
///      reconstrução ao trocar de usuário).
///   2. Registro de escutas (.On) com deduplicação por nome de evento, tanto para
///      as chaves dinâmicas da progress bar quanto para eventos fixos (EscutarEvento&lt;T&gt;).
///   3. Distribuição do progresso das automações para múltiplos observers por chave
///      (API pública original preservada — nenhum componente de progress bar muda).
///
/// Este serviço NÃO conhece regra de domínio de notificação — quem escuta
/// "ReceberNotificacao" é o ServicoNotificacao, via EscutarEvento&lt;T&gt;.
/// </summary>
public class SignalRService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private string _hubUrlProd = "";
    private string? _matricula;
    private bool _descartado;

    // Serializa tentativas concorrentes de start/reconstrução da conexão.
    // Substitui o esquema anterior de _startTask + Interlocked, que tinha um bug:
    // uma task de start COMPLETADA era reaproveitada mesmo com a conexão caída,
    // impedindo qualquer reconexão manual após o retry automático desistir.
    private readonly SemaphoreSlim _mutexConexao = new(1, 1);
    private readonly object _lockConstrucao = new();

    public string HubUrlProd => _hubUrlProd;
    public string? UsuarioAtual => _matricula;
    public bool Conectado => _hubConnection?.State == HubConnectionState.Connected;


    private const string EventoProgresso = "ProgressoProcessamento";
    public event Action<ObservadorAutomacao>? AoReceberProgresso;

    // ── Eventos de estado da conexão (a UI decide o que exibir; o serviço só avisa) ──
    public event Action? AoConectar;                // primeira conexão estabelecida
    public event Action<string?>? AoReconectar;     // reconexão automática concluída (ConnectionId novo)
    public event Action? AoDesconectar;             // conexão caiu / entrou em reconexão

    public SignalRService(string urlBase)
    {
        CriarHubUrl($"{urlBase.TrimEnd('/')}/");

        // Compatibilidade com consumidores que utilizam
        // Hub.AoReceberProgresso.
        _fabricasEscuta[EventoProgresso] = conexao =>
            conexao.On<int, ObservadorAutomacao>(
                EventoProgresso,
                (_, observador) =>
                {
                    AoReceberProgresso?.Invoke(observador);
                });
    }

    // ──────────────────────────────────────────────────────────────────────
    // Escutas registradas
    // ──────────────────────────────────────────────────────────────────────

    // Assinaturas ativas no SignalR (o ".On") — dedup por nome de evento/chave
    private readonly ConcurrentDictionary<string, IDisposable> _registeredKeys = new();

    // Fábrica de cada escuta: permite re-registrar tudo quando a conexão é
    // reconstruída (ex.: troca de usuário), já que os ".On" morrem com a HubConnection
    private readonly ConcurrentDictionary<string, Func<HubConnection, IDisposable>> _fabricasEscuta = new();

    // Último estado recebido por chave (cache) para entregar a novos observers imediatamente
    private readonly ConcurrentDictionary<string, ObservadorAutomacao> _progressoAtualPorHub = new();

    // Callbacks de progresso: vários componentes/abas podem escutar a mesma chave
    private readonly ConcurrentDictionary<string, List<Func<ObservadorAutomacao, Task>>> _observersPorChave = new();

    // Controle de conclusão por chave
    private readonly ConcurrentDictionary<string, bool> _flagCompletouPorHub = new();

    public event Action<bool>? OnProgressUpdateCompleted;
    public event Action<string>? OnProgressUpdateCompletedByKey;

    public void CriarHubUrl(string url)
    {
        _hubUrlProd = $"{url}chatHub";
    }

    // ──────────────────────────────────────────────────────────────────────
    // Identidade do usuário na conexão
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Define a matrícula que identifica esta conexão no hub (vai como ?userId= na URL).
    /// Chamar ANTES da primeira conexão (ex.: no bootstrap, assim que o serviço de
    /// usuário resolver a matrícula). Se chamado com matrícula diferente após já
    /// conectado, derruba a conexão, reconstrói com a nova identidade e re-registra
    /// automaticamente todas as escutas ativas (progress bar e eventos fixos).
    /// </summary>
    public async Task DefinirUsuarioAsync(string matricula)
    {
        if (string.Equals(_matricula, matricula, StringComparison.Ordinal))
            return;

        _matricula = matricula;

        if (_hubConnection is not null)
            await ReconstruirConexaoAsync();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Ciclo de vida da conexão
    // ──────────────────────────────────────────────────────────────────────

    public async Task IniciarHubConnection()
    {
        if (Conectado || _descartado)
            return;

        await _mutexConexao.WaitAsync();
        try
        {
            if (Conectado || _descartado)
                return;

            GarantirConexaoConstruida();

            if (_hubConnection!.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync();
                AoConectar?.Invoke();
            }
        }
        catch
        {
            // Servidor fora do ar no start inicial: o retry automático do SignalR só
            // cobre conexões JÁ estabelecidas, então agendamos nossa própria tentativa.
            AgendarNovaTentativa();
            throw;
        }
        finally
        {
            _mutexConexao.Release();
        }
    }

    private void GarantirConexaoConstruida()
    {
        if (_hubConnection is not null) return;
        lock (_lockConstrucao)
        {
            _hubConnection ??= ConstruirConexao();
        }
    }

    private HubConnection ConstruirConexao()
    {
        // A matrícula identifica a conexão no hub; o servidor a lê no OnConnectedAsync
        // e agrupa a conexão por usuário (roteamento das notificações). A progress bar
        // ignora essa identidade: ela é roteada pelo nome do evento (chave da operação).
        var url = string.IsNullOrWhiteSpace(_matricula)
            ? _hubUrlProd
            : $"{_hubUrlProd}?userId={Uri.EscapeDataString(_matricula)}";

        var conexao = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect(new RepetirSempreRetryPolicy())
            .Build();

        conexao.Reconnecting += _ =>
        {
            AoDesconectar?.Invoke();
            return Task.CompletedTask;
        };

        conexao.Reconnected += connectionId =>
        {
            AoReconectar?.Invoke(connectionId);
            return Task.CompletedTask;
        };

        conexao.Closed += _ =>
        {
            AoDesconectar?.Invoke();
            if (!_descartado)
                AgendarNovaTentativa();
            return Task.CompletedTask;
        };

        return conexao;
    }

    private void AgendarNovaTentativa()
    {
        _ = Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(async _ =>
        {
            if (_descartado || Conectado) return;
            try
            {
                await IniciarHubConnection();
            }
            catch
            {
                // IniciarHubConnection já agendou a próxima tentativa
            }
        });
    }

    /// <summary>
    /// Derruba a conexão atual, reconstrói (URL nova) e re-registra todas as
    /// escutas conhecidas a partir das fábricas guardadas.
    /// </summary>
    private async Task ReconstruirConexaoAsync()
    {
        await _mutexConexao.WaitAsync();
        try
        {
            foreach (var assinatura in _registeredKeys.Values)
                assinatura.Dispose();
            _registeredKeys.Clear();

            if (_hubConnection is not null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }

            GarantirConexaoConstruida();

            foreach (var fabrica in _fabricasEscuta)
                _registeredKeys.GetOrAdd(fabrica.Key, _ => fabrica.Value(_hubConnection!));
        }
        finally
        {
            _mutexConexao.Release();
        }

        await IniciarHubConnection();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Registro genérico de escuta
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Registra escuta para um evento de NOME FIXO do hub (ex.: "ReceberNotificacao"),
    /// com payload tipado. Deduplicado: registrar duas vezes o mesmo evento é no-op.
    /// A escuta é registrada mesmo se o servidor estiver fora do ar — passa a receber
    /// assim que a conexão subir — e sobrevive à reconstrução da conexão.
    /// </summary>
    public Task EscutarEvento<T>(string nomeEvento, Func<T, Task> handler)
        => RegistrarEscutaAsync(nomeEvento, conexao => conexao.On<T>(nomeEvento, handler));

    private async Task RegistrarEscutaAsync(string nomeEvento, Func<HubConnection, IDisposable> fabrica)
    {
        if (_registeredKeys.ContainsKey(nomeEvento))
            return;

        _fabricasEscuta[nomeEvento] = fabrica;

        // Registra o .On antes do start: o SignalR aceita registro com a conexão
        // parada, e assim a escuta não se perde se o start falhar agora.
        GarantirConexaoConstruida();
        _registeredKeys.GetOrAdd(nomeEvento, _ => fabrica(_hubConnection!));

        await IniciarHubConnection();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Progress bar (API pública original — INALTERADA para os módulos)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adiciona um componente interessado na lista de notificações dessa chave.
    /// </summary>
    public void RegistrarObserver(string chave, Func<ObservadorAutomacao, Task> callback)
    {
        _observersPorChave.AddOrUpdate(chave,
            new List<Func<ObservadorAutomacao, Task>> { callback },
            (key, list) =>
            {
                lock (list)
                {
                    if (!list.Contains(callback))
                    {
                        list.Add(callback);
                    }
                }
                return list;
            });

        // Se já temos dados em cache para essa chave, entregamos imediatamente para a UI não ficar vazia
        if (_progressoAtualPorHub.TryGetValue(chave, out var ultimoEstado))
        {
            _ = callback.Invoke(ultimoEstado);
        }
    }

    /// <summary>
    /// [IMPORTANTE] Remove um componente específico da lista de notificações.
    /// Chamado no Dispose do componente Blazor.
    /// </summary>
    public void RemoverObserver(string chave, Func<ObservadorAutomacao, Task> callback)
    {
        if (_observersPorChave.TryGetValue(chave, out var list))
        {
            lock (list)
            {
                list.Remove(callback);
            }
        }
    }

    /// <summary>
    /// Inicia a escuta real no SignalR (.On) para a chave dinâmica da operação.
    /// Gerencia a distribuição das mensagens para todos os observers da lista.
    /// </summary>
    public Task IniciarEscutaDaOperacao(string hubConnectId)
        => RegistrarEscutaAsync(hubConnectId, conexao =>
            conexao.On<int, ObservadorAutomacao>(hubConnectId, async (progresso, observer) =>
            {
                // 1. Atualiza o cache local
                _progressoAtualPorHub[hubConnectId] = observer;

                // 2. Verifica se existem componentes ouvindo essa chave
                if (_observersPorChave.TryGetValue(hubConnectId, out var callbacksList))
                {
                    Func<ObservadorAutomacao, Task>[] callbacksSnapshot;

                    // 3. Cópia segura da lista para iterar (evita "coleção modificada"
                    //    se um componente der Dispose enquanto o loop roda)
                    lock (callbacksList)
                    {
                        callbacksSnapshot = callbacksList.ToArray();
                    }

                    // 4. Dispara a atualização para todos os componentes (Aba 1, Aba 2, Componente X...)
                    if (callbacksSnapshot.Length > 0)
                    {
                        await Task.WhenAll(callbacksSnapshot.Select(cb => cb(observer)));
                    }
                }

                // 5. Gerencia conclusão
                if (observer.PercentualProcessado == 100 && !_flagCompletouPorHub.GetValueOrDefault(hubConnectId))
                {
                    _flagCompletouPorHub[hubConnectId] = true;
                    OnProgressUpdateCompleted?.Invoke(false);
                    OnProgressUpdateCompletedByKey?.Invoke(hubConnectId);
                }
            }));

    public void InterromperEscutaDaOperacao(string hubConnectId)
    {
        // Remove a escuta do SignalR (para de receber dados da rede para essa chave)
        if (_registeredKeys.TryRemove(hubConnectId, out var subscription))
        {
            subscription.Dispose();

            // Remove a fábrica para a escuta não ressuscitar numa reconstrução de conexão
            _fabricasEscuta.TryRemove(hubConnectId, out _);

            // Limpa caches
            _progressoAtualPorHub.TryRemove(hubConnectId, out _);
            _flagCompletouPorHub.TryRemove(hubConnectId, out _);
        }
    }

    public async Task ReiniciarEscutaDaOperacao(string hubConnectId)
    {
        InterromperEscutaDaOperacao(hubConnectId);
        await IniciarEscutaDaOperacao(hubConnectId);
    }

    public Task<ObservadorAutomacao?> ObterEstadoAtual(string hubConnectId)
    {
        if (_progressoAtualPorHub.TryGetValue(hubConnectId, out var observer))
        {
            return Task.FromResult<ObservadorAutomacao?>(observer);
        }
        return Task.FromResult<ObservadorAutomacao?>(null);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Dispose
    // ──────────────────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        _descartado = true;

        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }

        foreach (var subscription in _registeredKeys.Values)
        {
            try { subscription.Dispose(); }
            catch { }
        }
        _registeredKeys.Clear();
        _fabricasEscuta.Clear();
        _observersPorChave.Clear();
        _progressoAtualPorHub.Clear();
        _flagCompletouPorHub.Clear();
        _mutexConexao.Dispose();
    }

    /// <summary>
    /// Retry infinito: o padrão do WithAutomaticReconnect() desiste após 4 tentativas
    /// (~30s) e mata a conexão — tolerável para progress bar, inaceitável para
    /// notificação em tempo real, que exige conexão perene.
    /// </summary>
    private sealed class RepetirSempreRetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
            => retryContext.PreviousRetryCount switch
            {
                0 => TimeSpan.Zero,
                1 => TimeSpan.FromSeconds(2),
                2 => TimeSpan.FromSeconds(5),
                3 => TimeSpan.FromSeconds(10),
                _ => TimeSpan.FromSeconds(30)
            };
    }
}
