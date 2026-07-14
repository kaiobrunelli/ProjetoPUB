namespace SipubDesembolsos.Client.Modelos;

/// <summary>
/// Dados de um contrato AF retornados pela base (consulta ao clicar em Buscar).
/// Preenche automaticamente os campos mínimos da FPD quando o contrato existe.
/// </summary>
public class ContratoAfDto
{
    public string   ContratoAf           { get; set; } = "";
    public string   Gigov                { get; set; } = "";

    public string   AgenteFinanceiro     { get; set; } = "";
    public string   CnpjAgenteFinanceiro { get; set; } = "";
    public string   Tomador              { get; set; } = "";
    public string   CnpjTomador          { get; set; } = "";
    public string   AgentePromotor       { get; set; } = "";
    public string   CnpjAgentePromotor   { get; set; } = "";
    public string   Programa             { get; set; } = "";

    public decimal  Ve                   { get; set; }   // valor do empréstimo
    public decimal  CpAtual              { get; set; }
    public decimal  Desembolsado         { get; set; }
    public decimal  Integralizado        { get; set; }
    public decimal  SaldoDesembolsar     { get; set; }
    public decimal  SaldoIntegralizar    { get; set; }

    public decimal  PercObra             { get; set; }
    public string   SituacaoObra         { get; set; } = "Normal";
}
