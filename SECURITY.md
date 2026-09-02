# Política de segurança

## Versões suportadas

Este projeto demonstrativo mantém somente a versão mais recente da branch `main`. Correções de segurança não são retroportadas para versões anteriores.

## Como comunicar uma vulnerabilidade

Use **Security > Advisories > Report a vulnerability** no repositório do GitHub para enviar o relato de forma privada. Inclua:

- componente e versão afetados;
- passos mínimos para reprodução;
- impacto observado ou potencial;
- evidências sem dados pessoais, credenciais ou informações operacionais reais.

Não publique chaves, exploits funcionais ou detalhes sensíveis em uma issue pública. O recebimento será confirmado assim que possível; a análise, a correção e a divulgação coordenada dependerão da gravidade e da reprodutibilidade.

## Escopo

São considerados dentro do escopo a API, a interface web, os fluxos de autenticação e autorização, o banco de dados, as imagens Docker e os pipelines presentes neste repositório.

Dados e contas existentes no ambiente demonstrativo são fictícios. Nunca use credenciais ou documentos pertencentes a empresas, autoridades, embarcações ou pessoas reais ao reproduzir um problema.

## Controles implementados

Resumo do que está em vigor, para quem for avaliar o repositório ou retomar o trabalho.

### Sessão e identidade

- Access token JWT de curta duração, validado com emissor, público, tempo de vida e chave; tolerância de relógio reduzida para 30 segundos.
- O `SecurityStamp` do usuário é conferido contra o banco a cada requisição. Desativar uma conta ou trocar a senha derruba tokens que ainda não expiraram.
- Refresh token de 64 bytes aleatórios, guardado apenas como SHA-256, rotacionado a cada uso. Apresentar um token já rotacionado revoga todas as sessões daquele usuário.
- Cookie do refresh com `HttpOnly`, `Secure`, `SameSite=Strict` e caminho restrito. O front-end mantém o access token apenas em memória.
- Login e recuperação de senha gastam o mesmo tempo para e-mails existentes e inexistentes, para não permitir enumeração de contas por tempo de resposta.
- Bloqueio por tentativas inválidas e limite de 10 requisições por minuto, por endereço de cliente e por rota, nas rotas que recebem senha.

### Isolamento entre organizações

- O escopo de dados é derivado da requisição autenticada e **falha fechado**: sem requisição não há acesso.
- Processos internos que precisam ler todas as organizações pedem elevação explícita por `DataScopeContext.ElevateToSystem()`. A concessão nunca acontece por omissão.

### Rede e cabeçalhos

- `X-Forwarded-For` e `X-Forwarded-Proto` são aceitos apenas de proxies declarados em `Security:TrustedProxies`, com um único salto. Sem isso, o limite de tentativas seria global e a auditoria registraria sempre o IP do proxy.
- A interface envia `Content-Security-Policy`, `X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options`, `Permissions-Policy`, `Cross-Origin-Opener-Policy` e `Cross-Origin-Resource-Policy` em todos os `location` do nginx.
- HSTS e redirecionamento para HTTPS ficam ligados por padrão fora de desenvolvimento.

### Segredos e chaves

- As chaves de Data Protection precisam de um certificado que as cifre em repouso. Sem ele, a API recusa subir em produção, a menos que o risco seja assumido em `DataProtection:AllowUnprotectedKeys`.
- A chave de assinatura JWT exige no mínimo 32 bytes e vem de variável de ambiente.

### Cadeia de fornecimento

- Actions fixadas por SHA, `persist-credentials: false` e permissões mínimas por job.
- CodeQL em C# e TypeScript, auditoria de NuGet e npm, com execução semanal agendada.
- Imagens são construídas, varridas com Trivy e só então publicadas: uma vulnerabilidade alta ou crítica interrompe o release.
- SBOM e proveniência anexados às imagens publicadas.

### Contêineres

- API e interface rodam como usuário sem privilégios, com `no-new-privileges`, `cap_drop: ALL` e sistema de arquivos somente leitura na interface.
- As portas são publicadas apenas em `127.0.0.1`, para uso atrás de um proxy HTTPS.
