# Visão e escopo

## Contexto

Uma escala portuária reúne previsões, negociações, eventos realizados, recursos e responsabilidades de diferentes participantes. Quando essas informações ficam distribuídas em planilhas, mensagens e sistemas isolados, aumenta o risco de conflito de berço, perda de histórico e decisões baseadas em dados desatualizados.

## Problema a resolver

O sistema deverá manter uma visão única e auditável da passagem de um navio pelo porto, desde a solicitação da escala até seu encerramento. O planejamento precisa respeitar as características do navio, a capacidade do berço, as janelas existentes e as permissões de cada organização.

## Objetivos do produto

1. Manter cadastros confiáveis de navios, organizações, terminais e berços.
2. Planejar escalas e detectar conflitos antes da confirmação.
3. Registrar eventos estimados, solicitados, planejados e realizados.
4. Acompanhar fundeio, atracação, operação de carga e saída.
5. Preservar o histórico das alterações relevantes.
6. Disponibilizar indicadores de espera, operação e ocupação.
7. Permitir uma demonstração pública segura com dados sintéticos.

## Participantes

- Autoridade ou administração portuária.
- Terminal e operador portuário.
- Armador ou transportador marítimo.
- Agência marítima.
- Planejamento de berços.
- Equipe operacional.
- Auditoria.
- Visitante da demonstração.

## Escopo funcional da versão completa

- Identidade, organizações, usuários, papéis e permissões.
- Cadastro de navios, armadores, agentes, terminais e berços.
- Solicitação, análise, planejamento e acompanhamento de escalas.
- Planejamento de janelas de atracação com detecção de conflitos.
- Eventos de fundeio, berço, operação de carga e saída.
- Registro de carga e indicação de carga perigosa.
- Histórico de estados e trilha de auditoria.
- Notificações e atualizações em tempo real.
- Dashboard, relatórios e exportações.
- Simulador de posições para a demonstração.
- API preparada para integrações futuras.

## Fora do escopo inicial

- Substituir Porto Sem Papel, Siscomex, sistemas da Marinha ou outros sistemas oficiais.
- Emitir anuências, autorizações aduaneiras ou documentos oficiais.
- Processar cobrança portuária, folha de pagamento ou contratos comerciais.
- Utilizar dados AIS pagos ou credenciais de empresas na demonstração.
- Controlar equipamentos industriais ou decisões críticas de navegação.
- Garantir disponibilidade compatível com uma operação portuária real.

## Premissas

- Datas persistidas serão normalizadas em UTC e exibidas no fuso escolhido pelo usuário.
- A demonstração terá como fuso padrão `America/Sao_Paulo`.
- Registros públicos serão sintéticos e claramente identificados como demonstração.
- Integrações externas serão acessadas por adaptadores e poderão ser substituídas por simuladores.
- A arquitetura é um monólito modular com fronteiras bem definidas.

## Critérios de sucesso

- Nenhuma confirmação de escala cria sobreposição inválida no mesmo berço.
- Toda mudança operacional relevante registra autor, horário e valores alterados.
- Um usuário nunca consulta ou altera informações fora de seu escopo.
- A situação da escala sempre segue transições válidas.
- O ambiente demonstrativo pode ser restaurado sem intervenção manual.
- As principais regras possuem testes automatizados e documentação rastreável.

## Fontes de referência

- [DCSA Port Call Standard 2.0 — Purpose and Scope](https://reference.dcsa.org/content/standards/releases/port-call/v2-0-0/port-call-v2-0-0-purpose-and-scope)
- [IMO Integrated Identification Number Scheme](https://wwwcdn.imo.org/localresources/en/OurWork/IIIS/Documents/A%2034-Res.1215%20-%20INTEGRATED%20IMO%20IDENTIFICATION%20NUMBER%20SCHEME%20%28Secretariat%29.pdf)
- [Porto Sem Papel — manual de integração](https://www.gov.br/portos-e-aeroportos/pt-br/assuntos/transporte-aquaviario/porto-sem-papel/arquivos-para-download/manual-servicos-web-psp.pdf)
