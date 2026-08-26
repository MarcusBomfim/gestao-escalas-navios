# Mapa operacional simulado — Parte 18

## Objetivo

Oferecer uma visão espacial das escalas ativas sem consumir uma API AIS, serviço cartográfico, credencial externa ou dado operacional real. O recurso atende ao cenário de portfólio e demonstra como o sistema poderia receber posições de um provedor especializado no futuro.

## Fluxo da informação

1. o repositório da torre consulta as escalas que ainda não foram encerradas ou canceladas;
2. `VesselTrafficSimulator` associa cada situação da escala a um estado de navegação demonstrativo;
3. o simulador calcula coordenadas relativas entre 5% e 95% da área desenhada;
4. `ControlTowerEvaluator` inclui o tráfego no mesmo snapshot de indicadores e alertas;
5. `GET /api/v1/control-tower` entrega o estado inicial ao navegador;
6. o serviço em segundo plano publica atualizações autenticadas pelo hub `/hubs/control-tower`;
7. o TanStack Query recebe o novo snapshot pelo SignalR e atualiza o mapa sem recarregar a página.

Indicadores e posições compartilham a mesma consulta operacional. Essa decisão evita uma segunda leitura completa do banco a cada atualização.

## Estados apresentados

| Situação da escala | Estado no mapa | Velocidade demonstrativa |
| --- | --- | ---: |
| `Draft`, `Requested`, `UnderReview` | aguardando programação | 0 nó |
| `Planned` | em aproximação | 8,4 nós |
| `AtAnchorage` | no fundeadouro | 0 nó |
| `ClearedForBerthing` | em manobra | 3,2 nós |
| `Berthed` | atracado | 0 nó |
| `InOperation` | em operação | 0 nó |
| `OperationCompleted` | pronto para saída | 0 nó |
| `Unberthed` | em saída | 9,1 nós |

As posições são determinísticas a partir do identificador da escala e do instante arredondado em intervalos de cinco segundos. Navios em aproximação, manobra ou saída recebem um deslocamento visual pequeno; navios atracados permanecem estáveis.

## Contrato retornado

O campo `traffic` da torre contém:

- horário de geração em UTC;
- rótulo da cobertura demonstrativa;
- marcador global `isSimulated`;
- escala e navio relacionados;
- situação operacional e estado de navegação;
- coordenadas relativas `xPercent` e `yPercent`;
- velocidade e rumo demonstrativos;
- horário observado e marcador `isSimulated` por posição.

O mesmo contrato é publicado no OpenAPI em desenvolvimento. A rota e o canal em tempo real exigem autenticação; todos os papéis podem consultar o mapa em modo somente leitura.

## Interface e acessibilidade

O mapa usa somente SVG, HTML e CSS do próprio projeto. Não há tiles, fontes, scripts ou imagens carregados de terceiros.

Além dos pontos visuais, a interface oferece:

- nome e estado de cada navio como texto acessível no SVG;
- lista equivalente ao lado do mapa;
- links para abrir a escala correspondente;
- legenda por cor e descrição textual do estado;
- layout adaptado para telas menores;
- redução automática das animações quando o sistema operacional solicita menos movimento;
- aviso permanente de que as posições não representam rastreamento AIS real.

## Segurança e limites

- nenhuma latitude, longitude, mensagem AIS ou posição real é armazenada;
- não existe chave de API relacionada ao mapa;
- o algoritmo não deve ser usado para navegação, manobra ou decisão operacional;
- os dados não substituem autoridade portuária, praticagem, VTS ou sistemas oficiais;
- uma futura integração real deverá implementar contrato próprio, autenticação, timeout, resiliência, auditoria e separação por organização.

## Testes

`VesselTrafficSimulatorTests` verifica:

1. marcação explícita de dados simulados;
2. arredondamento do horário de observação;
3. limites das coordenadas relativas;
4. correspondência entre situação da escala e estado no mapa;
5. movimento dos navios em trânsito;
6. estabilidade dos navios atracados.

O cenário Playwright do perfil visitante também confirma que o mapa e o aviso sobre AIS aparecem no painel sem conceder permissões de escrita.
