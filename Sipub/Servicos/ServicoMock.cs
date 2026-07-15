using SipubDesembolsos.Sipub.Modelos;
using System.Globalization;

namespace SipubDesembolsos.Sipub.Servicos;

public static class ServicoMock
{
    private static readonly CultureInfo _culturaBR = new("pt-BR");

    // ──────────────────────────────────────────────────────────────────────────
    // FUNCIONÁRIOS
    // ──────────────────────────────────────────────────────────────────────────
    public static List<Funcionario> ObterFuncionarios() =>
    [
        new() { Id = 1, Nome = "Karina Souto",   Iniciais = "KS", Cor = "#005CA9" },
        new() { Id = 2, Nome = "Rafael Mendes",  Iniciais = "RM", Cor = "#6D28D9" },
        new() { Id = 3, Nome = "Ana Paula Lima", Iniciais = "AP", Cor = "#0E7490" },
        new() { Id = 4, Nome = "Bruno Costa",    Iniciais = "BC", Cor = "#065F46" },
        new() { Id = 5, Nome = "Tânia Ferreira", Iniciais = "TF", Cor = "#92400E" },
    ];

    // ──────────────────────────────────────────────────────────────────────────
    // DRP — controle de baixa (simula consulta ao banco)
    // ──────────────────────────────────────────────────────────────────────────
    public static List<RegistroDrp> ObterRegistrosDrp() =>
    [
        new() { Id = 1, Gigov = "7126", ContratoDv = "0512.345/2022-01-8", TipoDesembolso = "normal",
                ValorFgts = 1_450_000m, DataSolicitacao = new DateTime(2026, 5, 12),
                Responsavel = "c123456", Gestor = "c102944", Baixa = null },
        new() { Id = 2, Gigov = "7105", ContratoDv = "0512.346/2022-02-6", TipoDesembolso = "adiantamento",
                ValorFgts = 870_000m, DataSolicitacao = new DateTime(2026, 5, 20),
                Responsavel = "c134872", Gestor = "c102944", Baixa = "c145097" },
        new() { Id = 3, Gigov = "7164", ContratoDv = "0512.349/2022-05-1", TipoDesembolso = "normal",
                ValorFgts = 1_230_000m, DataSolicitacao = new DateTime(2026, 5, 25),
                Responsavel = "c123456", Gestor = "c110233", Baixa = null },
    ];

    // ──────────────────────────────────────────────────────────────────────────
    // DESEMBOLSOS
    // ──────────────────────────────────────────────────────────────────────────
    public static List<DesembolsoCAD> ObterDesembolsos() =>
    [
        new()
        {
            Id = "DSB-001", NumId = "0012483", Contrato = "0512.345/2022-01",
            Mutuario = "Prefeitura Municipal de Caruaru", Gigov = "GIGOV03",
            Valor = 1_450_000m, Fase = "2ª Medição",
            ValidacoesOk = 7, ValidacoesTotal = 10,
            Status = "pendencia",
            PrazoFinal = new DateTime(2025, 5, 16)
        },
        new()
        {
            Id = "DSB-002", NumId = "0012484", Contrato = "0512.346/2022-02",
            Mutuario = "Município de Campina Grande", Gigov = "GIGOV01",
            Valor = 870_000m, Fase = "1ª Medição",
            ValidacoesOk = 5, ValidacoesTotal = 10,
            Status = "pendente",
            PrazoFinal = new DateTime(2025, 5, 20)
        },
        new()
        {
            Id = "DSB-003", NumId = "0012485", Contrato = "0512.347/2022-03",
            Mutuario = "Prefeitura de Mossoró", Gigov = "GIGOV02",
            Valor = 2_100_000m, Fase = "3ª Medição",
            ValidacoesOk = 10, ValidacoesTotal = 10,
            Status = "aprovado",
            PrazoFinal = new DateTime(2025, 5, 12)
        },
        new()
        {
            Id = "DSB-004", NumId = "0012486", Contrato = "0512.348/2022-04",
            Mutuario = "Município de Feira de Santana", Gigov = "GIGOV05",
            Valor = 560_000m, Fase = "Final",
            ValidacoesOk = 3, ValidacoesTotal = 10,
            Status = "pendencia",
            PrazoFinal = new DateTime(2025, 5, 8)
        },
        new()
        {
            Id = "DSB-005", NumId = "0012487", Contrato = "0512.349/2022-05",
            Mutuario = "Prefeitura de Aracaju", Gigov = "GIGOV07",
            Valor = 1_230_000m, Fase = "1ª Medição",
            ValidacoesOk = 8, ValidacoesTotal = 10,
            Status = "pendente",
            PrazoFinal = new DateTime(2025, 5, 25)
        },
        new()
        {
            Id = "DSB-006", NumId = "0012488", Contrato = "0512.350/2022-06",
            Mutuario = "Município de Maceió", Gigov = "GIGOV04",
            Valor = 3_400_000m, Fase = "2ª Medição",
            ValidacoesOk = 10, ValidacoesTotal = 10,
            Status = "aprovado",
            PrazoFinal = new DateTime(2025, 5, 30)
        },
    ];

