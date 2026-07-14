using Microsoft.AspNetCore.Mvc;
using SipubDesembolsos.Server.Servicos;

namespace SipubDesembolsos.Server.Controllers;

[ApiController]
[Route("api/desembolso")]
public class ControladorDesembolso(ServicoDesembolso servico) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Obter(string id)
    {
        var desembolso = await servico.ObterAsync(id);
        return desembolso is null ? NotFound() : Ok(desembolso);
    }

    [HttpPut("{id}/aprovar")]
    public async Task<IActionResult> Aprovar(string id, [FromBody] RequisicaoAprovacao req)
    {
        var encontrado = await servico.AprovarAsync(id, req.MatriculaUsuario, req.UsuarioNome);
        return encontrado
            ? Ok(new { sucesso = true, desembolsoId = id, novoStatus = "Aprovado" })
            : NotFound(new { erro = $"Desembolso '{id}' não encontrado." });
    }

    [HttpPut("{id}/rejeitar")]
    public async Task<IActionResult> Rejeitar(string id, [FromBody] RequisicaoRejeicao req)
    {
        var encontrado = await servico.RejeitarAsync(
            id, req.MatriculaUsuario, req.UsuarioNome, req.CodigoCoordenacao);
        return encontrado
            ? Ok(new { sucesso = true, desembolsoId = id, novoStatus = "Rejeitado" })
            : NotFound(new { erro = $"Desembolso '{id}' não encontrado." });
    }
}

public record RequisicaoAprovacao(string MatriculaUsuario, string UsuarioNome);
public record RequisicaoRejeicao(string MatriculaUsuario, string UsuarioNome, string CodigoCoordenacao);
