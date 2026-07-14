using PlataformaNotificacao;
using SipubDesembolsos.Server.Data;
using SipubDesembolsos.Server.Entidades;
using SipubDesembolsos.Server.Eventos.Desembolso;

namespace SipubDesembolsos.Server.Servicos;

public class ServicoDesembolso(AppDbContext db, IPublicador publicador)
{
    public async Task<DesembolsoCAD?> ObterAsync(string id) =>
        await db.Desembolsos.FindAsync(id);

    public async Task<bool> AprovarAsync(string id, string matriculaUsuario, string usuarioNome)
    {
        var desembolso = await db.Desembolsos.FindAsync(id);
        if (desembolso is null) return false;

        desembolso.Status      = "Aprovado";
        desembolso.ValidadoEm  = DateTime.UtcNow;
        desembolso.ValidadoPor = matriculaUsuario;

        await db.SaveChangesAsync();

        await publicador.PublicarAsync(new DesembolsoAprovadoEvento(id, matriculaUsuario, usuarioNome));

        return true;
    }

    public async Task<bool> RejeitarAsync(
        string id, string matriculaUsuario, string usuarioNome, string codigoCoordenacao)
    {
        var desembolso = await db.Desembolsos.FindAsync(id);
        if (desembolso is null) return false;

        desembolso.Status      = "Rejeitado";
        desembolso.ValidadoEm  = DateTime.UtcNow;
        desembolso.ValidadoPor = matriculaUsuario;

        await db.SaveChangesAsync();

        await publicador.PublicarAsync(
            new DesembolsoRejeitadoEvento(id, matriculaUsuario, usuarioNome, codigoCoordenacao));

        return true;
    }

    public async Task InserirComentarioAsync(
        string desembolsoId, string autorNome, string comentario, string matriculaDestinatario)
    {
        // lógica de persistência do comentário aqui...

        await publicador.PublicarAsync(new ComentarioInseridoEvento(
            DesembolsoId:          desembolsoId,
            AutorNome:             autorNome,
            Comentario:            comentario,
            MatriculaDestinatario: matriculaDestinatario  // service sabe quem recebe
        ));
    }

    public async Task InserirDesembolsosAsync(List<string> ids, string operadorNome)
    {
        // lógica de persistência dos desembolsos aqui...

        await publicador.PublicarAsync(new DesembolsosInseridosEvento(
            DesembolsoIds: ids,
            OperadorNome:  operadorNome
        ));
    }
}