    // ──────────────────────────────────────────────────────────────────────────
    // VALIDAÇÕES — dados distintos por desembolso
    // ──────────────────────────────────────────────────────────────────────────
    public static List<Validacao> ObterValidacoes(string idDesembolso = "DSB-001") => idDesembolso switch
    {
        "DSB-002" => ObterValidacoesDSB002(),
        "DSB-003" => ObterValidacoesTodosValidos("Campina Grande"),
        "DSB-004" => ObterValidacoesDSB004(),
        "DSB-005" => ObterValidacoesDSB005(),
        "DSB-006" => ObterValidacoesTodosValidos("Maceió"),
        _         => ObterValidacoesDSB001(),
    };

    // ──────────────────────────────────────────────────────────────────────────
    // VALIDAÇÕES DO DESEMBOLSO — os MESMOS itens da árvore "Verificações" da
    // FPD-AF (ver PainelPreencherFpd), agora como checklist de análise do
    // desembolso: cada item é aprovado individualmente, com comentário.
    // ──────────────────────────────────────────────────────────────────────────
    public static List<Validacao> ObterValidacoesFpd(string idDesembolso)
    {
        var itens = new (string Titulo, string Detalhe)[]
        {
            ("Último desembolso",              "Confere se este não é o primeiro desembolso do contrato e se a funcionalidade/conclusão do anterior foram confirmadas."),
            ("Licença de operação",             "Verifica se a licença de operação está vigente para a fase atual da obra."),
            ("Licença de instalação",           "Verifica se a licença de instalação foi emitida e está regular."),
            ("Tomador adimplente",              "Confirma que o tomador/mutuário não possui pendências financeiras junto ao agente financeiro."),
            ("Agente Promotor adimplente",      "Confirma que o agente promotor está em situação regular."),
            ("Placa local",                     "Confirma a instalação da placa de identificação da obra no local."),
            ("Retorno parcial",                 "Verifica se há retorno parcial de recursos a ser considerado neste desembolso."),
            ("Excepcionalização",               "Verifica se este desembolso depende de alguma excepcionalização aprovada."),
            ("CP alterada",                     "Confirma se a contrapartida (CP) informada sofreu alteração em relação ao previsto."),
        };

        var seed = idDesembolso.GetHashCode();
        var rnd = new Random(Math.Abs(seed));

        return itens.Select((item, i) =>
        {
            // Mantém alguma variação determinística por desembolso, sem exagerar
            // em pendências — a maioria nasce "a analisar", poucas já inválidas.
            var status = rnd.NextDouble() switch
            {
                < 0.15 => "invalido",
                < 0.75 => "pendente",
                _      => "valido",
            };

            return new Validacao
            {
                Numero    = i + 1,
                Titulo    = item.Titulo,
                Resultado = status == "valido" ? "Em conformidade" : status == "invalido" ? "Não conforme" : "Aguardando análise",
                Status    = status,
                Icone     = "bi-check2-square",
                Detalhe   = item.Detalhe,
            };
        }).ToList();
    }

