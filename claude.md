# CLAUDE.md — Regras do projeto Invest

## Sobre o projeto

Invest é uma plataforma de investimentos que guia iniciantes do primeiro aporte até R$500k. Stack: React (frontend) + C# .NET com EF Core (API) + Python (batch IA) + PostgreSQL (Cloud SQL) + Claude via Vertex AI.

---

## Regras de segurança (críticas)

### Nunca expor segredos

- NUNCA incluir API keys, connection strings, senhas ou tokens no código
- NUNCA fazer commit de arquivos `.env`, `appsettings.Development.json` com credenciais reais, ou qualquer arquivo com segredos
- Sempre usar variáveis de ambiente para configurações sensíveis
- No GCP, usar Secret Manager para chaves de produção
- Se encontrar uma credencial hardcoded no código, remover imediatamente e alertar

### Arquivos que nunca devem ser commitados

```
.env
.env.local
.env.production
appsettings.Development.json (se contiver credenciais)
*.pfx
*.key
*.pem
service-account.json
credentials.json
```

Esses devem estar no `.gitignore`. Sempre verificar antes de sugerir um commit.

### Autenticação e autorização

- Todo endpoint exceto `/auth/*` requer JWT válido
- Nunca retornar dados de um usuário para outro — sempre filtrar por `user_id` do token
- Nunca logar tokens JWT, senhas ou dados sensíveis
- Senhas armazenadas com bcrypt, nunca em texto puro
- Tokens B3 (Open Finance) sempre encriptados em repouso

### Dados financeiros

- O app nunca executa compras ou vendas — é puramente sugestivo
- Nunca armazenar dados bancários (conta, agência, cartão)
- Nunca dar recomendação como conselho financeiro — sempre apresentar como sugestão

---

## Arquitetura C# — Clean Architecture + DDD + CQRS

O backend segue Clean Architecture com princípios de DDD (Domain-Driven Design) e CQRS (Command Query Responsibility Segregation), dividido em 4 projetos. Dependências sempre apontam pra dentro (pro Domain).

### Invest.Domain (centro — zero dependências)
- Entities: User, UserProfile, UserAsset, BatchRanking, RebalancingPlan, Alert, etc.
- Value Objects: Allocation, Deviation, Score, AssetIndicators, TierRange (imutáveis)
- Enums: PerfilRisco, FaixaPatrimonio, ClasseAtivo, SubEstrategiaAcoes, OrigemAtivo, etc.
- Domain Services: AllocationService, DeviationCalculator, ContributionDistributor, AssetQuantityCalculator, TaxCalculator
- Interfaces: IUserProfileRepository, IAiService, IMarketDataService, etc.
- Constants: AllocationTargets, AssetQuantityTiers, BusinessRules

### Invest.Application (use cases / CQRS — depende só do Domain)
- Commands: 1 arquivo por command (contrato de escrita)
- Queries: 1 arquivo por query (contrato de leitura)
- Handlers: 1 arquivo por domínio (executa commands e queries relacionados)
- Responses: 1 arquivo por response
- Validators: FluentValidation para inputs críticos
- Common: Result<T> pattern, PagedResult

### Invest.Infrastructure (mundo externo — implementa interfaces do Domain)
- Repositories: EF Core implementando IXxxRepository
- Services: VertexAiService, JwtService, B3SyncService
- Data: AppDbContext, Configurations, Migrations

### Invest.API (entrada — controllers finos)
- Controllers: roteiam para Handlers, [Authorize] em tudo exceto Auth
- Middleware: ErrorHandling, RateLimiting, RequestLogging
- Program.cs: composição de DI

---

## Convenções de código

### C# (API)

- .NET 8+
- Usar EF Core com migrations (code-first)
- Controllers finos — orquestram Use Cases via Handlers seguindo o padrão CQRS
- Commands e Queries separados (1 por arquivo), Handlers unificados (1 por domínio) organizando a lógica de aplicação
- Nomenclatura PascalCase para classes e métodos, camelCase para variáveis locais
- Async/await em todas as chamadas de banco e APIs externas
- Retornar Responses nos endpoints, nunca entidades do EF diretamente
- Validação de input com FluentValidation
- Injeção de dependência para todos os services
- Tratar exceções com middleware global, nunca try/catch em controllers
- Usar Result<T> pattern para fluxos de negócio (evitar exceções)

Estrutura de pastas:

