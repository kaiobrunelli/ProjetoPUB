namespace SipubDesembolsos.Server.Eventos.Desembolso;

public record DesembolsoAprovadoEvento(
    string DesembolsoId,
    string MatriculaUsuario,
    string UsuarioNome
);
