using PlataformaNotificacao.Domain.Enum;

namespace PlataformaNotificacao.Domain;

public class Notificacao
{
    public int                CodigoNotificacao    { get; set; }
    public CodigoAplicativo?  CodigoAplicativo     { get; set; }
    public int                CodigoUsuarioEmissor { get; set; }
    public string             Titulo               { get; set; } = "";
    public string             Mensagem             { get; set; } = "";
    public TipoNotificacao    Tipo                 { get; set; } = TipoNotificacao.Normal;
    public DateTime           DataCriacao          { get; set; } = DateTime.UtcNow;
    public DateTime           DataValidade         { get; set; } = DateTime.UtcNow.AddDays(7);

    public List<ControleVisualizacao> Destinatarios { get; set; } = [];
}
