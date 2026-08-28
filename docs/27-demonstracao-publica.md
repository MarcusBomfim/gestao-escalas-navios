# Demonstração pública somente leitura — Parte 23

## Objetivo

Permitir que recrutadores e visitantes conheçam a plataforma sem receber senhas ou permissões capazes de alterar os dados. A tela de login oferece a ação **Entrar como visitante**, que cria uma sessão restrita ao papel `Viewer`.

## Funcionamento

O frontend chama `POST /api/v1/auth/demo`. A API só atende a solicitação quando `Demo:PublicViewerEnabled` está ativo e quando a conta demonstrativa possui exatamente estas características:

- e-mail interno `viewer.demo@portmanagement.local`;
- conta ativa;
- somente o papel `Viewer`;
- ausência de vínculo com uma organização específica;
- claim explícita de leitura global dos dados sintéticos.

Se qualquer uma dessas condições deixar de existir, o acesso público fica indisponível. Isso impede que uma mudança administrativa acidental transforme a entrada demonstrativa em uma sessão privilegiada.

## Segurança

- nenhuma senha é enviada ao navegador;
- a senha definida em `DEMO_USER_PASSWORD` continua exclusiva do servidor e dos testes;
- a rota usa o limitador de autenticação por endereço IP;
- o access token continua apenas na memória;
- o refresh token continua em cookie `HttpOnly`, `SameSite=Strict` e seguro em produção;
- o visitante não recebe menus ou endpoints de escrita;
- a API, e não apenas a interface, aplica as permissões.

## Configuração

No ambiente local, o `compose.yaml` usa:

```env
PUBLIC_DEMO_ENABLED=true
```

Para desabilitar o recurso:

```env
PUBLIC_DEMO_ENABLED=false
```

Quando desabilitado, `POST /api/v1/auth/demo` responde como rota não encontrada.

## Validação

Os testes verificam a delegação da sessão pública, a presença da rota sem requisito Bearer no OpenAPI e o fluxo de navegador que entra como visitante e confirma a ausência de ações administrativas.
