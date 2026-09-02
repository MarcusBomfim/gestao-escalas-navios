# Operação portuária

Como o sistema conduz uma escala do pedido ao encerramento: quem faz o quê na
interface, como o berço é planejado, como a operação de carga é acompanhada,
o que a torre de controle consolida e o que fica registrado depois.

Para as regras de negócio em si, veja [dominio-e-regras.md](dominio-e-regras.md);
para o vocabulário do setor, [glossario-portuario.md](glossario-portuario.md).

## Fluxos e permissões na interface

O sistema acrescenta comandos de escrita à interface autenticada e completa a atualização dos dados de navios no backend. As decisões priorizam validação de domínio, autorização na API, prevenção de duplicidade e rastreabilidade.

### Funcionalidades

| Fluxo | Rota web | Papéis autorizados |
| --- | --- | --- |
| Cadastrar navio | `/navios/novo` | `Administrator`, `Planner` |
| Editar navio | `/navios/{id}/editar` | `Administrator`, `Planner` |
| Registrar escala | `/escalas/nova` | `Administrator`, `Planner` |
| Consultar escala | `/escalas/{publicCode}` | Qualquer usuário autenticado |
| Alterar situação | `/escalas/{publicCode}` | `Administrator`, `Planner`, `Operator` |

O papel `Viewer` pode acompanhar dados e histórico, mas não recebe controles de escrita. A ocultação de controles melhora a experiência, enquanto a autorização efetiva permanece nas políticas do ASP.NET Core.

### Atualização de navios

O endpoint `PUT /api/v1/vessels/{id}` atualiza identificação, classificação e dimensões. O caso de uso:

1. consulta a entidade com rastreamento do Entity Framework;
2. valida a existência do navio;
3. interpreta e valida o número IMO;
4. impede que outro navio ativo use o mesmo IMO;
5. aplica as regras no domínio;
6. persiste a alteração e converte conflitos de índice único em `409 Conflict`.

A alteração usa as colunas existentes, portanto não exige uma nova migration.

### Criação idempotente de escala

O formulário gera uma chave com `crypto.randomUUID()` e a envia no cabeçalho `Idempotency-Key`. A mesma instância do formulário reutiliza a chave durante a solicitação, protegendo o comando contra repetição causada pela rede.

Depois da criação, o usuário é encaminhado diretamente aos detalhes da escala. O cache das listagens é invalidado para que a nova escala apareça nas consultas seguintes.

### Transições e concorrência

A tela mostra apenas transições permitidas para a situação atual, mas o domínio valida novamente o comando. O envio inclui `expectedVersion`; se outro usuário tiver alterado a escala, a API retorna `409 Conflict` e a interface atualiza os detalhes antes de uma nova tentativa.

Cancelamentos exigem justificativa tanto no formulário quanto no domínio. Cada transição registra situação anterior, nova situação, data, responsável e justificativa opcional.

### Tratamento de interface

- Validação nativa dos campos antes do envio.
- Mensagens da API baseadas em `Problem Details`.
- Botões bloqueados durante a execução do comando.
- Atualização seletiva do cache com TanStack Query.
- Formulários responsivos e operáveis por teclado.
- Histórico exibido do evento mais recente para o mais antigo.

## Planejamento de atracação

O sistema conecta escalas, navios, terminais e berços em um fluxo de planejamento protegido por regras de domínio e restrições do PostgreSQL.

### Fluxo operacional

1. A escala chega à situação `UnderReview`.
2. Um `Administrator` ou `Planner` escolhe um berço compatível e informa início e fim previstos.
3. A janela nasce como `Requested`.
4. A confirmação verifica novamente conflitos e altera a escala para `Planned` quando necessário.
5. Reprogramações preservam período e berço anteriores em uma revisão auditável.
6. Cancelamentos exigem justificativa e liberam a atribuição planejada da escala.

### Compatibilidade de berço

O método `Berth.CanReceive` é a regra oficial. O navio somente pode ser planejado quando:

- o berço está disponível;
- o comprimento total não supera o comprimento útil;
- a boca não supera o limite do berço;
- o calado máximo não supera a profundidade operacional configurada;
- o tipo do navio está entre os tipos suportados, quando houver restrição.

A interface antecipa essa análise e desabilita opções incompatíveis, mas o backend repete todas as verificações antes de persistir.

### Proteção contra conflitos

Existem três camadas complementares:

- consulta preventiva por interseção de períodos;
- restrição `EXCLUDE USING gist` no PostgreSQL para janelas `Confirmed` do mesmo berço;
- conversão da violação da restrição em resposta `409 Conflict` compreensível pela interface.

