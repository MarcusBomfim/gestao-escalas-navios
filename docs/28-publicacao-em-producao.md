# Publicação em produção — Parte 24

## Estratégia

O projeto publica duas imagens no GitHub Container Registry quando uma tag semântica `vX.Y.Z` é criada:

- `ghcr.io/marcusbomfim/port-management-api`;
- `ghcr.io/marcusbomfim/port-management-web`.

O arquivo `compose.production.yaml` executa essas imagens, o PostgreSQL, as migrations e o seed demonstrativo. Banco e serviços ficam vinculados apenas à máquina local; um proxy reverso externo deve publicar os domínios com HTTPS.

## Preparação no GitHub

1. Em **Settings > Actions > Variables**, configure `PUBLIC_API_URL` com a URL HTTPS pública da API.
2. Confirme que os packages do GHCR poderão ser lidos pelo servidor de destino.
3. Aguarde os workflows **CI** e **Security** concluírem sem falhas na branch `main`.
4. Crie a tag somente depois do quality gate.

A URL da API é incorporada ao bundle do frontend durante a construção da imagem. Portanto, ela precisa estar correta antes da tag.

## Preparação do servidor

```powershell
Copy-Item .env.production.example .env.production
notepad .env.production
```

Substitua todos os valores `replace-with-*`, informe os domínios reais e configure o SMTP. Gere segredos independentes para PostgreSQL, JWT e contas demonstrativas. Não envie `.env.production` ao Git.

Valide a configuração:

```powershell
docker compose --env-file .env.production -f compose.production.yaml config --quiet
```

Inicie os serviços:

```powershell
docker compose --env-file .env.production -f compose.production.yaml pull
docker compose --env-file .env.production -f compose.production.yaml up -d
docker compose --env-file .env.production -f compose.production.yaml ps
```

Confira as tarefas de inicialização e a saúde da API:

```powershell
docker compose --env-file .env.production -f compose.production.yaml logs migrations
docker compose --env-file .env.production -f compose.production.yaml logs seed-demo
Invoke-RestMethod http://127.0.0.1:8080/health/ready
```

## Proxy reverso e TLS

O proxy reverso deve:

- redirecionar HTTP para HTTPS;
- encaminhar o domínio da interface para `127.0.0.1:5173`;
- encaminhar o domínio da API e WebSocket para `127.0.0.1:8080`;
- preservar `Host`, `X-Forwarded-For` e `X-Forwarded-Proto`;
- permitir upgrade de WebSocket em `/hubs/control-tower`;
- renovar automaticamente o certificado TLS.

`Security:EnforceHttps` permanece desativado no container porque o TLS termina no proxy. O redirecionamento obrigatório deve acontecer no proxy público. Nunca publique diretamente as portas locais do PostgreSQL ou da API na internet.

## Atualização e retorno

Para atualizar, altere `IMAGE_TAG` para uma versão já publicada, faça `pull` e execute `up -d`. Para retornar, restaure a tag anterior e repita os mesmos comandos. As migrations são progressivas; antes de cada release material, mantenha um backup testado do volume PostgreSQL.

## Limites da demonstração

Esta configuração é adequada para apresentação de portfólio com dados sintéticos. Uso empresarial exigiria gestão externa de segredos, backup automatizado, retenção de auditoria, monitoramento centralizado, política de disponibilidade e avaliação jurídica e operacional própria.
