namespace PlataformaNotificacao.UI.Servicos;

public class ServicoUsuario
{
    // Identidade = matrícula (c123456), a MESMA que vai no ?matriculaUsuario= do
    // SignalR e do REST, e que o EmpregadoService/banco usam. Não existe "id" separado.
    private string _matricula = "c151896";

    public string Matricula => _matricula;

    public string Nome => _matricula switch
    {
        "c151896" => "Kaio Brunelli",
        "c123456" => "Bruno Costa",
        "c123457" => "Carla Mendes",
        "c123000" => "Diego Santos",
        "c123001" => "Elena Ferreira",
        _         => _matricula
    };

    public string Iniciais => _matricula switch
    {
        "c151896" => "KB",
        "c123456" => "BC",
        "c123457" => "CM",
        "c123000" => "DS",
        "c123001" => "EF",
        _         => "?"
    };

    public string Cargo => _matricula switch
    {
        "c151896" => "Analista Sênior",
        "c123456" => "Gestor",
        "c123457" => "Analista Júnior",
        "c123000" => "Coordenador",
        "c123001" => "Diretora Financeira",
        _         => ""
    };

    public string Cor => _matricula switch
    {
        "c151896" => "#005CA9",
        "c123456" => "#065F46",
        "c123457" => "#7C3AED",
        "c123000" => "#B45309",
        "c123001" => "#BE185D",
        _         => "#6B7280"
    };

    public event Func<Task>? AoMudar;

    public async Task MudarParaAsync(string matricula)
    {
        _matricula = matricula;
        if (AoMudar is not null)
            await AoMudar.Invoke();
    }
}
