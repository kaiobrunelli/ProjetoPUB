namespace PlataformaNotificacao.Domain;

public class ControleVisualizacao
{
    public int          CodigoVisualizacao { get; set; }
    public int          CodigoNotificacao  { get; set; }
    public Notificacao  Notificacao        { get; set; } = null!;
    public string       MatriculaUsuario   { get; set; } = "";
    public DateTime?    DataVisualizacao   { get; set; }
    public string?      Link               { get; set; }
}
