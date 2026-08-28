# Finalização e release — Parte 25

## Resultado

A versão `1.0.0` encerra o ciclo planejado do projeto demonstrativo. O sistema possui fluxos completos de identidade, cadastro, planejamento, execução, acompanhamento, auditoria e administração, além de automação de qualidade e publicação.

## Quality gate local

Execute antes de commitar uma release:

```powershell
dotnet restore .\backend\PortManagement.slnx
dotnet format .\backend\PortManagement.slnx --verify-no-changes --no-restore
dotnet build .\backend\PortManagement.slnx --configuration Release --no-restore
dotnet test .\backend\PortManagement.slnx --configuration Release --no-build

Set-Location .\frontend
npm.cmd ci
npm.cmd run typecheck
npm.cmd run lint
npm.cmd run build
Set-Location ..

docker compose --env-file .env -f compose.yaml config --quiet
```

Com Docker em execução, rode também os testes Playwright e o smoke de desempenho conforme o README.

## Checklist da versão

- nenhum segredo ou dado operacional real está versionado;
- `dotnet ef migrations has-pending-model-changes` não aponta divergências;
- documentação, número de testes e changelog estão atualizados;
- CI, auditoria de dependências e CodeQL estão aprovados;
- imagens são construídas a partir do commit da `main`;
- dados demonstrativos continuam claramente identificados como fictícios;
- restauração de backup e retorno de versão foram ensaiados no ambiente de destino.

## Publicação da versão 1.0.0

Depois do commit final e do push para `main`:

```powershell
git tag -a v1.0.0 -m "release: versao 1.0.0"
git push origin v1.0.0
```

A tag dispara o workflow de release somente se pertencer à `main`. O workflow repete o quality gate e publica imagens com as tags `v1.0.0` e `latest`, incluindo SBOM e metadados de proveniência.

## Evoluções futuras

A versão está finalizada para portfólio; novas ideias devem entrar como versões posteriores, não como pendências da `1.0.0`. Exemplos: integração AIS real, notificações externas, anexos documentais e multi-porto em escala empresarial. Essas integrações exigiriam contratos, credenciais e dados que não fazem parte desta demonstração.
