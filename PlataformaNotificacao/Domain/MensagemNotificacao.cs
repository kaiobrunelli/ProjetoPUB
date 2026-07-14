using PlataformaNotificacao.Domain.Enum;

﻿namespace PlataformaNotificacao.Domain;

public class MensagemNotificacao
{
    public int               CodigoNotificacao { get; set; }
    public string            Titulo            { get; set; } = string.Empty;
    public string            Mensagem          { get; set; } = string.Empty;
    public DateTime          CriadaEm         { get; set; } = DateTime.UtcNow;
    public TipoNotificacao   Tipo              { get; set; } = TipoNotificacao.Normal;
    public EscopoNotificacao Escopo            { get; set; } = EscopoNotificacao.Geral;
    public CodigoAplicativo? CodigoAplicativo  { get; set; }
    public bool              ExigeConfirmacao  => Tipo == TipoNotificacao.Urgente;
    public string?           Link              { get; set; }
    public DateTime          DataValidade      { get; set; }
}
