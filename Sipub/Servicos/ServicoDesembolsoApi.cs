using System.Net.Http.Json;
using SipubDesembolsos.Sipub.Modelos;

namespace SipubDesembolsos.Sipub.Servicos;

/// <summary>
/// Fachada única para todas as chamadas HTTP de Desembolso e FPD-AF.
///
/// Métodos SEM comentário de bloqueio chamam endpoints que JÁ EXISTEM no
/// servidor (testados nesta sessão). Métodos comentados descrevem endpoints
/// que este ambiente de teste ainda não implementa — a assinatura, a rota, o
/// verbo HTTP e o formato do corpo já estão prontos; hoje quem cobre essa
/// lacuna é o ServicoMock (dados em memória, no cliente). Basta descomentar
/// e criar o Controller correspondente no Server quando for a hora.
/// </summary>
public class ServicoDesembolsoApi(HttpClient http)
{
    // ────────────────────────────────────────────────────────────────────
    // DESEMBOLSO — controle geral (listagem, detalhe, decisão, vínculo)
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lista todos os desembolsos — é a consulta que a página de análise faria
    /// AO ABRIR, em vez de <see cref="ServicoMock.ObterDesembolsos"/>. Endpoint real
    /// (api/desembolso), mas ainda não plugado nas páginas: o DesembolsoCAD do
    /// servidor hoje só tem Id/Municipio/Status — bem mais enxuto que o modelo
    /// rico usado pelas telas (Contrato, Mutuario, Valor, Fase...), então a UI
    /// continua no ServicoMock até o schema do banco crescer.
    /// </summary>
    public async Task<List<DesembolsoCAD>> ObterTodosAsync()
    {
        var lista = await http.GetFromJsonAsync<List<DesembolsoCAD>>("api/desembolso");
        return lista ?? [];
    }

    /// <summary>Consulta um desembolso específico por Id. Endpoint real (api/desembolso/{id}).</summary>
    public async Task<DesembolsoCAD?> ObterPorIdAsync(string id) =>
        await http.GetFromJsonAsync<DesembolsoCAD>($"api/desembolso/{id}");

    /// <summary>
    /// Detalhe completo do contrato (AF, mutuário, agentes, valores da FPD que
    /// originou o desembolso) — hoje vem de <see cref="ServicoMock.ObterDetalheContrato"/>.
    /// </summary>
    // GET api/desembolso/{id}/detalhe
    // public async Task<DetalheContrato?> ObterDetalheAsync(string id) =>
    //     await http.GetFromJsonAsync<DetalheContrato>($"api/desembolso/{id}/detalhe");

    /// <summary>Aprova um desembolso. Endpoint real (api/desembolso/{id}/aprovar).</summary>
    public async Task<bool> AprovarAsync(string id, string matriculaUsuario, string usuarioNome)
    {
        var req = new { MatriculaUsuario = matriculaUsuario, UsuarioNome = usuarioNome };
        var resposta = await http.PutAsJsonAsync($"api/desembolso/{id}/aprovar", req);
        return resposta.IsSuccessStatusCode;
    }

    /// <summary>Rejeita um desembolso, exigindo o código da coordenação responsável. Endpoint real.</summary>
    public async Task<bool> RejeitarAsync(string id, string matriculaUsuario, string usuarioNome, string codigoCoordenacao)
    {
        var req = new { MatriculaUsuario = matriculaUsuario, UsuarioNome = usuarioNome, CodigoCoordenacao = codigoCoordenacao };
        var resposta = await http.PutAsJsonAsync($"api/desembolso/{id}/rejeitar", req);
        return resposta.IsSuccessStatusCode;
    }

    /// <summary>
    /// Vincula (ou troca) o analista responsável pelo desembolso — hoje é só
    /// estado local em memória na página (Dictionary&lt;id, Funcionario&gt;).
    /// </summary>
    // PUT api/desembolso/{id}/vincular   { MatriculaAnalista: string }
    // public async Task<bool> VincularAnalistaAsync(string id, string matriculaAnalista)
    // {
    //     var req = new { MatriculaAnalista = matriculaAnalista };
    //     var resposta = await http.PutAsJsonAsync($"api/desembolso/{id}/vincular", req);
    //     return resposta.IsSuccessStatusCode;
    // }

    /// <summary>Remove o vínculo do analista responsável.</summary>
    // DELETE api/desembolso/{id}/vincular
    // public async Task<bool> RemoverVinculoAsync(string id)
    // {
    //     var resposta = await http.DeleteAsync($"api/desembolso/{id}/vincular");
    //     return resposta.IsSuccessStatusCode;
    // }

