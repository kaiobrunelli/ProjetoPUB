using Microsoft.EntityFrameworkCore;
using PlataformaNotificacao;
using SipubDesembolsos.Server.Data;
using SipubDesembolsos.Server.Entidades;
using SipubDesembolsos.Server.Eventos.Desembolso;

namespace SipubDesembolsos.Server.Servicos;

public enum ResultadoEdicaoComentario { Sucesso, NaoEncontrado, SemPermissao }

public class ServicoDesembolso(AppDbContext db, IPublicador publicador)
{
    /// <summary>Lista todos os desembolsos — consulta que monta a página de análise.</summary>
    public async Task<List<DesembolsoCAD>> ObterTodosAsync() =>
        await db.Desembolsos.ToListAsync();

    public async Task<DesembolsoCAD?> ObterAsync(string id) =>
        await db.Desembolsos.FindAsync(id);

    public async Task<bool> AprovarAsync(string id, string matriculaUsuario, string usuarioNome)
    {
        var desembolso = await db.Desembolsos.FindAsync(id);
        if (desembolso is null) return false;

        desembolso.Status      = "Aprovado";
        desembolso.ValidadoEm  = DateTime.UtcNow;
        desembolso.ValidadoPor = matriculaUsuario;

        // Aprovar tira o desembolso da lista de análise e o envia para a aba DRP.
        var fpd = await db.FichasPrevisaoDesembolso.FirstOrDefaultAsync(f => f.DesembolsoId == id);
        db.RegistrosDrp.Add(new RegistroDrp
        {
            DesembolsoId    = id,
            Gigov           = fpd?.Gigov ?? "",
            ContratoDv      = fpd?.ContratoAf ?? id,
            TipoDesembolso  = string.IsNullOrWhiteSpace(fpd?.TipoDesembolso) ? "normal" : fpd.TipoDesembolso,
            ValorFgts       = fpd?.ParticipacaoFgts ?? 0,
            DataSolicitacao = fpd?.DataSolicitado ?? DateTime.UtcNow,
            Responsavel     = matriculaUsuario,
            Gestor          = fpd?.Gestor ?? "",
        });

        await db.SaveChangesAsync();

        await publicador.PublicarAsync(new DesembolsoAprovadoEvento(id, matriculaUsuario, usuarioNome));

        return true;
    }

    public async Task<bool> RejeitarAsync(
        string id, string matriculaUsuario, string usuarioNome, string codigoCoordenacao)
    {
        var desembolso = await db.Desembolsos.FindAsync(id);
        if (desembolso is null) return false;

        desembolso.Status      = "Rejeitado";
        desembolso.ValidadoEm  = DateTime.UtcNow;
        desembolso.ValidadoPor = matriculaUsuario;

        await db.SaveChangesAsync();

        await publicador.PublicarAsync(
            new DesembolsoRejeitadoEvento(id, matriculaUsuario, usuarioNome, codigoCoordenacao));

        return true;
    }

    public async Task InserirComentarioAsync(
        string desembolsoId, string autorNome, string comentario, string matriculaDestinatario)
    {
        // lógica de persistência do comentário aqui...

        await publicador.PublicarAsync(new ComentarioInseridoEvento(
            DesembolsoId:          desembolsoId,
            AutorNome:             autorNome,
            Comentario:            comentario,
            MatriculaDestinatario: matriculaDestinatario  // service sabe quem recebe
        ));
    }

    public async Task InserirDesembolsosAsync(List<string> ids, string operadorNome)
    {
        // lógica de persistência dos desembolsos aqui...

        await publicador.PublicarAsync(new DesembolsosInseridosEvento(
            DesembolsoIds: ids,
            OperadorNome:  operadorNome
        ));
    }

