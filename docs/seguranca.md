# Segurança

Quem é o usuário, sobre quais dados ele pode agir e como a sessão é protegida.
As decisões de autorização acontecem sempre no back-end: esconder um controle
na interface melhora a experiência, nunca substitui a verificação no servidor.

O [SECURITY.md](../SECURITY.md) na raiz resume os controles em vigor e explica
como comunicar uma vulnerabilidade. Este documento detalha o desenho por trás
deles.

## Papéis, personas e matriz de permissões

### Modelo de acesso

O acesso é baseado em três elementos:

1. **Identidade:** quem realizou a ação.
2. **Organização:** em nome de qual empresa ou instituição o usuário atua.
3. **Permissão:** qual operação ele pode executar naquele contexto.

As decisões de autorização são feitas no back-end; esconder um botão no front-end não é considerado proteção suficiente. A associação simples com uma organização e o filtro das escalas por agência ou armador estão implementados. A participação em várias organizações, com papéis diferentes em cada uma, permanece como evolução planejada.

### Papéis e escopos implementados

| Papel técnico | Escopo atual |
| --- | --- |
| `Viewer` | Somente leitura; o visitante do seed recebe escopo demonstrativo global por claim assinada. |
| `Operator` | Consulta e opera somente escalas vinculadas à própria organização. |
| `Planner` | Planeja o escopo da própria organização; agências e armadores podem originar escalas. |
| `Administrator` | Escopo global, permissões operacionais e criação controlada de usuários. |

Esses quatro papéis formam a camada executável de autorização. As personas abaixo detalham o modelo de domínio, complementado pelo escopo organizacional e pelas permissões implementadas nas etapas posteriores.

### Personas de domínio planejadas

#### Visitante da demonstração

- Consulta apenas registros sintéticos publicados.
- Visualiza dashboard, escalas e detalhes não sensíveis.
- Não cria, altera, exporta dados completos ou acessa auditoria.

#### Agente marítimo

- Consulta navios e escalas vinculados à sua organização.
- Solicita uma escala e atualiza previsões permitidas.
- Anexa informações operacionais não sensíveis quando autorizado.
- Não confirma berço nem altera dados de outras organizações.

#### Operador do terminal

- Consulta escalas destinadas aos terminais em que atua.
- Registra eventos de atracação e operação.
- Atualiza informações de carga e produtividade.
- Não administra usuários globais ou terminais externos.

#### Planejador de berços

- Analisa solicitações de escala.
- Propõe, confirma e reprograma janelas de atracação.
- Consulta compatibilidade entre navio e berço.
- Precisa justificar reprogramações e exceções.

#### Administrador portuário

- Gerencia cadastros de referência e associações organizacionais.
- Configura terminais, berços e regras operacionais.
- Consulta auditoria e revoga acessos.
- Não pode apagar o histórico de operações concluídas.

#### Auditor

- Consulta registros, histórico de estados e trilhas de auditoria.
- Não altera informações operacionais.
- Exportações ficam registradas e sujeitas a permissão específica.

#### Administrador do sistema

- Realiza manutenção técnica e recuperação controlada.
- Não é utilizado na rotina operacional.
- Ações privilegiadas exigem auditoria reforçada.

### Matriz de domínio planejada

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

### Regras de autorização

- Toda operação começa negada e precisa ser explicitamente permitida.
- A consulta deve aplicar o escopo organizacional antes da paginação.
- Papéis não substituem regras de propriedade do recurso.
- A elevação de privilégio precisa ser auditada.
- Contas bloqueadas ou organizações inativas perdem acesso imediatamente.
- Tokens e sessões devem ser revogáveis.
- A conta de demonstração não recebe permissões de escrita sensíveis.
- Operações críticas podem exigir confirmação recente da identidade.

### Separação entre autenticação e autorização

- **Autenticação:** comprova a identidade do usuário.
- **Autorização:** decide se a identidade pode executar a ação no recurso solicitado.
- **Auditoria:** registra o resultado e o contexto das ações relevantes.

## Autenticação e sessão

O sistema substitui a trava temporária de escrita por identidade persistida e autorização aplicada no back-end. Nenhuma decisão de segurança depende de esconder elementos na interface.

### Fluxo da sessão

1. O usuário envia e-mail e senha para `POST /api/v1/auth/login`.
2. O ASP.NET Core Identity valida o hash da senha e o estado da conta.
3. A API devolve um access token JWT válido por 15 minutos.
4. O refresh token é enviado separadamente em cookie `HttpOnly` e `SameSite=Strict`.
5. `POST /api/v1/auth/refresh` rotaciona o refresh token e invalida o anterior.
6. `POST /api/v1/auth/logout` revoga a sessão de forma idempotente.

