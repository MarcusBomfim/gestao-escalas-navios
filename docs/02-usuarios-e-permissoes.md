# Usuários e permissões

## Modelo de acesso

O acesso será baseado em três elementos:

1. **Identidade:** quem realizou a ação.
2. **Organização:** em nome de qual empresa ou instituição o usuário atua.
3. **Permissão:** qual operação ele pode executar naquele contexto.

As decisões de autorização são feitas no back-end; esconder um botão no front-end não é considerado proteção suficiente. A associação simples com uma organização e o filtro das escalas por agência ou armador estão implementados. A participação em várias organizações, com papéis diferentes em cada uma, permanece como evolução planejada.

## Papéis e escopos implementados

| Papel técnico | Escopo atual |
| --- | --- |
| `Viewer` | Somente leitura; o visitante do seed recebe escopo demonstrativo global por claim assinada. |
| `Operator` | Consulta e opera somente escalas vinculadas à própria organização. |
| `Planner` | Planeja o escopo da própria organização; agências e armadores podem originar escalas. |
| `Administrator` | Escopo global, permissões operacionais e criação controlada de usuários. |

Esses quatro papéis formam a primeira camada executável de autorização. As personas abaixo detalham o modelo de domínio que será refinado com escopo organizacional e permissões mais granulares.

## Personas de domínio planejadas

### Visitante da demonstração

- Consulta apenas registros sintéticos publicados.
- Visualiza dashboard, escalas e detalhes não sensíveis.
- Não cria, altera, exporta dados completos ou acessa auditoria.

### Agente marítimo

- Consulta navios e escalas vinculados à sua organização.
- Solicita uma escala e atualiza previsões permitidas.
- Anexa informações operacionais não sensíveis quando autorizado.
- Não confirma berço nem altera dados de outras organizações.

### Operador do terminal

- Consulta escalas destinadas aos terminais em que atua.
- Registra eventos de atracação e operação.
- Atualiza informações de carga e produtividade.
- Não administra usuários globais ou terminais externos.

### Planejador de berços

- Analisa solicitações de escala.
- Propõe, confirma e reprograma janelas de atracação.
- Consulta compatibilidade entre navio e berço.
- Precisa justificar reprogramações e exceções.

### Administrador portuário

- Gerencia cadastros de referência e associações organizacionais.
- Configura terminais, berços e regras operacionais.
- Consulta auditoria e revoga acessos.
- Não pode apagar o histórico de operações concluídas.

### Auditor

- Consulta registros, histórico de estados e trilhas de auditoria.
- Não altera informações operacionais.
- Exportações ficam registradas e sujeitas a permissão específica.

### Administrador do sistema

- Realiza manutenção técnica e recuperação controlada.
- Não é utilizado na rotina operacional.
- Ações privilegiadas exigem auditoria reforçada.

## Matriz de domínio planejada

| Capacidade | Visitante | Agente | Operador | Planejador | Administrador | Auditor |
|---|---:|---:|---:|---:|---:|---:|
| Consultar demonstração | Sim | Sim | Sim | Sim | Sim | Sim |
| Solicitar escala | Não | Sim | Opcional | Sim | Sim | Não |
| Atualizar previsão própria | Não | Sim | Sim | Sim | Sim | Não |
| Confirmar janela de berço | Não | Não | Não | Sim | Sim | Não |
| Registrar evento operacional | Não | Limitado | Sim | Sim | Sim | Não |
| Gerenciar terminal e berço | Não | Não | Não | Não | Sim | Não |
| Gerenciar usuários | Não | Própria organização | Própria organização | Não | Sim | Não |
| Consultar auditoria | Não | Próprias ações | Escopo do terminal | Escopo portuário | Sim | Sim |
| Exportar relatório completo | Não | Escopo próprio | Escopo do terminal | Sim | Sim | Conforme permissão |

## Regras de autorização

- Toda operação começa negada e precisa ser explicitamente permitida.
- A consulta deve aplicar o escopo organizacional antes da paginação.
- Papéis não substituem regras de propriedade do recurso.
- A elevação de privilégio precisa ser auditada.
- Contas bloqueadas ou organizações inativas perdem acesso imediatamente.
- Tokens e sessões devem ser revogáveis.
- A conta de demonstração não recebe permissões de escrita sensíveis.
- Operações críticas podem exigir confirmação recente da identidade.

## Separação entre autenticação e autorização

- **Autenticação:** comprova a identidade do usuário.
- **Autorização:** decide se a identidade pode executar a ação no recurso solicitado.
- **Auditoria:** registra o resultado e o contexto das ações relevantes.
