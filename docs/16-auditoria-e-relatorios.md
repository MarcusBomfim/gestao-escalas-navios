# Auditoria e relatórios — Parte 12

Esta etapa acrescenta governança às operações demonstrativas. Alterações realizadas por usuários autenticados geram evidências consultáveis pelo administrador, e o estado atual da operação pode ser exportado para análise externa.

## Captura automática

O `AuditSaveChangesInterceptor` observa inclusões, atualizações e exclusões de entidades de domínio no momento em que o Entity Framework confirma a transação. Cada evidência registra:

- usuário e nome exibido no instante da ação;
- data e hora UTC;
- ação, tipo e identificador da entidade;
- nomes dos campos modificados;
- método e caminho da requisição;
- identificador de correlação da requisição.

O registro é incluído no mesmo `SaveChanges` da operação de negócio. Dessa forma, a evidência não é confirmada se a transação principal falhar.

## Minimização de dados

A auditoria não guarda valores anteriores ou posteriores, corpos de requisição, tokens, senhas, endereços IP ou cookies. Somente os nomes dos campos alterados são persistidos. Recibos de leitura das notificações também foram excluídos da captura para evitar ruído operacional.

## Acesso

As consultas e exportações exigem a política `ViewAuditReports`, exclusiva do papel `Administrator`. A proteção existe na API e também na rota visual `/auditoria`.

Rotas:

- `GET /api/v1/audit` — histórico paginado com filtros;
- `GET /api/v1/audit/export` — até 10 mil evidências em CSV;
- `GET /api/v1/reports/operations/export` — resumo e escalas monitoradas em CSV.

## Segurança das planilhas

Todos os campos do CSV são delimitados e escapados. Valores iniciados por `=`, `+`, `-`, `@`, tabulação ou retorno de carro recebem um prefixo neutro antes da exportação. Essa proteção reduz o risco de CSV Injection ao abrir o arquivo em editores de planilhas.

## Interface

A página administrativa apresenta:

- indicadores atuais da torre de controle;
- filtros por ação e entidade;
- histórico paginado com usuário, origem e campos modificados;
- exportação do relatório operacional;
- exportação da trilha de auditoria respeitando os filtros ativos.

## Banco de dados

A migration `AddAuditTrail` cria `port_management.audit_records` e índices por data, usuário e tipo de entidade. O histórico não possui exclusão automática nesta etapa; uma futura política de retenção deverá considerar os requisitos do ambiente em que o sistema for utilizado.

## Continuidade

A observabilidade técnica, as métricas da API e os health checks estão descritos em [Observabilidade e saúde — Parte 13](17-observabilidade-e-saude.md).
