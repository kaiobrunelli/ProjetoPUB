namespace SipubDesembolsos.Server.Entidades;

/// <summary>
/// Comentário/tratativa lançado em um item da checklist de validação. Só o
/// autor (<see cref="MatriculaAutor"/>) pode editar o próprio comentário.
/// </summary>
public class ComentarioValidacao
{
    public int       Id                    { get; set; }
    public int       ValidacaoDesembolsoId { get; set; }

    public string    Tipo           { get; set; } = "informativo";   // positivo | informativo | negativo
    public string    Texto          { get; set; } = "";
    public string    Autor          { get; set; } = "";
    public string    MatriculaAutor { get; set; } = "";
    public DateTime  Timestamp      { get; set; } = DateTime.UtcNow;
    public DateTime? EditadoEm      { get; set; }
}
