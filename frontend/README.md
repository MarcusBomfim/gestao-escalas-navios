# Interface do Sistema de Gestão de Escalas

Aplicação React com TypeScript para autenticação, consulta e execução controlada de operações portuárias demonstrativas.

## Tecnologias

- React 19.
- TypeScript em modo estrito.
- React Router.
- TanStack Query.
- Vite.
- Playwright com Chromium.
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

Com API, PostgreSQL e interface iniciados pelo Docker Compose:

```powershell
npx.cmd playwright install chromium
$env:DEMO_USER_PASSWORD = "A_MESMA_SENHA_DO_ARQUIVO_ENV"
npm.cmd run test:e2e
```

## Organização

```text
frontend/
├── e2e/          # jornadas de aceitação no navegador
├── src/
│   ├── api/      # cliente HTTP, contratos e consultas
│   ├── auth/     # contexto e proteção das rotas
│   ├── components/
│   ├── config/
│   ├── layouts/
│   └── pages/
└── playwright.config.ts
```

O access token não é persistido no navegador. A renovação utiliza exclusivamente o cookie `HttpOnly` emitido pela API.

## Fluxos disponíveis

- Cadastro e edição de navios para `Administrator` e `Planner`.
- Criação de escala com chave de idempotência gerada no navegador.
- Detalhes completos da escala e histórico de transições.
- Mudanças administrativas de situação para os papéis autorizados.
- Atualização coordenada do cache depois de cada escrita.
- Tratamento de validação, conflito de versão e falhas da API.
- Planejamento de terminal, berço e período previsto de ocupação.
- Verificação visual de compatibilidade pelas dimensões e tipo do navio.
- Agenda diária de janelas solicitadas e confirmadas.
- Confirmação, reprogramação e cancelamento com justificativa.
- Registro sequencial de chegada, praticagem, atracação, operação, desatracação e saída.
- Cadastro, início e conclusão de cargas com quantidade planejada e realizada.
- Linha do tempo auditável e indicadores de permanência, operação e produtividade.
- Torre de controle com atualização periódica, aderência à programação e ocupação de berços.
- Fila filtrável de alertas críticos, de atenção e informativos.
- Escalas monitoradas ordenadas por prioridade operacional.
- Centro de notificações disponível em todas as telas autenticadas.
- Atualizações em tempo real pelo cliente oficial do SignalR.
- Reconexão automática com consulta periódica como contingência.
- Confirmação de leitura persistida individualmente para cada usuário.
- Área administrativa de auditoria com filtros e paginação.
- Exportação do histórico e do relatório operacional em CSV.
- Neutralização de fórmulas potencialmente perigosas nas planilhas exportadas.
- Painel administrativo de saúde e observabilidade com atualização periódica.
- Métricas de volume, erros, latência média, percentil 95 e tempo ativo.
- Estado de prontidão dos componentes sem exposição de informações internas.

As rotas de escrita também são protegidas visualmente por papel, mas a API continua sendo a autoridade final para todas as permissões.
