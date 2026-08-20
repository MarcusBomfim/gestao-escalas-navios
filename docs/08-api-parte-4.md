# API — Parte 4

Esta etapa disponibiliza os primeiros casos de uso da aplicação por uma API REST. Os enums são enviados como texto e os instantes usam UTC.

## Endpoints de consulta

| Método | Rota | Finalidade |
| --- | --- | --- |
| `GET` | `/api/v1/reference-data/ports` | Lista portos, terminais e berços ativos. |
| `GET` | `/api/v1/vessels` | Lista navios com paginação e busca. |
| `GET` | `/api/v1/vessels/{id}` | Consulta um navio. |
| `GET` | `/api/v1/port-calls` | Lista escalas com filtros. |
| `GET` | `/api/v1/port-calls/{publicCode}` | Consulta detalhes e histórico de situação. |

Filtros disponíveis:

```text
GET /api/v1/vessels?page=1&pageSize=20&search=demo&activeOnly=true
GET /api/v1/port-calls?page=1&pageSize=20&status=Planned&portId={guid}&search=demo
```

O tamanho máximo de página é `100`.

## Endpoints de escrita no desenvolvimento

Enquanto a autenticação não está implementada, as rotas de escrita são registradas somente quando `Features:EnableUnauthenticatedWrites` está habilitada. O valor padrão é `false`; o ambiente `Development` habilita essas rotas para testes locais.

### Cadastrar um navio

```http
POST /api/v1/vessels
Content-Type: application/json

{
  "name": "Navio Portfolio",
  "imoNumber": "IMO9074729",
  "flagCode": "BR",
  "type": "ContainerShip",
  "lengthOverallMeters": 260,
  "beamMeters": 40,
  "maximumDraftMeters": 12.5,
  "callSign": "P9DEMO",
  "mmsi": null
}
```

### Criar uma escala

O cabeçalho `Idempotency-Key` é obrigatório. Repetir a mesma chave retorna a escala original sem criar duplicidade.

```http
POST /api/v1/port-calls
Idempotency-Key: portfolio-example-001
Content-Type: application/json

{
  "vesselId": "50000000-0000-0000-0000-000000000001",
  "portId": "10000000-0000-0000-0000-000000000001",
  "purpose": "CargoOperation",
  "voyageNumber": "PORTFOLIO-001",
  "previousPortUnLocode": "BRRIO",
  "nextPortUnLocode": "BRPNG"
}
```

### Alterar a situação de uma escala

`expectedVersion` deve conter a versão devolvida pela última consulta. Uma versão desatualizada retorna `409 Conflict`.

```http
POST /api/v1/port-calls/{publicCode}/transitions
Content-Type: application/json

{
  "newStatus": "Requested",
  "expectedVersion": 0,
  "reason": null
}
```

As transições permitidas continuam sendo controladas pelo domínio. Um salto de `Draft` diretamente para `InOperation`, por exemplo, retorna erro de validação.

## Erros

Erros esperados usam `Problem Details` e incluem um código estável:

```json
{
  "title": "Conflict",
  "status": 409,
  "detail": "A escala foi alterada por outra operação.",
  "code": "port_calls.version_conflict"
}
```

## Dados demonstrativos

O serviço `seed-demo` cria registros sintéticos de forma idempotente:

- Porto de Santos identificado explicitamente como ambiente demonstrativo.
- um terminal e dois berços fictícios;
- três navios sem números IMO reais;
- três escalas em situações diferentes;
- duas organizações fictícias.

Os identificadores fixos usados nos exemplos pertencem somente ao conjunto demonstrativo.
