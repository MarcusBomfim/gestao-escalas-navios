# Isolamento organizacional — Parte 19

## Objetivo

Aplicar autorização por propriedade aos dados operacionais. Papéis respondem o que uma conta pode fazer; o escopo organizacional define sobre quais escalas ela pode agir. A interface não participa dessa decisão de segurança.

## Origem do escopo

`HttpUserDataScope` interpreta somente claims do JWT validado pela API:

- `organization_id` identifica a organização única da conta;
- o papel `Administrator` concede acesso global;
- `data_scope=global` concede leitura global ao visitante demonstrativo criado pelo seed;
- uma requisição autenticada sem organização e sem concessão global é negada por padrão;
- tarefas internas sem contexto HTTP utilizam escopo de sistema.

A claim global não é recebida em nenhum contrato público. O seed a grava diretamente no ASP.NET Core Identity e `IdentityService` transporta somente esse tipo de claim previamente persistida.

## Regra de visibilidade

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

## Criação e idempotência

Ao criar uma escala:

1. o servidor obtém a organização da identidade autenticada;
2. confirma que ela está ativa;
3. associa `ShippingAgency` ao campo de agência ou `ShippingLine` ao campo de armador;
4. rejeita outros tipos com `403`;
5. combina organização e chave de idempotência e persiste um SHA-256 dessa composição.

Duas organizações podem reutilizar a mesma chave enviada pelo cliente sem colisão. O valor original não é persistido para contas organizacionais.

## Planejamento compartilhado

A detecção de conflito de berço permanece global. Embora um participante não possa consultar a janela de outra organização, uma reserva confirmada invisível ainda bloqueia uma nova sobreposição no mesmo berço. A regra protege a integridade do recurso físico sem revelar a escala conflitante.

## Tempo real sem vazamento

O serviço em segundo plano precisa observar o conjunto global para detectar mudanças, mas não envia esse snapshot aos clientes. O evento `ControlTowerInvalidated` transporta somente o horário da mudança. Cada navegador então refaz `GET /api/v1/control-tower` com seu próprio JWT e recebe o resultado filtrado.

Esse fluxo impede que um payload transmitido para todos os clientes contorne os filtros aplicados nos repositórios.

## Dados demonstrativos

- administrador: escopo global por papel;
- planejador: associado à agência marítima sintética;
- operador: associado ao armador sintético;
- visitante: leitura global do conjunto sintético por claim controlada.

O seed também atualiza contas demonstrativas existentes, portanto não é necessário apagar o banco para receber as associações.

## Testes

Os testes automatizados verificam:

- leitura correta da organização a partir da identidade;
- acesso global do administrador e da claim demonstrativa;
- ausência de acesso global implícito;
- escopo de sistema fora de requisições HTTP;
- SQL contendo os dois vínculos organizacionais;
- consulta vazia para uma conta sem escopo;
- associação automática de uma nova escala;
- rejeição de uma organização que não pode originar escalas;
- idempotência derivada por organização.
