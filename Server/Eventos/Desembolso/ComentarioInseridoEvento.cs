namespace SipubDesembolsos.Server.Eventos.Desembolso;

public record ComentarioInseridoEvento(
    string DesembolsoId,
    string AutorNome,
    string Comentario,
    string MatriculaDestinatario  // service já sabe quem deve receber
);
