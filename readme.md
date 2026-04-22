# Invest

Plataforma de investimentos que guia iniciantes do primeiro aporte até R$500k. Sugere alocação por perfil de risco, gera rankings de ativos com IA e monitora desvios da carteira com alertas automáticos.

## Stack

| Camada | Tecnologia |
|--------|-----------|
| API | C# .NET 8 — Clean Architecture + DDD + CQRS |
| Frontend | React 19 + TypeScript + Vite |
| Batch | Python 3.11 |
| Banco | PostgreSQL 15 |
| IA | Claude via Vertex AI (Google Cloud) |

---

## Estrutura do projeto

```
invest/
├── api/                    # Backend C# .NET 8
│   ├── Invest.Domain/      # Entidades, Value Objects, interfaces (sem dependências externas)
│   ├── Invest.Application/ # Use cases — Commands, Queries, Handlers, Validators
│   ├── Invest.Infrastructure/ # EF Core, repositórios, serviços externos
│   ├── Invest.API/         # Controllers, Middleware, Program.cs
│   ├── Invest.Tests/       # Testes unitários (xUnit)
│   └── Invest.Tests.Integration/ # Testes de integração
│
├── web/                    # Frontend React + TypeScript
│   └── src/
│       ├── components/     # ui/, charts/, layout/
│       ├── pages/          # auth/, onboarding/, portfolio/, dashboard/
│       ├── services/       # Clientes de API (profileService, rankingService…)
│       ├── hooks/          # useAuth, usePortfolio, useDashboard
│       └── contexts/       # AuthContext, OnboardingContext
│
├── batch/                  # Jobs Python
│   ├── jobs/               # rankings.py, alerts.py, market_data.py…
│   ├── services/           # yfinance, CVM, BCB, scoring, db
│   └── config.py
│
├── db/                     # schema.sql + seeds.sql
├── docker-compose.yml
└── .env.example
```

---

## Como rodar

### Docker Compose (recomendado)

```bash
cp .env.example .env
# Preencher .env com credenciais do GCP e senha do banco

docker compose up -d
```

| Serviço | URL |
|---------|-----|
| Web | http://localhost:5173 |
| API / Swagger | http://localhost:5000/swagger |
| PostgreSQL | localhost:5432 |

Para rodar os jobs do batch manualmente:

```bash
docker compose exec batch python jobs/rankings.py
docker compose exec batch python jobs/alerts.py
docker compose exec batch python jobs/run_all.py   # orquestrador (dias úteis)
```

---

### API (local)

```bash
cd api
dotnet restore
dotnet run --project Invest.API/Invest.API.csproj
```

### Web (local)

```bash
cd web
npm install
npm run dev
```

### Batch (local)

```bash
cd batch
pip install -r requirements.txt
python jobs/rankings.py
pytest tests/
```

---

## Variáveis de ambiente

Copie `.env.example` para `.env` e preencha:

```env
# PostgreSQL
POSTGRES_PASSWORD=sua-senha

# API C#
DATABASE_URL=Host=localhost;Database=invest_dev;Username=postgres;Password=sua-senha
JWT_SECRET=chave-secreta-minimo-32-caracteres
JWT_EXPIRATION=24h

# Google Cloud / Vertex AI
VERTEX_AI_PROJECT=seu-projeto-gcp
VERTEX_AI_LOCATION=us-central1

# Opcional
CORS_ORIGINS=http://localhost:5173
LOG_LEVEL=info
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
- **Ranking:** top 20 por sub-estratégia — score = 70% quantitativo (yfinance) + 30% qualitativo (Claude)
- **Desvio:** normal 0–3% | atenção 3–5% | extraordinário >5% (gera alerta imediato)
- **Rebalanceamento trimestral:** fevereiro, maio, agosto, novembro
- **O app é sugestivo** — nunca executa ordens de compra/venda

---

## Testes

```bash
# API — unitários
cd api && dotnet test Invest.Tests/

# API — integração
dotnet test Invest.Tests.Integration/

# Batch
cd batch && pytest tests/
```

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