O access token deve ser mantido apenas em memória pelo cliente Web. O cookie de renovação não pode ser lido por JavaScript. Em HTTPS, ele recebe também o atributo `Secure`.

### Endpoints de identidade

| Método | Rota | Acesso | Finalidade |
| --- | --- | --- | --- |
| `POST` | `/api/v1/auth/login` | Público, limitado por IP | Inicia uma sessão. |
| `POST` | `/api/v1/auth/refresh` | Cookie de sessão | Renova e rotaciona a sessão. |
| `POST` | `/api/v1/auth/logout` | Cookie de sessão | Revoga a sessão. |
| `GET` | `/api/v1/auth/me` | Autenticado | Retorna o usuário atual. |
| `POST` | `/api/v1/users` | `Administrator` | Cria usuário e atribui um papel. |

Login e renovação compartilham um limite de dez requisições por minuto para cada endereço de cliente e rota. O endereço vem do `X-Forwarded-For` processado pelo `UseForwardedHeaders`, aceito somente de proxies declarados em `Security:TrustedProxies`. Sem esse passo o limite seria por IP do proxy, ou seja, global.

### Papéis e políticas implementados

| Operação | Administrator | Planner | Operator | Viewer |
| --- | ---: | ---: | ---: | ---: |
| Consultar dados demonstrativos | Sim | Sim | Sim | Sim |
| Criar usuários | Sim | Não | Não | Não |
| Cadastrar navios | Sim | Sim | Não | Não |
| Criar escalas | Sim | Sim | Não | Não |
| Alterar situação da escala | Sim | Sim | Sim | Não |

As políticas são centralizadas na camada de aplicação. O papel é transportado como claim do JWT e validado pela autorização do ASP.NET Core antes da execução do endpoint.

### Escopo organizacional

Além do papel, o JWT pode transportar `organization_id`. As consultas de escalas, planejamento, execução, torre e notificações aplicam esse valor antes de filtrar, ordenar ou paginar. Uma escala é visível quando a organização do usuário corresponde à agência marítima ou ao armador associado.

O administrador possui escopo global por papel. O visitante criado pelo seed recebe a claim assinada `data_scope=global` para navegar por todo o conjunto sintético em modo somente leitura. Essa claim não é aceita em formulários nem pode ser atribuída pelo endpoint comum de criação de usuários. Uma conta operacional sem organização e sem escopo global recebe uma consulta vazia por padrão.

Na criação de escalas, o servidor consulta o tipo da organização autenticada e associa automaticamente uma `ShippingAgency` como agência ou uma `ShippingLine` como armador. A chave de idempotência é derivada com SHA-256 junto do identificador da organização, evitando colisões e inferência entre participantes diferentes.

### Proteções aplicadas

- Senhas gerenciadas pelo ASP.NET Core Identity e nunca armazenadas em texto puro.
- Política de senha forte: 12 caracteres, maiúscula, minúscula, número, símbolo e diversidade mínima.
- Bloqueio da conta por 15 minutos após cinco tentativas inválidas.
- Access token curto, com validação de emissor, audiência, assinatura e expiração.
- Refresh token aleatório de 512 bits, salvo apenas como hash SHA-256.
- Rotação obrigatória e revogação das sessões ativas quando um token já revogado é reutilizado.
- Concorrência otimista para impedir duas renovações simultâneas do mesmo token.
- CORS com lista explícita de origens e suporte a credenciais; curingas não são usados.
- Credenciais e chave de assinatura fornecidas por variáveis de ambiente ignoradas pelo Git.

### Dados demonstrativos

O comando `--seed-demo` cria os papéis e quatro usuários sintéticos. A senha vem exclusivamente de `Demo:UserPassword`, mapeada no Docker pela variável `DEMO_USER_PASSWORD`. O seed falha com uma mensagem clara quando a variável não foi informada.

As contas usam os domínios locais abaixo e não representam pessoas ou empresas reais:

```text
admin.demo@portmanagement.local
planner.demo@portmanagement.local
operator.demo@portmanagement.local
viewer.demo@portmanagement.local
```

### Persistência

As tabelas de negócio permanecem no schema `port_management`. Usuários, papéis, claims e sessões ficam no schema `identity`. A migration `AddIdentityAndRefreshTokens` adiciona essa estrutura sem reescrever as migrations anteriores.

### Próximas evoluções

- Confirmação e recuperação de e-mail com provedor transacional.
- Autenticação multifator para contas administrativas.
- Auditoria de login, alteração de papéis e revogação administrativa.

## Isolamento entre organizações

### Objetivo

Aplicar autorização por propriedade aos dados operacionais. Papéis respondem o que uma conta pode fazer; o escopo organizacional define sobre quais escalas ela pode agir. A interface não participa dessa decisão de segurança.

### Origem do escopo

`HttpUserDataScope` interpreta somente claims do JWT validado pela API:

