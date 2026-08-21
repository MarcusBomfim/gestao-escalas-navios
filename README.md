# Sistema de Gestão de Escalas de Navios

Plataforma para planejar, acompanhar e auditar escalas e operações de navios em terminais portuários. O projeto será desenvolvido em C# com ASP.NET Core, React com TypeScript e PostgreSQL.

## Situação do projeto

O projeto está na **Parte 5 — autenticação e autorização concluídas**. A API possui identidade persistida, sessões JWT, refresh token rotativo em cookie seguro e controle de acesso por papéis. As consultas demonstrativas continuam públicas; operações que alteram dados exigem autenticação e a permissão adequada.

## Tecnologias

- C# 14 e .NET 10 LTS.
- ASP.NET Core Web API.
- ASP.NET Core Identity e autenticação JWT Bearer.
- Entity Framework Core 10 e Npgsql.
- React 19 e TypeScript 6.
- Vite 8.
- PostgreSQL 17.
- Docker e Docker Compose.
- xUnit v3 com Microsoft Testing Platform.

## Estrutura

```text
gestao-escalas-navios/
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
├── infrastructure/
├── scripts/
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

Primeiro, inicie o PostgreSQL e aplique a migration. Use no comando a mesma senha definida em seu arquivo `.env`:

```powershell
docker compose up -d postgres
dotnet tool restore
$env:PORT_MANAGEMENT_DB = "Host=localhost;Port=5432;Database=port_management;Username=port_management;Password=SUA_SENHA_LOCAL"
dotnet ef database update `
  --project .\backend\src\PortManagement.Infrastructure `
  --startup-project .\backend\src\PortManagement.Api
$env:ConnectionStrings__Database = $env:PORT_MANAGEMENT_DB
$env:Jwt__SigningKey = "SUA_CHAVE_LOCAL_COM_PELO_MENOS_32_BYTES"
```

Depois, execute a API:

```powershell
dotnet restore .\backend\PortManagement.slnx
dotnet build .\backend\PortManagement.slnx
dotnet run --project .\backend\src\PortManagement.Api
```

A API ficará disponível em `http://localhost:8080`. Os endpoints iniciais são:

- `GET /api/v1`
- `GET /health`
- `GET /health/database`

### Interface

Em outro terminal:

```powershell
cd .\frontend
npm.cmd install
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

A suíte atual possui 30 testes e cobre regras do número IMO, transições de escala, compatibilidade de berço, histórico de reprogramação, casos de uso, idempotência, paginação, identidade, refresh tokens, modelo de persistência e dependências arquiteturais.

## API REST

Principais rotas:

- `GET /api/v1/reference-data/ports`
- `GET` e `POST /api/v1/vessels`
- `GET` e `POST /api/v1/port-calls`
- `GET /api/v1/port-calls/{publicCode}`
- `POST /api/v1/port-calls/{publicCode}/transitions`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `GET /api/v1/auth/me`
- `POST /api/v1/users`

As rotas `GET` de dados demonstrativos são públicas. O cadastro de usuários exige `Administrator`; o cadastro de navios e escalas aceita `Administrator` ou `Planner`; transições de situação também aceitam `Operator`. O papel `Viewer` permanece somente leitura.

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
- [ADR 001 — monólito modular](docs/decisions/ADR-001-monolito-modular.md)

## Referências de domínio

Os conceitos foram alinhados, quando aplicável, ao padrão de Port Call da DCSA, ao esquema de identificação de navios da IMO e à terminologia observada no Porto Sem Papel. O sistema será uma aplicação demonstrativa e não substituirá sistemas oficiais nem realizará anuências governamentais.

## Política de demonstração

Todos os registros disponibilizados publicamente serão fictícios. O repositório não deverá conter credenciais, documentos operacionais reais, dados pessoais, chaves de API ou informações pertencentes a empresas e autoridades portuárias.
