using Microsoft.EntityFrameworkCore;
using PlataformaNotificacao;
using SipubDesembolsos.Server.Entidades;
using PlataformaNotificacao.Application.Interface;
using PlataformaNotificacao.Domain;
using PlataformaNotificacao.Domain.DTO;
using PlataformaNotificacao.Domain.Enum;

namespace SipubDesembolsos.Server.Data;

public class AppDbContext : DbContext, INotificacaoDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DesembolsoCAD>        Desembolsos           => Set<DesembolsoCAD>();
    public DbSet<Notificacao>          Notificacoes          => Set<Notificacao>();
    public DbSet<ControleVisualizacao> ControleVisualizacoes => Set<ControleVisualizacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DesembolsoCAD>(e =>
        {
            e.ToTable("DesembolsoCAD");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(20);
            e.Property(x => x.Municipio).HasMaxLength(200);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.ValidadoPor).HasMaxLength(100);
        });

        modelBuilder.Entity<Notificacao>(e =>
        {
            e.ToTable("Notificacao");
            e.HasKey(x => x.CodigoNotificacao);
            e.Property(x => x.CodigoNotificacao).UseIdentityColumn();
            e.Property(x => x.Titulo).HasMaxLength(200);
            e.Property(x => x.Mensagem).HasMaxLength(1000);
            e.Property(x => x.Tipo)
             .HasConversion(v => v.ToString(), v => ParseTipo(v))
             .HasMaxLength(20);
            e.Property(x => x.CodigoAplicativo).HasConversion<string>().HasMaxLength(50);
        });

        modelBuilder.Entity<ControleVisualizacao>(e =>
        {
            e.ToTable("ControleVisualizacao");
            e.HasKey(x => x.CodigoVisualizacao);
            e.Property(x => x.CodigoVisualizacao).UseIdentityColumn();
            e.Property(x => x.MatriculaUsuario).HasMaxLength(100);
            e.Property(x => x.Link).HasMaxLength(500);
            e.HasIndex(x => new { x.CodigoNotificacao, x.MatriculaUsuario }).IsUnique();
            e.HasOne(x => x.Notificacao)
             .WithMany(n => n.Destinatarios)
             .HasForeignKey(x => x.CodigoNotificacao)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static TipoNotificacao ParseTipo(string v) =>
        Enum.TryParse<TipoNotificacao>(v, ignoreCase: true, out var r) ? r : TipoNotificacao.Normal;
}
