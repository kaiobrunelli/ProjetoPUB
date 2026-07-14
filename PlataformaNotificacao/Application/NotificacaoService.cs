using Microsoft.EntityFrameworkCore;
using PlataformaNotificacao.Application.Interface;
using PlataformaNotificacao.Domain;
using PlataformaNotificacao.Domain.DTO;
using PlataformaNotificacao.Domain.Enum;

namespace PlataformaNotificacao.Application;

public class NotificacaoService(INotificacaoDbContext db, IEmpregadoService empregados) : INotificacaoService
{
    public event EventHandler<NotificacaoEventArgs>? OnNotificacao;

    public async Task EnviarGeralAsync(
        string titulo, string mensagem, TipoNotificacao tipo = TipoNotificacao.Normal,
        string? link = null, int? dias = null, int? horas = null)
    {
        var matriculas = empregados.ObterMatriculasTodos();
        var notif = CriarNotificacao(titulo, mensagem, tipo, null, dias, horas);

        notif.Destinatarios = matriculas
            .Select(mat => new ControleVisualizacao { MatriculaUsuario = mat, Link = link })
            .ToList();

        db.Notificacoes.Add(notif);
        await db.SaveChangesAsync();

        OnNotificacao?.Invoke(this, new NotificacaoEventArgs
        {
            Escopo   = EscopoNotificacao.Geral,
            Mensagem = ToMensagem(notif, link, EscopoNotificacao.Geral)
        });
    }
    public async Task EnviarModuloAsync(
        string titulo, string mensagem, TipoNotificacao tipo,
        CodigoAplicativo codigoAplicativo, string? link = null,
        int? dias = null, int? horas = null)
    {
        var matriculas = empregados.ObterMatriculasPorModulo(codigoAplicativo.ToString());
        var notif = CriarNotificacao(titulo, mensagem, tipo, codigoAplicativo, dias, horas);

        notif.Destinatarios = matriculas
            .Select(mat => new ControleVisualizacao { MatriculaUsuario = mat, Link = link })
            .ToList();

        db.Notificacoes.Add(notif);
        await db.SaveChangesAsync();

        OnNotificacao?.Invoke(this, new NotificacaoEventArgs
        {
            Escopo          = EscopoNotificacao.Modulo,
            Destinatarios = matriculas,
            Mensagem        = ToMensagem(notif, link, EscopoNotificacao.Modulo)
        });
    }

    public async Task EnviarCoordenacaoAsync(
        string codigoCoordenacao,
        string titulo, string mensagem, TipoNotificacao tipo = TipoNotificacao.Normal,
        string? link = null, int? dias = null, int? horas = null)
    {
        var matriculas = empregados.ObterMatriculasPorCoordenacao(codigoCoordenacao);
        await EnviarIndividualAsync(titulo, mensagem, matriculas, null, link, dias, horas, tipo);
    }

    public async Task EnviarPorMatriculasAsync(
        IEnumerable<string> matriculas,
        string titulo, string mensagem, TipoNotificacao tipo = TipoNotificacao.Normal,
        CodigoAplicativo? codigoAplicativo = null, string? link = null,
        int? dias = null, int? horas = null)
    {
        // A lista passada pelo chamador É o destino — a API não valida matrículas.
        
        
        await EnviarIndividualAsync(titulo, mensagem, matriculas.ToList(), codigoAplicativo, link, dias, horas, tipo);
    }

    public async Task EnviarIndividualAsync(
        string titulo, string mensagem,
        List<string> matriculas, CodigoAplicativo? codigoAplicativo = null,
        string? link = null, int? dias = null, int? horas = null,
        TipoNotificacao tipo = TipoNotificacao.Normal)
    {
        var notif = CriarNotificacao(titulo, mensagem, tipo, codigoAplicativo, dias, horas);

        notif.Destinatarios = matriculas
            .Select(mat => new ControleVisualizacao { MatriculaUsuario = mat, Link = link })
            .ToList();

        db.Notificacoes.Add(notif);
        await db.SaveChangesAsync();

        OnNotificacao?.Invoke(this, new NotificacaoEventArgs
        {
            ChaveConexao    = "ReceberNotificacao",
            Escopo          = EscopoNotificacao.Individual,
            Destinatarios = matriculas,
            Mensagem        = ToMensagem(notif, link, EscopoNotificacao.Individual)
        });
    }

