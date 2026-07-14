namespace SipubDesembolsos.Client.Modelos;

public class Comentario
{
    public string Tipo { get; set; } = "informativo"; // positivo | informativo | negativo
    public string Texto { get; set; } = "";
    public string Autor { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public static class TiposComentario
{
    public static readonly Dictionary<string, ConfigTipo> Configs = new()
    {
        ["positivo"]    = new("#E0F2E7", "#1A7A4A", "#1A7A4A"),
        ["informativo"] = new("#E5F1FC", "#00437A", "#005CA9"),
        ["negativo"]    = new("#FCE6E3", "#9A2A1F", "#C0392B"),
    };

    public record ConfigTipo(string CorFundo, string CorTexto, string CorPonto);
}
