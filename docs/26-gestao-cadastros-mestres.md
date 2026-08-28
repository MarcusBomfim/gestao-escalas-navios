# Gestão de cadastros mestres — Parte 22

## Objetivo

Esta etapa disponibiliza uma área administrativa para manter as referências estruturais usadas em todo o planejamento portuário. Organizações, portos, terminais e berços passam a ser consultados, cadastrados e atualizados pela própria plataforma, sem exclusão física de registros históricos.

## Funcionalidades

- listagem paginada de organizações;
- busca por nome ou registro e filtros por tipo e situação;
- cadastro e atualização de organizações;
- visualização hierárquica de portos, terminais e berços;
- cadastro e atualização de portos e terminais;
- cadastro e atualização de berços, dimensões, situação e tipos de navio aceitos;
- atualização automática das referências usadas em usuários, formulários e planejamento;
- mensagens de erro específicas para dependências, duplicidades e concorrência.

## Regras de acesso

A rota `/cadastros` e os endpoints desta etapa exigem o papel `Administrator` pela política `ManageMasterData`. Os itens de navegação não são exibidos para `Planner`, `Operator` ou `Viewer`, e a API repete a autorização no servidor para que a interface não seja a única barreira de segurança.

## Integridade e histórico

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

## Concorrência

Cada resposta devolve `updatedAtUtc`, que a interface envia como `expectedUpdatedAtUtc` nas atualizações. A aplicação compara a versão recebida antes da alteração e o Entity Framework também usa esse campo como token de concorrência durante a gravação. Uma edição antiga recebe `409 Conflict` em vez de sobrescrever silenciosamente uma alteração mais recente.

## Endpoints

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

## Banco de dados

A etapa reutiliza as tabelas `organizations`, `ports`, `terminals` e `berths`, seus índices únicos e os vínculos já existentes. A verificação `dotnet ef migrations has-pending-model-changes` confirmou que nenhuma migration adicional é necessária.

## Validação automatizada

Doze novos testes unitários cobrem paginação, duplicidade, concorrência, hierarquia inativa, dependências e janelas de berço. O teste de contrato verifica a presença e a autenticação de todos os endpoints no OpenAPI. O fluxo Playwright confirma que apenas o administrador recebe o menu e consegue abrir a área de cadastros.
