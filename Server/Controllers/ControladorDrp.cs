using Microsoft.AspNetCore.Mvc;
using SipubDesembolsos.Server.Servicos;

namespace SipubDesembolsos.Server.Controllers;

[ApiController]
[Route("api/drp")]
[Produces("application/json")]
public class ControladorDrp(ServicoDrp servico) : ControllerBase
{
    /// <summary>Lista os registros da aba DRP (controle de baixa).</summary>
    [HttpGet]
    public async Task<IActionResult> ObterTodos()
    {
        var lista = await servico.ObterTodosAsync();
        return Ok(lista);
    }

    /// <summary>Registra a baixa (em lote ou individual) dos registros informados.</summary>
    [HttpPut("baixar")]
    public async Task<IActionResult> Baixar([FromBody] RequisicaoBaixaDrp req)
    {
        if (req.Ids is not { Count: > 0 })
            return BadRequest(new { erro = "Informe ao menos um registro para dar baixa." });

        var total = await servico.BaixarAsync(req.Ids, req.MatriculaUsuario, req.Senha);
        return Ok(new { sucesso = true, totalBaixado = total });
    }
}

public record RequisicaoBaixaDrp(List<int> Ids, string MatriculaUsuario, string Senha);
