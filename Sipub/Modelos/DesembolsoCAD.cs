namespace SipubDesembolsos.Sipub.Modelos;

public class DesembolsoCAD
{
    public string Id { get; set; } = "";
    public string NumId { get; set; } = "";
    public string Contrato { get; set; } = "";
    public string Mutuario { get; set; } = "";
    public string Gigov { get; set; } = "";
    public decimal Valor { get; set; }
    public string Fase { get; set; } = "";
    public int ValidacoesOk { get; set; }
    public int ValidacoesTotal { get; set; }
    public string Status { get; set; } = ""; // pendente | aprovado | pendencia
    public DateTime PrazoFinal { get; set; }
}
