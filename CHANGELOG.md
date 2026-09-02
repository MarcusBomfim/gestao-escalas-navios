# Histórico de versões

As mudanças relevantes deste projeto seguem o formato do [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e o versionamento semântico.

## [Não publicado]

### Segurança

- o escopo de dados passa a falhar fechado: sem requisição HTTP não há acesso, e processos internos pedem elevação explícita;
- `X-Forwarded-For` e `X-Forwarded-Proto` são processados, aceitos somente de proxies declarados e com um único salto, o que devolve o IP real ao limite de tentativas e à auditoria;
- login e recuperação de senha gastam o mesmo tempo para e-mails existentes e inexistentes;
- `Content-Security-Policy` na interface, e cabeçalhos de segurança incluídos em todos os `location` do nginx;
- HSTS e redirecionamento para HTTPS ligados por padrão fora de desenvolvimento;
- chaves de Data Protection exigem certificado que as cifre em repouso;
- interface servida por nginx sem privilégios, com sistema de arquivos somente leitura e `cap_drop: ALL`;
- imagens varridas com Trivy antes da publicação, interrompendo o release em vulnerabilidade alta ou crítica.

### Adicionado

- 23 testes de regressão para os controles acima, incluindo recusa de `X-Forwarded-For` forjado e verificação de que todo `location` do nginx carrega os cabeçalhos.

## [1.0.0] — 2026-08-28

### Adicionado

- domínio de navios, escalas, planejamento de berços e execução de carga;
- torre de controle com mapa operacional simulado e SignalR;
- autenticação JWT, refresh token rotativo e recuperação de senha;
- autorização por papéis e isolamento organizacional;
- gestão administrativa de usuários e cadastros mestres;
- acesso público demonstrativo somente leitura;
- auditoria, relatórios CSV, observabilidade e health checks;
- contrato OpenAPI, testes unitários, de integração, arquitetura, navegador e desempenho;
- pipelines de CI, segurança, release e imagens Docker;
- configuração de produção e documentação operacional.

### Segurança

- segredos fornecidos exclusivamente por variáveis de ambiente;
- senha e refresh tokens armazenados somente por hash;
- bloqueio de conta, limitação de tentativas e revogação de sessões;
- proteção contra sobreposição, concorrência e acesso fora do escopo;
- dados públicos integralmente sintéticos.

[1.0.0]: https://github.com/MarcusBomfim/gestao-escalas-navios/releases/tag/v1.0.0
