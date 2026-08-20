# ADR 001 — Adotar um monólito modular

- **Situação:** aceita
- **Data:** 2026-08-20

## Contexto

O produto terá autenticação, cadastros, planejamento, operações, notificações, relatórios e auditoria. Apesar da amplitude funcional, será desenvolvido inicialmente por uma pessoa e não possui requisitos comprovados de escala ou autonomia de equipes que justifiquem microserviços.

## Decisão

A aplicação será construída como um monólito modular. Cada módulo terá responsabilidades, modelos e contratos explícitos. O domínio e a aplicação não dependerão diretamente de interface web, banco de dados ou provedores externos.

Os módulos iniciais serão:

- Identidade e organizações.
- Cadastros portuários.
- Escalas.
- Planejamento.
- Operações.
- Notificações.
- Relatórios e auditoria.

## Consequências positivas

- Desenvolvimento, testes e execução local mais simples.
- Transações consistentes para regras como conflito de berço.
- Menor custo operacional e de publicação.
- Fronteiras preparadas para uma separação futura, se necessária.
- Facilidade para depurar fluxos que atravessam diferentes módulos.

## Consequências negativas

- Falhas podem afetar o mesmo processo da aplicação.
- Disciplina arquitetural será necessária para evitar dependências indevidas.
- Escalabilidade será inicialmente aplicada ao conjunto da API.

## Alternativas rejeitadas

### Microserviços desde o início

Rejeitados por adicionarem comunicação distribuída, consistência eventual, observabilidade e implantação múltipla sem uma necessidade comprovada.

### Aplicação em uma única camada

Rejeitada porque mistura regras, persistência e HTTP, dificultando testes e evolução.

## Critério para reavaliar

A decisão poderá ser revista se um módulo exigir escala, disponibilidade, tecnologia ou ciclo de implantação realmente independente. A separação será consequência de métricas e necessidades observadas, não apenas do tamanho do projeto.

