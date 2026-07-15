namespace SipubDesembolsos.Server.Entidades;

/// <summary>
/// Ficha de Previsão de Desembolso (FPD-AF) enviada pelo analista — grava a
/// solicitação que dá origem a um <see cref="DesembolsoCAD"/> na lista de análise.
/// </summary>
public class FichaPrevisaoDesembolso
{
    public int     Id           { get; set; }
    public string? DesembolsoId { get; set; }   // FK do DesembolsoCAD gerado a partir desta FPD

    // ── Cabeçalho ──
    public string    Solicitante        { get; set; } = "";   // matrícula
    public string    Gigov              { get; set; } = "";
    public string    Gestor             { get; set; } = "";   // matrícula
    public string    IdFpd              { get; set; } = "";
    public string    NumeroFpd          { get; set; } = "";
    public string    ContratoAf         { get; set; } = "";
    public DateTime? DataSolicitado     { get; set; }
    public bool      PrimeiroDesembolso { get; set; }

    // ── Agentes ──
    public string AgenteFinanceiro     { get; set; } = "";
    public string CnpjAgenteFinanceiro { get; set; } = "";
    public string Tomador              { get; set; } = "";
    public string CnpjTomador          { get; set; } = "";
    public string AgenteTecnico        { get; set; } = "";
    public string CnpjAgenteTecnico    { get; set; } = "";
    public string AgentePromotor       { get; set; } = "";
    public string CnpjAgentePromotor   { get; set; } = "";
    public string Programa             { get; set; } = "";

    // ── Sim/Não (sim | nao | nsa | null) ──
    public string? UltimoDesembolso   { get; set; }
    public string? Funcionalidade     { get; set; }
    public string? Conclusao          { get; set; }
    public string? TomadorAdimplente  { get; set; }
    public string? PromotorAdimplente { get; set; }
    public string? RetornoParcial     { get; set; }
    public string? PlacaLocal         { get; set; }
    public string? LicencaInstalacao  { get; set; }
    public string? LicencaOperacao    { get; set; }
    public string? Excepcionalizacao  { get; set; }
    public string? CpAlterada         { get; set; }

    // ── Obra / datas ──
    public DateTime? DataEmissaoEng      { get; set; }
    public string    SituacaoObra        { get; set; } = "";
    public DateTime? DataEmissaoSocioAmb { get; set; }
    public bool      Nsa                 { get; set; }
    public decimal?  PercObra            { get; set; }
    public string    TipoDesembolso      { get; set; } = "";   // normal | adiantamento
    public string    ObsAf               { get; set; } = "";
    public string    InssObs             { get; set; } = "";

    // ── Financeiro ──
    public decimal? SolicitadoVi      { get; set; }
    public decimal? GlosadoVi         { get; set; }
    public decimal? AceitoVi          { get; set; }
    public decimal? ParticipacaoFgts  { get; set; }
    public decimal? Contrapartida     { get; set; }
    public decimal? Ve                { get; set; }
    public decimal? CpAtual           { get; set; }
    public decimal? Desembolsado      { get; set; }
    public decimal? Integralizado     { get; set; }
    public decimal? ParcelaFgts       { get; set; }
    public decimal? Integralizar      { get; set; }
    public decimal? SaldoDesembolsar  { get; set; }
    public decimal? SaldoIntegralizar { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
