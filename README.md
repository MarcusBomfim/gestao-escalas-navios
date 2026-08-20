# Sistema de Gestão de Escalas de Navios

Plataforma para planejar, acompanhar e auditar escalas e operações de navios em terminais portuários. O projeto será desenvolvido em C# com ASP.NET Core, React com TypeScript e PostgreSQL.

## Situação do projeto

O projeto está na **Parte 2 — estrutura inicial**. O domínio e as regras foram documentados, a solução .NET foi dividida em camadas, o front-end React foi iniciado e o ambiente Docker foi preparado. A modelagem e a persistência no PostgreSQL serão implementadas na Parte 3.

## Tecnologias

- C# 14 e .NET 10 LTS.
- ASP.NET Core Web API.
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

As dependências seguem para dentro: `Api → Application → Domain`. A infraestrutura implementará contratos da aplicação sem transferir detalhes de banco e integração para o domínio.

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

```powershell
dotnet restore .\backend\PortManagement.slnx
dotnet build .\backend\PortManagement.slnx
dotnet run --project .\backend\src\PortManagement.Api
```

A API ficará disponível em `http://localhost:8080`. Os endpoints iniciais são:

- `GET /api/v1`
- `GET /health`

### Interface

Em outro terminal:

```powershell
cd .\frontend
npm.cmd install
npm.cmd run dev
```

Use a URL local apresentada pelo Vite.

## Execução com Docker

Crie o arquivo local de ambiente e altere a senha antes de iniciar:

```powershell
Copy-Item .env.example .env
docker compose up --build
```

O arquivo `.env` não é versionado.

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
- [ADR 001 — monólito modular](docs/decisions/ADR-001-monolito-modular.md)

## Referências de domínio

Os conceitos foram alinhados, quando aplicável, ao padrão de Port Call da DCSA, ao esquema de identificação de navios da IMO e à terminologia observada no Porto Sem Papel. O sistema será uma aplicação demonstrativa e não substituirá sistemas oficiais nem realizará anuências governamentais.

## Política de demonstração

Todos os registros disponibilizados publicamente serão fictícios. O repositório não deverá conter credenciais, documentos operacionais reais, dados pessoais, chaves de API ou informações pertencentes a empresas e autoridades portuárias.