    // ────────────────────────────────────────────────────────────────────
    // VALIDAÇÃO DO DESEMBOLSO — checklist gerada ao comparar os dados da
    // FPD com a macro de referência (fonte de verdade hoje fora do sistema).
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Roda a validação completa do desembolso, comparando os dados enviados
    /// na FPD com a macro de referência, e persiste a checklist resultante.
    /// TODO: integrar com a macro real — por enquanto gera os itens padrão
    /// como "pendente", para análise manual (mesmos itens hoje simulados em
    /// ServicoMock.ObterValidacoesFpd, no cliente).
    /// </summary>
    public async Task<List<ValidacaoDesembolso>> ValidarAsync(string desembolsoId)
    {
        var existentes = await db.ValidacoesDesembolso
            .Where(v => v.DesembolsoId == desembolsoId)
            .ToListAsync();

        if (existentes.Count > 0)
            return existentes;

        var itens = new (string Titulo, string Detalhe)[]
        {
            ("Último desembolso",          "Confere se este não é o primeiro desembolso do contrato e se a funcionalidade/conclusão do anterior foram confirmadas."),
            ("Licença de operação",        "Verifica se a licença de operação está vigente para a fase atual da obra."),
            ("Licença de instalação",      "Verifica se a licença de instalação foi emitida e está regular."),
            ("Tomador adimplente",         "Confirma que o tomador/mutuário não possui pendências financeiras junto ao agente financeiro."),
            ("Agente Promotor adimplente", "Confirma que o agente promotor está em situação regular."),
            ("Placa local",                "Confirma a instalação da placa de identificação da obra no local."),
            ("Retorno parcial",            "Verifica se há retorno parcial de recursos a ser considerado neste desembolso."),
            ("Excepcionalização",          "Verifica se este desembolso depende de alguma excepcionalização aprovada."),
            ("CP alterada",                "Confirma se a contrapartida (CP) informada sofreu alteração em relação ao previsto."),
        };

        var novas = itens.Select((item, i) => new ValidacaoDesembolso
        {
            DesembolsoId = desembolsoId,
            Numero       = i + 1,
            Titulo       = item.Titulo,
            Resultado    = "Aguardando análise",
            Status       = "pendente",
            Detalhe      = item.Detalhe,
        }).ToList();

        db.ValidacoesDesembolso.AddRange(novas);
        await db.SaveChangesAsync();

        return novas;
    }

    /// <summary>Lista a checklist de validações já persistida, com os comentários de cada item.</summary>
    public async Task<List<ValidacaoDesembolso>> ObterValidacoesAsync(string desembolsoId) =>
        await db.ValidacoesDesembolso
            .Include(v => v.Comentarios)
            .Where(v => v.DesembolsoId == desembolsoId)
            .OrderBy(v => v.Numero)
            .ToListAsync();

    /// <summary>
    /// Adiciona um comentário/tratativa a um item da checklist. Um comentário
    /// "positivo" em item "inválido"/"pendente" promove o item para "válido" —
    /// mesma regra hoje aplicada no cliente (DialogDesembolsoFpd.ObterValidacoesEfetivas).
    /// </summary>
    public async Task<ComentarioValidacao?> AdicionarComentarioValidacaoAsync(
        string desembolsoId, int numeroValidacao, string tipo, string texto, string autor, string matriculaAutor)
    {
        var validacao = await db.ValidacoesDesembolso
            .FirstOrDefaultAsync(v => v.DesembolsoId == desembolsoId && v.Numero == numeroValidacao);
        if (validacao is null) return null;

        var comentario = new ComentarioValidacao
        {
            ValidacaoDesembolsoId = validacao.Id,
            Tipo           = tipo,
            Texto          = texto,
            Autor          = autor,
            MatriculaAutor = matriculaAutor,
        };
        db.ComentariosValidacao.Add(comentario);

        if (tipo == "positivo" && validacao.Status is "invalido" or "pendente")
            validacao.Status = "valido";

        await db.SaveChangesAsync();
        return comentario;
    }

    /// <summary>Edita um comentário — só o autor original pode editar o próprio comentário.</summary>
    public async Task<ResultadoEdicaoComentario> EditarComentarioValidacaoAsync(
        int comentarioId, string novoTexto, string matriculaSolicitante)
    {
        var comentario = await db.ComentariosValidacao.FindAsync(comentarioId);
        if (comentario is null) return ResultadoEdicaoComentario.NaoEncontrado;
        if (comentario.MatriculaAutor != matriculaSolicitante) return ResultadoEdicaoComentario.SemPermissao;

        comentario.Texto     = novoTexto;
        comentario.EditadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return ResultadoEdicaoComentario.Sucesso;
    }
}
