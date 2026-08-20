# Sistema de Gestão de Escalas de Navios

Plataforma para planejar, acompanhar e auditar escalas e operações de navios em terminais portuários. O projeto será desenvolvido em C# com ASP.NET Core, React com TypeScript e PostgreSQL.

## Situação do projeto

O projeto está na **Parte 1 — definição do domínio**. Nesta etapa foram definidos o escopo, os usuários, os requisitos, as regras de negócio e os principais cenários de aceitação. A estrutura da aplicação será criada na Parte 2.

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

