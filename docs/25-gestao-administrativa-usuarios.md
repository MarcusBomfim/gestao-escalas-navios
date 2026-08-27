# Gestão administrativa de usuários — Parte 21

## Objetivo

Esta etapa disponibiliza uma área exclusiva para administradores consultarem, criarem e atualizarem as contas da plataforma. A gestão considera papéis, organização, situação da conta, concorrência e encerramento de sessões após mudanças de acesso.

## Funcionalidades

- listagem paginada de usuários;
- busca por nome ou e-mail;
- filtros por perfil e situação;
- cadastro com senha inicial sujeita à política do Identity;
- edição de nome, perfil, organização e situação;
- organizações ativas carregadas pelo servidor;
- indicação da conta usada pelo administrador atual;
- proteção contra alterações concorrentes por versão opaca;
- bloqueio imediato após mudança de acesso.

## Regras de acesso

Somente o papel `Administrator` acessa a interface `/usuarios` e os endpoints de gestão. Administradores usam escopo global e não recebem organização. Os demais perfis precisam ser vinculados a uma organização ativa.

O visitante demonstrativo criado pelo seed é uma exceção controlada: ele mantém uma claim global somente enquanto permanecer como visitante sem organização. Qualquer alteração de perfil ou vínculo remove essa claim especial.

## Proteções contra bloqueio administrativo

- Um administrador não pode bloquear a própria conta.
- Um administrador não pode remover o próprio papel administrativo.
- O último administrador ativo não pode ser bloqueado nem rebaixado.
- Alterações de perfil, organização ou situação atualizam o `SecurityStamp` do ASP.NET Core Identity.
- A API compara o `SecurityStamp` presente no JWT com o usuário atual em cada autenticação.
- Refresh tokens ativos são revogados após uma alteração de acesso.
- Alterações somente no nome não encerram a sessão.
- Criações e alterações são registradas na auditoria somente com os nomes dos campos alterados; senhas e valores não são armazenados.

## Concorrência

A resposta de usuário contém uma versão derivada do `ConcurrencyStamp` do Identity. A interface precisa devolver essa versão no `PUT`. Se outra operação já tiver modificado a conta, a API responde com `409 Conflict` e solicita a atualização da página, impedindo que uma edição antiga sobrescreva dados novos.

## Endpoints

- `GET /api/v1/users` — lista usuários com paginação e filtros;
- `GET /api/v1/users/options` — retorna papéis e organizações disponíveis;
- `POST /api/v1/users` — cria uma conta;
- `PUT /api/v1/users/{id}` — atualiza perfil, escopo e situação.

Todos exigem a política `ManageUsers`.

## Banco de dados

Esta etapa reutiliza as tabelas do ASP.NET Core Identity e as organizações existentes. Nenhuma migration adicional é necessária.

## Validação automatizada

Os testes cobrem validação de paginação, delegação dos casos de uso, presença e proteção das rotas no OpenAPI e acesso à página administrativa no fluxo de navegador. A suíte também continua verificando que perfis não administrativos não recebem o item de navegação.