Os períodos usam o intervalo semiaberto `[início, fim)`. Assim, uma janela pode terminar exatamente quando a próxima começa sem representar sobreposição.

### Concorrência

`BerthWindow.Version` é um token de concorrência otimista incrementado a cada alteração. Reprogramar, confirmar ou cancelar exige a versão esperada. Uma escrita concorrente retorna `planning.version_conflict`, e o frontend atualiza janela, escala e agenda antes de permitir outra tentativa.

Também há um índice único parcial para impedir mais de uma janela `Requested` ou `Confirmed` por escala.

### Auditoria

Cada reprogramação registra:

- berço anterior e novo berço;
- início e fim anteriores;
- início e fim novos;
- responsável, data e justificativa.

A migration `AddBerthWindowConcurrency` preenche os identificadores de berço em revisões antigas antes de tornar as novas colunas obrigatórias.

### Agenda web

A rota `/agenda` consulta janelas por período e situação. Cada item exibe horários, terminal, berço, navio, escala e estado, com acesso direto aos detalhes da operação.

Na tela da escala, o painel de planejamento permite:

- solicitar a primeira janela;
- confirmar uma solicitação;
- trocar período ou berço com justificativa;
- cancelar a janela;
- consultar revisões anteriores.

### Dados demonstrativos

O seed adiciona duas janelas fictícias em berços diferentes. O processo continua idempotente: se já existirem janelas, nenhum registro adicional é criado.

## Execução operacional

O sistema transforma a escala planejada em uma operação acompanhável. Chegada, praticagem, atracação, carga, desatracação e saída passam a ser eventos realizados, preservados em uma linha do tempo auditável.

### Sequência de marcos

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

### Operações de carga

Uma carga registra:

- embarque, descarga ou movimento misto;
- descrição e quantidade planejada;
- unidade em toneladas, metros cúbicos, TEU ou unidades;
- período planejado e horários realizados;
- quantidade efetivamente movimentada;
- indicação e classificação de carga perigosa.

Antes do marco de início operacional, é obrigatório cadastrar ao menos uma carga. Antes do encerramento da operação, todas as cargas precisam estar concluídas. Cada carga possui seu próprio token de concorrência otimista.

### Indicadores

A consulta operacional calcula:

- permanência no porto, da chegada ao fundeadouro até a saída;
- tempo atracado, da atracação até a desatracação;
- duração da operação, do início ao encerramento da carga;
- totais planejados e realizados agrupados por unidade;
- produtividade realizada por hora e por unidade.

Enquanto uma fase está aberta, o horário atual é usado para apresentar uma duração em andamento. Unidades diferentes nunca são somadas entre si.

### Permissões

- `Administrator` e `Operator`: registram marcos e movimentações.
- `Planner`: continua responsável pelo planejamento de berço e consulta a execução.
- `Viewer`: acompanha eventos, cargas e indicadores em modo somente leitura.

A API é a autoridade das permissões. Ocultar controles no frontend serve apenas para melhorar a experiência.

### Dados demonstrativos

O seed avança uma escala fictícia até `InOperation`, registra quatro marcos realizados e cria duas operações de granéis em toneladas. Uma carga aparece concluída e outra em andamento. A inclusão é idempotente e não duplica dados quando o seed é executado novamente.

### Endpoints

- `GET /api/v1/operations/port-calls/{publicCode}`
- `POST /api/v1/operations/port-calls/{publicCode}/milestones`
- `POST /api/v1/operations/port-calls/{publicCode}/cargo-operations`
- `POST /api/v1/operations/port-calls/{publicCode}/cargo-operations/{id}/start`
- `POST /api/v1/operations/port-calls/{publicCode}/cargo-operations/{id}/complete`

### Persistência

A migration `AddOperationalExecutionConcurrency` adiciona o campo `version` em `cargo_operations`. O Entity Framework configura esse campo como token de concorrência e o incrementa a cada modificação.

## Torre de controle

A torre de controle oferece uma leitura única da operação portuária. Em vez de exigir que o usuário abra cada escala, o backend consolida planejamento, eventos realizados e progresso de carga em indicadores e alertas acionáveis.

### Indicadores

O endpoint apresenta:

- total de escalas ainda ativas;
- navios atualmente em operação de carga;
- escalas que exigem atenção;
- quantidade de alertas críticos;
- berços ocupados e percentual de ocupação;
- aderência à programação das escalas com janela ativa.

