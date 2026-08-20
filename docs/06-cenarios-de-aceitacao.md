# Cenários de aceitação

Os cenários abaixo descrevem comportamentos observáveis. Eles serão transformados gradualmente em testes automatizados nas próximas partes.

## Cadastro de navio

### Cadastrar um IMO válido

```gherkin
Dado que não existe um navio com o número IMO informado
Quando um usuário autorizado cadastra o navio com dados válidos
Então o sistema cria o cadastro
E registra o autor e o horário da criação
```

### Impedir IMO duplicado

```gherkin
Dado que existe um navio ativo com determinado número IMO
Quando outro cadastro tenta utilizar o mesmo número
Então o sistema rejeita a operação
E não cria um segundo navio
```

## Planejamento de berço

### Confirmar uma janela disponível

```gherkin
Dado um berço disponível e compatível com o navio
E nenhuma reserva confirmada sobreposta
Quando o planejador confirma a janela
Então a reserva passa para confirmada
E a escala registra a alteração em seu histórico
```

### Impedir conflito de ocupação

```gherkin
Dado que um berço possui uma janela confirmada
Quando outra escala tenta confirmar um período sobreposto no mesmo berço
Então o sistema rejeita a confirmação
E informa que existe um conflito de ocupação
E nenhuma das reservas existentes é alterada
```

### Impedir berço incompatível

```gherkin
Dado que o calado informado para a escala supera o limite do berço
Quando o planejador tenta confirmar a janela
Então o sistema rejeita a confirmação
E apresenta a restrição que não foi atendida
```

## Ciclo da escala

### Executar uma transição válida

```gherkin
Dado que uma escala está planejada
Quando um usuário autorizado registra a chegada realizada ao fundeadouro
Então a escala passa para em fundeio
E o evento realizado é incluído na linha do tempo
```

### Impedir uma transição inválida

```gherkin
Dado que uma escala ainda está em análise
Quando um usuário tenta marcá-la diretamente como em operação
Então o sistema rejeita a transição
E mantém a situação anterior
```

### Cancelar sem apagar histórico

```gherkin
Dado que uma escala planejada precisa ser cancelada
Quando um usuário autorizado informa uma justificativa válida
Então a escala passa para cancelada
E seus eventos e reservas anteriores permanecem consultáveis
```

## Revisões temporais

### Atualizar uma previsão

```gherkin
Dado que uma escala possui uma ETA de berço registrada
Quando uma nova estimativa é informada
Então a nova ETA se torna a previsão vigente
E a estimativa anterior permanece no histórico
```

### Corrigir um evento realizado

```gherkin
Dado que um horário realizado foi registrado incorretamente
Quando um administrador autorizado envia a correção e a justificativa
Então o sistema preserva o evento anterior
E registra a correção, o autor, o motivo e o horário
```

## Concorrência

### Recusar atualização desatualizada

```gherkin
Dado que dois usuários abriram a mesma versão de uma escala
E o primeiro usuário salvou uma alteração
Quando o segundo tenta salvar usando a versão antiga
Então o sistema rejeita a sobrescrita
E solicita que os dados sejam atualizados antes de uma nova tentativa
```

## Autorização

### Restringir acesso organizacional

```gherkin
Dado um agente marítimo vinculado somente à organização A
Quando ele tenta consultar detalhes restritos de uma escala da organização B
Então o sistema nega o acesso
E não revela se o recurso existe
```

### Limitar conta demonstrativa

```gherkin
Dado um visitante autenticado na demonstração
Quando ele tenta alterar uma escala
Então o sistema nega a operação
E os dados permanecem inalterados
```

## Auditoria

### Registrar uma reprogramação

```gherkin
Dado que uma janela confirmada será reprogramada
Quando um planejador autorizado informa o novo período e a justificativa
Então o sistema salva a nova janela
E registra valores anteriores, novos valores, autor e justificativa
```

## Fusos horários

### Exibir um instante no fuso escolhido

```gherkin
Dado que um evento está armazenado em UTC
E o usuário escolheu o fuso America/Sao_Paulo
Quando a linha do tempo é exibida
Então o sistema converte o horário para o fuso escolhido
E informa visualmente qual fuso está sendo utilizado
```

