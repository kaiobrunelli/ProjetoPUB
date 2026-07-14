using PlataformaNotificacao.Domain;

using Microsoft.EntityFrameworkCore;

namespace PlataformaNotificacao;

public interface INotificacaoDbContext
{
    DbSet<Notificacao>          Notificacoes          { get; }
    DbSet<ControleVisualizacao> ControleVisualizacoes { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