    // DSB-001 — pendencia: 6 válido, 4 inválido (3 deles com sub-itens)
    private static List<Validacao> ObterValidacoesDSB001() =>
    [
        new()
        {
            Numero = 1, Titulo = "CND Municipal",
            Resultado = "Certidão válida — emitida em 03/03/2025, vence em 03/09/2025",
            Status = "valido", Icone = "bi-building",
            Detalhe = "A Certidão Negativa de Débitos Municipal foi emitida pela Prefeitura de Caruaru em 03/03/2025 e possui validade de 180 dias. Não foram encontrados débitos pendentes junto ao município."
        },
        new()
        {
            Numero = 2, Titulo = "Certidão INSS",
            Resultado = "Certidão válida — emitida em 15/02/2025",
            Status = "valido", Icone = "bi-shield-check",
            Detalhe = "A Certidão de Regularidade Fiscal junto ao INSS (CND) foi obtida via Receita Federal e está vigente. Não há débitos previdenciários em aberto."
        },
        new()
        {
            Numero = 3, Titulo = "Percentual de Desembolso",
            Resultado = "Percentual fora do intervalo permitido (62% vs. máx. 60%)",
            Status = "invalido", Icone = "bi-percent",
            Detalhe = "O percentual acumulado de desembolso (62%) excede o limite máximo permitido para esta fase (60%). É necessário adequação antes da liberação.",
            SubItens =
            [
                new() { Numero = "3.1", Titulo = "Percentual Acumulado de Obra",   Status = "valido",   Detalhe = "A medição acumulada de obra corresponde a 58% do total contratado, dentro da faixa esperada para a 2ª medição." },
                new() { Numero = "3.2", Titulo = "Percentual Máximo por Fase",      Status = "invalido", Detalhe = "O percentual máximo permitido para a 2ª medição é de 60%. O valor solicitado (62%) ultrapassa esse limite." },
                new() { Numero = "3.3", Titulo = "Consistência Acumulada", Status = "valido",   Detalhe = "Os valores acumulados de desembolso são consistentes com as medições anteriores." },
            ]
        },
        new()
        {
            Numero = 4, Titulo = "Sequência de Desembolso",
            Resultado = "Sequência correta — 2º desembolso após 1º aprovado",
            Status = "valido", Icone = "bi-list-ol",
            Detalhe = "O 1º desembolso foi aprovado em 12/11/2024 e o 2º está sendo solicitado dentro do prazo regulamentar. A sequência está correta."
        },
        new()
        {
            Numero = 5, Titulo = "Regularidade FGTS do Mutuário",
            Resultado = "Mutuário em situação regular junto ao FGTS",
            Status = "valido", Icone = "bi-person-check",
            Detalhe = "A Prefeitura Municipal de Caruaru está em situação regular junto ao FGTS, sem débitos em aberto. Certificado obtido em 01/04/2025."
        },
        new()
        {
            Numero = 6, Titulo = "Laudo de Medição do Engenheiro",
            Resultado = "Laudo com ressalvas — divergência na sub-item 6b",
            Status = "invalido", Icone = "bi-file-earmark-text",
            Detalhe = "O laudo de medição foi assinado pelo engenheiro responsável, mas a sub-validação de conformidade com o projeto original apresentou divergência.",
            SubItens =
            [
                new() { Numero = "6.1", Titulo = "Assinatura do RT",         Status = "valido",   Detalhe = "O Responsável Técnico (RT) assinou o laudo conforme exigido." },
                new() { Numero = "6.2", Titulo = "Conformidade com Projeto", Status = "invalido", Detalhe = "O laudo indica execução de 62% da obra, mas o projeto aprovado prevê no máximo 60% para esta etapa." },
                new() { Numero = "6.3", Titulo = "Registro no CREA/CAU",     Status = "valido",   Detalhe = "O profissional responsável está com o registro ativo no CREA-PE." },
            ]
        },
        new()
        {
            Numero = 7, Titulo = "Depósito de Contrapartida",
            Resultado = "Contrapartida depositada — R$ 290.000,00 em 05/04/2025",
            Status = "valido", Icone = "bi-bank",
            Detalhe = "A contrapartida obrigatória de 20% do valor do investimento foi depositada na conta vinculada em 05/04/2025, conforme comprovante anexado ao processo."
        },
        new()
        {
            Numero = 8, Titulo = "Prazo Contratual",
            Resultado = "Contrato com prazo vencido — venceu em 30/03/2025",
            Status = "invalido", Icone = "bi-calendar-x",
            Detalhe = "O prazo de execução contratual expirou em 30/03/2025. É necessário o aditamento do contrato antes da liberação do desembolso.",
            ComentarioPreenchido = new Comentario
            {
                Tipo = "negativo",
                Texto = "Contrato vencido. Mutuário foi notificado para apresentar documentação de prorrogação até 25/05/2025.",
                Autor = "Karina Souto",
                Timestamp = new DateTime(2025, 4, 18, 9, 30, 0)
            }
        },
        new()
        {
            Numero = 9, Titulo = "Regularidade Fiscal da Construtora",
            Resultado = "Certidão estadual com débito ativo de R$ 12.400,00",
            Status = "invalido", Icone = "bi-building-gear",
            Detalhe = "A construtora responsável pela obra apresenta débito ativo de R$ 12.400,00 junto à SEFAZ-PE, " +
                      "conforme consulta realizada em 02/05/2025. A liberação está condicionada à regularização fiscal."
        },
        new()
        {
            Numero = 10, Titulo = "Comunicação ao SCPO",
            Resultado = "Comunicação ao Ministério do Trabalho realizada",
            Status = "valido", Icone = "bi-send-check",
            Detalhe = "A comunicação ao Sistema de Controle de Projetos e Obras (SCPO) do Ministério do Trabalho foi realizada em 10/04/2025, dentro do prazo regulamentar."
        },
    ];

