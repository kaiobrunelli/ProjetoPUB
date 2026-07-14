using Microsoft.AspNetCore.Mvc;
using PlataformaNotificacao;
using PlataformaNotificacao.Application.Interface;
using PlataformaNotificacao.Domain;
using PlataformaNotificacao.Domain.DTO;
using PlataformaNotificacao.Domain.Enum;

namespace SipubDesembolsos.Server.Controllers;

[ApiController]
[Route("api/notificacao")]
[Produces("application/json")]
public class ControladorNotificacao(INotificacaoService servico) : ControllerBase
{
    /// <summary>Retorna as notificações do usuário com estado de leitura.</summary>
    [HttpGet("minhas")]
    [ProducesResponseType(typeof(List<NotificacaoDto>), 200)]
    public async Task<IActionResult> ObterMinhas([FromQuery] string matriculaUsuario)
    {
        if (string.IsNullOrWhiteSpace(matriculaUsuario))
            return BadRequest("matriculaUsuario é obrigatório.");

        var lista = await servico.ObterMinhasAsync(matriculaUsuario);
        return Ok(lista);
    }

    /// <summary>Retorna a contagem de notificações não lidas (badge do sino).</summary>
    [HttpGet("nao-lidas/total")]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<IActionResult> ContarNaoLidas([FromQuery] string matriculaUsuario)
    {
        if (string.IsNullOrWhiteSpace(matriculaUsuario))
            return BadRequest("matriculaUsuario é obrigatório.");

        var total = await servico.ContarNaoLidasAsync(matriculaUsuario);
        return Ok(total);
    }

    /// <summary>Marca uma notificação específica como lida.</summary>
    [HttpPut("{id:int}/lida")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> MarcarLida(int id, [FromQuery] string matriculaUsuario)
    {
        if (string.IsNullOrWhiteSpace(matriculaUsuario))
            return BadRequest("matriculaUsuario é obrigatório.");

        var ok = await servico.MarcarLidaAsync(id, matriculaUsuario);
        return ok ? Ok(new { sucesso = true }) : NotFound();
    }

    /// <summary>Marca todas as notificações do usuário como lidas.</summary>
    [HttpPut("marcar-todas-lidas")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> MarcarTodasLidas([FromQuery] string matriculaUsuario)
    {
        if (string.IsNullOrWhiteSpace(matriculaUsuario))
            return BadRequest("matriculaUsuario é obrigatório.");

        await servico.MarcarTodasLidasAsync(matriculaUsuario);
        return Ok(new { sucesso = true });
    }

    /// <summary>Verifica se a API está online.</summary>
    [HttpGet("status")]
    public IActionResult ObterStatus() =>
        Ok(new { online = true, timestamp = DateTime.UtcNow });
}
