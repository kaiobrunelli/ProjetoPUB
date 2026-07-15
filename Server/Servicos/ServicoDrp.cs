using Microsoft.EntityFrameworkCore;
using SipubDesembolsos.Server.Data;
using SipubDesembolsos.Server.Entidades;

namespace SipubDesembolsos.Server.Servicos;

public class ServicoDrp(AppDbContext db)
{
    /// <summary>Lista os registros da aba DRP (controle de baixa).</summary>
    public async Task<List<RegistroDrp>> ObterTodosAsync() =>
        await db.RegistrosDrp.OrderByDescending(r => r.DataSolicitacao).ToListAsync();

    /// <summary>
    /// Registra a baixa dos registros selecionados. TODO: validar a senha do
    /// usuário contra o sistema de autenticação real — por enquanto qualquer
    /// senha não vazia é aceita, a exemplo do mock atual no cliente (DialogSenhaBaixa).
    /// </summary>
    public async Task<int> BaixarAsync(List<int> ids, string matriculaUsuario, string senha)
    {
        var registros = await db.RegistrosDrp
            .Where(r => ids.Contains(r.Id) && r.Baixa == null)
            .ToListAsync();

        foreach (var r in registros)
        {
            r.Baixa     = matriculaUsuario;
            r.BaixadoEm = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return registros.Count;
    }
}
