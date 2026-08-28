# Conta Corrente — Desafio Técnico

API em C# / .NET 9 para controlar as movimentações de uma conta empresarial — entradas,
saídas, saldo e histórico — **sem jamais permitir saldo negativo**, acompanhada de uma
interface em React que a consome.

```
┌──────────────────┐   proxy /api    ┌──────────────────┐
│ ContaCorrente.Web│ ──────────────▶ │ ContaCorrente.Api│ ──▶ SQLite
│  React + Vite    │  localhost:5232 │  .NET 9 + EF Core│
│  localhost:5173  │                 │   CQRS           │
└──────────────────┘                 └──────────────────┘
```

## Como executar

**Pré-requisitos:** .NET SDK 9 e Node.js 20+.

### 1. API

Sobe em <http://localhost:5232>. As migrations são aplicadas no startup — não há passo
manual de banco. Documentação interativa em <http://localhost:5232/swagger>.

### 2. Interface

Sobe em <http://localhost:5173> já apontando para a API. O front chama `/api/...` na
própria origem e o proxy do Vite encaminha para a API — **em desenvolvimento não há CORS**.

Para ver a tela sem subir o backend, troque `VITE_USE_MOCK=true` em
`ContaCorrente.Web/.env.development`: um repositório em memória reproduz as mesmas regras,
inclusive a recusa de saldo insuficiente.

## Endpoints

| Método | Rota | Descrição |
| --- | --- | --- |
| `POST` | `/api/contas` | Cria conta (saldo inicial zero) |
| `GET` | `/api/contas` | Lista contas |
| `GET` | `/api/contas/{id}/saldo` | **Consulta o saldo disponível** |
| `POST` | `/api/contas/{id}/movimentacoes` | **Registra entrada ou saída** |
| `GET` | `/api/contas/{id}/movimentacoes` | **Consulta o histórico** (paginado, filtros de período, tipo e forma de pagamento) |
| `GET` | `/health` | Verificação de saúde |

Entrada e saída compartilham a mesma rota, distinguidas por `tipo` no corpo — elas dividem
validação, transação e formato de resposta; o que muda é só qual método do domínio é chamado.

### Forma de pagamento

Cada movimentação pode registrar **como** o dinheiro entrou ou saiu:

| Valor | Significado |
| --- | --- |
| `Pix` | PIX |
| `Boleto` | Boleto |
| `CartaoCredito` | Cartão de crédito |
| `CartaoDebito` | Cartão de débito |
| `TransferenciaBancaria` | Transferência bancária |
| `Dinheiro` | Dinheiro |

O campo é **opcional**: lançamentos antigos não têm essa informação, e o extrato é registro
histórico — não se reescreve o passado. Um valor fora da lista é recusado com `400`.

Repare que `CartaoCredito`/`CartaoDebito` **não** têm relação com `tipo: Credito`/`Debito`:
lá o sentido é contábil (entrada ou saída), aqui é o instrumento de pagamento. Os nomes são
longos justamente para que as duas dimensões não se confundam.

Os filtros do histórico são combináveis:

```
GET /api/contas/{id}/movimentacoes?tipo=Debito&formaPagamento=Pix&pagina=1&tamanho=10
```

### Fluxo completo com `curl`

# Cria a conta e guarda o id
ID=$(curl -s -X POST $BASE -H "Content-Type: application/json" \
  -d '{"nome":"Act Digital LTDA","documento":"12345678000199"}' \
  | sed -n 's/.*"id":"\([^"]*\)".*/\1/p')

# Entrada de 1000 via PIX
curl -s -X POST $BASE/$ID/movimentacoes -H "Content-Type: application/json" \
  -d '{"tipo":"Credito","valor":1000,"descricao":"Aporte inicial","formaPagamento":"Pix"}'

# Saldo -> 1000
curl -s $BASE/$ID/saldo

# Saída de 300
curl -s -X POST $BASE/$ID/movimentacoes -H "Content-Type: application/json" \
  -d '{"tipo":"Debito","valor":300,"descricao":"Pagamento fornecedor"}'

# Saída de 5000 -> 422, recusada
curl -s -X POST $BASE/$ID/movimentacoes -H "Content-Type: application/json" \
  -d '{"tipo":"Debito","valor":5000}'

# Histórico, do mais recente para o mais antigo
curl -s "$BASE/$ID/movimentacoes?pagina=1&tamanho=10"

