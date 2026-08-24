# CI/CD e segurança automatizada — Parte 14

## Objetivo

Impedir que alterações sem compilação, testes ou verificações de segurança cheguem à branch principal e criar uma entrega versionada e reproduzível das imagens da aplicação.

## Pipelines

### CI

O workflow `.github/workflows/ci.yml` é executado em pushes e pull requests destinados à `main`, além de aceitar execução manual. Ele usa somente permissões de leitura e cancela uma execução anterior da mesma referência quando uma alteração mais recente é enviada.

Verificações obrigatórias:

- `Backend`: restauração, `dotnet format`, build em `Release` e todos os testes;
- `Frontend`: instalação reproduzível com `npm ci`, verificação de tipos, lint e build;
- `Container images`: build real das imagens da API e da interface, executado somente após backend e frontend passarem.
- `End-to-end`: aplicação completa em Docker, banco isolado, seed demonstrativo e testes Playwright no Chromium.

### Segurança

O workflow `.github/workflows/security.yml` roda nos mesmos eventos, semanalmente e sob demanda:

- consulta advisories do NuGet, incluindo dependências transitivas;
- bloqueia vulnerabilidades npm de severidade alta ou crítica;
- executa CodeQL para C# e JavaScript/TypeScript com as consultas de segurança e qualidade;
- publica os resultados estáticos na área **Security > Code scanning** do GitHub.

O Dependabot acompanha NuGet, npm, Docker e as próprias GitHub Actions toda segunda-feira. Atualizações minor e patch de NuGet e npm são agrupadas para reduzir ruído, mas cada pull request continua sujeito a todo o pipeline.

As Actions usadas pelos workflows estão fixadas por SHA completo, evitando que uma tag mutável altere o código executado sem revisão. O comentário com a versão principal permite que o Dependabot continue propondo atualizações rastreáveis.

### Release

O workflow `.github/workflows/release.yml` não é executado em pushes comuns. Uma tag no formato `vX.Y.Z` publica duas imagens no GitHub Container Registry:

- `ghcr.io/marcusbomfim/port-management-api:vX.Y.Z`;
- `ghcr.io/marcusbomfim/port-management-web:vX.Y.Z`.

As mesmas imagens também recebem a tag `latest`. O workflow usa o `GITHUB_TOKEN` temporário e solicita apenas `contents: read` e `packages: write`; nenhuma senha permanente de registry é armazenada.

Antes da publicação, o release reutiliza todo o workflow de CI e confirma que o commit marcado pertence ao histórico da `main`. As imagens são enviadas com SBOM e atestação de proveniência geradas pelo Buildx.

Antes de publicar a interface para um ambiente externo, crie no repositório a variável `PUBLIC_API_URL` em **Settings > Secrets and variables > Actions > Variables**. Na ausência dela, a imagem usa `http://localhost:8080`, adequado somente ao ambiente local.

Para criar uma versão depois que a `main` estiver validada:

```powershell
git switch main
git pull --ff-only
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0
```

## Configuração recomendada no GitHub

Após enviar estes arquivos, configure uma ruleset para a branch `main` em **Settings > Rules > Rulesets**:

1. impeça exclusão e force push;
2. exija pull request antes do merge;
3. exija pelo menos uma aprovação quando houver outro colaborador;
4. descarte aprovações quando novos commits forem enviados;
5. exija que todas as conversas sejam resolvidas;
6. marque como obrigatórios `Backend`, `Frontend`, `Container images`, `End-to-end`, `Dependency audit`, `CodeQL (csharp)` e `CodeQL (javascript-typescript)`;
7. exija que a branch esteja atualizada antes do merge.

Os nomes dos checks aparecem para seleção depois da primeira execução dos workflows. Em um repositório mantido por uma única pessoa, mantenha a aprovação opcional até existir outro colaborador para não bloquear todos os merges.

Crie também uma ruleset de tags para o padrão `v*` que impeça atualização e exclusão das versões já publicadas. Restrinja a criação dessas tags aos responsáveis pelo release.

Em **Settings > Code security**, habilite também:

- Dependabot alerts e Dependabot security updates;
- secret scanning e push protection, quando disponíveis;
- private vulnerability reporting.

O CodeQL por workflow é uma configuração avançada. Não ative simultaneamente o setup padrão do CodeQL, pois isso duplicaria as análises.

## Política de contribuição e resposta

Todo pull request recebe um checklist para testes, documentação e proteção de dados. Relatos de vulnerabilidade devem seguir o arquivo `SECURITY.md` e nunca expor detalhes sensíveis em issues públicas.

## Limites desta etapa

A publicação no GHCR produz artefatos prontos para implantação, mas não seleciona uma infraestrutura de produção nem executa migrations em um banco externo. Essa decisão exige ambiente, domínio, TLS, gestão de segredos, backups e estratégia de rollback definidos.