    // DSB-002 — pendente (em análise): 5 válido + 5 pendente, nenhum inválido
    private static List<Validacao> ObterValidacoesDSB002() =>
    [
        new()
        {
            Numero = 1, Titulo = "CND Municipal",
            Resultado = "Certidão válida — emitida em 10/04/2025, vence em 10/10/2025",
            Status = "valido", Icone = "bi-building",
            Detalhe = "A Certidão Negativa de Débitos Municipal do Município de Campina Grande foi emitida em 10/04/2025 e está vigente. Não há débitos municipais em aberto."
        },
        new()
        {
            Numero = 2, Titulo = "Certidão INSS",
            Resultado = "Certidão válida — emitida em 02/04/2025",
            Status = "valido", Icone = "bi-shield-check",
            Detalhe = "A CND previdenciária foi obtida em 02/04/2025 e encontra-se vigente. Não há débitos junto ao INSS."
        },
        new()
        {
            Numero = 3, Titulo = "Percentual de Desembolso",
            Resultado = "Aguardando análise",
            Status = "pendente", Icone = "bi-percent",
            Detalhe = "O percentual de desembolso ainda não foi verificado pelo analista responsável.",
            SubItens =
            [
                new() { Numero = "3.1", Titulo = "Percentual Acumulado de Obra",   Status = "pendente", Detalhe = "Percentual acumulado de obra ainda não conferido." },
                new() { Numero = "3.2", Titulo = "Percentual Máximo por Fase",      Status = "pendente", Detalhe = "Limite por fase ainda não verificado." },
                new() { Numero = "3.3", Titulo = "Consistência Acumulada", Status = "pendente", Detalhe = "Consistência com medições anteriores ainda não analisada." },
            ]
        },
        new()
        {
            Numero = 4, Titulo = "Sequência de Desembolso",
            Resultado = "Sequência correta — 1º desembolso",
            Status = "valido", Icone = "bi-list-ol",
            Detalhe = "É o 1º desembolso deste contrato. Não há desembolso anterior a verificar. Sequência validada."
        },
        new()
        {
            Numero = 5, Titulo = "Regularidade FGTS do Mutuário",
            Resultado = "Mutuário em situação regular junto ao FGTS",
            Status = "valido", Icone = "bi-person-check",
            Detalhe = "O Município de Campina Grande está em situação regular junto ao FGTS. Certificado emitido em 15/04/2025."
        },
        new()
        {
            Numero = 6, Titulo = "Laudo de Medição do Engenheiro",
            Resultado = "Aguardando análise",
            Status = "pendente", Icone = "bi-file-earmark-text",
            Detalhe = "O laudo de medição ainda não foi analisado pelo técnico responsável.",
            SubItens =
            [
                new() { Numero = "6.1", Titulo = "Assinatura do RT",         Status = "pendente", Detalhe = "Verificação da assinatura do Responsável Técnico pendente." },
                new() { Numero = "6.2", Titulo = "Conformidade com Projeto", Status = "pendente", Detalhe = "Conformidade do laudo com o projeto aprovado ainda não verificada." },
                new() { Numero = "6.3", Titulo = "Registro no CREA/CAU",     Status = "pendente", Detalhe = "Validação do registro profissional ainda não realizada." },
            ]
        },
        new()
        {
            Numero = 7, Titulo = "Depósito de Contrapartida",
            Resultado = "Aguardando análise",
            Status = "pendente", Icone = "bi-bank",
            Detalhe = "Verificação do depósito de contrapartida ainda não realizada. Aguarda comprovante bancário."
        },
        new()
        {
            Numero = 8, Titulo = "Prazo Contratual",
            Resultado = "Aguardando análise",
            Status = "pendente", Icone = "bi-calendar-x",
            Detalhe = "Verificação da vigência do contrato ainda não realizada pelo analista."
        },
        new()
        {
            Numero = 9, Titulo = "Regularidade Fiscal da Construtora",
            Resultado = "Aguardando análise",
            Status = "pendente", Icone = "bi-building-gear",
            Detalhe = "Consulta à regularidade fiscal da construtora ainda não foi realizada."
        },
        new()
        {
            Numero = 10, Titulo = "Comunicação ao SCPO",
            Resultado = "Comunicação confirmada",
            Status = "valido", Icone = "bi-send-check",
            Detalhe = "A comunicação ao SCPO foi realizada em 18/04/2025, dentro do prazo regulamentar."
        },
    ];

