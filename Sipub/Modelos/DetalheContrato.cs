namespace SipubDesembolsos.Sipub.Modelos;

public class DetalheContrato
{
    public string Id { get; set; } = "";
    public string Gigov { get; set; } = "";
    public string NumeroContrato { get; set; } = "";
    public DadosMutuario Mutuario { get; set; } = new();
    public DadosAf Af { get; set; } = new();
    public DadosFinanciamento Financiamento { get; set; } = new();

    // ── Espelha os dados da FPD-AF que originou o desembolso ──
    public string AgenteFinanceiro { get; set; } = "";
    public string AgentePromotor   { get; set; } = "";
    public bool   PrimeiroDesembolso { get; set; }
    public string TipoDesembolso     { get; set; } = "normal";   // normal | adiantamento
}

public class DadosMutuario
{
    public string Nome { get; set; } = "";
    public string Tipo { get; set; } = "";
}

public class DadosAf
{
    public string Nome { get; set; } = "";
    public string Sigla { get; set; } = "";
}

public class DadosFinanciamento
{
    public string Programa { get; set; } = "";
    public decimal ValorInvestimento { get; set; }
    public decimal ValorFinanciamento { get; set; }
    public string NumeroDesembolso { get; set; } = "";
    public string Fase { get; set; } = "";
    public decimal PercentualObra { get; set; }
    public string Amortizacao { get; set; } = "";
    public decimal PercentualContrapartida { get; set; }

    // ── Valores da FPD-AF (equivalentes aos campos de "Valores" do formulário) ──
    public decimal Ve               { get; set; }   // VE — Valor do Empréstimo
    public decimal ParticipacaoFgts { get; set; }   // parcela FGTS deste desembolso
    public decimal Contrapartida    { get; set; }   // valor de contrapartida deste desembolso

    public decimal? PercFgts => Ve > 0 ? Math.Round(ParticipacaoFgts / Ve * 100, 3) : null;
    public decimal? PercGlobal => Ve > 0 ? Math.Round((ParticipacaoFgts + Contrapartida) / Ve * 100, 3) : null;
}
