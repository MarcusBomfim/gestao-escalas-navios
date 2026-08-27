# Sistema de Gestão de Escalas de Navios

[![CI](https://github.com/MarcusBomfim/gestao-escalas-navios/actions/workflows/ci.yml/badge.svg)](https://github.com/MarcusBomfim/gestao-escalas-navios/actions/workflows/ci.yml)
[![Security](https://github.com/MarcusBomfim/gestao-escalas-navios/actions/workflows/security.yml/badge.svg)](https://github.com/MarcusBomfim/gestao-escalas-navios/actions/workflows/security.yml)
[![Performance](https://github.com/MarcusBomfim/gestao-escalas-navios/actions/workflows/performance.yml/badge.svg)](https://github.com/MarcusBomfim/gestao-escalas-navios/actions/workflows/performance.yml)

Plataforma para planejar, acompanhar e auditar escalas e operações de navios em terminais portuários. O projeto é desenvolvido em C# com ASP.NET Core, React com TypeScript e PostgreSQL.

## Situação do projeto

O projeto está na **Parte 21 — gestão administrativa de usuários concluída**. Administradores agora contam com uma interface para pesquisar, filtrar, cadastrar e atualizar usuários, perfis, organizações e situações de acesso. O backend impede autobloqueio e remoção do último administrador, protege edições concorrentes e invalida imediatamente sessões após mudanças de permissão.

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
└── compose.yaml
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

Antes de iniciar, substitua no `.env` os valores de `POSTGRES_PASSWORD`, `JWT_SIGNING_KEY` e `DEMO_USER_PASSWORD`. O arquivo não é versionado. A chave JWT precisa ter pelo menos 32 bytes e a senha demonstrativa deve cumprir a política exibida abaixo.

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

## Validação

```powershell
dotnet restore .\backend\PortManagement.slnx
dotnet build .\backend\PortManagement.slnx --no-restore
dotnet test .\backend\PortManagement.slnx --no-build
```

A suíte de backend possui 100 testes e cobre regras do número IMO, atualização de navios, transições de escala, compatibilidade e agenda de berços, histórico de reprogramação, sequência de marcos realizados, progresso de carga, avaliação de alertas, notificações e leituras por usuário, auditoria, proteção de CSV, correlação segura, métricas HTTP, indicadores consolidados, simulação determinística de posições, escopo organizacional, negação por padrão, concorrência otimista, casos de uso, idempotência, paginação, identidade, recuperação de senha, gestão de usuários, refresh tokens, resiliência, contrato OpenAPI, modelo de persistência e dependências arquiteturais.

Para validar a interface:

```powershell
cd .\frontend
npm.cmd run typecheck
npm.cmd run lint
npm.cmd run build
```

Com a aplicação completa em execução, os seis testes Playwright validam os fluxos de navegador, incluindo recuperação sem enumeração de contas, gestão administrativa, sessão, permissões e a identificação do mapa demonstrativo, e elevam o total para 106 testes automatizados:

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

Releases são deliberadas: somente uma tag `vX.Y.Z` publica as imagens `port-management-api` e `port-management-web` no GHCR. Consulte [CI/CD e segurança — Parte 14](docs/18-ci-cd-e-seguranca.md) antes de proteger a branch ou criar a primeira versão.

## Interface Web

Rotas disponíveis:

- `/` — apresentação pública do projeto.
- `/login` — seleção e autenticação de uma conta demonstrativa.
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
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/reset-password`
- `GET /api/v1/auth/me`
- `GET /api/v1/users`
- `GET /api/v1/users/options`
- `POST /api/v1/users`
- `PUT /api/v1/users/{id}`

Todas as rotas de negócio exigem autenticação. O cadastro de usuários, a auditoria e o diagnóstico detalhado exigem `Administrator`; navios, escalas e planejamento aceitam `Administrator` ou `Planner`; a execução operacional aceita `Administrator` ou `Operator`. Além do papel, escalas, janelas, eventos, cargas, alertas e posições aplicam o escopo organizacional. Recursos fora desse escopo se comportam como não encontrados. Os health checks públicos retornam somente estado e duração, sem detalhes internos. O papel `Viewer` permanece somente leitura.

Após o login, envie o `accessToken` no cabeçalho `Authorization: Bearer {token}`. O refresh token não aparece no JSON: ele é mantido em cookie `HttpOnly`, rotacionado a cada renovação e armazenado no PostgreSQL somente como hash SHA-256.

O seed cria quatro contas fictícias, todas com a senha definida por você em `DEMO_USER_PASSWORD`:

- `admin.demo@portmanagement.local`
- `planner.demo@portmanagement.local`
- `operator.demo@portmanagement.local`
- `viewer.demo@portmanagement.local`

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

Consulte [API — Parte 4](docs/08-api-parte-4.md) para filtros, exemplos de requisição, idempotência e tratamento de erros.
Consulte [Segurança — Parte 5](docs/09-autenticacao-e-autorizacao.md) para o fluxo de sessão, matriz de permissões e decisões de segurança.
Consulte [Interface — Parte 6](docs/10-interface-autenticada.md) para as rotas, estados de interface e estratégia de integração com a API.
Consulte [Fluxos operacionais — Parte 7](docs/11-fluxos-operacionais.md) para formulários, permissões, idempotência e transições de escala.
Consulte [Planejamento de atracação — Parte 8](docs/12-planejamento-atracacao.md) para compatibilidade, agenda, concorrência e proteção contra sobreposição.
Consulte [Execução operacional — Parte 9](docs/13-execucao-operacional.md) para marcos realizados, cargas, indicadores e integridade do fluxo.
Consulte [Torre de controle — Parte 10](docs/14-torre-de-controle.md) para indicadores, critérios de alerta e priorização operacional.
Consulte [Notificações em tempo real — Parte 11](docs/15-notificacoes-em-tempo-real.md) para SignalR, reconexão e estado de leitura.
Consulte [Auditoria e relatórios — Parte 12](docs/16-auditoria-e-relatorios.md) para captura transacional, minimização de dados e exportações CSV.
Consulte [Observabilidade e saúde — Parte 13](docs/17-observabilidade-e-saude.md) para correlação, logs, métricas e health checks.
Consulte [CI/CD e segurança — Parte 14](docs/18-ci-cd-e-seguranca.md) para pipelines, atualização de dependências, proteção da branch e releases no GHCR.
Consulte [Testes end-to-end — Parte 15](docs/19-testes-end-to-end.md) para cenários, execução local, evidências de falha e integração com Docker.
Consulte [Desempenho e resiliência — Parte 16](docs/20-desempenho-e-resiliencia.md) para perfis k6, limites de latência, timeouts, retry e desligamento gracioso.
Consulte [Contrato OpenAPI — Parte 17](docs/21-contrato-openapi.md) para versionamento, autenticação documentada, ambiente de exposição e testes de contrato.
Consulte [Mapa operacional simulado — Parte 18](docs/22-mapa-operacional-simulado.md) para o modelo de posições, atualização em tempo real, acessibilidade e limites da demonstração.
Consulte [Isolamento organizacional — Parte 19](docs/23-isolamento-organizacional.md) para claims, filtros, criação de escalas, SignalR e negação por padrão.
Consulte [Recuperação segura de senha — Parte 20](docs/24-recuperacao-segura-de-senha.md) para tokens temporários, envio SMTP, proteção contra enumeração e revogação de sessões.
Consulte [Gestão administrativa de usuários — Parte 21](docs/25-gestao-administrativa-usuarios.md) para perfis, organizações, bloqueio imediato, concorrência e proteção do último administrador.

## Objetivos

- Centralizar informações de navios, terminais, berços e escalas.
- Apoiar o planejamento de atracações e impedir conflitos de ocupação.
- Registrar previsões, planos e horários realizados sem apagar o histórico.
- Acompanhar operações de carga, atrasos e mudanças de situação.
- Oferecer rastreabilidade, controle de acesso e dados demonstrativos seguros.

## Documentação

- [Visão e escopo](docs/01-visao-e-escopo.md)
- [Usuários e permissões](docs/02-usuarios-e-permissoes.md)
- [Domínio e regras de negócio](docs/03-dominio-e-regras.md)
- [Requisitos](docs/04-requisitos.md)
- [Glossário portuário](docs/05-glossario-portuario.md)
- [Cenários de aceitação](docs/06-cenarios-de-aceitacao.md)
- [Modelo de dados](docs/07-modelo-de-dados.md)
- [API — Parte 4](docs/08-api-parte-4.md)
- [Segurança — Parte 5](docs/09-autenticacao-e-autorizacao.md)
- [Interface — Parte 6](docs/10-interface-autenticada.md)
- [Fluxos operacionais — Parte 7](docs/11-fluxos-operacionais.md)
- [Planejamento de atracação — Parte 8](docs/12-planejamento-atracacao.md)
- [Execução operacional — Parte 9](docs/13-execucao-operacional.md)
- [Torre de controle — Parte 10](docs/14-torre-de-controle.md)
- [Notificações em tempo real — Parte 11](docs/15-notificacoes-em-tempo-real.md)
- [Auditoria e relatórios — Parte 12](docs/16-auditoria-e-relatorios.md)
- [Observabilidade e saúde — Parte 13](docs/17-observabilidade-e-saude.md)
- [CI/CD e segurança — Parte 14](docs/18-ci-cd-e-seguranca.md)
- [Testes end-to-end — Parte 15](docs/19-testes-end-to-end.md)
- [Desempenho e resiliência — Parte 16](docs/20-desempenho-e-resiliencia.md)
- [Contrato OpenAPI — Parte 17](docs/21-contrato-openapi.md)
- [Mapa operacional simulado — Parte 18](docs/22-mapa-operacional-simulado.md)
- [Isolamento organizacional — Parte 19](docs/23-isolamento-organizacional.md)
- [Recuperação segura de senha — Parte 20](docs/24-recuperacao-segura-de-senha.md)
- [Gestão administrativa de usuários — Parte 21](docs/25-gestao-administrativa-usuarios.md)
- [Política de segurança](SECURITY.md)
- [ADR 001 — monólito modular](docs/decisions/ADR-001-monolito-modular.md)

## Referências de domínio

Os conceitos foram alinhados, quando aplicável, ao padrão de Port Call da DCSA, ao esquema de identificação de navios da IMO e à terminologia observada no Porto Sem Papel. O sistema será uma aplicação demonstrativa e não substituirá sistemas oficiais nem realizará anuências governamentais.

## Política de demonstração

Todos os registros disponibilizados publicamente serão fictícios. O repositório não deverá conter credenciais, documentos operacionais reais, dados pessoais, chaves de API ou informações pertencentes a empresas e autoridades portuárias.