    public async Task<List<NotificacaoDto>> ObterMinhasAsync(string matriculaUsuario, int limite = 50)
    {
        var agora = DateTime.UtcNow;
        return await db.ControleVisualizacoes
            .Where(cv => cv.MatriculaUsuario == matriculaUsuario && cv.Notificacao.DataValidade > agora)
            .OrderByDescending(cv => cv.Notificacao.DataCriacao)
            .Take(limite)
            .Select(cv => new NotificacaoDto
            {
                CodigoNotificacao = cv.Notificacao.CodigoNotificacao,
                Titulo            = cv.Notificacao.Titulo,
                Mensagem          = cv.Notificacao.Mensagem,
                Tipo              = cv.Notificacao.Tipo,
                CodigoAplicativo  = cv.Notificacao.CodigoAplicativo,
                DataCriacao       = cv.Notificacao.DataCriacao,
                DataValidade      = cv.Notificacao.DataValidade,
                DataVisualizacao  = cv.DataVisualizacao,
                Link              = cv.Link
            })
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> ContarNaoLidasAsync(string matriculaUsuario)
    {
        var agora = DateTime.UtcNow;
        return await db.ControleVisualizacoes
            .Where(cv => cv.MatriculaUsuario == matriculaUsuario
                      && cv.DataVisualizacao == null
                      && cv.Notificacao.DataValidade > agora)
            .CountAsync();
    }

    public async Task<bool> MarcarLidaAsync(int codigoNotificacao, string matriculaUsuario)
    {
        var cv = await db.ControleVisualizacoes
            .FirstOrDefaultAsync(x => x.CodigoNotificacao == codigoNotificacao
                                   && x.MatriculaUsuario  == matriculaUsuario);

        if (cv is null || cv.DataVisualizacao != null) return false;

        cv.DataVisualizacao = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task MarcarTodasLidasAsync(string matriculaUsuario)
    {
        var agora    = DateTime.UtcNow;
        var naoLidas = await db.ControleVisualizacoes
            .Where(cv => cv.MatriculaUsuario == matriculaUsuario
                      && cv.DataVisualizacao == null
                      && cv.Notificacao.DataValidade > agora)
            .ToListAsync();

        foreach (var cv in naoLidas)
            cv.DataVisualizacao = agora;

        await db.SaveChangesAsync();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static Notificacao CriarNotificacao(
        string titulo, string mensagem, TipoNotificacao tipo,
        CodigoAplicativo? codigoAplicativo,
        int? dias, int? horas) => new()
    {
        Titulo           = titulo,
        Mensagem         = mensagem,
        Tipo             = tipo,
        CodigoAplicativo = codigoAplicativo,
        DataValidade     = CalcularExpiracao(dias, horas)
    };

    private static DateTime CalcularExpiracao(int? dias, int? horas)
    {
        if (dias is null && horas is null) return DateTime.UtcNow.AddDays(7);
        return DateTime.UtcNow.AddDays(dias ?? 0).AddHours(horas ?? 0);
    }

    private static MensagemNotificacao ToMensagem(Notificacao n, string? link, EscopoNotificacao escopo) => new()
    {
        CodigoNotificacao = n.CodigoNotificacao,
        Titulo            = n.Titulo,
        Mensagem          = n.Mensagem,
        Tipo              = n.Tipo,
        Escopo            = escopo,
        CodigoAplicativo  = n.CodigoAplicativo,
        Link              = link,
        CriadaEm         = n.DataCriacao,
        DataValidade      = n.DataValidade
    };
}