    // DSB-003 / DSB-006 — aprovado: todos os 10 válidos
    private static List<Validacao> ObterValidacoesTodosValidos(string municipio) =>
    [
        new() { Numero = 1,  Titulo = "CND Municipal",                  Resultado = "Certidão válida",                              Status = "valido", Icone = "bi-building",          Detalhe = $"Certidão Negativa de Débitos do município de {municipio} válida e vigente." },
        new() { Numero = 2,  Titulo = "Certidão INSS",                  Resultado = "Certidão válida",                              Status = "valido", Icone = "bi-shield-check",      Detalhe = "CND previdenciária vigente. Nenhum débito em aberto." },
        new() { Numero = 3,  Titulo = "Percentual de Desembolso",       Resultado = "Percentual dentro do limite permitido",         Status = "valido", Icone = "bi-percent",           Detalhe = "O percentual de desembolso está dentro do intervalo permitido para esta fase." },
        new() { Numero = 4,  Titulo = "Sequência de Desembolso",        Resultado = "Sequência de desembolso correta",              Status = "valido", Icone = "bi-list-ol",           Detalhe = "A sequência de desembolsos está correta e em conformidade com o cronograma." },
        new() { Numero = 5,  Titulo = "Regularidade FGTS do Mutuário",  Resultado = "Mutuário em situação regular junto ao FGTS",   Status = "valido", Icone = "bi-person-check",      Detalhe = $"O município de {municipio} está em situação regular junto ao FGTS." },
        new() { Numero = 6,  Titulo = "Laudo de Medição do Engenheiro", Resultado = "Laudo aprovado sem ressalvas",                  Status = "valido", Icone = "bi-file-earmark-text", Detalhe = "Laudo de medição assinado pelo RT, em conformidade com o projeto aprovado." },
        new() { Numero = 7,  Titulo = "Depósito de Contrapartida",      Resultado = "Contrapartida depositada integralmente",       Status = "valido", Icone = "bi-bank",              Detalhe = "Contrapartida obrigatória depositada na conta vinculada dentro do prazo." },
        new() { Numero = 8,  Titulo = "Prazo Contratual",               Resultado = "Contrato dentro do prazo de execução",         Status = "valido", Icone = "bi-calendar-check",    Detalhe = "O contrato está vigente e dentro do prazo de execução estabelecido." },
        new() { Numero = 9,  Titulo = "Regularidade Fiscal da Construtora", Resultado = "Construtora sem débitos fiscais",          Status = "valido", Icone = "bi-building-gear",     Detalhe = "A construtora não apresenta débitos ativos junto às fazendas estadual e federal." },
        new() { Numero = 10, Titulo = "Comunicação ao SCPO",            Resultado = "Comunicação realizada no prazo",               Status = "valido", Icone = "bi-send-check",        Detalhe = "Comunicação ao SCPO realizada dentro do prazo regulamentar." },
    ];

