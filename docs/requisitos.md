# Requisitos

## Requisitos funcionais

### Identidade e acesso

- **RF-001:** autenticar usuários e permitir encerramento e revogação de sessões.
- **RF-002:** associar usuários a organizações com papéis específicos.
- **RF-003:** autorizar operações por permissão, organização e propriedade do recurso.
- **RF-004:** oferecer recuperação segura de acesso sem revelar a existência de uma conta.
- **RF-005:** disponibilizar contas de demonstração com privilégios limitados.

### Cadastros

- **RF-006:** cadastrar e consultar navios com validação do número IMO.
- **RF-007:** manter organizações, armadores e agências marítimas.
- **RF-008:** manter portos, terminais, berços, capacidades e indisponibilidades.
- **RF-009:** manter tipos de navio, carga e unidade de medida.
- **RF-010:** pesquisar, filtrar, ordenar e paginar os cadastros.

### Escalas e planejamento

- **RF-011:** criar e submeter uma solicitação de escala.
- **RF-012:** analisar, aprovar, rejeitar, planejar e cancelar uma escala.
- **RF-013:** propor e confirmar uma janela de atracação.
- **RF-014:** detectar sobreposição e incompatibilidade antes da confirmação.
- **RF-015:** reprogramar uma janela mediante permissão e justificativa.
- **RF-016:** controlar as transições da situação da escala.
- **RF-017:** consultar a linha do tempo completa da escala.

### Eventos e operações

- **RF-018:** registrar horários estimados, solicitados, planejados e realizados.
- **RF-019:** registrar chegada ao fundeadouro, atracação, operação e saída.
- **RF-020:** registrar operação de carga planejada e realizada.
- **RF-021:** registrar correções sem apagar o valor anterior.
- **RF-022:** distribuir atualizações relevantes em tempo real.

### Comunicação e análise

- **RF-023:** gerar notificações por atraso, conflito e mudança operacional.
- **RF-024:** disponibilizar dashboard conforme o escopo do usuário.
- **RF-025:** calcular espera, permanência, operação e ocupação de berço.
- **RF-026:** exportar relatórios autorizados em CSV e PDF.
- **RF-027:** consultar auditoria com filtros e paginação.

### Demonstração

- **RF-028:** popular o ambiente com dados sintéticos coerentes.
- **RF-029:** restaurar periodicamente o estado conhecido da demonstração.
- **RF-030:** fornecer posições simuladas sem depender de uma API AIS paga.

## Requisitos não funcionais

### Segurança

- **RNF-001:** aplicar menor privilégio e negação por padrão.
- **RNF-002:** proteger senhas com o mecanismo mantido pelo ASP.NET Core Identity.
- **RNF-003:** manter segredos fora do repositório e dos logs.
- **RNF-004:** validar toda entrada no limite da API.
- **RNF-005:** utilizar HTTPS fora do ambiente local.
- **RNF-006:** aplicar limitação de requisições nos endpoints expostos.
- **RNF-007:** manter dependências e imagens de container verificáveis.

### Confiabilidade e dados

- **RNF-008:** proteger regras críticas com transações e restrições no banco.
- **RNF-009:** utilizar controle otimista nas alterações concorrentes.
- **RNF-010:** preservar histórico operacional e auditoria.
- **RNF-011:** executar migrations de forma controlada.
- **RNF-012:** possuir estratégia documentada de backup e restauração para produção.

### Desempenho

- **RNF-013:** consultas paginadas comuns devem responder em até 500 ms no percentil 95, sob a carga de referência documentada em [observabilidade-e-resiliencia.md](observabilidade-e-resiliencia.md).
- **RNF-014:** operações demoradas devem ser assíncronas e não bloquear requisições web.
- **RNF-015:** índices e planos das consultas críticas devem ser avaliados com dados representativos.

### Qualidade

- **RNF-016:** regras de negócio críticas devem possuir testes unitários.
- **RNF-017:** persistência, autenticação e autorização devem possuir testes de integração.
- **RNF-018:** os principais fluxos do usuário devem possuir testes ponta a ponta.
- **RNF-019:** a compilação e os testes devem ser executados em integração contínua.
- **RNF-020:** decisões arquiteturais relevantes devem ser registradas em ADRs curtos.

### Operação e observabilidade

- **RNF-021:** cada requisição deve possuir identificador de correlação.
- **RNF-022:** logs devem ser estruturados e livres de segredos.
- **RNF-023:** API, banco e tarefas em segundo plano devem fornecer health checks.
- **RNF-024:** métricas e rastreamentos devem permitir investigar falhas e lentidão.

### Experiência e acessibilidade

- **RNF-025:** a interface deve ser responsiva e utilizável por teclado.
- **RNF-026:** componentes essenciais devem seguir WCAG 2.2 nível AA quando aplicável.
- **RNF-027:** carregamento, ausência de dados, sucesso e erro devem possuir estados visíveis.
- **RNF-028:** datas devem exibir o fuso utilizado e evitar ambiguidades.

### Portabilidade e manutenção

- **RNF-029:** desenvolvimento local deve ser reproduzível com Docker Compose.
- **RNF-030:** módulos do domínio não devem depender diretamente de interface, banco ou provedor externo.
- **RNF-031:** integrações externas devem possuir contrato, timeout, repetição controlada e simulador.
- **RNF-032:** a API deverá manter documentação OpenAPI atualizada.

## Priorização

### Essencial para a primeira versão funcional

- Identidade e autorização.
- Navios, terminais e berços.
- Escalas e transições.
- Planejamento e conflito de berço.
- Auditoria básica.
- Testes das regras críticas.

### Evolução após a primeira versão

- Tempo real.
- Simulação de posições.
- Notificações assíncronas.
- Relatórios avançados.
- Observabilidade completa.
- Exportações e integrações externas.
