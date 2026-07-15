namespace SipubDesembolsos.Server.Entidades;

/// <summary>
/// Item da checklist de validação de um desembolso (ex.: "Tomador adimplente",
/// "Licença de operação"). Gerada por <c>ServicoDesembolso.ValidarAsync</c> ao
/// comparar os dados da FPD com a macro de referência.
/// </summary>
public class ValidacaoDesembolso
{
    public int    Id           { get; set; }
    public string DesembolsoId { get; set; } = "";
    public int    Numero       { get; set; }
    public string Titulo       { get; set; } = "";
    public string Resultado    { get; set; } = "";
    public string Status       { get; set; } = "pendente";   // valido | invalido | pendente
    public string Icone        { get; set; } = "bi-check2-square";
    public string Detalhe      { get; set; } = "";

    public List<ComentarioValidacao> Comentarios { get; set; } = [];
}