- `organization_id` identifica a organização única da conta;
- o papel `Administrator` concede acesso global;
- `data_scope=global` concede leitura global ao visitante demonstrativo criado pelo seed;
- uma requisição autenticada sem organização e sem concessão global é negada por padrão;
- a ausência de contexto HTTP não concede nada: o escopo derivado da requisição falha fechado;
- tarefas internas que precisam ler todas as organizações pedem elevação explícita por `DataScopeContext.ElevateToSystem()`, nunca por omissão.

A claim global não é recebida em nenhum contrato público. O seed a grava diretamente no ASP.NET Core Identity e `IdentityService` transporta somente esse tipo de claim previamente persistida.

### Regra de visibilidade

Uma escala pertence ao escopo quando uma destas relações corresponde à organização da conta:

```text
port_call.agent_organization_id = organization_id
OU
port_call.shipping_line_organization_id = organization_id
```

O filtro é aplicado no `IQueryable` antes de busca, contagem, ordenação e paginação. Ele alcança:

- listagem e detalhes de escalas;
- transições de situação;
- janelas e agenda de berços;
- eventos realizados e operações de carga;
- indicadores, alertas, notificações e posições simuladas da torre.

Um código de escala válido fora do escopo retorna o mesmo resultado de recurso inexistente. Isso reduz a possibilidade de enumerar identificadores de outras organizações.

### Criação e idempotência

Ao criar uma escala:

1. o servidor obtém a organização da identidade autenticada;
2. confirma que ela está ativa;
3. associa `ShippingAgency` ao campo de agência ou `ShippingLine` ao campo de armador;
4. rejeita outros tipos com `403`;
5. combina organização e chave de idempotência e persiste um SHA-256 dessa composição.

Duas organizações podem reutilizar a mesma chave enviada pelo cliente sem colisão. O valor original não é persistido para contas organizacionais.

### Planejamento compartilhado

A detecção de conflito de berço permanece global. Embora um participante não possa consultar a janela de outra organização, uma reserva confirmada invisível ainda bloqueia uma nova sobreposição no mesmo berço. A regra protege a integridade do recurso físico sem revelar a escala conflitante.

### Tempo real sem vazamento

O serviço em segundo plano precisa observar o conjunto global para detectar mudanças, mas não envia esse snapshot aos clientes. O evento `ControlTowerInvalidated` transporta somente o horário da mudança. Cada navegador então refaz `GET /api/v1/control-tower` com seu próprio JWT e recebe o resultado filtrado.

Esse fluxo impede que um payload transmitido para todos os clientes contorne os filtros aplicados nos repositórios.

### Dados demonstrativos

- administrador: escopo global por papel;
- planejador: associado à agência marítima sintética;
- operador: associado ao armador sintético;
- visitante: leitura global do conjunto sintético por claim controlada.

O seed também atualiza contas demonstrativas existentes, portanto não é necessário apagar o banco para receber as associações.

### Testes

Os testes automatizados verificam:

- leitura correta da organização a partir da identidade;
- acesso global do administrador e da claim demonstrativa;
- ausência de acesso global implícito;
- ausência de acesso fora de requisições HTTP;
- concessão de escopo de sistema apenas por elevação explícita;
- SQL contendo os dois vínculos organizacionais;
- consulta vazia para uma conta sem escopo;
- associação automática de uma nova escala;
- rejeição de uma organização que não pode originar escalas;
- idempotência derivada por organização.

## Recuperação de senha

### Objetivo

O sistema adiciona um fluxo completo de recuperação de acesso sem expor a existência de contas. O usuário solicita um link por e-mail, define uma nova senha em uma rota pública e tem as sessões anteriores revogadas após a alteração.

### Fluxo

1. A interface envia o e-mail para `POST /api/v1/auth/forgot-password`.
2. A API sempre responde com `202 Accepted` e a mesma mensagem, independentemente de a conta existir.
3. Para uma conta ativa e com e-mail confirmado, o ASP.NET Core Identity gera um token temporário.
4. O token é codificado para uso seguro na URL e enviado por SMTP.
5. O link abre `/redefinir-senha` com um identificador técnico da conta e o token nos parâmetros da URL; o e-mail não é exposto.
6. A interface envia a nova senha para `POST /api/v1/auth/reset-password`.
7. O Identity valida o token e a política de senha, altera a credencial e invalida o token utilizado.
8. Todos os refresh tokens ainda ativos da conta são revogados.

### Decisões de segurança

