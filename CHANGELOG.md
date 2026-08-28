# Histórico de versões

As mudanças relevantes deste projeto seguem o formato do [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e o versionamento semântico.

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
