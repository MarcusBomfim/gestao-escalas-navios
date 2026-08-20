# Glossário portuário

Este glossário padroniza os termos usados no projeto. Algumas organizações podem empregar siglas diferentes; o sistema manterá nomes explícitos no domínio e apresentará abreviações apenas quando forem úteis ao usuário.

| Termo | Significado no projeto |
|---|---|
| Navio / vessel | Embarcação marítima associada a uma ou mais escalas. |
| Escala / port call | Passagem planejada e realizada de um navio por um porto. |
| Número IMO | Identificador permanente formado por `IMO` e sete dígitos, quando aplicável ao navio. |
| MMSI | Identidade numérica utilizada em comunicações marítimas e AIS; não substitui o número IMO. |
| Indicativo de chamada | Código de rádio atribuído à estação do navio. |
| Armador | Organização responsável comercialmente pela operação do navio, conforme o contexto cadastrado. |
| Agente marítimo | Representante que atua em nome do armador durante a escala. |
| Terminal | Instalação portuária que reúne um ou mais berços e operações. |
| Berço / berth | Local específico no qual o navio atraca. |
| Fundeadouro / anchorage | Área em que o navio permanece fundeado antes ou durante a escala. |
| Janela de atracação | Intervalo reservado para uso de um berço por uma escala. |
| Atracação | Chegada e posicionamento do navio junto ao berço. |
| Desatracação | Saída do navio do berço. |
| LOA | Comprimento total do navio, do inglês *Length Overall*. |
| Boca / beam | Maior largura do navio. |
| Calado / draft | Distância vertical entre a linha d'água e a parte inferior do casco, usada na avaliação de compatibilidade. |
| ETA | Horário estimado de chegada a um local ou fase explicitamente indicada. |
| RTA | Horário solicitado de chegada. |
| PTA | Horário planejado e confirmado de chegada. |
| ATA | Horário realizado de chegada. |
| ETD | Horário estimado de partida. |
| RTD | Horário solicitado de partida. |
| PTD | Horário planejado e confirmado de partida. |
| ATD | Horário realizado de partida. |
| ETS / ATS | Horários estimado e realizado de início de um serviço. |
| ETC / ATC | Horários estimado e realizado de conclusão de um serviço. |
| ERPA | Padrão de negociação temporal: estimado, solicitado, planejado e realizado. |
| DUV | Documento Único Virtual utilizado no contexto do Porto Sem Papel; será apenas uma referência opcional de integração. |
| UN/LOCODE | Código internacional de cinco caracteres para localidades ligadas ao comércio e transporte. |
| AIS | Sistema de identificação automática que transmite dados de identidade, navegação e posição. |
| Carga perigosa | Carga sujeita a classificação e controles específicos; o projeto registra a condição, mas não concede autorização. |
| Auditoria | Registro de negócio que identifica quem alterou o quê, quando e em qual contexto. |
| Log técnico | Registro usado para operação e diagnóstico do software, separado da auditoria de negócio. |

## Convenção para horários

Uma sigla temporal isolada pode ser ambígua. A aplicação deverá associar sempre:

1. classificação: estimado, solicitado, planejado ou realizado;
2. ação: chegada, partida, início ou conclusão;
3. local ou serviço: fundeadouro, berço, praticagem ou operação de carga.

Exemplo: `ETA Berço` e `ETA Fundeadouro` são eventos diferentes.