    // DSB-004 — pendencia grave: 3 válido + 7 inválido
    private static List<Validacao> ObterValidacoesDSB004() =>
    [
        new()
        {
            Numero = 1, Titulo = "CND Municipal",
            Resultado = "Certidão vencida — expirou em 01/03/2025",
            Status = "invalido", Icone = "bi-building",
            Detalhe = "A Certidão Negativa de Débitos do município de Feira de Santana expirou em 01/03/2025. É necessária nova certidão vigente para prosseguimento."
        },
        new()
        {
            Numero = 2, Titulo = "Certidão INSS",
            Resultado = "Débito previdenciário ativo — R$ 34.200,00",
            Status = "invalido", Icone = "bi-shield-x",
            Detalhe = "O município possui débito previdenciário ativo de R$ 34.200,00 junto ao INSS. A CND não pode ser emitida até a regularização."
        },
        new()
        {
            Numero = 3, Titulo = "Percentual de Desembolso",
            Resultado = "Percentual solicitado excede o limite (95% vs. máx. 80%)",
            Status = "invalido", Icone = "bi-percent",
            Detalhe = "O desembolso final solicita 95% do valor contratado. O limite para a fase final é 80%. Há incompatibilidade com o cronograma.",
            SubItens =
            [
                new() { Numero = "3.1", Titulo = "Percentual Acumulado de Obra",   Status = "invalido", Detalhe = "O percentual acumulado de 95% é incompatível com o estágio físico da obra." },
                new() { Numero = "3.2", Titulo = "Percentual Máximo por Fase",      Status = "invalido", Detalhe = "O limite máximo para a fase final é de 80%. O valor solicitado ultrapassa em 15 pontos percentuais." },
                new() { Numero = "3.3", Titulo = "Consistência Acumulada", Status = "valido",   Detalhe = "Os valores acumulados são internamente consistentes com as medições anteriores." },
            ]
        },
        new()
        {
            Numero = 4, Titulo = "Sequência de Desembolso",
            Resultado = "Sequência correta — desembolso final",
            Status = "valido", Icone = "bi-list-ol",
            Detalhe = "Os três desembolsos anteriores foram aprovados. A sequência para o desembolso final está correta."
        },
        new()
        {
            Numero = 5, Titulo = "Regularidade FGTS do Mutuário",
            Resultado = "Irregularidade FGTS — parcelamento ativo",
            Status = "invalido", Icone = "bi-person-x",
            Detalhe = "O município possui parcelamento ativo de débitos do FGTS. A situação impede a emissão de certificado de regularidade.",
            ComentarioPreenchido = new Comentario
            {
                Tipo = "negativo",
                Texto = "Município informou que parcelamento está em dia, mas a situação no sistema ainda não foi atualizada. Aguardando documentação.",
                Autor = "Ana Paula Lima",
                Timestamp = new DateTime(2025, 4, 25, 14, 0, 0)
            }
        },
        new()
        {
            Numero = 6, Titulo = "Laudo de Medição do Engenheiro",
            Resultado = "Laudo com divergências graves",
            Status = "invalido", Icone = "bi-file-earmark-x",
            Detalhe = "O laudo de medição apresenta divergências entre o percentual executado e as fotografias da obra.",
            SubItens =
            [
                new() { Numero = "6.1", Titulo = "Assinatura do RT",         Status = "valido",   Detalhe = "O Responsável Técnico assinou o laudo conforme exigido." },
                new() { Numero = "6.2", Titulo = "Conformidade com Projeto", Status = "invalido", Detalhe = "As fotografias apresentadas não condizem com o percentual de 95% declarado no laudo." },
                new() { Numero = "6.3", Titulo = "Registro no CREA/CAU",     Status = "invalido", Detalhe = "O registro do RT no CREA venceu em fevereiro de 2025 e ainda não foi renovado." },
            ]
        },
        new()
        {
            Numero = 7, Titulo = "Depósito de Contrapartida",
            Resultado = "Contrapartida depositada integralmente",
            Status = "valido", Icone = "bi-bank",
            Detalhe = "A contrapartida de 20% foi depositada integralmente na conta vinculada. Comprovante verificado."
        },
        new()
        {
            Numero = 8, Titulo = "Prazo Contratual",
            Resultado = "Contrato vencido — expirou em 08/01/2025",
            Status = "invalido", Icone = "bi-calendar-x",
            Detalhe = "O prazo de execução contratual expirou em 08/01/2025. O aditamento não foi formalizado.",
            ComentarioPreenchido = new Comentario
            {
                Tipo = "negativo",
                Texto = "Prazo vencido há mais de 4 meses sem formalização de aditamento. Processo suspenso até regularização.",
                Autor = "Bruno Costa",
                Timestamp = new DateTime(2025, 5, 2, 10, 15, 0)
            }
        },
        new()
        {
            Numero = 9, Titulo = "Regularidade Fiscal da Construtora",
            Resultado = "Construtora com débito estadual de R$ 89.600,00",
            Status = "invalido", Icone = "bi-building-gear",
            Detalhe = "A construtora possui débito ativo de R$ 89.600,00 junto à SEFAZ-BA. Está incluída no CADIN estadual."
        },
        new()
        {
            Numero = 10, Titulo = "Comunicação ao SCPO",
            Resultado = "Comunicação realizada no prazo",
            Status = "valido", Icone = "bi-send-check",
            Detalhe = "A comunicação ao SCPO foi realizada dentro do prazo regulamentar, ainda que os demais itens estejam pendentes."
        },
    ];

