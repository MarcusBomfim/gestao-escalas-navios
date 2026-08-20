# Domínio e regras de negócio

## Módulos do domínio

### Identidade e organizações

Responsável por usuários, organizações, vínculos, papéis, permissões e sessões.

### Cadastros portuários

Responsável por navios, armadores, agentes, portos, terminais, berços e tipos de carga.

### Escalas

Responsável pelo ciclo de vida da passagem de um navio pelo porto, participantes, previsões, eventos e situação operacional.

### Planejamento

Responsável pelas solicitações de janela, compatibilidade do berço, conflitos e confirmação do planejamento.

### Operações

Responsável por fundeio, atracação, movimentação de carga, conclusão da operação e saída.

### Notificações

Responsável por alertas internos, preferências e entregas assíncronas.

### Relatórios e auditoria

Responsável por indicadores, exportações, histórico de estados e rastreabilidade.

## Principais entidades

### Navio

- Identificador interno imutável.
- Número IMO, quando aplicável.
- Nome atual e nomes anteriores relevantes.
- Bandeira.
- Tipo do navio.
- Comprimento total (LOA).
- Boca.
- Calado máximo informado.
- Arqueação bruta e porte bruto, quando disponíveis.
- Indicativo de chamada e MMSI, quando disponíveis.
- Situação cadastral.

### Organização

- Tipo: autoridade, terminal, operador, armador ou agência.
- Nome e identificador interno.
- Situação cadastral.
- Usuários associados e seus papéis.

### Terminal

- Porto ao qual pertence.
- Nome, código e fuso horário.
- Situação operacional.
- Conjunto de berços.

### Berço

- Terminal ao qual pertence.
- Código e nome.
- Comprimento útil.
- Calado máximo permitido.
- Restrições de boca e tipos de navio, quando aplicáveis.
- Tipos de carga suportados.
- Situação: disponível, indisponível ou em manutenção.

### Escala

- Identificador público não sequencial.
- Navio e organizações participantes.
- Porto anterior e próximo porto.
- Terminal e berço planejados.
- Viagem e finalidade da escala.
- Situação atual.
- Versão de concorrência.
- Datas de criação, atualização e encerramento.

### Janela de berço

- Escala vinculada.
- Berço.
- Início e fim planejados.
- Situação da reserva.
- Responsável e justificativa da última alteração.

### Evento da escala

Representa um marco da operação, sem depender de uma única coluna mutável. Cada evento possui:

- Serviço ou fase: fundeio, praticagem, berço, operação de carga ou saída.
- Ação: chegada, início, conclusão ou partida.
- Classificação temporal: estimado, solicitado, planejado ou realizado.
- Instante do evento.
- Fonte, autor e instante de registro.
- Correlação com o evento substituído, quando houver revisão.

Esse modelo permite representar ETA, RTA, PTA e ATA sem apagar estimativas anteriores.

### Operação de carga

- Escala.
- Tipo de operação: embarque, descarga ou ambos.
- Tipo de carga.
- Quantidade planejada e realizada.
- Unidade de medida.
- Indicação de carga perigosa.
- Início e conclusão planejados e realizados.

## Ciclo de vida da escala

```text
Rascunho
   ↓
Solicitada
   ↓
Em análise
   ↓
Planejada
   ↓
Em fundeio ──────────┐
   ↓                 │
Liberada para atracação
   ↓
Atracada
   ↓
Em operação
   ↓
Operação concluída
   ↓
Desatracada
   ↓
Encerrada
```

Uma escala poderá ser cancelada antes do encerramento, desde que a permissão, a situação atual e a justificativa permitam. Cancelamento não significa exclusão.

## Regras do navio

**RN-001** — O número IMO, quando informado, deve usar o prefixo `IMO` seguido de sete dígitos e passar pela validação oficial aplicável.

**RN-002** — Não podem existir dois navios ativos com o mesmo número IMO.

**RN-003** — Alterar o nome de um navio não altera sua identidade nem apaga seu histórico.

**RN-004** — Navios inativos permanecem consultáveis nas escalas históricas.

## Regras da escala

**RN-005** — Toda escala pertence a exatamente um navio e a um porto.

**RN-006** — Uma escala só pode avançar para uma situação permitida pelo fluxo.

**RN-007** — Toda transição registra situação anterior, nova situação, autor, horário e justificativa quando exigida.

**RN-008** — Uma escala encerrada é imutável para usuários operacionais; correções posteriores usam procedimento administrativo auditado.

**RN-009** — Cancelamentos exigem motivo e não removem reservas ou eventos históricos.

**RN-010** — Repetir uma solicitação com a mesma chave de idempotência não pode duplicar a escala.

## Regras de planejamento

**RN-011** — Duas janelas confirmadas não podem ocupar o mesmo berço em períodos sobrepostos.

**RN-012** — O navio precisa respeitar comprimento, boca, calado, tipo e restrições operacionais do berço.

**RN-013** — Berços indisponíveis ou em manutenção não aceitam novas confirmações no período afetado.

**RN-014** — Reprogramar uma janela confirmada exige justificativa e gera histórico.

**RN-015** — A verificação de conflito e a confirmação devem ocorrer na mesma transação.

**RN-016** — Alterações concorrentes usam controle otimista; uma versão desatualizada deve ser rejeitada, não sobrescrita.

## Regras temporais

**RN-017** — Instantes são persistidos em UTC com precisão definida pela aplicação.

**RN-018** — O fuso usado na apresentação não modifica o instante original.

**RN-019** — Eventos estimados, solicitados e planejados podem receber revisões, mas as versões anteriores permanecem no histórico.

**RN-020** — Um evento realizado não pode ser substituído silenciosamente; correções exigem motivo e auditoria.

**RN-021** — A partida realizada não pode ser anterior à chegada realizada da mesma fase.

## Regras operacionais e de carga

**RN-022** — Uma operação só começa quando a escala está atracada e autorizada para operação no sistema.

**RN-023** — A conclusão da escala exige que as operações obrigatórias estejam encerradas ou formalmente dispensadas.

**RN-024** — Quantidades realizadas não podem ser negativas e devem registrar unidade de medida.

**RN-025** — Carga perigosa exige classificação e sinalização específicas; o sistema apenas registra a informação e não emite autorização oficial.

## Regras de segurança e auditoria

**RN-026** — Consultas e comandos aplicam organização, papel e propriedade do recurso.

**RN-027** — Logs não armazenam senha, token, segredo ou conteúdo sensível desnecessário.

**RN-028** — Alterações relevantes geram auditoria append-only, separada dos logs técnicos.

**RN-029** — Exportações completas exigem permissão específica e deixam registro.

**RN-030** — Dados demonstrativos são sintéticos e podem ser restaurados periodicamente.

## Invariantes críticas

- Número IMO único entre navios ativos.
- Uma escala encerrada nunca retorna ao fluxo operacional comum.
- Um berço não possui reservas confirmadas sobrepostas.
- Um evento realizado não é apagado por uma nova previsão.
- Uma ação fora do escopo organizacional nunca é autorizada.
- Toda correção administrativa relevante é rastreável.

