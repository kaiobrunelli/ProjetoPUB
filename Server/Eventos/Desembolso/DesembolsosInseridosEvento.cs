namespace SipubDesembolsos.Server.Eventos.Desembolso;

public record DesembolsosInseridosEvento(
    List<string> DesembolsoIds,  // 1 ou N — handler decide a mensagem
    string OperadorNome
);