```
api/
  Invest.Domain/
    Entities/          # Entidades com identidade
    ValueObjects/      # Objetos imutáveis comparados por valor
    Enums/             # Enumerações do domínio
    Services/          # Regras de negócio puras
    Interfaces/        # Contratos de repositórios e serviços
    Constants/         # Valores fixos do negócio

  Invest.Application/
    Commands/          # 1 arquivo por command, organizados por domínio
    Queries/           # 1 arquivo por query, organizados por domínio
    Handlers/          # 1 arquivo por domínio (agrupa commands + queries)
    Responses/         # 1 arquivo por response
    Validators/        # FluentValidation
    Mappings/          # AutoMapper
    Common/            # Result<T>, PagedResult

  Invest.Infrastructure/
    Data/              # DbContext, Configurations, Migrations
    Repositories/      # Implementações com EF Core
    Services/          # VertexAiService, JwtService, B3SyncService
    Extensions/        # ServiceCollectionExtensions

  Invest.API/
    Controllers/       # Finos, só roteamento
    Middleware/         # ErrorHandling, RateLimiting, Logging
    Extensions/        # AuthExtensions, SwaggerExtensions
```

### React (frontend)

- Componentes funcionais com hooks (nunca class components)
- TypeScript obrigatório
- Nomenclatura: PascalCase para componentes, camelCase para hooks e funções
- Estado global com Context API ou Zustand (não Redux)
- Chamadas de API centralizadas em `services/`
- Componentes de UI reutilizáveis em `components/ui/`
- Páginas em `pages/` espelhando as rotas
- Nunca chamar APIs externas direto do frontend — sempre via API C#
- Nunca armazenar JWT no localStorage — usar httpOnly cookies ou memory

Estrutura de pastas:

```
web/src/
  components/
    ui/            # Botões, inputs, cards, modals
    charts/        # Gráficos
    layout/        # Header, sidebar, footer
  pages/           # 1 pasta por módulo (auth, onboarding, portfolio, dashboard, settings)
  services/        # 1 arquivo por módulo (profileService, rankingService, etc.)
  hooks/           # Custom hooks (useAuth, usePortfolio, useDashboard)
  contexts/        # Context providers (AuthContext, OnboardingContext)
  types/           # TypeScript interfaces
  utils/           # Formatters, validators, constants
```

### Python (batch)

- Python 3.11+
- Type hints em todas as funções
- Nomenclatura snake_case para tudo
- Cada job é um script independente em `jobs/`
- Lógica compartilhada em `services/`
- Usar psycopg2 ou asyncpg para conexão direta com PostgreSQL
- Nunca usar ORM no batch — queries SQL otimizadas para performance
- Logging estruturado (JSON) para Cloud Logging
- Tratamento de erro robusto — o batch nunca deve falhar silenciosamente
- Se um job falhar, logar o erro e retornar exit code != 0 para retry

Estrutura de pastas:

```
batch/
  jobs/            # Scripts de execução (1 por job)
  services/        # yfinance_service, cvm_service, bcb_service, vertex_ai_service, scoring, db
  utils/           # logger, retry
  tests/           # pytest
  config.py        # Variáveis de ambiente
```

---

## Convenções de banco de dados

- PostgreSQL 15+
- Nomes de tabelas: snake_case, plural (ex: `user_profiles`, `batch_rankings`)
- Nomes de colunas: snake_case
- Toda tabela tem `id` (uuid), `created_at` (timestamp) e `updated_at` (timestamp quando aplicável)
- Usar enum types para campos com valores fixos (perfil, status, tipo)
- Campos flexíveis como indicadores e alocações usar `jsonb`
- Índices explícitos nas colunas mais consultadas
- Tabelas de histórico diário particionadas por mês
- Soft delete para dados de usuário (campo `status` = `inativo`, manter 30 dias)
- Nunca deletar registros de histórico

---

## Convenções de API

- RESTful com verbos HTTP corretos (GET, POST, PUT, DELETE)
- Respostas sempre em JSON
- Códigos HTTP corretos (200, 201, 400, 401, 403, 404, 500)
- Paginação com `limit` e `offset` para listas
- Filtros via query params
- Erros no formato `{ error: { code, message, field } }`
- Rate limiting: 60 req/min geral, 10 req/min para `/chat`, 5 req/hora para `/b3/sync`

---

## Regras de negócio importantes

### Alocação (Modelagem 1)

- A tabela de alocação é fixa — nunca calculada por IA
- 3 perfis: conservador, moderado, arrojado
- 3 faixas: até R$10k (4 classes), R$10k–100k (5 classes), acima de R$100k (7 classes)
- Os percentuais do moderado e arrojado acima de R$100k foram definidos pelo cliente e não devem ser alterados sem aprovação

