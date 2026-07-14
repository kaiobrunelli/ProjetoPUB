using PlataformaNotificacao.Domain.Enum;

﻿namespace PlataformaNotificacao.Domain.DTO;

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
