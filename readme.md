# Invest

Plataforma de investimentos que guia iniciantes do primeiro aporte até R$500k. Sugere alocação por perfil de risco, gera rankings de ativos com IA e monitora desvios da carteira com alertas automáticos.

## Stack

| Camada | Tecnologia |
|--------|-----------|
| API | C# .NET 8 — Clean Architecture + DDD + CQRS |
| Frontend | React 19 + TypeScript + Vite |
| Batch | Python 3.11 |
| Banco | PostgreSQL 15 |
| IA | Gemini via Vertex AI (GCP) ou Google AI Studio |

---

## Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado e rodando
- [Google Cloud CLI](https://cloud.google.com/sdk/docs/install) — necessário apenas na Opção B (Vertex AI)

---

## Configuração inicial

### 1. Copie o arquivo de variáveis de ambiente

```bash
cp .env.example .env
```

Preencha os valores obrigatórios no `.env`:

```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=uma-senha-segura

JWT_SECRET=uma-chave-com-no-minimo-32-caracteres
```

### 2. Configure o Gemini — escolha uma opção

#### Opção A — Google AI Studio (mais simples, gratuito)

1. Gere uma chave em [aistudio.google.com/apikey](https://aistudio.google.com/apikey)
2. Adicione ao `.env`:

```env
GEMINI_API_KEY=sua-chave-aqui
VERTEX_AI_MODEL=gemini-2.5-flash
```

> Quando `GEMINI_API_KEY` está definida, as variáveis `VERTEX_AI_PROJECT` e `VERTEX_AI_LOCATION` são ignoradas.

#### Opção B — Vertex AI no GCP

1. No [Console GCP](https://console.cloud.google.com), ative a API **Vertex AI** no seu projeto
2. Autentique localmente:

```bash
gcloud auth application-default login
```

3. Adicione ao `.env`:

```env
VERTEX_AI_PROJECT=id-do-seu-projeto-gcp
VERTEX_AI_LOCATION=us-central1
VERTEX_AI_MODEL=gemini-2.5-flash
```

> O `docker-compose.yml` monta `~/.config/gcloud` dentro dos containers automaticamente — o ADC funciona sem variável de chave adicional.

### 3. Verifique a conexão com o Gemini

```bash
docker compose run --rm batch python jobs/test_vertex_ai.py
```

Uma resposta do modelo confirma que a configuração está correta.

---

## Subindo o ambiente

```bash
docker compose up --build
```

| Serviço | URL |
|---------|-----|
| Frontend | http://localhost:5173 |
| API / Swagger | http://localhost:5000/swagger |
| PostgreSQL | localhost:5432 |

Para rodar em background:

```bash
docker compose up --build -d
```

Para parar:

```bash
docker compose down
```

---

## Testes

### Testes unitários da API (C#)

```bash
docker build -f api/Dockerfile.test -t invest_tests ./api
docker run --rm invest_tests
```

> O `docker build` compila os testes na imagem. O `docker run` apenas executa — rápido e sem recompilar.

### Testes unitários do batch (Python)

```bash
docker compose run --rm batch python -m pytest tests/ -v
```

---

## Batch jobs

O container `batch` fica em `sleep infinity` — os jobs são executados via `exec` ou `run`.

### Subir o container de batch

```bash
docker compose --profile batch up -d batch
```

### Rodar todos os jobs em sequência

```bash
docker compose exec batch python jobs/run_all.py
```

### Rodar um job individualmente

```bash
docker compose exec batch python jobs/benchmarks.py
docker compose exec batch python jobs/market_data.py
docker compose exec batch python jobs/rankings.py
docker compose exec batch python jobs/portfolio_history.py
docker compose exec batch python jobs/alerts.py
```

### Rodar o batch sem deixar o container ativo

```bash
docker compose run --rm --profile batch batch python jobs/run_all.py
```

---

## Estrutura do projeto

```
invest/
├── api/                       # Backend C# .NET 8
│   ├── Invest.Domain/         # Entidades, Value Objects, interfaces (sem dependências externas)
│   ├── Invest.Application/    # Use cases — Commands, Queries, Handlers, Validators
│   ├── Invest.Infrastructure/ # EF Core, repositórios, serviços externos
│   ├── Invest.API/            # Controllers, Middleware, Program.cs
│   └── Invest.Tests/          # Testes unitários (xUnit)
│
├── web/                       # Frontend React + TypeScript
│   └── src/
│       ├── components/        # ui/, charts/, layout/
│       ├── pages/             # auth/, onboarding/, portfolio/, dashboard/
│       ├── services/          # Clientes de API (profileService, rankingService…)
│       ├── hooks/             # useAuth, usePortfolio, useDashboard
│       └── contexts/          # AuthContext, OnboardingContext
│
├── batch/                     # Jobs Python
│   ├── jobs/                  # rankings.py, alerts.py, market_data.py…
│   ├── services/              # yfinance, CVM, BCB, scoring, db
│   └── tests/                 # pytest
│
├── db/                        # schema.sql + seeds.sql
├── docker-compose.yml
└── .env.example
```

---

## Endpoints principais

### Auth (público)
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/auth/register` | Cadastro |
| POST | `/auth/login` | Login (retorna JWT) |
| POST | `/auth/refresh` | Renovar token |
| POST | `/auth/forgot-password` | Solicitar reset de senha |
| POST | `/auth/reset-password` | Confirmar reset de senha |

### Usuário / Perfil (JWT obrigatório)
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/users/me` | Dados do usuário |
| PUT | `/users/me` | Atualizar usuário |
| POST | `/profile` | Criar perfil de investimento |
| GET | `/profile` | Buscar perfil |
| PUT | `/profile` | Atualizar perfil |

### Carteira
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/portfolio/import/b3` | Importar extrato B3 (Excel) |
| GET | `/portfolio/assets` | Listar ativos |
| POST | `/portfolio/compare` | Comparar carteira atual vs. sugerida |

### Ranking / IA
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/allocation` | Tabela de alocação por perfil e faixa |
| GET | `/ranking/top20` | Top 20 ativos |
| GET | `/ranking/suggestion` | Sugestão personalizada |
| POST | `/chat` | Chat com assistente IA |

### Dashboard / Alertas
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/dashboard` | Resumo da carteira |
| GET | `/dashboard/history` | Histórico de patrimônio |
| GET | `/alerts` | Alertas do usuário |
| POST | `/alerts/{id}/read` | Marcar alerta como lido |

---

## Regras de negócio relevantes

- **Alocação:** tabela fixa por perfil (conservador / moderado / arrojado) e faixa de patrimônio (até R$10k / R$10k–R$100k / acima de R$100k)
- **Ranking:** top 20 por sub-estratégia — score = 70% quantitativo (yfinance) + 30% qualitativo (Gemini)
- **Desvio:** normal 0–3% | atenção 3–5% | extraordinário >5% (gera alerta imediato)
- **Rebalanceamento trimestral:** fevereiro, maio, agosto, novembro
- **O app é sugestivo** — nunca executa ordens de compra/venda

---

## Git

```
main        → produção (sempre estável)
develop     → integração
feature/xxx → novas funcionalidades
fix/xxx     → correções
hotfix/xxx  → correções urgentes em produção
```

PRs sempre para `develop`. Usar [Conventional Commits](https://www.conventionalcommits.org/).
