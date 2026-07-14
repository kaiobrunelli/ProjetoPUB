namespace SipubDesembolsos.Server.Eventos.Desembolso;

public record DesembolsoRejeitadoEvento(
    string DesembolsoId,
    string MatriculaUsuario,
    string UsuarioNome,
    string CoordenaçãoResponsavel
);