### Batch (Modelagem 2)

- O batch gera rankings de top 20, nunca mais e nunca menos
- Sub-estratégias de ações: valor, dividendos, misto
- Sub-estratégias de FIIs: renda, valorização, misto
- Ações sugeridas: 5 a 15 (escalando com valor alocado)
- FIIs sugeridos: 5 a 8 (escalando com valor alocado)
- Score final = 70% quantitativo (yfinance) + 30% qualitativo (Claude + web search)
- O batch roda apenas em dias úteis
- O batch Python acessa o banco diretamente (leitura + escrita), não passa pela API C#

### Monitoramento (Modelagem 3)

- Desvio normal: 0–3% (sem ação)
- Desvio atenção: 3–5% (sem alerta, rebalanceamento trimestral resolve)
- Desvio extraordinário: acima de 5% (alerta imediato)
- Rebalanceamento trimestral: fevereiro, maio, agosto, novembro
- Alertas de batch só para usuários na Modelagem 3 que possuem o ativo
- Alertas de batch só para ativos com origem "sugerido" (não "próprio")
- Máximo 1 alerta por classe por dia

### Mudança de faixa

- Buffer de 5% para evitar ping-pong (upgrade em R$10k, downgrade em R$9.5k)
- Nunca sugerir vender uma classe só porque o patrimônio caiu de faixa

### Tributação

- Ações: isenção até R$20k/mês em vendas (operações comuns)
- FIIs: sem isenção, 20% sobre lucro
- O app orienta mas não é um contador — sempre recomendar consulta profissional

---

## Variáveis de ambiente

### Obrigatórias (API C#)

```
DATABASE_URL         # Connection string do PostgreSQL
JWT_SECRET           # Chave secreta para assinar tokens
JWT_EXPIRATION       # Tempo de expiração (ex: "24h")
VERTEX_AI_PROJECT    # ID do projeto GCP
VERTEX_AI_LOCATION   # Região do Vertex AI (ex: "us-central1")
```

### Obrigatórias (Batch Python)

```
DATABASE_URL         # Connection string do PostgreSQL
VERTEX_AI_PROJECT    # ID do projeto GCP
VERTEX_AI_LOCATION   # Região do Vertex AI
```

### Opcionais

```
LOG_LEVEL            # "info", "debug", "error" (default: "info")
CORS_ORIGINS         # Origens permitidas (default: "*" em dev)
RATE_LIMIT_GENERAL   # Req/min geral (default: 60)
RATE_LIMIT_CHAT      # Req/min para /chat (default: 10)
```

---

## Git

### Branches

- `main` — produção, sempre estável
- `develop` — integração, base para features
- `feature/xxx` — features novas
- `fix/xxx` — correções
- `hotfix/xxx` — correções urgentes em produção

### Commits

Usar conventional commits:

```
feat: adicionar endpoint de rebalanceamento
fix: corrigir cálculo de desvio com classes vazias
chore: atualizar dependências do batch
docs: atualizar CLAUDE.md com novas regras
refactor: extrair lógica de scoring para service
```

### Pull requests

- Sempre para `develop`, nunca direto para `main`
- Descrever o que mudou e por quê
- Rodar testes antes de abrir PR
- Nunca aprovar PR que exponha credenciais

---

## Testes

### API C#

- Testes unitários nos Domain Services e Handlers (xUnit)
- Testes de integração nos Controllers (WebApplicationFactory + TestContainers)
- Mocks para Vertex AI e banco nos testes unitários
- Projeto Invest.Tests para unitários
- Projeto Invest.Tests.Integration para integração

### Batch Python

- Testes unitários em scoring e services (pytest)
- Mocks para yfinance, CVM e Vertex AI
- Testes de integração para jobs com banco de teste

### Frontend React

- Testes de componente com Testing Library
- Testes de hooks customizados
- Mocks para chamadas de API

---

## Quando estiver em dúvida

- Segurança primeiro — na dúvida, não expor dados
- Simples primeiro — na dúvida, usar a solução mais simples
- O app é sugestivo — nunca dar ordens de compra/venda como fato
- Consultar a documentação em `docs/` antes de tomar decisões de arquitetura
- Os percentuais de alocação são regras de negócio — não alterar sem aprovação
- Padrão CQRS: Commands e Queries são contratos (1 por arquivo), Handlers implementam a execução (1 por domínio)
- Domain-Driven Design: Domain não conhece EF Core, Vertex AI nem HTTP — se precisou importar algo externo, está na camada errada