namespace PlataformaNotificacao.Application.Interface;

public interface IEmpregadoService
{
    List<string> ObterMatriculasTodos();
    List<string> ObterMatriculasPorModulo(string modulo);
    List<string> ObterMatriculasPorCoordenacao(string codigoCoordenacao);
}
