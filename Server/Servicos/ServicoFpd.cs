using PlataformaNotificacao.Application;
using SipubDesembolsos.Server.Data;
using SipubDesembolsos.Server.Entidades;
using SipubDesembolsos.Server.Modelos;

namespace SipubDesembolsos.Server.Servicos;

public class ServicoFpd(AppDbContext db, ServicoDesembolso servicoDesembolso, EmpregadoService empregados)
{
    /// <summary>
    /// Salva a FPD-AF no banco e cria o desembolso correspondente na lista de
    /// análise, disparando o mesmo evento/notificação de
    /// <see cref="ServicoDesembolso.InserirDesembolsosAsync"/>.
    /// </summary>
    public async Task<string> SolicitarAsync(SolicitacaoFpdRequest req)
    {
        var desembolsoId = $"DSB-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        db.Desembolsos.Add(new DesembolsoCAD
        {
            Id        = desembolsoId,
            Municipio = req.Tomador,
            Status    = "Pendente",
        });

        db.FichasPrevisaoDesembolso.Add(new FichaPrevisaoDesembolso
        {
            DesembolsoId       = desembolsoId,
            Solicitante        = req.Solicitante,
            Gigov              = req.Gigov,
            Gestor             = req.Gestor,
            IdFpd              = req.IdFpd,
            NumeroFpd          = req.NumeroFpd,
            ContratoAf         = req.ContratoAf,
            DataSolicitado     = req.DataSolicitado,
            PrimeiroDesembolso = req.PrimeiroDesembolso,

            AgenteFinanceiro     = req.AgenteFinanceiro,
            CnpjAgenteFinanceiro = req.CnpjAgenteFinanceiro,
            Tomador              = req.Tomador,
            CnpjTomador          = req.CnpjTomador,
            AgenteTecnico        = req.AgenteTecnico,
            CnpjAgenteTecnico    = req.CnpjAgenteTecnico,
            AgentePromotor       = req.AgentePromotor,
            CnpjAgentePromotor   = req.CnpjAgentePromotor,
            Programa             = req.Programa,

            UltimoDesembolso   = req.UltimoDesembolso,
            Funcionalidade     = req.Funcionalidade,
            Conclusao          = req.Conclusao,
            TomadorAdimplente  = req.TomadorAdimplente,
            PromotorAdimplente = req.PromotorAdimplente,
            RetornoParcial     = req.RetornoParcial,
            PlacaLocal         = req.PlacaLocal,
            LicencaInstalacao  = req.LicencaInstalacao,
            LicencaOperacao    = req.LicencaOperacao,
            Excepcionalizacao  = req.Excepcionalizacao,
            CpAlterada         = req.CpAlterada,

            DataEmissaoEng      = req.DataEmissaoEng,
            SituacaoObra        = req.SituacaoObra,
            DataEmissaoSocioAmb = req.DataEmissaoSocioAmb,
            Nsa                 = req.Nsa,
            PercObra            = req.PercObra,
            TipoDesembolso      = req.TipoDesembolso,
            ObsAf               = req.ObsAf,
            InssObs             = req.InssObs,

            SolicitadoVi      = req.SolicitadoVi,
            GlosadoVi         = req.GlosadoVi,
            AceitoVi          = req.AceitoVi,
            ParticipacaoFgts  = req.ParticipacaoFgts,
            Contrapartida     = req.Contrapartida,
            Ve                = req.Ve,
            CpAtual           = req.CpAtual,
            Desembolsado      = req.Desembolsado,
            Integralizado     = req.Integralizado,
            ParcelaFgts       = req.ParcelaFgts,
            Integralizar      = req.Integralizar,
            SaldoDesembolsar  = req.SaldoDesembolsar,
            SaldoIntegralizar = req.SaldoIntegralizar,
        });

        await db.SaveChangesAsync();

        var nomeSolicitante = empregados.ObterPorMatricula(req.Solicitante)?.Nome ?? req.Solicitante;
        await servicoDesembolso.InserirDesembolsosAsync([desembolsoId], nomeSolicitante);

        return desembolsoId;
    }
}
