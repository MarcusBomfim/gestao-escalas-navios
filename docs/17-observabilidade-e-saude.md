# Observabilidade e saúde — Parte 13

Esta etapa torna o comportamento técnico da aplicação mensurável e rastreável sem publicar detalhes internos. A instrumentação usa recursos nativos do .NET e mantém compatibilidade com uma futura coleta por OpenTelemetry.

## Correlação de requisições

O middleware aceita o cabeçalho `X-Correlation-ID` somente quando ele possui até 64 caracteres alfanuméricos, hífen, sublinhado ou ponto. Valores ausentes ou inseguros são substituídos por um identificador aleatório.

O identificador validado:

- é devolvido no cabeçalho da resposta;
- passa a ser o `TraceIdentifier` da requisição;
- é incluído nos logs estruturados;
- é reutilizado pela trilha de auditoria da operação.

Quando uma chamada da interface falha, essa correlação é apresentada como referência de suporte. O CORS expõe somente esse cabeçalho adicional ao cliente web.

Essa validação evita quebra de linhas e outros conteúdos inadequados nos cabeçalhos e logs.

## Logs estruturados

A API usa o console JSON do ASP.NET Core com horário UTC e escopos habilitados. O encerramento de cada requisição registra método, padrão da rota, status HTTP, duração e correlação. Query strings, corpos, cookies e tokens não são registrados.

## Métricas

`ApiTelemetry` publica instrumentos por `System.Diagnostics.Metrics`:

- quantidade de requisições;
- erros de cliente e de servidor;
- requisições ativas;
- histograma de duração.

Os rótulos usam o padrão da rota, nunca o caminho contendo identificadores. Isso controla a cardinalidade das métricas. A instância também conserva uma janela limitada às 512 medições mais recentes para o painel administrativo, além de acumuladores desde o início do processo.

As sondas `/health` não entram nos contadores da API e são registradas somente no nível `Debug`, evitando que verificações frequentes distorçam os indicadores ou gerem ruído excessivo nos logs.

## Health checks

- `GET /health/live` — confirma que o processo responde;
- `GET /health/ready` — confirma acesso ao PostgreSQL;
- `GET /health` — alias de prontidão para compatibilidade.

As respostas apresentam apenas estado, componente e duração. Exceções, servidores, credenciais e strings de conexão não são retornados. O Docker Compose usa `/health/ready` e somente inicia o frontend depois que a API está pronta.

## Diagnóstico administrativo

`GET /api/v1/observability/summary` exige a política `ViewObservability`, exclusiva do `Administrator`. A página `/observabilidade` atualiza a cada 15 segundos e apresenta:

- volume total e do último minuto;
- tempo médio e percentil 95;
- erros de servidor e taxa correspondente;
- tempo ativo e requisições em andamento;
- prontidão e duração da verificação dos componentes.

Os contadores representam a instância atual e reiniciam junto com o processo. Para múltiplas réplicas, a evolução adequada é conectar os instrumentos existentes a um coletor OpenTelemetry e a um backend de métricas.

## Continuidade

A automação de entrega, análise de segurança, imagens versionadas e regras de qualidade foi implementada na [Parte 14](18-ci-cd-e-seguranca.md).