A aderência considera como não conformes as escalas com atraso de chegada, janela pendente, excesso de ocupação, atraso de carga ou desvio relevante de atracação. Alertas de ausência de atualização são operacionais, mas não alteram esse percentual.

### Regras de alerta

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

### Arquitetura

`ControlTowerRepository` monta um snapshot somente leitura com dados ativos. `ControlTowerEvaluator` aplica regras puras e determinísticas sobre esse snapshot. Essa separação permite testar os critérios sem banco de dados e evita persistir alertas derivados que ficariam desatualizados.

O endpoint autenticado é:

```text
GET /api/v1/control-tower
```

O frontend usa TanStack Query e atualiza a consulta automaticamente a cada 60 segundos. Alertas podem ser filtrados por criticidade e cada ocorrência oferece acesso direto à escala correspondente.

### Dados demonstrativos

O seed acrescenta uma quarta escala fictícia com janela solicitada e início vencido, gerando um alerta crítico compreensível. Ele também alinha a janela da escala em operação aos eventos realizados e completa a rastreabilidade da escala no fundeadouro.

Nenhum registro representa empresa, navio ou operação real.

## Notificações em tempo real

O sistema distribui mudanças operacionais para todos os usuários autenticados sem exigir atualização manual da página. O centro de notificações permanece disponível no cabeçalho de toda a área interna.

### Fluxo em tempo real

1. `ControlTowerBroadcastService` calcula um snapshot a cada 15 segundos.
2. Um fingerprint representa estados, alertas, atividades e o intervalo atual das posições simuladas.
3. Quando o fingerprint muda, o backend publica `ControlTowerInvalidated` pelo hub, sem transportar dados operacionais.
4. Cada cliente invalida os caches da torre e das notificações e refaz as consultas autenticadas dentro do próprio escopo organizacional.

O canal é:

```text
/hubs/control-tower
```

O hub exige o mesmo JWT Bearer da API. Para WebSockets, o cliente oficial do SignalR envia o access token durante a negociação. A aplicação mantém esse token apenas em memória.

### Resiliência

O cliente configura reconexão automática com intervalos progressivos. Se a conexão inicial falhar, tenta novamente a cada dez segundos. As consultas HTTP continuam sendo atualizadas a cada 60 segundos, portanto uma indisponibilidade temporária do hub não interrompe o painel.

O painel exibe quatro estados:

- conectando;
- tempo real ativo;
- reconectando;
- atualização periódica.

### Estado de leitura

A tabela `notification_receipts` armazena:

- usuário;
- identificador estável do alerta;
- data e hora da leitura.

Existe uma restrição única por usuário e alerta. A escrita usa `ON CONFLICT`, tornando “marcar como lida” idempotente mesmo com cliques repetidos ou requisições concorrentes. O recibo possui chave estrangeira para o usuário e é removido em cascata caso a conta seja excluída.

Nenhum texto sensível, token ou payload completo é persistido no recibo.

### Endpoints

- `GET /api/v1/notifications`
- `POST /api/v1/notifications/{alertId}/read`
- `POST /api/v1/notifications/read-all`

Todos exigem autenticação. Cada resposta combina os alertas atualmente ativos com os recibos do usuário conectado.

### Interface

O sino apresenta a quantidade de alertas não lidos. O painel lateral permite:

- consultar severidade, horário, navio e escala;
- abrir diretamente a escala relacionada;
- marcar uma notificação como lida;
- marcar todas as notificações ativas como lidas;
- acompanhar o estado da conexão em tempo real.

### Migration

`AddNotificationReceipts` cria a tabela, o índice único e a chave estrangeira para `identity.users`.

## Mapa operacional simulado

### Objetivo

Oferecer uma visão espacial das escalas ativas sem consumir uma API AIS, serviço cartográfico, credencial externa ou dado operacional real. O recurso atende ao cenário de portfólio e demonstra como o sistema poderia receber posições de um provedor especializado no futuro.

### Fluxo da informação

1. o repositório da torre consulta as escalas que ainda não foram encerradas ou canceladas;
2. `VesselTrafficSimulator` associa cada situação da escala a um estado de navegação demonstrativo;
3. o simulador calcula coordenadas relativas entre 5% e 95% da área desenhada;
4. `ControlTowerEvaluator` inclui o tráfego no mesmo snapshot de indicadores e alertas;
5. `GET /api/v1/control-tower` entrega o estado inicial ao navegador;
6. o serviço em segundo plano publica somente um aviso de invalidação pelo hub `/hubs/control-tower`;
7. o TanStack Query refaz a consulta autenticada e recebe um novo snapshot já limitado ao escopo organizacional.