    // DSB-005 — pendente (em análise): 8 válido + 2 pendente, nenhum inválido
    private static List<Validacao> ObterValidacoesDSB005() =>
    [
        new()
        {
            Numero = 1, Titulo = "CND Municipal",
            Resultado = "Certidão válida — emitida em 20/03/2025",
            Status = "valido", Icone = "bi-building",
            Detalhe = "Certidão Negativa de Débitos do município de Aracaju emitida em 20/03/2025 e vigente por 180 dias."
        },
        new()
        {
            Numero = 2, Titulo = "Certidão INSS",
            Resultado = "Certidão válida — emitida em 05/04/2025",
            Status = "valido", Icone = "bi-shield-check",
            Detalhe = "CND previdenciária vigente. Nenhum débito em aberto junto ao INSS."
        },
        new()
        {
            Numero = 3, Titulo = "Percentual de Desembolso",
            Resultado = "Percentual dentro do limite — 48% vs. máx. 55%",
            Status = "valido", Icone = "bi-percent",
            Detalhe = "O percentual de desembolso está dentro do intervalo permitido para a 1ª medição.",
            SubItens =
            [
                new() { Numero = "3.1", Titulo = "Percentual Acumulado de Obra",   Status = "valido", Detalhe = "Percentual acumulado de obra (48%) está dentro da faixa para a 1ª medição." },
                new() { Numero = "3.2", Titulo = "Percentual Máximo por Fase",      Status = "valido", Detalhe = "Limite de 55% para a 1ª medição. Percentual solicitado (48%) está abaixo do limite." },
                new() { Numero = "3.3", Titulo = "Consistência Acumulada", Status = "valido", Detalhe = "É o primeiro desembolso. Não há histórico anterior com inconsistência." },
            ]
        },
        new()
        {
            Numero = 4, Titulo = "Sequência de Desembolso",
            Resultado = "Sequência correta — 1º desembolso",
            Status = "valido", Icone = "bi-list-ol",
            Detalhe = "É o 1º desembolso do contrato. Sequência validada automaticamente."
        },
        new()
        {
            Numero = 5, Titulo = "Regularidade FGTS do Mutuário",
            Resultado = "Mutuário em situação regular junto ao FGTS",
            Status = "valido", Icone = "bi-person-check",
            Detalhe = "A Prefeitura de Aracaju está em situação regular. Certificado emitido em 08/04/2025."
        },
        new()
        {
            Numero = 6, Titulo = "Laudo de Medição do Engenheiro",
            Resultado = "Laudo aprovado sem ressalvas",
            Status = "valido", Icone = "bi-file-earmark-text",
            Detalhe = "Laudo de medição assinado pelo RT em 12/04/2025. Em conformidade com o projeto aprovado.",
            SubItens =
            [
                new() { Numero = "6.1", Titulo = "Assinatura do RT",         Status = "valido", Detalhe = "Responsável Técnico assinou o laudo em 12/04/2025." },
                new() { Numero = "6.2", Titulo = "Conformidade com Projeto", Status = "valido", Detalhe = "Percentual de obra declarado (48%) é compatível com as fotografias e projeto aprovado." },
                new() { Numero = "6.3", Titulo = "Registro no CREA/CAU",     Status = "valido", Detalhe = "Registro profissional ativo no CREA-SE." },
            ]
        },
        new()
        {
            Numero = 7, Titulo = "Depósito de Contrapartida",
            Resultado = "Contrapartida depositada — R$ 246.000,00 em 22/04/2025",
            Status = "valido", Icone = "bi-bank",
            Detalhe = "Contrapartida obrigatória de 20% depositada em 22/04/2025 na conta vinculada."
        },
        new()
        {
            Numero = 8, Titulo = "Prazo Contratual",
            Resultado = "Contrato vigente até 31/12/2025",
            Status = "valido", Icone = "bi-calendar-check",
            Detalhe = "O contrato possui prazo de execução até 31/12/2025. Está dentro do prazo para realização da 1ª medição."
        },
        new()
        {
            Numero = 9, Titulo = "Regularidade Fiscal da Construtora",
            Resultado = "Aguardando análise",
            Status = "pendente", Icone = "bi-building-gear",
            Detalhe = "A consulta à regularidade fiscal da construtora junto à SEFAZ-SE ainda não foi realizada."
        },
        new()
        {
            Numero = 10, Titulo = "Comunicação ao SCPO",
            Resultado = "Aguardando análise",
            Status = "pendente", Icone = "bi-send-check",
            Detalhe = "A verificação da comunicação ao SCPO ainda está pendente de confirmação pelo analista responsável."
        },
    ];

