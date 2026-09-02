# Entrega e publicação

Do commit à imagem publicada: o que a automação verifica, o que interrompe um
release e como o ambiente de produção é preparado.

## Integração contínua e automação de segurança

### Objetivo

Impedir que alterações sem compilação, testes ou verificações de segurança cheguem à branch principal e criar uma entrega versionada e reproduzível das imagens da aplicação.

### Pipelines

#### CI

O workflow `.github/workflows/ci.yml` é executado em pushes e pull requests destinados à `main`, além de aceitar execução manual. Ele usa somente permissões de leitura e cancela uma execução anterior da mesma referência quando uma alteração mais recente é enviada.

Verificações obrigatórias:

- `Backend`: restauração, `dotnet format`, build em `Release` e todos os testes;
- `Frontend`: instalação reproduzível com `npm ci`, verificação de tipos, lint e build;
- `Container images`: build real das imagens da API e da interface, executado somente após backend e frontend passarem.
- `End-to-end`: aplicação completa em Docker, banco isolado, seed demonstrativo, testes Playwright no Chromium e smoke de desempenho com k6.

O workflow `.github/workflows/performance.yml` executa uma carga controlada aos domingos, às 08:17 UTC, e também pode ser iniciado manualmente em **Actions > Performance > Run workflow**. Ele usa dados sintéticos, credenciais efêmeras e um banco descartável; seus resultados não representam a capacidade de uma infraestrutura de produção.

#### Segurança

O workflow `.github/workflows/security.yml` roda nos mesmos eventos, semanalmente e sob demanda:

- consulta advisories do NuGet, incluindo dependências transitivas;
- bloqueia vulnerabilidades npm de severidade alta ou crítica;
- executa CodeQL para C# e JavaScript/TypeScript com as consultas de segurança e qualidade;
- publica os resultados estáticos na área **Security > Code scanning** do GitHub.

O Dependabot acompanha NuGet, npm, Docker e as próprias GitHub Actions toda segunda-feira. Atualizações minor e patch de NuGet e npm são agrupadas para reduzir ruído, mas cada pull request continua sujeito a todo o pipeline.

As Actions usadas pelos workflows estão fixadas por SHA completo, evitando que uma tag mutável altere o código executado sem revisão. O comentário com a versão principal permite que o Dependabot continue propondo atualizações rastreáveis.

#### Release

O workflow `.github/workflows/release.yml` não é executado em pushes comuns. Uma tag no formato `vX.Y.Z` publica duas imagens no GitHub Container Registry:

- `ghcr.io/marcusbomfim/port-management-api:vX.Y.Z`;
- `ghcr.io/marcusbomfim/port-management-web:vX.Y.Z`.

As mesmas imagens também recebem a tag `latest`. O workflow usa o `GITHUB_TOKEN` temporário e solicita apenas `contents: read` e `packages: write`; nenhuma senha permanente de registry é armazenada.

Antes da publicação, o release reutiliza todo o workflow de CI e confirma que o commit marcado pertence ao histórico da `main`. As imagens são construídas localmente, varridas com o Trivy e só então publicadas: uma vulnerabilidade alta ou crítica com correção disponível interrompe o release antes que a tag chegue ao registro. As imagens publicadas levam SBOM e atestação de proveniência geradas pelo Buildx.

Antes de publicar a interface para um ambiente externo, crie no repositório a variável `PUBLIC_API_URL` em **Settings > Secrets and variables > Actions > Variables**. Na ausência dela, a imagem usa `http://localhost:8080`, adequado somente ao ambiente local.

Para criar uma versão depois que a `main` estiver validada:

```powershell
git switch main
git pull --ff-only
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0
```

### Configuração recomendada no GitHub

Após enviar estes arquivos, configure uma ruleset para a branch `main` em **Settings > Rules > Rulesets**:

1. impeça exclusão e force push;
2. exija pull request antes do merge;
3. exija pelo menos uma aprovação quando houver outro colaborador;
4. descarte aprovações quando novos commits forem enviados;
5. exija que todas as conversas sejam resolvidas;
6. marque como obrigatórios `Backend`, `Frontend`, `Container images`, `End-to-end`, `Dependency audit`, `CodeQL (csharp)` e `CodeQL (javascript-typescript)`; a carga semanal não deve bloquear pull requests;
7. exija que a branch esteja atualizada antes do merge.

