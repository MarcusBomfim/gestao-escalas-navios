# Planejamento de atracação — Parte 8

Esta etapa conecta escalas, navios, terminais e berços em um fluxo de planejamento protegido por regras de domínio e restrições do PostgreSQL.

## Fluxo operacional

1. A escala chega à situação `UnderReview`.
2. Um `Administrator` ou `Planner` escolhe um berço compatível e informa início e fim previstos.
3. A janela nasce como `Requested`.
4. A confirmação verifica novamente conflitos e altera a escala para `Planned` quando necessário.
5. Reprogramações preservam período e berço anteriores em uma revisão auditável.
6. Cancelamentos exigem justificativa e liberam a atribuição planejada da escala.

## Compatibilidade de berço

O método `Berth.CanReceive` é a regra oficial. O navio somente pode ser planejado quando:

- o berço está disponível;
- o comprimento total não supera o comprimento útil;
- a boca não supera o limite do berço;
- o calado máximo não supera a profundidade operacional configurada;
- o tipo do navio está entre os tipos suportados, quando houver restrição.

A interface antecipa essa análise e desabilita opções incompatíveis, mas o backend repete todas as verificações antes de persistir.

## Proteção contra conflitos

Existem três camadas complementares:

- consulta preventiva por interseção de períodos;
- restrição `EXCLUDE USING gist` no PostgreSQL para janelas `Confirmed` do mesmo berço;
- conversão da violação da restrição em resposta `409 Conflict` compreensível pela interface.

Os períodos usam o intervalo semiaberto `[início, fim)`. Assim, uma janela pode terminar exatamente quando a próxima começa sem representar sobreposição.

## Concorrência

`BerthWindow.Version` é um token de concorrência otimista incrementado a cada alteração. Reprogramar, confirmar ou cancelar exige a versão esperada. Uma escrita concorrente retorna `planning.version_conflict`, e o frontend atualiza janela, escala e agenda antes de permitir outra tentativa.

Também há um índice único parcial para impedir mais de uma janela `Requested` ou `Confirmed` por escala.

## Auditoria

Cada reprogramação registra:

- berço anterior e novo berço;
- início e fim anteriores;
- início e fim novos;
- responsável, data e justificativa.

A migration `AddBerthWindowConcurrency` preenche os identificadores de berço em revisões antigas antes de tornar as novas colunas obrigatórias.

## Agenda web

A rota `/agenda` consulta janelas por período e situação. Cada item exibe horários, terminal, berço, navio, escala e estado, com acesso direto aos detalhes da operação.

Na tela da escala, o painel de planejamento permite:

- solicitar a primeira janela;
- confirmar uma solicitação;
- trocar período ou berço com justificativa;
- cancelar a janela;
- consultar revisões anteriores.

## Dados demonstrativos

O seed adiciona duas janelas fictícias em berços diferentes. O processo continua idempotente: se já existirem janelas, nenhum registro adicional é criado.

## Limite desta etapa

A Parte 8 não registra horários efetivamente realizados, produtividade de carga ou eventos externos. Esses dados serão tratados no módulo de execução operacional.
