namespace PlataformaOperacional.Model.Plataforma
{
    /// <summary>
    /// Notificação retornada pela API REST ao carregar o histórico do banco
    /// (ex.: GET api/notificacao/minhas). Difere de MensagemNotificacao por
    /// carregar o estado de leitura (DataVisualizacao) persistido por usuário.
    /// </summary>
    public class NotificacaoDto
    {
        public int               CodigoNotificacao { get; set; }
        public CodigoAplicativo? CodigoAplicativo  { get; set; }
        public string            Titulo            { get; set; } = "";
        public string            Mensagem          { get; set; } = "";
        public TipoNotificacao   Tipo              { get; set; } = TipoNotificacao.Normal;
        public bool              ExigeConfirmacao  => Tipo == TipoNotificacao.Urgente;
        public DateTime          DataCriacao       { get; set; }
        public DateTime          DataValidade      { get; set; }
        public DateTime?         DataVisualizacao  { get; set; }
        public string?           Link              { get; set; }
    }
}
