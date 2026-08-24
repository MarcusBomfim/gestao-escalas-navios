# Torre de controle — Parte 10

A torre de controle oferece uma leitura única da operação portuária. Em vez de exigir que o usuário abra cada escala, o backend consolida planejamento, eventos realizados e progresso de carga em indicadores e alertas acionáveis.

## Indicadores

O endpoint apresenta:

- total de escalas ainda ativas;
- navios atualmente em operação de carga;
- escalas que exigem atenção;
- quantidade de alertas críticos;
- berços ocupados e percentual de ocupação;
- aderência à programação das escalas com janela ativa.

A aderência considera como não conformes as escalas com atraso de chegada, janela pendente, excesso de ocupação, atraso de carga ou desvio relevante de atracação. Alertas de ausência de atualização são operacionais, mas não alteram esse percentual.

## Regras de alerta

| Alerta | Critério principal |
| --- | --- |
| Escala sem janela | Escala em análise ou planejada sem janela ativa |
| Confirmação pendente | Janela solicitada a menos de duas horas do início |
| Chegada atrasada | Janela confirmada iniciada há mais de uma hora sem chegada registrada |
| Excesso de berço | Navio continua atracado após o fim da janela |
| Carga atrasada | Movimentação aberta após seu término planejado |
| Desvio de atracação | Diferença de pelo menos 60 minutos entre início previsto e atracação |
| Atualização vencida | Fase operacional sem evento ou avanço de carga por mais de quatro horas |

As severidades são calculadas pelo tamanho do desvio. Atrasos maiores recebem prioridade crítica e aparecem primeiro na fila.

## Arquitetura

`ControlTowerRepository` monta um snapshot somente leitura com dados ativos. `ControlTowerEvaluator` aplica regras puras e determinísticas sobre esse snapshot. Essa separação permite testar os critérios sem banco de dados e evita persistir alertas derivados que ficariam desatualizados.

O endpoint autenticado é:

```text
GET /api/v1/control-tower
```

O frontend usa TanStack Query e atualiza a consulta automaticamente a cada 60 segundos. Alertas podem ser filtrados por criticidade e cada ocorrência oferece acesso direto à escala correspondente.

## Dados demonstrativos

O seed acrescenta uma quarta escala fictícia com janela solicitada e início vencido, gerando um alerta crítico compreensível. Ele também alinha a janela da escala em operação aos eventos realizados e completa a rastreabilidade da escala no fundeadouro.

Nenhum registro representa empresa, navio ou operação real.

## Continuidade

A [Parte 11 — notificações em tempo real](15-notificacoes-em-tempo-real.md) distribui as mudanças desta torre pelo SignalR e mantém o estado de leitura individual de cada alerta.
