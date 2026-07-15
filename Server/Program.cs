using Microsoft.EntityFrameworkCore;
using PlataformaNotificacao;
using PlataformaNotificacao.Application;
using PlataformaNotificacao.Application.Interface;
using SipubDesembolsos.Server.Hubs;
using SipubDesembolsos.Server.Data;
using SipubDesembolsos.Server.Eventos.Desembolso;
using SipubDesembolsos.Server.Servicos;

var builder = WebApplication.CreateBuilder(args);

// EF Core — SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Expõe AppDbContext como INotificacaoDbContext para PlataformaNotificacao
builder.Services.AddScoped<INotificacaoDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// EmpregadoService implementa IEmpregadoService — ambos registrados como Singleton
builder.Services.AddSingleton<EmpregadoService>();
builder.Services.AddSingleton<IEmpregadoService>(sp => sp.GetRequiredService<EmpregadoService>());

builder.Services.AddSingleton<SignalRService>();

builder.Services.AddScoped<INotificacaoService>(provider =>
{
    var db         = provider.GetRequiredService<INotificacaoDbContext>();
    var empregados = provider.GetRequiredService<IEmpregadoService>();
    var signalR    = provider.GetRequiredService<SignalRService>();
    var servico    = new NotificacaoService(db, empregados);
    servico.OnNotificacao += (sender, e) => _ = signalR.HandlerObserver(sender, e);
    return servico;
});

// ── Domain Events ──────────────────────────────────────────────────────────
builder.Services.AddScoped<IPublicador, Publicador>();
builder.Services.AddScoped<ServicoDesembolso>();
builder.Services.AddScoped<ServicoFpd>();
builder.Services.AddScoped<ServicoDrp>();

builder.Services.AddScoped<IEventHandler<DesembolsoAprovadoEvento>,   NotificarAprovacaoHandler>();
builder.Services.AddScoped<IEventHandler<DesembolsoRejeitadoEvento>,  NotificarRejeicaoHandler>();
builder.Services.AddScoped<IEventHandler<ComentarioInseridoEvento>,   NotificarComentarioHandler>();
builder.Services.AddScoped<IEventHandler<DesembolsosInseridosEvento>, NotificarInsercaoDesembolsosHandler>();

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title       = "SIPUB Desembolsos — API",
        Version     = "v1",
        Description = "API com SignalR + persistência de notificações no banco de dados."
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SIPUB Desembolsos v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "SIPUB API — Swagger";
    });
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();
// Rota do hub espelha o SIPUB original — o front monta a URL em CriarHubUrl: {base}chatHub
app.MapHub<HubNotificacao>("/chatHub");

app.Run();
