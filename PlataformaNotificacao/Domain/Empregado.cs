namespace PlataformaNotificacao.Domain;

public class Empregado
{
    // Matrícula é a identidade única do empregado (formato c123456). Não há mais "Id".
    public string   Matricula         { get; set; } = "";
    public string   Nome              { get; set; } = "";
    public string   Iniciais          { get; set; } = "";
    public string   Cargo             { get; set; } = "";
    public string   Cor               { get; set; } = "#005CA9";
    public string[] Modulos           { get; set; } = [];
    public string   CodigoCoordenacao { get; set; } = "";
}
