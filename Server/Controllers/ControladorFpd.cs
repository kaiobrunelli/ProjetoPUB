using Microsoft.AspNetCore.Mvc;
using SipubDesembolsos.Server.Modelos;

namespace SipubDesembolsos.Server.Controllers;

[ApiController]
[Route("api/fpd")]
[Produces("application/json")]
public class ControladorFpd : ControllerBase
{
    // GIGOVs cadastradas (mock — no real, viria do banco)
    private static readonly HashSet<string> _gigovsConhecidas =
        new(["7126", "7105", "7164", "7238", "7091", "7121"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Busca um contrato AF na base. Se existir, devolve os dados para preencher
    /// automaticamente os campos mínimos da FPD. Mock: só "0558698-45" existe.
    /// </summary>
    [HttpGet("contrato")]
    [ProducesResponseType(typeof(ContratoAfDto), 200)]
    public async Task<IActionResult> BuscarContrato([FromQuery] string af)
    {
        await Task.Delay(Random.Shared.Next(500, 900));   // latência simulada

        var chave = (af ?? "").Replace(" ", "").Replace("-", "");
        if (chave != "055869845")
            return NotFound(new { erro = $"Contrato AF '{af}' não encontrado na base." });

        return Ok(new ContratoAfDto
        {
            ContratoAf           = "0558698-45",
            Gigov                = "7121",
            AgenteFinanceiro     = "CAIXA ECONOMICA FEDERAL",
            CnpjAgenteFinanceiro = "00.360.305/0001-04",
            Tomador              = "MUNICÍPIO DE ARACAJU/SE",
            CnpjTomador          = "13.128.780/0001-00",
            AgentePromotor       = "MUNICÍPIO DE ARACAJU/SE",
            CnpjAgentePromotor   = "13.128.780/0001-00",
            Programa             = "Saneamento para Todos",
            Ve                   = 49_986_278.10m,
            CpAtual              = 5_995_200.00m,
            Desembolsado         = 10_962_739.23m,
            Integralizado        = 2_199_300.00m,
            SaldoDesembolsar     = 38_060_263.90m,
            SaldoIntegralizar    = 3_795_900.00m,
            PercObra             = 25.23m,
            SituacaoObra         = "Normal",
        });
    }

    /// <summary>
    /// Consulta prévia da FPD: roda todas as validações de negócio e retorna as
    /// etapas (ok/falha + detalhe). O front apenas exibe — a regra vive aqui.
    /// </summary>
    [HttpPost("validar")]
    [ProducesResponseType(typeof(ValidacaoFpdResultado), 200)]
    public async Task<IActionResult> Validar([FromBody] ValidacaoFpdRequest req)
    {
        // Simula latência de consulta ao sistema/banco
        await Task.Delay(Random.Shared.Next(600, 1100));

        var etapas = new List<EtapaValidacaoDto>();
        var br = new System.Globalization.CultureInfo("pt-BR");

        bool Sim(string? v) => v == "sim";

        // ── 1. Preenchimento dos campos obrigatórios ──
        var faltando = new List<string>();
        if (string.IsNullOrWhiteSpace(req.Gigov))      faltando.Add("GIGOV");
        if (string.IsNullOrWhiteSpace(req.ContratoAf)) faltando.Add("Contrato AF");
        if (string.IsNullOrWhiteSpace(req.Tomador))    faltando.Add("Tomador");
        if (req.Ve is null or <= 0)                    faltando.Add("Valor do Empréstimo");
        if (req.ParticipacaoFgts is null or <= 0)      faltando.Add("Participação FGTS");

        etapas.Add(faltando.Count == 0
            ? Ok("Campos obrigatórios preenchidos", "todos os campos exigidos presentes")
            : Falha("Campos obrigatórios preenchidos", $"faltam: {string.Join(", ", faltando)}"));

        // ── 2. GIGOV existe no sistema ──
        etapas.Add(_gigovsConhecidas.Contains(req.Gigov?.Trim() ?? "")
            ? Ok("Situação do contrato", $"GIGOV {req.Gigov?.Trim()} · AF {req.ContratoAf}")
            : Falha("Situação do contrato", $"GIGOV {req.Gigov?.Trim()} não encontrada no sistema"));

        // ── 3. Empréstimo não pode ultrapassar o % de obra construída ──
        // % global (FGTS + contrapartida) sobre o VE deve ser ≤ % de obra medida
        if (req.Ve is > 0 && req.ParticipacaoFgts is not null && req.PercObra is not null)
        {
            var percGlobal = ((req.ParticipacaoFgts.Value + (req.Contrapartida ?? 0)) / req.Ve.Value) * 100m;
            etapas.Add(percGlobal <= req.PercObra.Value + 0.01m
                ? Ok("Desembolso × execução da obra", $"{percGlobal:0.###}% global ≤ {req.PercObra:0.##}% de obra")
                : Falha("Desembolso × execução da obra", $"{percGlobal:0.###}% global excede {req.PercObra:0.##}% de obra construída"));
        }

        // ── 4. Contrapartida mínima de 5% do VE ──
        if (req.Ve is > 0)
        {
            var minimo = req.Ve.Value * 0.05m;
            var cp = req.Contrapartida ?? 0;
            etapas.Add(cp >= minimo
                ? Ok("Contrapartida mínima (5% do VE)", $"{cp.ToString("C", br)} ≥ {minimo.ToString("C", br)}")
                : Falha("Contrapartida mínima (5% do VE)", $"{cp.ToString("C", br)} abaixo do mínimo de {minimo.ToString("C", br)}"));
        }

        // ── 6. Adimplência ──
        etapas.Add(Sim(req.TomadorAdimplente) && Sim(req.PromotorAdimplente)
            ? Ok("Adimplência", "tomador e agente promotor adimplentes")
            : Falha("Adimplência", "tomador e/ou agente promotor não marcados como adimplentes"));

        // ── 7. Placa de obra ──
        etapas.Add(Sim(req.PlacaLocal)
            ? Ok("Placa de obra", "placa instalada no local")
            : Falha("Placa de obra", "placa de obra não confirmada"));

        // ── 8. Último desembolso exige Funcionalidade E Conclusão = SIM ──
        if (Sim(req.UltimoDesembolso))
        {
            var pend = new List<string>();
            if (!Sim(req.Funcionalidade)) pend.Add("Funcionalidade");
            if (!Sim(req.Conclusao))      pend.Add("Conclusão");

            etapas.Add(pend.Count == 0
                ? Ok("Requisitos do último desembolso", "Funcionalidade e Conclusão confirmadas")
                : Falha("Requisitos do último desembolso", $"último desembolso exige SIM em: {string.Join(" e ", pend)}"));
        }

        var resultado = new ValidacaoFpdResultado
        {
            Etapas   = etapas,
            Aprovado = etapas.All(e => e.Ok)
        };
        return Ok(resultado);
    }

    private static EtapaValidacaoDto Ok(string texto, string detalhe)    => new() { Texto = texto, Ok = true,  Detalhe = detalhe };
    private static EtapaValidacaoDto Falha(string texto, string detalhe) => new() { Texto = texto, Ok = false, Detalhe = detalhe };
}
