# Porte das notificações em tempo real → SIPUB original

Arquivos prontos para copiar para a Plataforma Operacional. Esta pasta **não** faz
parte do build do SipubDesembolsosV2 — é só área de staging.

```
PortarParaSipub/
├── Model/                      → copiar para PlataformaOperacional.Model.Plataforma
│   ├── MensagemNotificacao.cs      (payload SignalR em tempo real)
│   ├── NotificacaoDto.cs           (retorno da API REST / histórico com estado de leitura)
│   ├── TipoNotificacao.cs          (Normal, Alerta, Urgente)
│   ├── EscopoNotificacao.cs        (Geral, Modulo, Individual)
│   └── CodigoAplicativo.cs         (usar o enum de módulos existente, se houver)
└── Service/                    → copiar para PlataformaOperacional.Service.Middleware
    ├── SignalRService.cs           (substitui o atual — API pública de progresso preservada)
    └── ServicoNotificacao.cs       (novo — domínio de notificação)
```

## O que mudou no SignalRService (vs. o original)

**Correções:**
1. **Bug de reconexão morta** — o esquema `_startTask` + `Interlocked` reaproveitava uma
   task de start *completada* mesmo com a conexão caída; depois que o retry automático
   desistia, nada mais reconectava. Substituído por `SemaphoreSlim` com dupla checagem
   de estado.
2. **Retry infinito** — `WithAutomaticReconnect()` padrão desiste após 4 tentativas
   (~30s). Agora usa `RepetirSempreRetryPolicy` (0s, 2s, 5s, 10s, depois 30s para
   sempre) + handler em `Closed` + retry do start inicial a cada 5s. Notificação em
   tempo real exige conexão perene.
3. Removidos os `catch (Exception ex) { throw; }` vazios; `_flagCompletouPorHub`
   agora é `readonly`.

**Adições (sem quebrar nada):**
4. **`EscutarEvento<T>(nomeEvento, handler)`** — registro genérico de escuta para
   eventos de nome fixo (é o que o ServicoNotificacao usa). Mesmo dedup das chaves de
   progresso; a escuta sobrevive à reconstrução da conexão (fábricas re-aplicadas).
5. **`DefinirUsuarioAsync(matricula)`** — a matrícula vai como `?userId=` na URL do
   hub. Chamar antes da primeira conexão; se mudar depois, a conexão é reconstruída e
   todas as escutas re-registradas automaticamente.
6. **Estado observável** — `Conectado` (bool) e eventos `AoConectar`, `AoReconectar`,
   `AoDesconectar`. A UI decide o que exibir (o transporte não injeta mais nenhuma
   "notificação sintética").

**O que NÃO mudou:** `RegistrarObserver`, `RemoverObserver`, `IniciarEscutaDaOperacao`,
`InterromperEscutaDaOperacao`, `ReiniciarEscutaDaOperacao`, `ObterEstadoAtual`,
`OnProgressUpdateCompleted`, `OnProgressUpdateCompletedByKey`, `CriarHubUrl`,
`IniciarHubConnection`, construtor. Nenhum componente de progress bar precisa de ajuste.
A progress bar continua roteada por **chave** (nome do evento) e ignora a matrícula da URL.

## Registro no Program.cs (front)

```csharp
builder.Services.AddSingleton<SignalRService>();
builder.Services.AddSingleton<ServicoNotificacao>();
```

## Inicialização (uma vez, no bootstrap — ex.: MainLayout)

```csharp
// depois que o serviço de usuário resolver a matrícula:
await ServicoNotificacao.IniciarAsync(usuario.Matricula);
ServicoNotificacao.AoReceberNotificacao += TratarNotificacao;   // sino/toast/badge
```

## Lado servidor (chatHub) — o que precisa existir

1. **Agrupar conexão por usuário** no `OnConnectedAsync`:

```csharp
public override async Task OnConnectedAsync()
{
    var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
    if (!string.IsNullOrWhiteSpace(userId))
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
    await base.OnConnectedAsync();
}
```

2. **Emitir a notificação** pelo mesmo hub (evento fixo `ReceberNotificacao`):

```csharp
// individual:
await hub.Clients.Group($"user-{matricula}").SendAsync("ReceberNotificacao", mensagem);
// geral:
await hub.Clients.All.SendAsync("ReceberNotificacao", mensagem);
```

O envio de progresso (`SendAsync(chaveDaOperacao, progresso, observer)`) continua
exatamente como está — uma conexão, um hub, dois tipos de tráfego.

3. **API REST do histórico** (painel do sino): `GET api/notificacao/minhas?usuarioId=`,
`GET api/notificacao/nao-lidas/total?usuarioId=`, `PUT api/notificacao/{id}/lida`,
`PUT api/notificacao/marcar-todas-lidas` — referência de implementação em
`Server/Controllers/ControladorNotificacao.cs` do SipubDesembolsosV2.

## Referência de UI

A `BarraSuperior.razor` do V2 (`Client/Layout/BarraSuperior.razor`) é a referência do
consumo: painel, badge, toast e dialog de confirmação. Ao portar, trocar
`ServicoNotificacaoHub` por `ServicoNotificacao` (a superfície é equivalente:
`AoReceberNotificacao`, `Conectado`) e mover a assinatura para o layout/bootstrap.
