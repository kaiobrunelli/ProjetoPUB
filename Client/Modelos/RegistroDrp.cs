namespace SipubDesembolsos.Client.Modelos;

/// <summary>
/// Registro da tabela de controle de baixa (DRP) — Demonstrativo de Recursos
/// Pendentes. Cada linha é um desembolso aguardando (ou já com) baixa.
/// </summary>
public class RegistroDrp
{
    public int      Id            { get; set; }
    public string   Gigov         { get; set; } = "";
    public string   ContratoDv    { get; set; } = "";   // nº do contrato com dígito verificador
    public string   TipoDesembolso{ get; set; } = "";   // "normal" | "adiantamento"
    public decimal  ValorFgts     { get; set; }         // valor da parcela FGTS
    public DateTime DataSolicitacao { get; set; }
    public string   Responsavel   { get; set; } = "";   // matrícula: c123456
    public string   Gestor        { get; set; } = "";   // matrícula: c123456
    public string?  Baixa         { get; set; }         // matrícula de quem baixou; null = aguardando

    public bool Baixado => !string.IsNullOrEmpty(Baixa);
}
