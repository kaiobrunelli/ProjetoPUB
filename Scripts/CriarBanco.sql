-- ============================================================
-- SIPUB Desembolsos — Sistema de Notificações (plataforma completa)
-- Execute no SSMS. Dropa as tabelas se existirem e recria tudo.
-- ============================================================

USE SipubNotificacoes;
GO

-- ────────────────────────────────────────────────────────────
-- Drop na ordem correta (filho antes do pai por causa da FK)
-- ────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.ControleVisualizacao', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.ControleVisualizacao;
    PRINT 'Tabela ControleVisualizacao removida.';
END

-- Retrocompatibilidade: remove nome antigo se ainda existir
IF OBJECT_ID('dbo.NotificacaoUsuario', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.NotificacaoUsuario;
    PRINT 'Tabela NotificacaoUsuario (legado) removida.';
END

IF OBJECT_ID('dbo.Notificacao', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Notificacao;
    PRINT 'Tabela Notificacao removida.';
END

IF OBJECT_ID('dbo.Desembolso', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Desembolso;
    PRINT 'Tabela Desembolso removida.';
END
GO

-- ────────────────────────────────────────────────────────────
-- Tabela: Notificacao
-- 1 linha por evento disparado.
--
-- Escopo: 'Geral'      = disparada para toda a plataforma
--         'Modulo'     = disparada para usuários de um módulo
--         'Individual' = disparada para usuários específicos
--
-- Modulo: NULL         = notificação de plataforma (sem módulo)
--         'Sipub' etc. = módulo de origem
--
-- ExigeConfirmacao = 1 → usuário precisa clicar "Confirmo que li"
-- ────────────────────────────────────────────────────────────
CREATE TABLE dbo.Notificacao (
    Id               INT            NOT NULL IDENTITY(1,1),
    Titulo           NVARCHAR(200)  NOT NULL,
    Descricao        NVARCHAR(1000) NOT NULL,
    Tipo             NVARCHAR(20)   NOT NULL,   -- 'informacao' | 'sucesso' | 'alerta' | 'confirmacao'
    Escopo           NVARCHAR(20)   NOT NULL,   -- 'Geral' | 'Modulo' | 'Individual'
    Modulo           NVARCHAR(50)   NULL,        -- NULL = plataforma; ex: 'Sipub'
    EntidadeId       NVARCHAR(50)   NULL,        -- ID do negócio (para relatórios)
    ExigeConfirmacao BIT            NOT NULL DEFAULT 0,
    CriadaEm        DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    ExpiraEm         DATETIME2      NOT NULL,
    CONSTRAINT PK_Notificacao PRIMARY KEY (Id)
);
PRINT 'Tabela Notificacao criada.';
GO

-- ────────────────────────────────────────────────────────────
-- Tabela: ControleVisualizacao
-- 1 linha por empregado por notificação — sempre eager (criada no envio).
-- Renomeada de NotificacaoUsuario: registra quem recebeu e se já visualizou.
--
-- LidaEm = NULL  → não lida
-- LidaEm = data  → lida / confirmada
--
-- Rota: caminho relativo ex: '/?contratoId=DSB-001'
--       NULL = sem ação de navegação
-- ────────────────────────────────────────────────────────────
CREATE TABLE dbo.ControleVisualizacao (
    Id              INT            NOT NULL IDENTITY(1,1),
    NotificacaoId   INT            NOT NULL,
    UsuarioId       NVARCHAR(100)  NOT NULL,
    LidaEm          DATETIME2      NULL,         -- NULL = não lida
    EntregueViaHub  BIT            NOT NULL DEFAULT 0,
    Rota            NVARCHAR(500)  NULL,
    CONSTRAINT PK_ControleVisualizacao   PRIMARY KEY (Id),
    CONSTRAINT FK_CtrlVis_Notificacao    FOREIGN KEY (NotificacaoId)
        REFERENCES dbo.Notificacao(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_CtrlVis_NotifUsuario   UNIQUE (NotificacaoId, UsuarioId)
);
PRINT 'Tabela ControleVisualizacao criada.';
GO

-- Índice principal: busca de não lidas por usuário
CREATE INDEX IX_CtrlVis_UsuarioId_LidaEm
    ON dbo.ControleVisualizacao (UsuarioId, LidaEm)
    INCLUDE (NotificacaoId);
GO

-- ────────────────────────────────────────────────────────────
-- Tabela: Desembolso
-- Entidade de negócio — alterações disparam Domain Events
-- que geram notificações automaticamente.
-- ────────────────────────────────────────────────────────────
CREATE TABLE dbo.Desembolso (
    Id          NVARCHAR(20)  NOT NULL,
    Municipio   NVARCHAR(200) NOT NULL,
    Status      NVARCHAR(50)  NOT NULL DEFAULT 'Pendente',
    CriadoEm   DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    ValidadoEm  DATETIME2     NULL,
    ValidadoPor NVARCHAR(100) NULL,
    CONSTRAINT PK_Desembolso PRIMARY KEY (Id)
);
PRINT 'Tabela Desembolso criada.';
GO

-- Dados de teste
INSERT INTO dbo.Desembolso (Id, Municipio, Status) VALUES
    ('DSB-001', 'Prefeitura de Caruaru',        'Pendente'),
    ('DSB-002', 'Prefeitura de Campina Grande',  'Pendente'),
    ('DSB-003', 'Prefeitura de Aracaju',         'Pendente'),
    ('DSB-004', 'Município de Feira de Santana', 'Pendente'),
    ('DSB-005', 'Prefeitura de Natal',           'Pendente');
PRINT 'Dados de teste inseridos em Desembolso.';
GO

PRINT 'Script concluído com sucesso!';
GO
