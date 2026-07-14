using PlataformaNotificacao.Domain;
using PlataformaNotificacao.Domain.Enum;

namespace PlataformaNotificacao;

public class NotificacaoEventArgs : EventArgs
{
    public string              ChaveConexao  { get; set; } = "ReceberNotificacao";
    public EscopoNotificacao   Escopo        { get; set; } = EscopoNotificacao.Geral;
    public List<string>        Destinatarios { get; set; } = [];   // matrículas
    public MensagemNotificacao Mensagem      { get; set; } = new();
}
