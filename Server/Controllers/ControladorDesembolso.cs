using Microsoft.AspNetCore.Mvc;
using SipubDesembolsos.Server.Servicos;

namespace SipubDesembolsos.Server.Controllers;

[ApiController]
[Route("api/desembolso")]
public class ControladorDesembolso(ServicoDesembolso servico) : ControllerBase
{
    /// <summary>Lista todos os desembolsos — consulta que monta a página de análise.</summary>
    [HttpGet]
    public async Task<IActionResult> ObterTodos()
    {
        var lista = await servico.ObterTodosAsync();
        return Ok(lista);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Obter(string id)
    {
        var desembolso = await servico.ObterAsync(id);
        return desembolso is null ? NotFound() : Ok(desembolso);
    }

    /// <summary>
    /// Roda a validação completa do desembolso (compara os dados da FPD com a
    /// macro de referência) e persiste a checklist resultante.
    /// </summary>
    [HttpPost("{id}/validar")]
    public async Task<IActionResult> Validar(string id)
    {
        var validacoes = await servico.ValidarAsync(id);
        return Ok(validacoes);
    }

    /// <summary>Lista a checklist de validações já persistida, com os comentários de cada item.</summary>
    [HttpGet("{id}/validacoes")]
    public async Task<IActionResult> ObterValidacoes(string id)
    {
        var validacoes = await servico.ObterValidacoesAsync(id);
        return Ok(validacoes);
    }

    /// <summary>Adiciona um comentário/tratativa a um item da checklist de validação.</summary>
    [HttpPost("{id}/validacoes/{numero}/comentarios")]
    public async Task<IActionResult> AdicionarComentario(string id, int numero, [FromBody] RequisicaoNovoComentario req)
    {
        var comentario = await servico.AdicionarComentarioValidacaoAsync(
            id, numero, req.Tipo, req.Texto, req.Autor, req.MatriculaAutor);

        return comentario is null
            ? NotFound(new { erro = $"Validação {numero} do desembolso '{id}' não encontrada." })
            : Ok(comentario);
    }

    /// <summary>Edita um comentário — só o autor original pode editar o próprio comentário.</summary>
    [HttpPut("{id}/validacoes/{numero}/comentarios/{comentarioId}")]
    public async Task<IActionResult> EditarComentario(
        string id, int numero, int comentarioId, [FromBody] RequisicaoEdicaoComentario req)
    {
        var resultado = await servico.EditarComentarioValidacaoAsync(comentarioId, req.Texto, req.MatriculaSolicitante);
        return resultado switch
        {
            ResultadoEdicaoComentario.Sucesso      => Ok(new { sucesso = true }),
            ResultadoEdicaoComentario.SemPermissao => StatusCode(403, new { erro = "Apenas o autor pode editar este comentário." }),
            _                                      => NotFound(new { erro = "Comentário não encontrado." }),
        };
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
public record RequisicaoNovoComentario(string Tipo, string Texto, string Autor, string MatriculaAutor);
public record RequisicaoEdicaoComentario(string Texto, string MatriculaSolicitante);
