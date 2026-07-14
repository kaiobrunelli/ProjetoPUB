using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SipubDesembolsos.Client;
using SipubDesembolsos.Client.Servicos;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var urlServidor = builder.Configuration["ServidorUrl"] ?? "https://localhost:7001";

// HttpClient aponta para a API do servidor
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(urlServidor + "/") });

// SignalR: transporte (conexão única) + domínio de notificação — mesma arquitetura do SIPUB original
builder.Services.AddSingleton(_ => new SignalRService(urlServidor));
builder.Services.AddSingleton<ServicoNotificacao>();
builder.Services.AddSingleton<ServicoUsuario>();

await builder.Build().RunAsync();
