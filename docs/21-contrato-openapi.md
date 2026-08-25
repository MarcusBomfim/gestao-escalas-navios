# Contrato OpenAPI — Parte 17

## Objetivo

Manter um contrato executável e versionado para a API, permitindo que desenvolvedores consultem rotas, parâmetros, modelos e requisitos de autenticação sem depender de documentação manual desatualizada.

O documento é gerado pelo suporte oficial do ASP.NET Core a partir dos endpoints reais. A interface Scalar apenas apresenta esse documento e permite exercitar requisições durante o desenvolvimento.

## Endereços locais

Com a aplicação executada em `Development`:

- `GET /openapi/v1.json` retorna a especificação OpenAPI 3.1;
- `GET /docs/` apresenta a referência interativa.

O nome `v1` acompanha o prefixo atual `/api/v1`. Uma futura versão incompatível deverá receber outro documento e outro prefixo, preservando o contrato existente durante a transição.

## Segurança da documentação

As rotas OpenAPI e Scalar são mapeadas somente quando o ambiente ASP.NET Core é `Development`. Em `Production`, ambas retornam `404`.

A especificação declara o esquema HTTP `Bearer` com formato JWT. Somente operações que possuem metadados reais de autorização recebem o requisito de segurança; rotas públicas não são marcadas artificialmente como protegidas.

A interface interativa:

- não recebe token ou senha predefinidos;
- não persiste autenticação no navegador;
- desativa o recurso de agente do Scalar;
- desativa fontes externas e usa os recursos locais do pacote;
- exige que o desenvolvedor obtenha e informe seu próprio access token.

## Respostas padronizadas

O transformador OpenAPI acrescenta ao contrato:

- `401` para operações que exigem uma sessão válida;
- `403` para operações que exigem permissão específica;
- `500` como resposta de falha interna comum.

Os nomes e resumos definidos nos endpoints continuam sendo usados como identificadores e descrições das operações.

## Testes de contrato

Os testes em `OpenApiContractTests.cs` iniciam a aplicação com um servidor HTTP em memória e sem acessar um PostgreSQL real. Eles validam:

1. versão, título e rotas essenciais da especificação;
2. presença do esquema Bearer;
3. requisito de autenticação na torre de controle;
4. ausência desse requisito no login;
5. disponibilidade da referência interativa em desenvolvimento;
6. retorno `404` para documentação e especificação em produção.

Esses testes fazem parte do job `Backend` no CI. Uma alteração acidental em rota, política de autorização ou configuração da documentação interrompe o pipeline antes do merge.

## Execução

Para conferir o contrato automaticamente:

```powershell
dotnet test .\backend\PortManagement.slnx --no-build
```

Para consultar visualmente:

```powershell
docker compose up --build -d
Start-Process http://localhost:8080/docs/
```

O Scalar é uma ferramenta de desenvolvimento. Ele não substitui autenticação, autorização, testes automatizados ou um portal público de integração.
