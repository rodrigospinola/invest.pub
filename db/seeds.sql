-- Seeds: allocation_targets
-- Fonte: docs/modelagem-1.md — percentuais definidos pelo cliente. NÃO ALTERAR sem aprovação.

-- === FAIXA 1 — Até R$10.000 (4 classes) ===

INSERT INTO allocation_targets (faixa, perfil, classe, percentual) VALUES
    ('ate_10k', 'conservador', 'RF Dinâmica',         45.00),
    ('ate_10k', 'conservador', 'RF Pós',              30.00),
    ('ate_10k', 'conservador', 'Fundos imobiliários', 15.00),
    ('ate_10k', 'conservador', 'Ações',               10.00);

INSERT INTO allocation_targets (faixa, perfil, classe, percentual) VALUES
    ('ate_10k', 'moderado', 'RF Dinâmica',         30.00),
    ('ate_10k', 'moderado', 'RF Pós',              25.00),
    ('ate_10k', 'moderado', 'Fundos imobiliários', 22.00),
    ('ate_10k', 'moderado', 'Ações',               23.00);

INSERT INTO allocation_targets (faixa, perfil, classe, percentual) VALUES
    ('ate_10k', 'arrojado', 'RF Dinâmica',         22.00),
    ('ate_10k', 'arrojado', 'RF Pós',              18.00),
    ('ate_10k', 'arrojado', 'Fundos imobiliários', 25.00),
    ('ate_10k', 'arrojado', 'Ações',               35.00);

-- === FAIXA 2 — R$10.000 a R$100.000 (5 classes) ===

INSERT INTO allocation_targets (faixa, perfil, classe, percentual) VALUES
    ('10k_100k', 'conservador', 'RF Dinâmica',         35.00),
    ('10k_100k', 'conservador', 'RF Pós',              28.00),
    ('10k_100k', 'conservador', 'Fundos imobiliários', 15.00),
    ('10k_100k', 'conservador', 'Ações',               12.00),
    ('10k_100k', 'conservador', 'Internacional',       10.00);

INSERT INTO allocation_targets (faixa, perfil, classe, percentual) VALUES
    ('10k_100k', 'moderado', 'RF Dinâmica',         24.00),
    ('10k_100k', 'moderado', 'RF Pós',              22.00),
    ('10k_100k', 'moderado', 'Fundos imobiliários', 18.00),
    ('10k_100k', 'moderado', 'Ações',               18.00),
    ('10k_100k', 'moderado', 'Internacional',       18.00);

INSERT INTO allocation_targets (faixa, perfil, classe, percentual) VALUES
    ('10k_100k', 'arrojado', 'RF Dinâmica',         22.00),
    ('10k_100k', 'arrojado', 'RF Pós',              17.00),
    ('10k_100k', 'arrojado', 'Fundos imobiliários', 16.00),
    ('10k_100k', 'arrojado', 'Ações',               20.00),
    ('10k_100k', 'arrojado', 'Internacional',       25.00);

-- === FAIXA 3 — Acima de R$100.000 (7 classes) ===
-- Percentuais do moderado e arrojado definidos pelo cliente — NÃO ALTERAR.

INSERT INTO allocation_targets (faixa, perfil, classe, percentual) VALUES
    ('acima_100k', 'conservador', 'RF Dinâmica',          25.00),
    ('acima_100k', 'conservador', 'RF Pós',               25.00),
    ('acima_100k', 'conservador', 'Fundos imobiliários',  15.00),
    ('acima_100k', 'conservador', 'Ações',                10.00),
    ('acima_100k', 'conservador', 'Internacional',        10.00),
    ('acima_100k', 'conservador', 'Fundos multimercados', 14.00),
    ('acima_100k', 'conservador', 'Alternativos',          1.00);

INSERT INTO allocation_targets (faixa, perfil, classe, percentual) VALUES
    ('acima_100k', 'moderado', 'RF Dinâmica',          20.00),
    ('acima_100k', 'moderado', 'RF Pós',               20.00),
    ('acima_100k', 'moderado', 'Fundos imobiliários',  15.00),
    ('acima_100k', 'moderado', 'Ações',                15.00),
    ('acima_100k', 'moderado', 'Internacional',        15.00),
    ('acima_100k', 'moderado', 'Fundos multimercados', 14.00),
    ('acima_100k', 'moderado', 'Alternativos',          1.00);

INSERT INTO allocation_targets (faixa, perfil, classe, percentual) VALUES
    ('acima_100k', 'arrojado', 'RF Dinâmica',          20.00),
    ('acima_100k', 'arrojado', 'RF Pós',               15.00),
    ('acima_100k', 'arrojado', 'Fundos imobiliários',  15.00),
    ('acima_100k', 'arrojado', 'Ações',                15.00),
    ('acima_100k', 'arrojado', 'Internacional',        20.00),
    ('acima_100k', 'arrojado', 'Fundos multimercados', 12.00),
    ('acima_100k', 'arrojado', 'Alternativos',          3.00);
