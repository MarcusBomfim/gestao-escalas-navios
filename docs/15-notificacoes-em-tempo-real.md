# Notificações em tempo real — Parte 11

Esta etapa distribui mudanças operacionais para todos os usuários autenticados sem exigir atualização manual da página. O centro de notificações permanece disponível no cabeçalho de toda a área interna.

## Fluxo em tempo real

1. `ControlTowerBroadcastService` calcula um snapshot a cada 15 segundos.
2. Um fingerprint desconsidera o horário de geração e representa somente estados, alertas e atividades relevantes.
3. Quando o fingerprint muda, o backend publica `ControlTowerUpdated` pelo hub.
4. O frontend atualiza imediatamente o cache da torre e recarrega o centro de notificações.

O canal é:

```text
/hubs/control-tower
```

O hub exige o mesmo JWT Bearer da API. Para WebSockets, o cliente oficial do SignalR envia o access token durante a negociação. A aplicação mantém esse token apenas em memória.

## Resiliência

O cliente configura reconexão automática com intervalos progressivos. Se a conexão inicial falhar, tenta novamente a cada dez segundos. As consultas HTTP continuam sendo atualizadas a cada 60 segundos, portanto uma indisponibilidade temporária do hub não interrompe o painel.

O painel exibe quatro estados:

- conectando;
- tempo real ativo;
- reconectando;
- atualização periódica.

## Estado de leitura

A tabela `notification_receipts` armazena:

- usuário;
- identificador estável do alerta;
- data e hora da leitura.

Existe uma restrição única por usuário e alerta. A escrita usa `ON CONFLICT`, tornando “marcar como lida” idempotente mesmo com cliques repetidos ou requisições concorrentes. O recibo possui chave estrangeira para o usuário e é removido em cascata caso a conta seja excluída.

Nenhum texto sensível, token ou payload completo é persistido no recibo.

## Endpoints

- `GET /api/v1/notifications`
- `POST /api/v1/notifications/{alertId}/read`
- `POST /api/v1/notifications/read-all`

Todos exigem autenticação. Cada resposta combina os alertas atualmente ativos com os recibos do usuário conectado.

## Interface

O sino apresenta a quantidade de alertas não lidos. O painel lateral permite:

- consultar severidade, horário, navio e escala;
- abrir diretamente a escala relacionada;
- marcar uma notificação como lida;
- marcar todas as notificações ativas como lidas;
- acompanhar o estado da conexão em tempo real.

## Migration

`AddNotificationReceipts` cria a tabela, o índice único e a chave estrangeira para `identity.users`.

## Continuidade

A trilha de mudanças e as exportações administrativas estão descritas em [Auditoria e relatórios — Parte 12](16-auditoria-e-relatorios.md).
