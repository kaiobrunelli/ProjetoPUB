namespace SipubDesembolsos.Server.Modelos;

/// <summary>
/// Dados da FPD enviados do front para a API validar. Booleanos: o que não vier
/// marcado é false ("não") — comportamento natural de checkbox.
/// </summary>
public class ValidacaoFpdRequest
{
    public string    Gigov            { get; set; } = "";
    public string    ContratoAf       { get; set; } = "";
    public string    Tomador          { get; set; } = "";

    // Financeiro
    public decimal?  Ve               { get; set; }   // valor do empréstimo
    public decimal?  ParticipacaoFgts { get; set; }   // parcela FGTS
    public decimal?  Contrapartida    { get; set; }
    public decimal?  PercObra         { get; set; }

    // Sim/Não (sim | nao | nsa | null)
    public string?   UltimoDesembolso { get; set; }
    public string?   Funcionalidade   { get; set; }
    public string?   Conclusao        { get; set; }
    public string?   PlacaLocal       { get; set; }
    public string?   TomadorAdimplente  { get; set; }
    public string?   PromotorAdimplente { get; set; }
}

/// <summary>Uma linha do resultado da validação (aparece no painel da consulta prévia).</summary>
public class EtapaValidacaoDto
{
    public string Texto   { get; set; } = "";
    public bool   Ok      { get; set; }
    public string Detalhe { get; set; } = "";
}

/// <summary>Resultado consolidado da validação da FPD.</summary>
public class ValidacaoFpdResultado
{
    public bool                    Aprovado { get; set; }
    public List<EtapaValidacaoDto> Etapas   { get; set; } = [];
}