Os nomes dos checks aparecem para seleção depois da primeira execução dos workflows. Em um repositório mantido por uma única pessoa, mantenha a aprovação opcional até existir outro colaborador para não bloquear todos os merges.

Crie também uma ruleset de tags para o padrão `v*` que impeça atualização e exclusão das versões já publicadas. Restrinja a criação dessas tags aos responsáveis pelo release.

Em **Settings > Code security**, habilite também:

- Dependabot alerts e Dependabot security updates;
- secret scanning e push protection, quando disponíveis;
- private vulnerability reporting.

O CodeQL por workflow é uma configuração avançada. Não ative simultaneamente o setup padrão do CodeQL, pois isso duplicaria as análises.

### Política de contribuição e resposta

Todo pull request recebe um checklist para testes, documentação e proteção de dados. Relatos de vulnerabilidade devem seguir o arquivo `SECURITY.md` e nunca expor detalhes sensíveis em issues públicas.

## Publicação em produção

### Estratégia

O projeto publica duas imagens no GitHub Container Registry quando uma tag semântica `vX.Y.Z` é criada:

- `ghcr.io/marcusbomfim/port-management-api`;
- `ghcr.io/marcusbomfim/port-management-web`.

O arquivo `compose.production.yaml` executa essas imagens, o PostgreSQL, as migrations e o seed demonstrativo. Banco e serviços ficam vinculados apenas à máquina local; um proxy reverso externo deve publicar os domínios com HTTPS.

### Preparação no GitHub

1. Em **Settings > Actions > Variables**, configure `PUBLIC_API_URL` com a URL HTTPS pública da API.
2. Confirme que os packages do GHCR poderão ser lidos pelo servidor de destino.
3. Aguarde os workflows **CI** e **Security** concluírem sem falhas na branch `main`.
4. Crie a tag somente depois do quality gate.

A URL da API é incorporada ao bundle do frontend durante a construção da imagem. Portanto, ela precisa estar correta antes da tag.

### Preparação do servidor

```powershell
Copy-Item .env.production.example .env.production
notepad .env.production
```

Substitua todos os valores `replace-with-*`, informe os domínios reais e configure o SMTP. Gere segredos independentes para PostgreSQL, JWT e contas demonstrativas. Não envie `.env.production` ao Git.

Valide a configuração:

```powershell
docker compose --env-file .env.production -f compose.production.yaml config --quiet
```

Inicie os serviços:

```powershell
docker compose --env-file .env.production -f compose.production.yaml pull
docker compose --env-file .env.production -f compose.production.yaml up -d
docker compose --env-file .env.production -f compose.production.yaml ps
```

Confira as tarefas de inicialização e a saúde da API:

```powershell
docker compose --env-file .env.production -f compose.production.yaml logs migrations
docker compose --env-file .env.production -f compose.production.yaml logs seed-demo
Invoke-RestMethod http://127.0.0.1:8080/health/ready
```

### Proxy reverso e TLS

O proxy reverso deve:

- redirecionar HTTP para HTTPS;
- encaminhar o domínio da interface para `127.0.0.1:5173`;
- encaminhar o domínio da API e WebSocket para `127.0.0.1:8080`;
- preservar `Host`, `X-Forwarded-For` e `X-Forwarded-Proto`;
- permitir upgrade de WebSocket em `/hubs/control-tower`;
- renovar automaticamente o certificado TLS.

`Security:EnforceHttps` permanece desativado no container porque o TLS termina no proxy. O redirecionamento obrigatório deve acontecer no proxy público. Nunca publique diretamente as portas locais do PostgreSQL ou da API na internet.

### Atualização e retorno

Para atualizar, altere `IMAGE_TAG` para uma versão já publicada, faça `pull` e execute `up -d`. Para retornar, restaure a tag anterior e repita os mesmos comandos. As migrations são progressivas; antes de cada release material, mantenha um backup testado do volume PostgreSQL.

### Limites da demonstração

Esta configuração é adequada para apresentação de portfólio com dados sintéticos. Uso empresarial exigiria gestão externa de segredos, backup automatizado, retenção de auditoria, monitoramento centralizado, política de disponibilidade e avaliação jurídica e operacional própria.
