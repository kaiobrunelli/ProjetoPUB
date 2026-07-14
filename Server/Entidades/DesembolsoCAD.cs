namespace SipubDesembolsos.Server.Entidades;

public class DesembolsoCAD
{
    public string    Id          { get; set; } = "";
    public string    Municipio   { get; set; } = "";
    public string    Status      { get; set; } = "Pendente";
    public DateTime  CriadoEm   { get; set; } = DateTime.UtcNow;
    public DateTime? ValidadoEm  { get; set; }
    public string?   ValidadoPor { get; set; }
}
