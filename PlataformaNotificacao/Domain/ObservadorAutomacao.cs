namespace PlataformaNotificacao.Domain;

public class ObservadorAutomacao
{
    public string ChaveConexao          { get; set; } = string.Empty;
    public string NomeProcesso          { get; set; } = string.Empty;
    public int    NumeroFaseAtual       { get; set; }
    public int    TotalAProcessar       { get; set; }
    public int    TotalProcessado       { get; set; }
    public int    PercentualProcessado  { get; set; }
    public string Mensagem              { get; set; } = string.Empty;
    public string Severity              { get; set; } = string.Empty;
    public bool   ExecutandoSP          { get; set; } = false;
}