- A solicitação não informa se o e-mail existe, está inativo ou ainda não foi confirmado.
- Os dois endpoints usam o limitador de autenticação por endereço IP.
- O endereço base do link vem da configuração confiável do servidor, e não da requisição recebida.
- O e-mail do usuário não é incluído no link, reduzindo sua exposição em históricos e logs de infraestrutura.
- O token dura 30 minutos por padrão e é validado pelo provedor do ASP.NET Core Identity.
- A senha continua sujeita à política global: 12 caracteres, maiúscula, minúscula, número, símbolo e pelo menos quatro caracteres diferentes.
- Uma redefinição concluída revoga todas as sessões persistidas da conta.
- Tokens, links e endereços de destinatários não são registrados nos logs.
- As chaves do ASP.NET Data Protection são persistidas em volume próprio no Docker, evitando que reinicializações invalidem links ainda válidos.

### E-mail local com Mailpit

O ambiente Docker utiliza o Mailpit apenas como caixa SMTP de desenvolvimento. Ele captura as mensagens localmente e oferece uma interface web, sem enviar e-mails reais.

Após iniciar a aplicação:

- interface do sistema: `http://localhost:5173`;
- API: `http://localhost:8080`;
- caixa de e-mail Mailpit: `http://localhost:8025`.

Para testar, solicite a recuperação de uma das contas demonstrativas e abra a mensagem no Mailpit. Em produção, configure `PasswordRecovery__SmtpHost`, `PasswordRecovery__SmtpPort`, `PasswordRecovery__EnableSsl`, `PasswordRecovery__FromAddress`, `PasswordRecovery__FromName`, `PasswordRecovery__Username`, `PasswordRecovery__Password` e `PasswordRecovery__PublicWebUrl` com o provedor de e-mail e a URL reais.

### Rotas de interface

- `/recuperar-senha` — solicita o envio das instruções;
- `/redefinir-senha` — recebe o link temporário e permite criar a nova senha.

### Validação automatizada

Os testes verificam a delegação dos novos casos de uso, a presença pública das rotas no OpenAPI, a origem confiável e o escape dos parâmetros do link, a rejeição de esquemas de URL inseguros e a resposta genérica do fluxo no navegador.

## Administração de contas

### Objetivo

O sistema disponibiliza uma área exclusiva para administradores consultarem, criarem e atualizarem as contas da plataforma. A gestão considera papéis, organização, situação da conta, concorrência e encerramento de sessões após mudanças de acesso.

### Funcionalidades

- listagem paginada de usuários;
- busca por nome ou e-mail;
- filtros por perfil e situação;
- cadastro com senha inicial sujeita à política do Identity;
- edição de nome, perfil, organização e situação;
- organizações ativas carregadas pelo servidor;
- indicação da conta usada pelo administrador atual;
- proteção contra alterações concorrentes por versão opaca;
- bloqueio imediato após mudança de acesso.

### Regras de acesso

Somente o papel `Administrator` acessa a interface `/usuarios` e os endpoints de gestão. Administradores usam escopo global e não recebem organização. Os demais perfis precisam ser vinculados a uma organização ativa.

O visitante demonstrativo criado pelo seed é uma exceção controlada: ele mantém uma claim global somente enquanto permanecer como visitante sem organização. Qualquer alteração de perfil ou vínculo remove essa claim especial.

### Proteções contra bloqueio administrativo

- Um administrador não pode bloquear a própria conta.
- Um administrador não pode remover o próprio papel administrativo.
- O último administrador ativo não pode ser bloqueado nem rebaixado.
- Alterações de perfil, organização ou situação atualizam o `SecurityStamp` do ASP.NET Core Identity.
- A API compara o `SecurityStamp` presente no JWT com o usuário atual em cada autenticação.
- Refresh tokens ativos são revogados após uma alteração de acesso.
- Alterações somente no nome não encerram a sessão.
- Criações e alterações são registradas na auditoria somente com os nomes dos campos alterados; senhas e valores não são armazenados.

### Concorrência

A resposta de usuário contém uma versão derivada do `ConcurrencyStamp` do Identity. A interface precisa devolver essa versão no `PUT`. Se outra operação já tiver modificado a conta, a API responde com `409 Conflict` e solicita a atualização da página, impedindo que uma edição antiga sobrescreva dados novos.

### Endpoints

- `GET /api/v1/users` — lista usuários com paginação e filtros;
- `GET /api/v1/users/options` — retorna papéis e organizações disponíveis;
- `POST /api/v1/users` — cria uma conta;
- `PUT /api/v1/users/{id}` — atualiza perfil, escopo e situação.

Todos exigem a política `ManageUsers`.

### Banco de dados

O sistema reutiliza as tabelas do ASP.NET Core Identity e as organizações existentes. Nenhuma migration adicional é necessária.

### Validação automatizada

Os testes cobrem validação de paginação, delegação dos casos de uso, presença e proteção das rotas no OpenAPI e acesso à página administrativa no fluxo de navegador. A suíte também continua verificando que perfis não administrativos não recebem o item de navegação.
