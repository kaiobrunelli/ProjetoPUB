namespace SipubDesembolsos.Sipub.Modelos;

public class Validacao
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public string Titulo { get; set; } = "";
    public string Resultado { get; set; } = "";
    public string Status { get; set; } = ""; // valido | invalido
    public string Icone { get; set; } = "";
    public string Detalhe { get; set; } = "";
    public List<SubValidacao> SubItens { get; set; } = new();
    public Comentario? ComentarioPreenchido { get; set; }
}

public class SubValidacao
{
    public string Numero { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Status { get; set; } = "";
    public string Detalhe { get; set; } = "";
}
