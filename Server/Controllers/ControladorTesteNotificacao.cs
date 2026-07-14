using Microsoft.AspNetCore.Mvc;
using PlataformaNotificacao;
using PlataformaNotificacao.Application.Interface;
using PlataformaNotificacao.Domain;
using PlataformaNotificacao.Domain.DTO;
using PlataformaNotificacao.Domain.Enum;

namespace SipubDesembolsos.Server.Controllers;

[ApiController]
[Route("api/teste/notificacao")]
[Produces("application/json")]
public class ControladorTesteNotificacao(INotificacaoService servico) : ControllerBase
{
    #region TESTE

    [HttpPost("geral")]
    public async Task<IActionResult> EnviarGeral([FromBody] RequisicaoTesteNotificacao req)
    {
        await servico.EnviarGeralAsync(req.Titulo, req.Mensagem, req.Tipo, link: req.Link);
        return Ok(new { sucesso = true, escopo = "Geral" });
    }

    [HttpPost("modulo")]
    public async Task<IActionResult> EnviarModulo([FromBody] RequisicaoTesteNotificacao req)
    {
        var app = req.CodigoAplicativo ?? CodigoAplicativo.Sipub;
        await servico.EnviarModuloAsync(req.Titulo, req.Mensagem, req.Tipo, app, link: req.Link);
        return Ok(new { sucesso = true, escopo = "Modulo", aplicativo = app.ToString() });
    }

    [HttpPost("lista")]
    public async Task<IActionResult> EnviarLista([FromBody] RequisicaoTesteListaNotificacao req)
    {
        if (req.Matriculas is null || req.Matriculas.Count == 0)
            return BadRequest("Matriculas não pode ser vazio.");

        await servico.EnviarPorMatriculasAsync(
            req.Matriculas,
            req.Titulo, req.Mensagem, req.Tipo,
            codigoAplicativo: req.CodigoAplicativo,
            link: req.Link);

        return Ok(new { sucesso = true, escopo = "Lista", total = req.Matriculas.Count });
    }

    [HttpPost("coordenacao/{codigoCoordenacao}")]
    public async Task<IActionResult> EnviarCoordenacao(
        string codigoCoordenacao, [FromBody] RequisicaoTesteNotificacao req)
    {
        if (string.IsNullOrWhiteSpace(codigoCoordenacao))
            return BadRequest("codigoCoordenacao é obrigatório.");

        await servico.EnviarCoordenacaoAsync(
            codigoCoordenacao,
            req.Titulo, req.Mensagem, req.Tipo,
            link: req.Link);

        return Ok(new { sucesso = true, escopo = "Coordenacao", codigoCoordenacao });
    }

    [HttpPost("matriculas")]
    public async Task<IActionResult> EnviarPorMatriculas([FromBody] RequisicaoTesteMatriculasNotificacao req)
    {
        if (req.Matriculas is null || !req.Matriculas.Any())
            return BadRequest("Matriculas não pode ser vazio.");

        await servico.EnviarPorMatriculasAsync(
            req.Matriculas,
            req.Titulo, req.Mensagem, req.Tipo,
            codigoAplicativo: req.CodigoAplicativo,
            link: req.Link);

        return Ok(new { sucesso = true, escopo = "Matriculas", total = req.Matriculas.Count(), req.Matriculas });
    }

    [HttpGet("usuarios")]
    public IActionResult ListarUsuarios()
    {
        var usuarios = new[]
        {
            new { Matricula = "c123456", Nome = "Ana Lima",       Cargo = "Analista Sênior",    Coordenacao = "COORD-SIPUB", Aplicativos = new[] { "Sipub", "EncontroDeContas" } },
            new { Matricula = "c102944", Nome = "Bruno Costa",    Cargo = "Gestor",              Coordenacao = "COORD-SIPUB", Aplicativos = new[] { "Sipub", "EncontroDeContas" } },
            new { Matricula = "c134872", Nome = "Carla Mendes",   Cargo = "Analista Júnior",     Coordenacao = "COORD-SIPUB", Aplicativos = new[] { "Sipub" } },
            new { Matricula = "c110233", Nome = "Diego Santos",   Cargo = "Coordenador",         Coordenacao = "COORD-FIN",   Aplicativos = new[] { "EncontroDeContas", "Amortizacao" } },
            new { Matricula = "c145097", Nome = "Elena Ferreira", Cargo = "Diretora Financeira", Coordenacao = "COORD-DIR",   Aplicativos = new[] { "Sipub", "EncontroDeContas", "Amortizacao", "Geral" } },
        };
        return Ok(usuarios);
    }

    #endregion TESTE
}

// ── DTOs de teste ─────────────────────────────────────────────────────────────

/// <summary>
/// Tipo: Normal | Alerta | Urgente.
/// Urgente → ExigeConfirmacao derivado automaticamente pela API.
/// </summary>
public record RequisicaoTesteNotificacao(
    string            Titulo,
    string            Mensagem,
    TipoNotificacao   Tipo             = TipoNotificacao.Normal,
    CodigoAplicativo? CodigoAplicativo = null,
    string?           Link             = null);

public record RequisicaoTesteListaNotificacao(
    List<string>      Matriculas,
    string            Titulo,
    string            Mensagem,
    TipoNotificacao   Tipo             = TipoNotificacao.Normal,
    CodigoAplicativo? CodigoAplicativo = null,
    string?           Link             = null);

public record RequisicaoTesteMatriculasNotificacao(
    IEnumerable<string> Matriculas,
    string              Titulo,
    string              Mensagem,
    TipoNotificacao     Tipo             = TipoNotificacao.Normal,
    CodigoAplicativo?   CodigoAplicativo = null,
    string?             Link             = null);
