-- Reinvest — Schema inicial
-- Este arquivo cria apenas o que o EF Core NÃO gerencia via migrations.
-- A tabela `users` e estruturas relacionadas são gerenciadas pelo EF Core (Reinvest.Infrastructure/Migrations).

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Tabela de referência de alocação (fixa — não muda sem aprovação)
-- Gerenciada aqui (e não pelo EF) porque é uma tabela de configuração imutável
-- populada por seeds, sem entidade mapeada no DbContext.
CREATE TABLE IF NOT EXISTS allocation_targets (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    faixa       VARCHAR(20) NOT NULL,
    perfil      VARCHAR(20) NOT NULL,
    classe      VARCHAR(50) NOT NULL,
    percentual  DECIMAL(5,2) NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (faixa, perfil, classe)
);
