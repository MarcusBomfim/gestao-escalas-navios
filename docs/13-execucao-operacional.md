# Execução operacional — Parte 9

Esta etapa transforma a escala planejada em uma operação acompanhável. Chegada, praticagem, atracação, carga, desatracação e saída passam a ser eventos realizados, preservados em uma linha do tempo auditável.

## Sequência de marcos

Cada marco possui uma situação de origem e uma situação de destino definida no backend:

| Marco | Situação de origem | Situação de destino |
| --- | --- | --- |
| Chegada ao fundeadouro | `Planned` | `AtAnchorage` |
| Início da praticagem | `AtAnchorage` | `ClearedForBerthing` |
| Atracação concluída | `ClearedForBerthing` | `Berthed` |
| Início da operação de carga | `Berthed` | `InOperation` |
| Operação de carga concluída | `InOperation` | `OperationCompleted` |
| Desatracação concluída | `OperationCompleted` | `Unberthed` |
| Saída do porto | `Unberthed` | `Closed` |

O registro do evento e a transição da escala são persistidos na mesma unidade de trabalho. O endpoint genérico de transições não aceita esses avanços, impedindo uma situação operacional sem o evento correspondente.

Eventos realizados não podem estar mais de cinco minutos no futuro nem anteceder o último evento da escala. A versão esperada da escala protege a gravação contra alterações concorrentes.

## Operações de carga

Uma carga registra:

- embarque, descarga ou movimento misto;
- descrição e quantidade planejada;
- unidade em toneladas, metros cúbicos, TEU ou unidades;
- período planejado e horários realizados;
- quantidade efetivamente movimentada;
- indicação e classificação de carga perigosa.

Antes do marco de início operacional, é obrigatório cadastrar ao menos uma carga. Antes do encerramento da operação, todas as cargas precisam estar concluídas. Cada carga possui seu próprio token de concorrência otimista.

## Indicadores

A consulta operacional calcula:

- permanência no porto, da chegada ao fundeadouro até a saída;
- tempo atracado, da atracação até a desatracação;
- duração da operação, do início ao encerramento da carga;
- totais planejados e realizados agrupados por unidade;
- produtividade realizada por hora e por unidade.

Enquanto uma fase está aberta, o horário atual é usado para apresentar uma duração em andamento. Unidades diferentes nunca são somadas entre si.

## Permissões

- `Administrator` e `Operator`: registram marcos e movimentações.
- `Planner`: continua responsável pelo planejamento de berço e consulta a execução.
- `Viewer`: acompanha eventos, cargas e indicadores em modo somente leitura.

A API é a autoridade das permissões. Ocultar controles no frontend serve apenas para melhorar a experiência.

## Dados demonstrativos

O seed avança uma escala fictícia até `InOperation`, registra quatro marcos realizados e cria duas operações de granéis em toneladas. Uma carga aparece concluída e outra em andamento. A inclusão é idempotente e não duplica dados quando o seed é executado novamente.

## Endpoints

- `GET /api/v1/operations/port-calls/{publicCode}`
- `POST /api/v1/operations/port-calls/{publicCode}/milestones`
- `POST /api/v1/operations/port-calls/{publicCode}/cargo-operations`
- `POST /api/v1/operations/port-calls/{publicCode}/cargo-operations/{id}/start`
- `POST /api/v1/operations/port-calls/{publicCode}/cargo-operations/{id}/complete`

## Persistência

A migration `AddOperationalExecutionConcurrency` adiciona o campo `version` em `cargo_operations`. O Entity Framework configura esse campo como token de concorrência e o incrementa a cada modificação.
