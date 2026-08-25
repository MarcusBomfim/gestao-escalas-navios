# Desempenho e resiliência — Parte 16

## Objetivo

Verificar continuamente os principais caminhos de leitura da plataforma e impedir que indisponibilidades transitórias, requisições sem limite ou encerramentos abruptos prejudiquem a operação.

Esta etapa cria uma referência técnica reproduzível. Os resultados obtidos em notebook ou runner compartilhado do GitHub não constituem dimensionamento de produção; capacidade real exige infraestrutura equivalente, volume representativo e monitoramento durante o ensaio.

## Proteções da API

A configuração padrão em `appsettings.json` define:

- timeout global de 30 segundos para requisições HTTP;
- cancelamento propagado pelos `CancellationToken` dos endpoints;
- timeout de comando do PostgreSQL em 30 segundos;
- até três novas tentativas para falhas transitórias de conexão, com atraso máximo de 5 segundos;
- até 30 segundos para o host encerrar tarefas em andamento;
- 35 segundos de tolerância do Docker Compose antes de finalizar o container da API.

O endpoint SignalR `/hubs/control-tower` desabilita o timeout global porque mantém conexões legítimas de longa duração. Falhas de regra de negócio, autorização e validação não são repetidas. A estratégia do Npgsql atua somente sobre condições reconhecidas como transitórias.

Os limites podem ser substituídos pelo mecanismo padrão de configuração do ASP.NET Core:

```powershell
$env:Resilience__RequestTimeoutSeconds = "30"
$env:Resilience__ShutdownTimeoutSeconds = "30"
$env:Resilience__Database__CommandTimeoutSeconds = "30"
$env:Resilience__Database__MaxRetryCount = "3"
$env:Resilience__Database__MaxRetryDelaySeconds = "5"
```

Valores fora das faixas seguras impedem a inicialização, tornando um erro de configuração visível antes de receber tráfego.

## Perfis k6

O arquivo `tests/performance/port-management.js` autentica uma única conta `Viewer` durante o `setup` e consulta, em lotes, quatro leituras representativas:

- torre de controle autenticada;
- primeira página de navios;
- primeira página de escalas;
- estrutura de portos, terminais e berços.

Nenhuma rota de escrita é usada, portanto os ensaios são repetíveis e não alteram a base demonstrativa.

### Smoke

Executa quatro iterações com um usuário virtual. Seu propósito é detectar falhas grosseiras de disponibilidade, autenticação ou regressões de latência com baixo custo. Ele faz parte do check obrigatório `End-to-end`.

### Carga controlada

Sobe gradualmente de zero para 5 e depois 15 usuários virtuais, mantém 15 usuários por 30 segundos e reduz a carga até zero. Como cada iteração dispara quatro leituras em paralelo, o perfil exercita concorrência sem simular volume de produção.

O workflow `Performance` roda semanalmente e pode ser iniciado manualmente. Use-o somente contra a aplicação isolada criada pelo próprio workflow ou outro ambiente que você tenha autorização para testar.

## Critérios de aprovação

O k6 encerra com falha quando qualquer limite abaixo não é atendido:

| Métrica | Limite |
|---|---:|
| verificações funcionais corretas | mais de 99% |
| requisições HTTP com falha | menos de 1% |
| latência global p95 | menos de 1.000 ms |
| latência global p99 | menos de 1.800 ms |
| páginas de navios e escalas p95 | menos de 500 ms |
| torre de controle p95 | menos de 1.200 ms |
| estrutura portuária p95 | menos de 600 ms |

Os limites por endpoint utilizam a tag estável `name`, evitando que parâmetros de URL criem séries diferentes.

## Execução local

Inicie a aplicação e confirme que todos os serviços estão saudáveis:

```powershell
docker compose up --build -d
docker compose ps
```

Em seguida, informe a mesma senha de `DEMO_USER_PASSWORD` usada no `.env` e execute o perfil desejado:

```powershell
$env:DEMO_USER_PASSWORD = "A_MESMA_SENHA_DO_ARQUIVO_ENV"
powershell -ExecutionPolicy Bypass -File .\scripts\run-performance-tests.ps1 -Profile smoke
powershell -ExecutionPolicy Bypass -File .\scripts\run-performance-tests.ps1 -Profile load
```

O script usa `host.docker.internal` no Windows para alcançar a API no host. Os resumos são gravados em `TestResults/performance` e permanecem fora do versionamento.

## Análise de uma falha

1. identifique qual threshold foi violado no resumo do k6;
2. compare a latência por endpoint, não apenas a média global;
3. correlacione o horário com `/api/v1/observability/summary` e os logs JSON da API;
4. verifique saturação de CPU, memória, conexões e consultas do PostgreSQL;
5. repita no mesmo ambiente depois da correção, sem apenas aumentar o limite.

Falha ocasional deve ser investigada. O pipeline não repete automaticamente o teste, evitando que uma segunda tentativa esconda instabilidade.