    // ────────────────────────────────────────────────────────────────────
    // VALIDAÇÕES DO DESEMBOLSO — checklist (mesmos itens da FPD)
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Roda a validação completa do desembolso (compara os dados da FPD com a
    /// macro de referência) e persiste a checklist resultante. Endpoint real.
    /// </summary>
    public async Task<List<Validacao>> ValidarDesembolsoAsync(string id)
    {
        var resposta = await http.PostAsync($"api/desembolso/{id}/validar", null);
        resposta.EnsureSuccessStatusCode();
        return await resposta.Content.ReadFromJsonAsync<List<Validacao>>() ?? [];
    }

    /// <summary>
    /// Lista a checklist de validações do desembolso, já com status e
    /// comentários persistidos. Endpoint real (api/desembolso/{id}/validacoes).
    /// </summary>
    public async Task<List<Validacao>> ObterValidacoesAsync(string id)
    {
        var lista = await http.GetFromJsonAsync<List<Validacao>>($"api/desembolso/{id}/validacoes");
        return lista ?? [];
    }

    /// <summary>
    /// Adiciona um comentário/tratativa a um item específico da checklist. Um
    /// comentário "positivo" em item "invalido"/"pendente" promove o status
    /// para "valido" (mesma regra hoje aplicada no cliente, em
    /// DialogDesembolsoFpd.ObterValidacoesEfetivas). Endpoint real.
    /// </summary>
    public async Task<Comentario?> AdicionarComentarioValidacaoAsync(string id, int numeroValidacao, Comentario comentario)
    {
        var resposta = await http.PostAsJsonAsync($"api/desembolso/{id}/validacoes/{numeroValidacao}/comentarios", comentario);
        return resposta.IsSuccessStatusCode ? await resposta.Content.ReadFromJsonAsync<Comentario>() : null;
    }

    /// <summary>Edita um comentário — só o autor original pode editar o próprio comentário. Endpoint real.</summary>
    public async Task<bool> EditarComentarioValidacaoAsync(string id, int numeroValidacao, int comentarioId, string novoTexto, string matriculaSolicitante)
    {
        var req = new { Texto = novoTexto, MatriculaSolicitante = matriculaSolicitante };
        var resposta = await http.PutAsJsonAsync($"api/desembolso/{id}/validacoes/{numeroValidacao}/comentarios/{comentarioId}", req);
        return resposta.IsSuccessStatusCode;
    }

    // ────────────────────────────────────────────────────────────────────
    // FPD-AF — Ficha de Previsão de Desembolso
    // ────────────────────────────────────────────────────────────────────

    /// <summary>Busca um contrato AF já cadastrado, para pré-preencher a FPD. Endpoint real.</summary>
    public async Task<ContratoAfDto?> BuscarContratoAsync(string contratoAf)
    {
        var resposta = await http.GetAsync($"api/fpd/contrato?af={Uri.EscapeDataString(contratoAf)}");
        return resposta.IsSuccessStatusCode
            ? await resposta.Content.ReadFromJsonAsync<ContratoAfDto>()
            : null;
    }

    /// <summary>Roda a consulta prévia de validação da FPD (etapas ok/falha). Endpoint real.</summary>
    public async Task<ValidacaoFpdResultado> ValidarFpdAsync(ValidacaoFpdRequest req)
    {
        var resposta = await http.PostAsJsonAsync("api/fpd/validar", req);
        resposta.EnsureSuccessStatusCode();
        return await resposta.Content.ReadFromJsonAsync<ValidacaoFpdResultado>() ?? new();
    }

    /// <summary>
    /// Envia a FPD para solicitar o desembolso de fato — grava no banco e
    /// dispara o fluxo de aprovação/notificação. Os dialogs de preenchimento
    /// (PainelPreencherFpd, DialogPreencherFpd, PainelPreencherFpdEtapas) já
    /// chamam esta rota diretamente via HttpClient; este método fica disponível
    /// para quem preferir passar pela fachada. Endpoint real.
    /// </summary>
    public async Task<string?> SolicitarFpdAsync(SolicitacaoFpdRequest req)
    {
        var resposta = await http.PostAsJsonAsync("api/fpd/solicitar", req);
        if (!resposta.IsSuccessStatusCode) return null;

        var corpo = await resposta.Content.ReadFromJsonAsync<RespostaSolicitacaoFpd>();
        return corpo?.DesembolsoId;
    }

    private record RespostaSolicitacaoFpd(bool Sucesso, string DesembolsoId);

    // ────────────────────────────────────────────────────────────────────
    // DRP — controle de baixa
    // ────────────────────────────────────────────────────────────────────

    /// <summary>Lista os registros da aba DRP (controle de baixa). Endpoint real (api/drp).</summary>
    public async Task<List<RegistroDrp>> ObterRegistrosDrpAsync()
    {
        var lista = await http.GetFromJsonAsync<List<RegistroDrp>>("api/drp");
        return lista ?? [];
    }

    /// <summary>Registra a baixa (em lote ou individual) dos registros informados. Endpoint real.</summary>
    public async Task<bool> BaixarDrpAsync(List<int> ids, string matriculaUsuario, string senha)
    {
        var req = new { Ids = ids, MatriculaUsuario = matriculaUsuario, Senha = senha };
        var resposta = await http.PutAsJsonAsync("api/drp/baixar", req);
        return resposta.IsSuccessStatusCode;
    }
}
