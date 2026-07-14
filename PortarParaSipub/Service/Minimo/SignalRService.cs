using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using PlataformaOperacional.Model.Plataforma;
using System.Collections.Concurrent;

namespace PlataformaOperacional.Service.Middleware
{
    // ═════════════════════════════════════════════════════════════════════════
    // VERSÃO MÍNIMA/ADITIVA: o código original da progress bar está INTACTO.
    // As mudanças para notificação estão em DOIS pontos, marcados com "[NOVO]":
    //   1. StartHubInternalAsync — a URL passa a carregar ?userId={matricula}
    //      (e os handlers Reconnected/Closed são pendurados na conexão nova)
    //   2. Bloco "NOTIFICAÇÕES" no fim da classe — campos, eventos e métodos novos
    // ═════════════════════════════════════════════════════════════════════════

    public class SignalRService : IAsyncDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClient _httpLocal;
        private readonly BlazorMockService _mockBlazor;
        private HubConnection _hubConnection;
        private Task _startTask;

        public string _baseAdress => _mockBlazor.MockarDados ? _httpLocal.BaseAddress.ToString() : _httpClient.BaseAddress.ToString();

        public SignalRService(IHttpClientFactory httpClientFactory, BlazorMockService blazorMockService)
        {
            //_httpClient = httpClientFactory.CreateClient(ClientName);
            _httpClient = httpClientFactory.CreateClient("Api");

            _httpLocal = httpClientFactory.CreateClient("ApiLocal");
            _mockBlazor = blazorMockService;
            CriarHubUrl(_baseAdress);
        }

        // Controla as conexões ativas no SignalR (o ".On") para não duplicar listeners na mesma chave
        private readonly ConcurrentDictionary<string, IDisposable> _registeredKeys = new();

        // Armazena o último estado recebido (Cache) para entregar a novos observadores imediatamente (ex: troca de aba)
        private readonly ConcurrentDictionary<string, ObservadorAutomacao> _progressoAtualPorHub = new();

        // LISTA de Callbacks: Permite que vários componentes ou abas escutem a mesma chave
        private readonly ConcurrentDictionary<string, List<Func<ObservadorAutomacao, Task>>> _observersPorChave = new();

        // URL do Hub (Ajuste conforme sua necessidade real)
        //private readonly string _hubUrlProd = "https://www.ativo.fgts.caixa/PlataformaOperacional/chatHub";
        private string _hubUrlProd;
        public string HubUrlProd => _hubUrlProd;

        // Controle de conclusão
        private ConcurrentDictionary<string, bool> _flagCompletouPorHub = new();

        // Evento global opcional
        public event Action<bool> OnProgressUpdateCompleted;
        public event Action<string> OnProgressUpdateCompletedByKey;

        // [NOVO - ADITIVO] Progresso de QUALQUER operação (chave, estado) — para a
        // barra global do layout, que mostra o que estiver rodando sem conhecer a
        // chave. Se ninguém assinar, o Invoke é no-op: zero efeito no fluxo atual.
        public event Action<string, ObservadorAutomacao> AoReceberProgressoGlobal;

        public void CriarHubUrl(string url)
        {
            _hubUrlProd = $"{url}chatHub";
        }