    // ──────────────────────────────────────────────────────────────────────────
    // DETALHE DO CONTRATO
    // ──────────────────────────────────────────────────────────────────────────
    public static DetalheContrato ObterDetalheContrato(DesembolsoCAD row)
    {
        if (row.Id == "DSB-001")
        {
            return new DetalheContrato
            {
                Id = "DSB-001",
                Gigov = "GIG-2022-04512",
                NumeroContrato = "0512.345/2022-01",
                Mutuario = new() { Nome = "Prefeitura Municipal de Caruaru", Tipo = "Município" },
                Af = new() { Nome = "Agência Nordeste — Recife", Sigla = "AF/REC" },
                AgenteFinanceiro   = "CAIXA ECONOMICA FEDERAL",
                AgentePromotor     = "MUNICÍPIO DE CARUARU/PE",
                PrimeiroDesembolso = false,
                TipoDesembolso     = "normal",
                Financiamento = new()
                {
                    Programa           = "Pró-Moradia",
                    ValorInvestimento  = 1_812_500m,
                    ValorFinanciamento = 1_450_000m,
                    NumeroDesembolso   = "2º",
                    Fase               = "2ª Medição",
                    PercentualObra     = 62m,
                    Amortizacao        = "SAC / TR",
                    PercentualContrapartida = 20m,
                    Ve                 = 1_812_500m,
                    ParticipacaoFgts   = 1_160_000m,
                    Contrapartida      = 290_000m,
                }
            };
        }

        // Gera dados plausíveis para os demais desembolsos
        var seed = row.Id.GetHashCode();
        var programas = new[] { "Pró-Moradia", "Saneamento", "Mobilidade Urbana", "Infraestrutura" };
        var afs = new[]
        {
            new DadosAf { Nome = "Agência Norte — Belém",       Sigla = "AF/BEL" },
            new DadosAf { Nome = "Agência Centro-Oeste — BSB",  Sigla = "AF/BSB" },
            new DadosAf { Nome = "Agência Sul — Porto Alegre",  Sigla = "AF/POA" },
        };
        var contrapartidaVlr = row.Valor * 0.2m;
        var participacaoFgts = row.Valor - contrapartidaVlr;

        return new DetalheContrato
        {
            Id             = row.Id,
            Gigov          = row.Gigov,
            NumeroContrato = row.Contrato,
            Mutuario       = new() { Nome = row.Mutuario, Tipo = "Município" },
            Af             = afs[Math.Abs(seed) % afs.Length],
            AgenteFinanceiro   = "CAIXA ECONOMICA FEDERAL",
            AgentePromotor     = row.Mutuario.ToUpper(),
            PrimeiroDesembolso = row.Fase.Contains("1"),
            TipoDesembolso     = Math.Abs(seed) % 5 == 0 ? "adiantamento" : "normal",
            Financiamento  = new()
            {
                Programa           = programas[Math.Abs(seed) % programas.Length],
                ValorInvestimento  = row.Valor * 1.25m,
                ValorFinanciamento = row.Valor,
                NumeroDesembolso   = row.Fase.Contains("1") ? "1º" : row.Fase.Contains("2") ? "2º" : "3º",
                Fase               = row.Fase,
                PercentualObra     = 45m + (Math.Abs(seed) % 30),
                Amortizacao        = "SAC / TR",
                PercentualContrapartida = 20m,
                Ve                 = row.Valor * 1.25m,
                ParticipacaoFgts   = participacaoFgts,
                Contrapartida      = contrapartidaVlr,
            }
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UTILITÁRIOS DE FORMATAÇÃO
    // ──────────────────────────────────────────────────────────────────────────
    public static string FormatarBRL(decimal valor) =>
        valor.ToString("C", _culturaBR);

    public static string FormatarPrazo(DateTime data)
    {
        string[] meses = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];
        return $"{data.Day} {meses[data.Month - 1]}";
    }

    public static string FormatarTempo(DateTime timestamp)
    {
        var diff = DateTime.Now - timestamp;
        if (diff.TotalMinutes < 1)   return "agora";
        if (diff.TotalMinutes < 60)  return $"{(int)diff.TotalMinutes}min atrás";
        if (diff.TotalHours   < 24)  return $"{(int)diff.TotalHours}h atrás";
        return timestamp.ToString("dd/MM/yyyy HH:mm");
    }
}