Indicadores e posições compartilham a mesma consulta operacional. Essa decisão evita uma segunda leitura completa do banco a cada atualização.

### Estados apresentados

| Situação da escala | Estado no mapa | Velocidade demonstrativa |
| --- | --- | ---: |
| `Draft`, `Requested`, `UnderReview` | aguardando programação | 0 nó |
| `Planned` | em aproximação | 8,4 nós |
| `AtAnchorage` | no fundeadouro | 0 nó |
| `ClearedForBerthing` | em manobra | 3,2 nós |
| `Berthed` | atracado | 0 nó |
| `InOperation` | em operação | 0 nó |
| `OperationCompleted` | pronto para saída | 0 nó |
| `Unberthed` | em saída | 9,1 nós |

As posições são determinísticas a partir do identificador da escala e do instante arredondado em intervalos de cinco segundos. Navios em aproximação, manobra ou saída recebem um deslocamento visual pequeno; navios atracados permanecem estáveis.

### Contrato retornado

O campo `traffic` da torre contém:

- horário de geração em UTC;
- rótulo da cobertura demonstrativa;
- marcador global `isSimulated`;
- escala e navio relacionados;
- situação operacional e estado de navegação;
- coordenadas relativas `xPercent` e `yPercent`;
- velocidade e rumo demonstrativos;
- horário observado e marcador `isSimulated` por posição.

O mesmo contrato é publicado no OpenAPI em desenvolvimento. A rota e o canal em tempo real exigem autenticação; todos os papéis podem consultar o mapa em modo somente leitura.

### Interface e acessibilidade

O mapa usa somente SVG, HTML e CSS do próprio projeto. Não há tiles, fontes, scripts ou imagens carregados de terceiros.

Além dos pontos visuais, a interface oferece:

- nome e estado de cada navio como texto acessível no SVG;
- lista equivalente ao lado do mapa;
- links para abrir a escala correspondente;
- legenda por cor e descrição textual do estado;
- layout adaptado para telas menores;
- redução automática das animações quando o sistema operacional solicita menos movimento;
- aviso permanente de que as posições não representam rastreamento AIS real.

### Segurança e limites

- nenhuma latitude, longitude, mensagem AIS ou posição real é armazenada;
- não existe chave de API relacionada ao mapa;
- o algoritmo não deve ser usado para navegação, manobra ou decisão operacional;
- os dados não substituem autoridade portuária, praticagem, VTS ou sistemas oficiais;
- uma futura integração real deverá implementar contrato próprio, autenticação, timeout, resiliência, auditoria e separação por organização.

### Testes

`VesselTrafficSimulatorTests` verifica:

1. marcação explícita de dados simulados;
2. arredondamento do horário de observação;
3. limites das coordenadas relativas;
4. correspondência entre situação da escala e estado no mapa;
5. movimento dos navios em trânsito;
6. estabilidade dos navios atracados.

O cenário Playwright do perfil visitante também confirma que o mapa e o aviso sobre AIS aparecem no painel sem conceder permissões de escrita.

## Cadastros mestres

### Objetivo

O sistema disponibiliza uma área administrativa para manter as referências estruturais usadas em todo o planejamento portuário. Organizações, portos, terminais e berços passam a ser consultados, cadastrados e atualizados pela própria plataforma, sem exclusão física de registros históricos.

### Funcionalidades

- listagem paginada de organizações;
- busca por nome ou registro e filtros por tipo e situação;
- cadastro e atualização de organizações;
- visualização hierárquica de portos, terminais e berços;
- cadastro e atualização de portos e terminais;
- cadastro e atualização de berços, dimensões, situação e tipos de navio aceitos;
- atualização automática das referências usadas em usuários, formulários e planejamento;
- mensagens de erro específicas para dependências, duplicidades e concorrência.

### Regras de acesso

A rota `/cadastros` e os endpoints desta etapa exigem o papel `Administrator` pela política `ManageMasterData`. Os itens de navegação não são exibidos para `Planner`, `Operator` ou `Viewer`, e a API repete a autorização no servidor para que a interface não seja a única barreira de segurança.

### Integridade e histórico

