# Sistema de Gestão de Escalas de Navios

[![CI](https://github.com/MarcusBomfim/gestao-escalas-navios/actions/workflows/ci.yml/badge.svg)](https://github.com/MarcusBomfim/gestao-escalas-navios/actions/workflows/ci.yml)
[![Security](https://github.com/MarcusBomfim/gestao-escalas-navios/actions/workflows/security.yml/badge.svg)](https://github.com/MarcusBomfim/gestao-escalas-navios/actions/workflows/security.yml)
[![Performance](https://github.com/MarcusBomfim/gestao-escalas-navios/actions/workflows/performance.yml/badge.svg)](https://github.com/MarcusBomfim/gestao-escalas-navios/actions/workflows/performance.yml)

Plataforma para planejar, acompanhar e auditar escalas e operações de navios em terminais portuários. O projeto é desenvolvido em C# com ASP.NET Core, React com TypeScript e PostgreSQL.

## Situação do projeto

O projeto está **concluído na versão 1.0.0 — Parte 25**. A plataforma reúne identidade, isolamento organizacional, cadastros mestres, planejamento, execução, torre de controle, notificações, auditoria, observabilidade e publicação automatizada. Para apresentação de portfólio, visitantes podem entrar com um clique em uma conta global estritamente somente leitura, sem receber senhas ou permissões administrativas.

## Tecnologias

- C# 14 e .NET 10 LTS.
- ASP.NET Core Web API.
- ASP.NET Core SignalR.
- ASP.NET Core Health Checks e `System.Diagnostics.Metrics`.
- OpenAPI 3.1 e Scalar API Reference.
- ASP.NET Core Identity e autenticação JWT Bearer.
- Entity Framework Core 10 e Npgsql.
- React 19 e TypeScript 6.
- React Router e TanStack Query.
- SVG responsivo para visualização operacional sem dependência cartográfica externa.
- Vite 8.
- PostgreSQL 17.
- Docker e Docker Compose.
- Mailpit para captura local de e-mails de desenvolvimento.
- GitHub Actions, CodeQL e Dependabot.
- Playwright com Chromium.
- Grafana k6.
- xUnit v3 com Microsoft Testing Platform.

## Estrutura

```text
gestao-escalas-navios/
├── .github/
│   └── workflows/
├── backend/
│   ├── src/
│   │   ├── PortManagement.Api/
│   │   ├── PortManagement.Application/
│   │   ├── PortManagement.Domain/
│   │   └── PortManagement.Infrastructure/
│   └── tests/
│       ├── PortManagement.UnitTests/
│       ├── PortManagement.IntegrationTests/
│       └── PortManagement.ArchitectureTests/
├── frontend/
│   └── e2e/
├── infrastructure/
├── scripts/
├── tests/
│   └── performance/
├── docs/
├── CHANGELOG.md
├── compose.yaml
└── compose.production.yaml
```

As dependências seguem para dentro: `Api → Application → Domain`. A infraestrutura implementa persistência e integrações sem transferir detalhes tecnológicos para o domínio.

O banco usa o schema `port_management`, migrations versionadas e nomes em `snake_case`. A proteção contra janelas confirmadas sobrepostas é aplicada pelo próprio PostgreSQL por meio de uma restrição de exclusão.

## Requisitos locais

- .NET SDK 10.
- Node.js 24 ou versão LTS compatível.
- npm.
- Docker Desktop com Docker Compose.

Para conferir os requisitos no Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-prerequisites.ps1
```

## Execução sem Docker

### API

Primeiro, inicie o PostgreSQL e a caixa SMTP local, depois aplique a migration. Use no comando a mesma senha definida em seu arquivo `.env`:

```powershell
docker compose up -d postgres mailpit
dotnet tool restore
$env:PORT_MANAGEMENT_DB = "Host=localhost;Port=5432;Database=port_management;Username=port_management;Password=SUA_SENHA_LOCAL"
dotnet ef database update `
  --project .\backend\src\PortManagement.Infrastructure `
  --startup-project .\backend\src\PortManagement.Api
$env:ConnectionStrings__Database = $env:PORT_MANAGEMENT_DB
$env:Jwt__SigningKey = "SUA_CHAVE_LOCAL_COM_PELO_MENOS_32_BYTES"
$env:Demo__UserPassword = "SUA_SENHA_DEMO"
$env:Demo__PublicViewerEnabled = "true"
```

Depois, execute a API:

```powershell
dotnet restore .\backend\PortManagement.slnx
dotnet build .\backend\PortManagement.slnx
dotnet run --project .\backend\src\PortManagement.Api -- --seed-demo
dotnet run --project .\backend\src\PortManagement.Api
```

A API ficará disponível em `http://localhost:8080`. Os endpoints iniciais são:

- `GET /api/v1`
- `GET /health/live`
- `GET /health/ready`
- `GET /health`

### Interface

Em outro terminal:

```powershell
cd .\frontend
npm.cmd install
npm.cmd run typecheck
npm.cmd run lint
npm.cmd run dev
```

Use a URL local apresentada pelo Vite.

## Execução com Docker

Crie o arquivo local de ambiente:

```powershell
Copy-Item .env.example .env
notepad .env
docker compose up --build -d
docker compose ps
```

Antes de iniciar, substitua no `.env` os valores de `POSTGRES_PASSWORD`, `JWT_SIGNING_KEY` e `DEMO_USER_PASSWORD`. O arquivo não é versionado. A chave JWT precisa ter pelo menos 32 bytes e a senha demonstrativa deve cumprir a política exibida abaixo. `PUBLIC_DEMO_ENABLED=true` libera somente o acesso público do perfil `Viewer`.

Você pode gerar uma chave JWT local no PowerShell:

```powershell
[Convert]::ToBase64String(
  [Security.Cryptography.RandomNumberGenerator]::GetBytes(48)
)
```

Política de senha: mínimo de 12 caracteres, com letra maiúscula, letra minúscula, número, símbolo e pelo menos quatro caracteres diferentes. Os serviços `migrations` e `seed-demo` precisam terminar com o estado `Exited (0)` antes da API iniciar.

O Mailpit fica disponível em `http://localhost:8025`. Ele captura localmente os e-mails de recuperação e não entrega mensagens reais. Solicite a redefinição na tela de login e abra o link recebido nessa caixa.

Para acompanhar a inicialização:

```powershell
docker compose logs migrations
docker compose logs seed-demo
docker compose logs api
```

Na tela de login, **Entrar como visitante** abre a demonstração sem senha. Os demais perfis continuam exigindo a senha técnica definida em `DEMO_USER_PASSWORD`.

## Publicação em produção

A publicação usa imagens versionadas do GHCR, PostgreSQL privado, migrations automáticas e portas vinculadas somente ao host local para uso atrás de um proxy HTTPS:

```powershell
Copy-Item .env.production.example .env.production
notepad .env.production
docker compose --env-file .env.production -f compose.production.yaml config --quiet
docker compose --env-file .env.production -f compose.production.yaml up -d
```

Configure a variável `PUBLIC_API_URL` do repositório antes de criar uma tag, pois a URL é incorporada ao frontend durante o build. Consulte [Entrega e publicação](docs/entrega-e-publicacao.md) para TLS, domínios, atualização e retorno de versão.

## Validação

```powershell
dotnet restore .\backend\PortManagement.slnx
dotnet build .\backend\PortManagement.slnx --no-restore
dotnet test .\backend\PortManagement.slnx --no-build
```

A suíte de backend possui 137 testes e cobre regras do número IMO, atualização de navios, transições de escala, compatibilidade e agenda de berços, histórico de reprogramação, sequência de marcos realizados, progresso de carga, avaliação de alertas, notificações e leituras por usuário, auditoria, proteção de CSV, correlação segura, métricas HTTP, indicadores consolidados, simulação determinística de posições, escopo organizacional que falha fechado, elevação explícita para processos internos, aceitação de cabeçalhos de proxy apenas de origens confiáveis, partição do limite de tentativas por cliente real, cabeçalhos de segurança da interface, negação por padrão, concorrência otimista, casos de uso, idempotência, paginação, identidade, acesso público somente leitura, recuperação de senha, gestão de usuários, cadastros mestres, refresh tokens, resiliência, contrato OpenAPI, modelo de persistência e dependências arquiteturais.

Para validar a interface:

```powershell
cd .\frontend
npm.cmd run typecheck
npm.cmd run lint
npm.cmd run build
```

Com a aplicação completa em execução, os seis testes Playwright validam os fluxos de navegador, incluindo acesso público somente leitura, recuperação sem enumeração de contas, gestão administrativa de usuários e cadastros mestres, sessão, permissões e a identificação do mapa demonstrativo, e elevam o total para 143 testes automatizados:

```powershell
npx.cmd playwright install chromium
$env:DEMO_USER_PASSWORD = "A_MESMA_SENHA_DO_ARQUIVO_ENV"
npm.cmd run test:e2e
```

Para executar o smoke de desempenho contra o ambiente Docker:

```powershell
$env:DEMO_USER_PASSWORD = "A_MESMA_SENHA_DO_ARQUIVO_ENV"
powershell -ExecutionPolicy Bypass -File .\scripts\run-performance-tests.ps1 -Profile smoke
```

Use `-Profile load` somente em ambiente próprio e controlado. O resultado JSON é gravado em `TestResults/performance`, diretório ignorado pelo Git.

## Integração e entrega contínuas

Os workflows em `.github/workflows` executam build, testes, lint, auditoria de dependências, CodeQL, construção das imagens e smoke de desempenho em cada alteração da `main`. A carga controlada roda sob demanda e semanalmente. O Dependabot acompanha atualizações de NuGet, npm, Docker e GitHub Actions.

Releases são deliberadas: somente uma tag `vX.Y.Z` publica as imagens `port-management-api` e `port-management-web` no GHCR. A versão final planejada é `1.0.0`; consulte o [changelog](CHANGELOG.md) e o [changelog](CHANGELOG.md).

## Interface Web

Rotas disponíveis:

- `/` — apresentação pública do projeto.
- `/login` — acesso público somente leitura ou autenticação de uma conta técnica.
- `/recuperar-senha` — solicitação de um link temporário de recuperação.
- `/redefinir-senha` — definição de uma nova senha a partir do link recebido.
- `/painel` — torre de controle com mapa operacional simulado, indicadores, alertas priorizados e escalas monitoradas.
- `/navios` — consulta paginada e busca de navios.
- `/navios/novo` — cadastro de navio para `Administrator` e `Planner`.
- `/navios/{id}/editar` — atualização dos dados operacionais de um navio.
- `/escalas` — consulta de escalas com busca e filtro por situação.
- `/escalas/nova` — criação idempotente de uma escala.
- `/escalas/{publicCode}` — detalhes, planejamento, marcos realizados, cargas, indicadores e histórico.
- `/agenda` — agenda diária de janelas solicitadas e confirmadas por berço.
- `/usuarios` — gestão de contas, perfis, organizações e acessos para `Administrator`.
- `/cadastros` — gestão de organizações, portos, terminais e berços para `Administrator`.
- `/auditoria` — evidências, filtros e relatórios exclusivos do `Administrator`.
- `/observabilidade` — métricas e prontidão da instância para o `Administrator`.

As rotas operacionais exigem sessão válida. O access token permanece somente na memória do navegador; ao recarregar a aplicação, o cliente solicita uma nova sessão usando o cookie `HttpOnly`. Uma resposta `401` durante uma consulta provoca uma única tentativa de renovação e repetição segura da chamada original.

## API REST

No ambiente local, a especificação e a documentação interativa ficam disponíveis em:

- `http://localhost:8080/openapi/v1.json` — contrato OpenAPI 3.1;
- `http://localhost:8080/docs/` — referência interativa Scalar.

Essas duas rotas não são mapeadas em produção. A interface não contém credenciais predefinidas, persistência de token, fontes externas ou recursos de agente. Para testar uma rota protegida, faça login e informe manualmente o `accessToken` no esquema `Bearer`.

Principais rotas:

- `GET /api/v1/reference-data/ports`
- `GET` e `POST /api/v1/vessels`
- `PUT /api/v1/vessels/{id}`
- `GET` e `POST /api/v1/port-calls`
- `GET /api/v1/port-calls/{publicCode}`
- `POST /api/v1/port-calls/{publicCode}/transitions`
- `GET /api/v1/planning/berth-windows`
- `GET`, `POST` e `PUT /api/v1/planning/port-calls/{publicCode}/berth-window`
- `POST /api/v1/planning/port-calls/{publicCode}/berth-window/confirm`
- `POST /api/v1/planning/port-calls/{publicCode}/berth-window/cancel`
- `GET /api/v1/operations/port-calls/{publicCode}`
- `POST /api/v1/operations/port-calls/{publicCode}/milestones`
- `POST /api/v1/operations/port-calls/{publicCode}/cargo-operations`
- `POST /api/v1/operations/port-calls/{publicCode}/cargo-operations/{id}/start`
- `POST /api/v1/operations/port-calls/{publicCode}/cargo-operations/{id}/complete`
- `GET /api/v1/control-tower` — inclui o snapshot de tráfego simulado.
- `GET /api/v1/notifications`
- `POST /api/v1/notifications/{alertId}/read`
- `POST /api/v1/notifications/read-all`
- `/hubs/control-tower` — canal SignalR autenticado.
- `GET /api/v1/audit`
- `GET /api/v1/audit/export`
- `GET /api/v1/reports/operations/export`
- `GET /api/v1/observability/summary`
- `GET /health/live`
- `GET /health/ready`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/demo` — cria somente a sessão pública `Viewer` quando habilitada.
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/reset-password`
- `GET /api/v1/auth/me`
- `GET /api/v1/users`
- `GET /api/v1/users/options`
- `POST /api/v1/users`
- `PUT /api/v1/users/{id}`
- `GET` e `POST /api/v1/admin/master-data/organizations`
- `PUT /api/v1/admin/master-data/organizations/{id}`
- `GET` e `POST /api/v1/admin/master-data/ports`
- `PUT /api/v1/admin/master-data/ports/{id}`
- `POST /api/v1/admin/master-data/ports/{portId}/terminals`
- `PUT /api/v1/admin/master-data/terminals/{id}`
- `POST /api/v1/admin/master-data/terminals/{terminalId}/berths`
- `PUT /api/v1/admin/master-data/berths/{id}`

Todas as rotas de negócio exigem autenticação. O cadastro de usuários, os cadastros mestres, a auditoria e o diagnóstico detalhado exigem `Administrator`; navios, escalas e planejamento aceitam `Administrator` ou `Planner`; a execução operacional aceita `Administrator` ou `Operator`. Além do papel, escalas, janelas, eventos, cargas, alertas e posições aplicam o escopo organizacional. Recursos fora desse escopo se comportam como não encontrados. Os health checks públicos retornam somente estado e duração, sem detalhes internos. O papel `Viewer` permanece somente leitura.

Após o login, envie o `accessToken` no cabeçalho `Authorization: Bearer {token}`. O refresh token não aparece no JSON: ele é mantido em cookie `HttpOnly`, rotacionado a cada renovação e armazenado no PostgreSQL somente como hash SHA-256.

O seed cria quatro contas técnicas fictícias, todas com a senha definida por você em `DEMO_USER_PASSWORD`:

- `admin.demo@portmanagement.local`
- `planner.demo@portmanagement.local`
- `operator.demo@portmanagement.local`
- `viewer.demo@portmanagement.local`

O botão público não revela nem utiliza essa senha no navegador. A API só cria a sessão se a conta `viewer.demo` continuar ativa, sem organização, com escopo global e exclusivamente com o papel `Viewer`.

Exemplo de login no PowerShell:

```powershell
$body = @{
  email = "planner.demo@portmanagement.local"
  password = "SUA_SENHA_DEMO"
} | ConvertTo-Json

$session = Invoke-RestMethod `
  -Uri "http://localhost:8080/api/v1/auth/login" `
  -Method Post `
  -ContentType "application/json" `
  -Body $body `
  -SessionVariable PortSession

$headers = @{ Authorization = "Bearer $($session.accessToken)" }
Invoke-RestMethod `
  -Uri "http://localhost:8080/api/v1/auth/me" `
  -Headers $headers
```

Para filtros, idempotência e tratamento de erros, o contrato em `/openapi/v1.json` é a referência autoritativa.
Para o fluxo de sessão e a matriz de permissões, consulte [Segurança](docs/seguranca.md).
Para os fluxos operacionais completos, consulte [Operação portuária](docs/operacao-portuaria.md).

## Objetivos

- Centralizar informações de navios, terminais, berços e escalas.
- Apoiar o planejamento de atracações e impedir conflitos de ocupação.
- Registrar previsões, planos e horários realizados sem apagar o histórico.
- Acompanhar operações de carga, atrasos e mudanças de situação.
- Oferecer rastreabilidade, controle de acesso e dados demonstrativos seguros.

## Documentação

Organizada por assunto, não pela ordem em que foi construída.

**O problema e as regras**

- [Visão e escopo](docs/visao-e-escopo.md)
- [Domínio e regras de negócio](docs/dominio-e-regras.md)
- [Glossário portuário](docs/glossario-portuario.md)
- [Requisitos](docs/requisitos.md)
- [Cenários de aceitação](docs/cenarios-de-aceitacao.md)
- [Modelo de dados](docs/modelo-de-dados.md)

**Como o sistema funciona**

- [Operação portuária](docs/operacao-portuaria.md) — fluxos, planejamento de berço, execução, torre de controle, cadastros e auditoria
- [Segurança](docs/seguranca.md) — papéis, sessão, isolamento entre organizações e administração de contas
- [Observabilidade e resiliência](docs/observabilidade-e-resiliencia.md) — correlação, métricas, health checks e comportamento sob carga
- [Entrega e publicação](docs/entrega-e-publicacao.md) — pipelines, automação de segurança e preparação do ambiente

**Decisões**

- [ADR-001 — monólito modular](docs/decisions/ADR-001-monolito-modular.md)

**Referência gerada**

O contrato da API não é mantido à mão: `/openapi/v1.json` é gerado a partir do código e a referência interativa fica em `/docs/`.

- [Histórico de versões](CHANGELOG.md)
- [Política de segurança](SECURITY.md)
- [ADR 001 — monólito modular](docs/decisions/ADR-001-monolito-modular.md)

## Referências de domínio

Os conceitos foram alinhados, quando aplicável, ao padrão de Port Call da DCSA, ao esquema de identificação de navios da IMO e à terminologia observada no Porto Sem Papel. O sistema é uma aplicação demonstrativa e não substitui sistemas oficiais nem realiza anuências governamentais.

## Política de demonstração

Todos os registros disponibilizados publicamente são fictícios. O repositório não contém credenciais, documentos operacionais reais, dados pessoais, chaves de API ou informações pertencentes a empresas e autoridades portuárias.
