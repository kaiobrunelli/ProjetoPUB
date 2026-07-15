namespace SipubDesembolsos.Server.Entidades;

/// <summary>
/// Linha da aba DRP — desembolso aprovado aguardando (ou já com) baixa.
/// Criado automaticamente quando um desembolso é aprovado
/// (<c>ServicoDesembolso.AprovarAsync</c>).
/// </summary>
public class RegistroDrp
{
    public int       Id              { get; set; }
    public string    DesembolsoId    { get; set; } = "";
    public string    Gigov           { get; set; } = "";
    public string    ContratoDv      { get; set; } = "";   // nº do contrato com dígito verificador
    public string    TipoDesembolso  { get; set; } = "normal";   // normal | adiantamento
    public decimal   ValorFgts       { get; set; }         // valor da parcela FGTS
    public DateTime  DataSolicitacao { get; set; }
    public string    Responsavel     { get; set; } = "";   // matrícula: c123456
    public string    Gestor          { get; set; } = "";   // matrícula: c123456
    public string?   Baixa           { get; set; }         // matrícula de quem baixou; null = aguardando
    public DateTime? BaixadoEm       { get; set; }
}