- Registros não são apagados; a inativação preserva referências históricas.
- Uma organização com usuários ativos não pode ser desativada.
- Um porto com terminais ativos não pode ser desativado.
- Um terminal com berços disponíveis não pode ser desativado.
- Novos terminais e berços só podem ser vinculados a pais ativos.
- Terminais e berços não podem voltar a uma situação ativa enquanto o respectivo pai estiver inativo.
- O UN/LOCODE é único e possui cinco caracteres alfanuméricos.
- Códigos de terminal e berço são únicos dentro do respectivo pai.
- Capacidade, tipos aceitos e situação de um berço não podem mudar enquanto houver janelas futuras solicitadas ou confirmadas.
- Nomes, registros e códigos são normalizados no domínio antes da persistência.

### Concorrência

Cada resposta devolve `updatedAtUtc`, que a interface envia como `expectedUpdatedAtUtc` nas atualizações. A aplicação compara a versão recebida antes da alteração e o Entity Framework também usa esse campo como token de concorrência durante a gravação. Uma edição antiga recebe `409 Conflict` em vez de sobrescrever silenciosamente uma alteração mais recente.

### Endpoints

- `GET /api/v1/admin/master-data/organizations`
- `POST /api/v1/admin/master-data/organizations`
- `PUT /api/v1/admin/master-data/organizations/{id}`
- `GET /api/v1/admin/master-data/ports`
- `POST /api/v1/admin/master-data/ports`
- `PUT /api/v1/admin/master-data/ports/{id}`
- `POST /api/v1/admin/master-data/ports/{portId}/terminals`
- `PUT /api/v1/admin/master-data/terminals/{id}`
- `POST /api/v1/admin/master-data/terminals/{terminalId}/berths`
- `PUT /api/v1/admin/master-data/berths/{id}`

Todas as operações exigem a política `ManageMasterData`.

### Banco de dados

A etapa reutiliza as tabelas `organizations`, `ports`, `terminals` e `berths`, seus índices únicos e os vínculos já existentes. A verificação `dotnet ef migrations has-pending-model-changes` confirmou que nenhuma migration adicional é necessária.

### Validação automatizada

Doze novos testes unitários cobrem paginação, duplicidade, concorrência, hierarquia inativa, dependências e janelas de berço. O teste de contrato verifica a presença e a autenticação de todos os endpoints no OpenAPI. O fluxo Playwright confirma que apenas o administrador recebe o menu e consegue abrir a área de cadastros.

## Auditoria e relatórios

O sistema acrescenta governança às operações demonstrativas. Alterações realizadas por usuários autenticados geram evidências consultáveis pelo administrador, e o estado atual da operação pode ser exportado para análise externa.

### Captura automática

O `AuditSaveChangesInterceptor` observa inclusões, atualizações e exclusões de entidades de domínio no momento em que o Entity Framework confirma a transação. Cada evidência registra:

- usuário e nome exibido no instante da ação;
- data e hora UTC;
- ação, tipo e identificador da entidade;
- nomes dos campos modificados;
- método e caminho da requisição;
- identificador de correlação da requisição.

O registro é incluído no mesmo `SaveChanges` da operação de negócio. Dessa forma, a evidência não é confirmada se a transação principal falhar.

### Minimização de dados

A auditoria não guarda valores anteriores ou posteriores, corpos de requisição, tokens, senhas, endereços IP ou cookies. Somente os nomes dos campos alterados são persistidos. Recibos de leitura das notificações também foram excluídos da captura para evitar ruído operacional.

### Acesso

As consultas e exportações exigem a política `ViewAuditReports`, exclusiva do papel `Administrator`. A proteção existe na API e também na rota visual `/auditoria`.

Rotas:

- `GET /api/v1/audit` — histórico paginado com filtros;
- `GET /api/v1/audit/export` — até 10 mil evidências em CSV;
- `GET /api/v1/reports/operations/export` — resumo e escalas monitoradas em CSV.

### Segurança das planilhas

Todos os campos do CSV são delimitados e escapados. Valores iniciados por `=`, `+`, `-`, `@`, tabulação ou retorno de carro recebem um prefixo neutro antes da exportação. Essa proteção reduz o risco de CSV Injection ao abrir o arquivo em editores de planilhas.

### Interface

A página administrativa apresenta:

- indicadores atuais da torre de controle;
- filtros por ação e entidade;
- histórico paginado com usuário, origem e campos modificados;
- exportação do relatório operacional;
- exportação da trilha de auditoria respeitando os filtros ativos.

### Banco de dados

A migration `AddAuditTrail` cria `port_management.audit_records` e índices por data, usuário e tipo de entidade. O histórico não possui exclusão automática nesta etapa; uma futura política de retenção deverá considerar os requisitos do ambiente em que o sistema for utilizado.
