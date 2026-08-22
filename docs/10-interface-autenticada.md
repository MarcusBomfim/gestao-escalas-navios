# Interface autenticada — Parte 6

Esta etapa transforma a base visual em uma aplicação React conectada à API. A interface apresenta dados sintéticos e respeita as decisões de autenticação e autorização implementadas no back-end.

## Rotas

| Rota | Acesso | Finalidade |
| --- | --- | --- |
| `/` | Público | Apresentação do produto demonstrativo. |
| `/login` | Público | Autenticação e escolha do perfil fictício. |
| `/painel` | Autenticado | Indicadores, escalas recentes e estrutura portuária. |
| `/navios` | Autenticado | Paginação e busca de navios ativos. |
| `/escalas` | Autenticado | Paginação, busca e filtro por situação. |

O Nginx usa fallback para `index.html`, permitindo abrir as rotas diretamente no ambiente Docker sem retornar erro `404`.

## Sessão no navegador

- O access token existe apenas na memória do módulo HTTP.
- `localStorage` e `sessionStorage` não são usados para credenciais.
- O refresh token continua inacessível ao JavaScript por estar em cookie `HttpOnly`.
- Ao iniciar, o `AuthProvider` tenta restaurar a sessão em `/api/v1/auth/refresh`.
- Consultas que recebem `401` fazem uma única renovação automática e repetem a chamada.
- Renovações simultâneas compartilham a mesma Promise, impedindo rotação concorrente do cookie.
- O logout revoga a sessão no servidor e limpa o estado local mesmo se a chamada falhar.

## Consulta e cache

TanStack Query gerencia cache, carregamento, erro e atualização dos dados remotos. As chaves incluem página, texto de busca e situação selecionada, evitando misturar resultados de filtros diferentes.

As páginas possuem:

- indicadores de carregamento sem alterar bruscamente o layout;
- mensagens de erro vindas de `Problem Details` da API;
- estados vazios;
- paginação controlada;
- tabelas com rolagem segura em telas estreitas;
- indicadores visuais de situação com texto, sem depender apenas de cor.

## Perfis

A interface identifica o papel principal do usuário e informa quando o perfil permite cadastrar navios ou atuar em escalas. A ausência de um botão nunca é tratada como mecanismo de segurança: a autorização efetiva continua na API.

## Responsividade e acessibilidade

- Layout lateral no desktop e navegação inferior no celular.
- Link para pular diretamente ao conteúdo principal.
- Navegação e formulários com nomes acessíveis.
- Foco visível para teclado.
- Respeito à preferência `prefers-reduced-motion`.
- Textos associados aos estados operacionais.

## Escopo original desta etapa

A Parte 6 implementou autenticação e consultas. Os formulários de cadastro, comandos de transição e detalhes completos foram adicionados posteriormente na [Parte 7](11-fluxos-operacionais.md), usando as permissões e a infraestrutura preparadas aqui.
