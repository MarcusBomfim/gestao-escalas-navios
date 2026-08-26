# Autenticação e autorização — Parte 5

Esta etapa substitui a trava temporária de escrita por identidade persistida e autorização aplicada no back-end. Nenhuma decisão de segurança depende de esconder elementos na interface.

## Fluxo da sessão

1. O usuário envia e-mail e senha para `POST /api/v1/auth/login`.
2. O ASP.NET Core Identity valida o hash da senha e o estado da conta.
3. A API devolve um access token JWT válido por 15 minutos.
4. O refresh token é enviado separadamente em cookie `HttpOnly` e `SameSite=Strict`.
5. `POST /api/v1/auth/refresh` rotaciona o refresh token e invalida o anterior.
6. `POST /api/v1/auth/logout` revoga a sessão de forma idempotente.

O access token deve ser mantido apenas em memória pelo cliente Web. O cookie de renovação não pode ser lido por JavaScript. Em HTTPS, ele recebe também o atributo `Secure`.

## Endpoints de identidade

| Método | Rota | Acesso | Finalidade |
| --- | --- | --- | --- |
| `POST` | `/api/v1/auth/login` | Público, limitado por IP | Inicia uma sessão. |
| `POST` | `/api/v1/auth/refresh` | Cookie de sessão | Renova e rotaciona a sessão. |
| `POST` | `/api/v1/auth/logout` | Cookie de sessão | Revoga a sessão. |
| `GET` | `/api/v1/auth/me` | Autenticado | Retorna o usuário atual. |
| `POST` | `/api/v1/users` | `Administrator` | Cria usuário e atribui um papel. |

Login e renovação compartilham um limite de dez requisições por minuto para cada endereço IP.

## Papéis e políticas implementados

| Operação | Administrator | Planner | Operator | Viewer |
| --- | ---: | ---: | ---: | ---: |
| Consultar dados demonstrativos | Sim | Sim | Sim | Sim |
| Criar usuários | Sim | Não | Não | Não |
| Cadastrar navios | Sim | Sim | Não | Não |
| Criar escalas | Sim | Sim | Não | Não |
| Alterar situação da escala | Sim | Sim | Sim | Não |

As políticas são centralizadas na camada de aplicação. O papel é transportado como claim do JWT e validado pela autorização do ASP.NET Core antes da execução do endpoint.

## Escopo organizacional

Além do papel, o JWT pode transportar `organization_id`. As consultas de escalas, planejamento, execução, torre e notificações aplicam esse valor antes de filtrar, ordenar ou paginar. Uma escala é visível quando a organização do usuário corresponde à agência marítima ou ao armador associado.

O administrador possui escopo global por papel. O visitante criado pelo seed recebe a claim assinada `data_scope=global` para navegar por todo o conjunto sintético em modo somente leitura. Essa claim não é aceita em formulários nem pode ser atribuída pelo endpoint comum de criação de usuários. Uma conta operacional sem organização e sem escopo global recebe uma consulta vazia por padrão.

Na criação de escalas, o servidor consulta o tipo da organização autenticada e associa automaticamente uma `ShippingAgency` como agência ou uma `ShippingLine` como armador. A chave de idempotência é derivada com SHA-256 junto do identificador da organização, evitando colisões e inferência entre participantes diferentes.

## Proteções aplicadas

- Senhas gerenciadas pelo ASP.NET Core Identity e nunca armazenadas em texto puro.
- Política de senha forte: 12 caracteres, maiúscula, minúscula, número, símbolo e diversidade mínima.
- Bloqueio da conta por 15 minutos após cinco tentativas inválidas.
- Access token curto, com validação de emissor, audiência, assinatura e expiração.
- Refresh token aleatório de 512 bits, salvo apenas como hash SHA-256.
- Rotação obrigatória e revogação das sessões ativas quando um token já revogado é reutilizado.
- Concorrência otimista para impedir duas renovações simultâneas do mesmo token.
- CORS com lista explícita de origens e suporte a credenciais; curingas não são usados.
- Credenciais e chave de assinatura fornecidas por variáveis de ambiente ignoradas pelo Git.

## Dados demonstrativos

O comando `--seed-demo` cria os papéis e quatro usuários sintéticos. A senha vem exclusivamente de `Demo:UserPassword`, mapeada no Docker pela variável `DEMO_USER_PASSWORD`. O seed falha com uma mensagem clara quando a variável não foi informada.

As contas usam os domínios locais abaixo e não representam pessoas ou empresas reais:

```text
admin.demo@portmanagement.local
planner.demo@portmanagement.local
operator.demo@portmanagement.local
viewer.demo@portmanagement.local
```

## Persistência

As tabelas de negócio permanecem no schema `port_management`. Usuários, papéis, claims e sessões ficam no schema `identity`. A migration `AddIdentityAndRefreshTokens` adiciona essa estrutura sem reescrever as migrations anteriores.

## Próximas evoluções

- Confirmação e recuperação de e-mail com provedor transacional.
- Autenticação multifator para contas administrativas.
- Auditoria de login, alteração de papéis e revogação administrativa.
