# Recuperação segura de senha — Parte 20

## Objetivo

Esta etapa adiciona um fluxo completo de recuperação de acesso sem expor a existência de contas. O usuário solicita um link por e-mail, define uma nova senha em uma rota pública e tem as sessões anteriores revogadas após a alteração.

## Fluxo

1. A interface envia o e-mail para `POST /api/v1/auth/forgot-password`.
2. A API sempre responde com `202 Accepted` e a mesma mensagem, independentemente de a conta existir.
3. Para uma conta ativa e com e-mail confirmado, o ASP.NET Core Identity gera um token temporário.
4. O token é codificado para uso seguro na URL e enviado por SMTP.
5. O link abre `/redefinir-senha` com um identificador técnico da conta e o token nos parâmetros da URL; o e-mail não é exposto.
6. A interface envia a nova senha para `POST /api/v1/auth/reset-password`.
7. O Identity valida o token e a política de senha, altera a credencial e invalida o token utilizado.
8. Todos os refresh tokens ainda ativos da conta são revogados.

## Decisões de segurança

- A solicitação não informa se o e-mail existe, está inativo ou ainda não foi confirmado.
- Os dois endpoints usam o limitador de autenticação por endereço IP.
- O endereço base do link vem da configuração confiável do servidor, e não da requisição recebida.
- O e-mail do usuário não é incluído no link, reduzindo sua exposição em históricos e logs de infraestrutura.
- O token dura 30 minutos por padrão e é validado pelo provedor do ASP.NET Core Identity.
- A senha continua sujeita à política global: 12 caracteres, maiúscula, minúscula, número, símbolo e pelo menos quatro caracteres diferentes.
- Uma redefinição concluída revoga todas as sessões persistidas da conta.
- Tokens, links e endereços de destinatários não são registrados nos logs.
- As chaves do ASP.NET Data Protection são persistidas em volume próprio no Docker, evitando que reinicializações invalidem links ainda válidos.

## E-mail local com Mailpit

O ambiente Docker utiliza o Mailpit apenas como caixa SMTP de desenvolvimento. Ele captura as mensagens localmente e oferece uma interface web, sem enviar e-mails reais.

Após iniciar a aplicação:

- interface do sistema: `http://localhost:5173`;
- API: `http://localhost:8080`;
- caixa de e-mail Mailpit: `http://localhost:8025`.

Para testar, solicite a recuperação de uma das contas demonstrativas e abra a mensagem no Mailpit. Em produção, configure `PasswordRecovery__SmtpHost`, `PasswordRecovery__SmtpPort`, `PasswordRecovery__EnableSsl`, `PasswordRecovery__FromAddress`, `PasswordRecovery__FromName`, `PasswordRecovery__Username`, `PasswordRecovery__Password` e `PasswordRecovery__PublicWebUrl` com o provedor de e-mail e a URL reais.

## Rotas de interface

- `/recuperar-senha` — solicita o envio das instruções;
- `/redefinir-senha` — recebe o link temporário e permite criar a nova senha.

## Validação automatizada

Os testes verificam a delegação dos novos casos de uso, a presença pública das rotas no OpenAPI, a origem confiável e o escape dos parâmetros do link, a rejeição de esquemas de URL inseguros e a resposta genérica do fluxo no navegador.