        public Task IniciarHubConnection()
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                return Task.CompletedTask;
            }

            var existingTask = _startTask;
            if (existingTask != null && !existingTask.IsFaulted && !existingTask.IsCanceled)
            {
                return existingTask;
            }

            var newTask = StartHubInternalAsync();
            var winner = Interlocked.CompareExchange(ref _startTask, newTask, existingTask);

            return winner == existingTask ? newTask : winner;
        }

        private async Task StartHubInternalAsync()
        {
            if (_hubConnection == null)
            {
                var built = new HubConnectionBuilder()
                  .WithUrl(MontarUrlComUsuario())   // [NOVO] antes: .WithUrl(_hubUrlProd) — agora carrega ?userId={matricula}
                  .WithAutomaticReconnect()
                  .Build();

                // [NOVO] estado da conexão para a UI do sino ("Conectado em tempo real"/"Offline")
                built.Reconnected += id => { AoReconectar?.Invoke(id); return Task.CompletedTask; };
                built.Closed += _ => { AoDesconectar?.Invoke(); return Task.CompletedTask; };

                Interlocked.CompareExchange(ref _hubConnection, built, null);
            }

            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _hubConnection.StartAsync();
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Adiciona um componente interessado na lista de notificações dessa chave.
        /// </summary>
        public void RegistrarObserver(string chave, Func<ObservadorAutomacao, Task> callback)
        {
            _observersPorChave.AddOrUpdate(chave,
                // Se a chave não existe, cria uma nova lista com o callback
                new List<Func<ObservadorAutomacao, Task>> { callback },
                // Se a chave já existe, adiciona o callback na lista existente
                (key, list) =>
                {
                    lock (list) // Lock para garantir que não haja conflito de thread
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
                // Executa sem await (fire-and-forget) para não travar a thread atual
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
                lock (list) // Lock é essencial aqui para não corromper a lista enquanto ela está sendo lida no loop
                {
                    list.Remove(callback);
                }
            }
        }

        /// <summary>
        /// Inicia a escuta real no SignalR (.On).
        /// Gerencia a distribuição das mensagens para todos os observers da lista.
        /// </summary>
        public async Task IniciarEscutaDaOperacao(string hubConnectId)
        {
            if (_registeredKeys.ContainsKey(hubConnectId))
            {
                return;
            }

            try
            {
                await IniciarHubConnection();

                _registeredKeys.GetOrAdd(hubConnectId, key =>
                {
                    return _hubConnection.On<int, ObservadorAutomacao>(key, async (progresso, observer) =>
                    {
                        // 1. Atualiza o cache local
                        _progressoAtualPorHub[key] = observer;

                        // [NOVO - ADITIVO] repassa aos ouvintes globais (barra do layout)
                        AoReceberProgressoGlobal?.Invoke(key, observer);

                        // 2. Verifica se existem componentes ouvindo essa chave
                        if (_observersPorChave.TryGetValue(key, out var callbacksList))
                        {
                            Func<ObservadorAutomacao, Task>[] callbacksSnapshot;

                            // 3. Cria uma cópia segura da lista para iterar
                            // Isso evita erro de "Coleção modificada" se um componente der Dispose enquanto o loop roda
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
                        if (observer.PercentualProcessado == 100 && !_flagCompletouPorHub.GetValueOrDefault(key))
                        {
                            _flagCompletouPorHub[key] = true;
                            OnProgressUpdateCompleted?.Invoke(false);
                            OnProgressUpdateCompletedByKey?.Invoke(key);
                        }
                    });
                });
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public void InterromperEscutaDaOperacao(string hubConnectId)
        {
            // Remove a escuta do SignalR (Para de receber dados da rede para essa chave)
            if (_registeredKeys.TryRemove(hubConnectId, out var subscription))
            {
                subscription.Dispose();

                // Limpa caches
                _progressoAtualPorHub.TryRemove(hubConnectId, out _);
                _flagCompletouPorHub.TryRemove(hubConnectId, out _);

                // Opcional: Limpa a lista de observers se a conexão for derrubada forçadamente
                // _observersPorChave.TryRemove(hubConnectId, out _);

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

        public async ValueTask DisposeAsync()
        {
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
            _observersPorChave.Clear();
            _progressoAtualPorHub.Clear();
            _flagCompletouPorHub.Clear();
        }

        // ══════════════════════════════════════════════════════════════════════
        // [NOVO] NOTIFICAÇÕES — tudo abaixo é ADIÇÃO; nada da progress bar mudou
        // ══════════════════════════════════════════════════════════════════════

        private string _matricula;

        /// <summary>Matrícula atual da conexão (null se ainda não definida).</summary>
        public string UsuarioAtual => _matricula;

        /// <summary>Estado da conexão, para a UI ("Conectado em tempo real" / "Offline").</summary>
        public bool Conectado => _hubConnection?.State == HubConnectionState.Connected;

        /// <summary>Reconexão automática concluída (novo ConnectionId).</summary>
        public event Action<string> AoReconectar;

        /// <summary>Conexão caiu e o retry automático desistiu.</summary>
        public event Action AoDesconectar;

        /// <summary>
        /// Define a matrícula que vai como ?userId= na URL do hub — é o que faz o
        /// OnConnectedAsync do chatHub colocar a conexão no grupo do usuário.
        ///
        /// [ATENÇÃO] Deve ser chamada ANTES da primeira conexão (a URL é fixada na
        /// construção da HubConnection e esta versão NÃO reconstrói a conexão).
        /// No fluxo normal o MainLayout resolve o usuário antes de qualquer página,
        /// então a ordem é garantida. Se chamada tarde demais, loga o aviso abaixo.
        /// </summary>
        public void DefinirUsuario(string matricula)
        {
            _matricula = matricula;

            if (_hubConnection is not null)
                Console.WriteLine("[SignalR] AVISO: DefinirUsuario chamado após a conexão já existir — " +
                                  "a identidade NÃO se aplica à conexão atual (notificações individuais não chegarão).");
        }

        private string MontarUrlComUsuario() =>
            string.IsNullOrWhiteSpace(_matricula)
                ? _hubUrlProd
                : $"{_hubUrlProd}?userId={Uri.EscapeDataString(_matricula)}";

        /// <summary>
        /// Registra escuta para um evento de NOME FIXO do hub (ex.: "ReceberNotificacao").
        /// Usa o mesmo dedup (_registeredKeys) das chaves de progresso.
        /// Se o start falhar (servidor fora do ar), a escuta é registrada mesmo assim —
        /// a HubConnection aceita .On com a conexão parada e ela passa a receber quando
        /// a conexão subir (o ServicoNotificacao cuida das novas tentativas de start).
        /// </summary>
        public async Task EscutarEvento<T>(string nomeEvento, Func<T, Task> handler)
        {
            if (_registeredKeys.ContainsKey(nomeEvento))
                return;

            try
            {
                await IniciarHubConnection();
            }
            catch
            {
                // A conexão foi construída mesmo com o start falhando — seguimos e
                // registramos a escuta; o retry de conexão fica por conta do chamador.
            }

            if (_hubConnection is not null)
                _registeredKeys.GetOrAdd(nomeEvento, key => _hubConnection.On<T>(key, handler));
        }
    }
}
