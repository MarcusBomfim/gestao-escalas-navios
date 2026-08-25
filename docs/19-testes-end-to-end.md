# Testes end-to-end — Parte 15

## Objetivo

Validar os fluxos críticos no navegador contra a aplicação completa, incluindo React, API ASP.NET Core, PostgreSQL, migrations, seed demonstrativo, cookies e autorização por perfil.

Os testes unitários e de integração continuam responsáveis por regras isoladas e combinações de domínio. O Playwright cobre apenas jornadas de alto valor, reduzindo duplicação, tempo de execução e fragilidade.

## Cenários cobertos

Os quatro testes em `frontend/e2e/access-and-navigation.spec.ts` verificam:

1. apresentação pública, chamada para acesso e aviso de dados fictícios;
2. redirecionamento de uma rota protegida, retorno ao destino após login, permissões de Planejador, restauração da sessão por cookie e logout;
3. rejeição de credenciais inválidas sem criação de sessão;
4. navegação do Visitante em modo somente leitura, sem ações de cadastro ou áreas administrativas.

Os seletores usam papéis, labels e nomes acessíveis. Classes CSS, posições e detalhes visuais não são usados como contrato de teste.

## Organização

```text
frontend/
├── e2e/
│   ├── support/
│   │   └── authentication.ts
│   └── access-and-navigation.spec.ts
├── playwright.config.ts
└── tsconfig.e2e.json
```

O projeto roda somente no Chromium para manter o quality gate rápido. Em caso de falha são preservados trace, screenshot e vídeo; localmente, o relatório HTML pode ser aberto com `npm.cmd run test:e2e:report`.

## Execução local

Inicie a aplicação completa com Docker usando seu `.env`:

```powershell
docker compose up --build -d
docker compose ps
```

Depois, em outro terminal, informe ao Playwright a mesma senha definida em `DEMO_USER_PASSWORD`:

```powershell
cd .\frontend
npm.cmd install
npx.cmd playwright install chromium
$env:DEMO_USER_PASSWORD = "A_MESMA_SENHA_DO_ARQUIVO_ENV"
npm.cmd run test:e2e
```

Para acompanhar o navegador durante o teste:

```powershell
npm.cmd run test:e2e:headed
```

Não registre a senha no código, no `package.json` ou em arquivos versionados.

## Execução no CI

O job `End-to-end` do workflow `CI`:

1. instala as dependências de forma reproduzível com `npm ci`;
2. instala somente o Chromium e suas dependências do sistema;
3. gera senhas e chave JWT aleatórias e mascaradas para aquela execução;
4. sobe todos os serviços com Docker Compose;
5. aguarda `/health/ready` na API e `/health` na interface;
6. executa os quatro testes;
7. executa o smoke de desempenho da Parte 16 contra a mesma API isolada;
8. em falhas, publica relatório, traces, mídia e logs como artefato privado da execução;
9. remove containers e volume do banco mesmo se algum passo falhar.

O ambiente não usa credenciais permanentes, dados reais ou o banco de desenvolvimento do usuário.

## Critério de aprovação

Uma alteração não deve ser integrada à `main` quando o check `End-to-end` falhar. A correção deve considerar primeiro o trace e a requisição de API registrada pelo Playwright e, depois, os logs de `migrations`, `seed-demo`, `api` e `web` presentes no artefato.

O CI não repete automaticamente um cenário reprovado. Essa escolha impede que uma segunda tentativa esconda instabilidade e preserva o comportamento real do limitador de autenticação; testes intermitentes precisam ser tratados como defeitos.