# Só as saídas feitas por PIX
curl -s "$BASE/$ID/movimentacoes?tipo=Debito&formaPagamento=Pix"
```

A recusa devolve `ProblemDetails` (RFC 7807):

```json
{
  "title": "Saldo insuficiente",
  "status": 422,
  "detail": "Saldo insuficiente. Disponível: 700,00, solicitado: 5000,00.",
  "saldoDisponivel": 700,
  "valorSolicitado": 5000
}
```

## Estrutura

```
ContaCorrente.Api/          API .NET 9
├── Domain/                 entidades e regras de negócio
├── Application/            CQRS: abstrações, dispatcher, commands, queries
├── Infrastructure/         EF Core, migrations, conversores
└── Api/                    controllers, requests e tradução de exceções

ContaCorrente.Tests/        60 testes: domínio, handlers, HTTP, concorrência
ContaCorrente.Web/          interface React (README próprio)
docs/DECISOES.md            registro das decisões técnicas
```

### O fluxo de uma requisição

```
HTTP → ContasController → IDispatcher → [validação → transação] → Handler → Conta (domínio)
```

O controller só traduz HTTP. O handler orquestra. **Quem aceita ou recusa a movimentação é
a entidade `Conta`** — `Debitar` lança `SaldoInsuficienteException` e a transação inteira é
revertida. O `DomainExceptionHandler` converte a exceção em `422`.

## Decisões técnicas

Detalhamento em [docs/DECISOES.md](docs/DECISOES.md). Em resumo:

- **A regra vive no domínio, não no handler.** `Conta.Debitar` é o único lugar que decide
  sobre saldo. Setters privados e coleção somente leitura tornam impossível deixar a
  entidade inconsistente por fora.
- **Saldo materializado + token de concorrência.** O saldo fica em coluna própria (leitura
  O(1)) e é protegido por concorrência otimista; cada movimentação guarda o
  `saldoResultante`, permitindo reconciliar o extrato com o saldo.
- **Dinheiro em centavos.** `decimal` no domínio, `INTEGER` no banco via `ValueConverter` —
  o SQLite não tem decimal nativo e cairia em ponto flutuante.
- **CQRS com dispatcher próprio (~150 linhas), sem MediatR.** Commands passam por
  validação → transação → handler; queries vão direto ao banco com `AsNoTracking` e
  projeção. Sem dependência externa e sem a licença comercial que o MediatR passou a exigir.
- **Sem repositório sobre o `DbContext`.** Ele já é Unit of Work + Repository; envolvê-lo de
  novo seria indireção sem ganho.

## Consistência sob concorrência

O requisito de nunca permitir saldo negativo só é interessante sob acesso simultâneo — é
onde uma verificação ingênua (`if (saldo >= valor)` seguida de um `UPDATE`) falha.

A defesa tem três camadas: a regra dentro do agregado, a transação aberta pelo dispatcher em
volta do handler, e um token de concorrência otimista que faz o EF Core rejeitar escritas
sobrepostas na mesma conta (com um retry automático; esgotado, a resposta é `409`).

Dois testes cobrem isso em `ContaCorrente.Tests/Api/ConcorrenciaTests.cs`:

- 20 saídas simultâneas numa conta que comporta 5 — exatamente 5 são aceitas, 15 recebem
  `422`, nenhuma resulta em erro interno, e o saldo final é zero.
- 30 movimentações mistas disparadas juntas — ao final, a soma do extrato bate exatamente
  com o saldo materializado.

## O que não foi implementado

Fora do escopo do desafio, mas os caminhos naturais de evolução:

- **Autenticação e autorização** — a API é aberta.
- **Idempotência** via header `Idempotency-Key`, para que um retry de rede não duplique
  uma movimentação.
- **Estorno/reversão** de lançamento, preservando o extrato como log append-only.
- **Extrato por período com saldo de abertura**, o formato que um contador espera.
- **Pipeline de behaviors encadeados** no dispatcher (logging, métricas), no lugar dos dois
  passos fixos de hoje.
- **Troca do SQLite por PostgreSQL** — isolada em uma linha do `Program.cs`, mas exigiria
  revisar o conversor de centavos e o token de concorrência.
- **Observabilidade** — logs estruturados, métricas e tracing.
- **Testes end-to-end** (Playwright) cobrindo front e API juntos.
- **Cliente da API gerado a partir do OpenAPI** no front, hoje tipado à mão.
