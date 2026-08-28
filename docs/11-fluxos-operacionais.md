# Fluxos operacionais — Parte 7

Esta etapa acrescenta comandos de escrita à interface autenticada e completa a atualização dos dados de navios no backend. As decisões priorizam validação de domínio, autorização na API, prevenção de duplicidade e rastreabilidade.

## Funcionalidades

| Fluxo | Rota web | Papéis autorizados |
| --- | --- | --- |
| Cadastrar navio | `/navios/novo` | `Administrator`, `Planner` |
| Editar navio | `/navios/{id}/editar` | `Administrator`, `Planner` |
| Registrar escala | `/escalas/nova` | `Administrator`, `Planner` |
| Consultar escala | `/escalas/{publicCode}` | Qualquer usuário autenticado |
| Alterar situação | `/escalas/{publicCode}` | `Administrator`, `Planner`, `Operator` |

O papel `Viewer` pode acompanhar dados e histórico, mas não recebe controles de escrita. A ocultação de controles melhora a experiência, enquanto a autorização efetiva permanece nas políticas do ASP.NET Core.

## Atualização de navios

O endpoint `PUT /api/v1/vessels/{id}` atualiza identificação, classificação e dimensões. O caso de uso:

1. consulta a entidade com rastreamento do Entity Framework;
2. valida a existência do navio;
3. interpreta e valida o número IMO;
4. impede que outro navio ativo use o mesmo IMO;
5. aplica as regras no domínio;
6. persiste a alteração e converte conflitos de índice único em `409 Conflict`.

A alteração usa as colunas existentes, portanto não exige uma nova migration.

## Criação idempotente de escala

O formulário gera uma chave com `crypto.randomUUID()` e a envia no cabeçalho `Idempotency-Key`. A mesma instância do formulário reutiliza a chave durante a solicitação, protegendo o comando contra repetição causada pela rede.

Depois da criação, o usuário é encaminhado diretamente aos detalhes da escala. O cache das listagens é invalidado para que a nova escala apareça nas consultas seguintes.

## Transições e concorrência

A tela mostra apenas transições permitidas para a situação atual, mas o domínio valida novamente o comando. O envio inclui `expectedVersion`; se outro usuário tiver alterado a escala, a API retorna `409 Conflict` e a interface atualiza os detalhes antes de uma nova tentativa.

Cancelamentos exigem justificativa tanto no formulário quanto no domínio. Cada transição registra situação anterior, nova situação, data, responsável e justificativa opcional.

## Tratamento de interface

- Validação nativa dos campos antes do envio.
- Mensagens da API baseadas em `Problem Details`.
- Botões bloqueados durante a execução do comando.
- Atualização seletiva do cache com TanStack Query.
- Formulários responsivos e operáveis por teclado.
- Histórico exibido do evento mais recente para o mais antigo.

## Escopo original desta etapa

O planejamento de terminal, berço e janelas de atracação foi implementado na [Parte 8](12-planejamento-atracacao.md), as operações de carga na [Parte 9](13-execucao-operacional.md) e as notificações na [Parte 11](15-notificacoes-em-tempo-real.md).
