# Interface do Sistema de Gestão de Escalas

Aplicação React com TypeScript para autenticação e consulta das operações portuárias demonstrativas.

## Tecnologias

- React 19.
- TypeScript em modo estrito.
- React Router.
- TanStack Query.
- Vite.
- CSS responsivo sem biblioteca visual externa.

## Execução local

Com a API disponível em `http://localhost:8080`:

```powershell
npm.cmd install
npm.cmd run dev
```

Acesse `http://localhost:5173`. A variável `VITE_API_URL` define outro endereço para a API quando necessário.

## Validação

```powershell
npm.cmd run typecheck
npm.cmd run lint
npm.cmd run build
```

## Organização

```text
src/
├── api/          # cliente HTTP, contratos e consultas
├── auth/         # contexto e proteção das rotas
├── components/   # componentes reutilizáveis
├── config/       # variáveis do ambiente
├── layouts/      # estrutura da área autenticada
├── pages/        # páginas da aplicação
└── styles/       # estilos globais
```

O access token não é persistido no navegador. A renovação utiliza exclusivamente o cookie `HttpOnly` emitido pela API.
