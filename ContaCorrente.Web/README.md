# ContaCorrente.Web

Interface em React que consome a API `ContaCorrente.Api` (C# / .NET) do desafio técnico:
registro de entradas e saídas, consulta de saldo e histórico de movimentações.

O layout segue a identidade visual da act digital: cabeçalho azul, fundo azul-acinzentado
e cards escuros com faixa de destaque em gradiente.

## Stack

| Item | Escolha | Motivo |
| --- | --- | --- |
| Build | Vite | Dev server rápido, sem configuração extra |
| Linguagem | TypeScript | O contrato da API fica explícito e verificado em tempo de compilação |
| Estado | `useState` + um hook (`useConta`) | O escopo é uma tela; Redux/Zustand seriam complexidade desnecessária |
| Estilo | CSS puro com variáveis | Sem dependência de framework de UI para reproduzir a identidade visual |
| Testes | Vitest + Testing Library | Mesmo runner do Vite; testes focados em comportamento, não em implementação |

## Como executar

Pré-requisitos: Node.js 20+.

```bash
npm install
npm run dev
```

A aplicação sobe em <http://localhost:5173>.

### Consumindo a API .NET (padrão)

`.env.development` vem com `VITE_USE_MOCK=false`, então a tela consome a API real. Suba-a
antes, a partir da raiz do repositório:

```bash
dotnet run --project ContaCorrente.Api
```

Com `VITE_API_URL` vazia, o front chama `/api/...` na própria origem e o **proxy do Vite**
encaminha para `VITE_API_PROXY` — assim não é preciso configurar CORS em desenvolvimento.
Em produção, basta apontar `VITE_API_URL` para a URL pública da API.

### Modo demonstração (sem backend)

Trocando para `VITE_USE_MOCK=true` e reiniciando o `npm run dev`, a tela passa a usar um
repositório em memória que reproduz as regras da API — inclusive o bloqueio de saldo
negativo. Útil para ver a interface sem subir o .NET. Os dados somem ao recarregar a página.

## Scripts

| Comando | Descrição |
| --- | --- |
| `npm run dev` | Servidor de desenvolvimento com HMR |
| `npm run build` | Checagem de tipos (`tsc -b`) + build de produção em `dist/` |
| `npm run preview` | Serve o build de produção |
| `npm test` | Executa a suíte de testes |
| `npm run test:watch` | Testes em modo observação |

## Integração com a API

| Método | Rota | Corpo |
| --- | --- | --- |
| `GET` | `/api/contas` | — |
| `POST` | `/api/contas` | `{ "nome": "...", "documento": "..." }` |
| `GET` | `/api/contas/{id}/saldo` | — |
| `GET` | `/api/contas/{id}/movimentacoes?pagina=1&tamanho=10&tipo=Debito&formaPagamento=Pix` | — |
| `POST` | `/api/contas/{id}/movimentacoes` | `{ "tipo": "Credito", "valor": 100.00, "descricao": "Venda", "formaPagamento": "Pix" }` |

### Tradução de vocabulário

A API fala `Credito`/`Debito`; a tela fala **Entrada**/**Saída**, que é o termo do enunciado
e o que o usuário entende. A conversão — junto com `ocorridaEm → dataHora` e
`saldoResultante → saldoApos` — acontece num **único lugar**: [`src/api/httpApi.ts`](src/api/httpApi.ts).
Nenhum componente conhece o formato do backend, então mudanças de contrato ficam contidas
nesse arquivo.

A **forma de pagamento** é a exceção: os valores trafegados (`Pix`, `CartaoCredito`, …) são
os mesmos da API, porque não há ambiguidade a resolver. O que existe é uma tabela de rótulos
de exibição em [`src/utils/rotulos.ts`](src/utils/rotulos.ts) — `CartaoCredito` vira
"Cartão de crédito" na tela, mas continua `CartaoCredito` no fio.

### Filtros do histórico

Tipo e forma de pagamento são filtrados **no servidor**: cada mudança dispara uma consulta
nova com os parâmetros na query string. Filtrar o array em memória só funcionaria sobre a
página já carregada — com 10 itens por página, daria resultado errado.

Trocar de filtro sempre volta para a página 1. Registrar uma movimentação **preserva** os
filtros ativos: se o novo lançamento não corresponder a eles, ele não aparece na tabela, e a
confirmação de sucesso é o que sinaliza que deu certo.

### Erros

O cliente lê `detail`, `mensagem`, `errors` ou `title` de um `ProblemDetails` (RFC 7807) e
transforma em `ApiError` com mensagem exibível:

- **422** — saldo insuficiente. `ApiError.isSaldoInsuficiente` fica `true` e o texto do
  domínio (`"Saldo insuficiente. Disponível: 1000,00, solicitado: 5000,00."`) aparece no
  formulário. É a API que decide, não o front.
- **400** — validação. Os erros por campo ficam em `ApiError.errosPorCampo`.
- **404** — conta inexistente.
- **status 0** — API fora do ar, com aviso amigável em vez de erro cru de rede.

## Estrutura

```
src/
├── api/
│   ├── client.ts        fetch + tratamento de erro (ApiError)
│   ├── httpApi.ts       implementação HTTP do contrato
│   ├── mockApi.ts       implementação em memória (modo demonstração)
│   └── index.ts         escolhe a implementação por variável de ambiente
├── components/          Header, Card, SeletorConta, FiltrosHistorico e os blocos da tela
├── hooks/useConta.ts    estado da conta (saldo, histórico, paginação, envio, erro)
├── utils/format.ts      moeda, data e parsing do valor digitado
├── utils/rotulos.ts     rotulos de exibicao (CartaoCredito -> "Cartao de credito")
├── types.ts             contrato compartilhado (ContaCorrenteApi)
├── styles/global.css    paleta e componentes visuais
└── App.tsx              composição da página
```

## Decisões técnicas

- **A API é a fonte da verdade do saldo.** O front nunca soma valores localmente para
  exibir o saldo: após cada movimentação, saldo e histórico são recarregados juntos
  (`Promise.all`). Isso evita divergência caso outra sessão movimente a mesma conta.
- **Validação dividida por responsabilidade.** O formulário valida apenas formato
  (valor > 0, descrição obrigatória). A regra de saldo insuficiente permanece na API, e
  a mensagem devolvida por ela é exibida — não há duplicação da regra de negócio.
- **API abstraída por interface (`ContaCorrenteApi`).** O mesmo contrato tem duas
  implementações (HTTP e memória) e é injetável no `App`, o que permite testar a tela
  sem rede e demonstrar o front sem o backend.
- **Valor aceita `1.500,50` e `1500.50`.** A conversão fica isolada em `paraNumero` e é
  arredondada para 2 casas antes do envio.
- **Sem biblioteca de estado ou de UI.** Uma tela com poucas operações não justifica o
  custo dessas dependências.
- **Vocabulário da tela isolado do da API.** Ver "Tradução de vocabulário" acima.

## Testes

```bash
npm test
```

33 testes cobrindo:

- `format.test.ts` — formatação de moeda/data e parsing do valor digitado.
- `httpApi.test.ts` — tradução Entrada↔Credito e Saída↔Debito, envio da forma de pagamento,
  montagem da query string dos filtros (e a omissão dos ausentes), conversão da movimentação
  e da página de histórico, e a transformação de 422/400/rede em `ApiError` exibível.
- `MovimentacaoForm.test.tsx` — bloqueio de valor zero e descrição vazia, envio de
  entrada e saída com o payload correto, envio (e omissão) da forma de pagamento, exibição
  do erro devolvido pela API.
- `App.test.tsx` — saldo/histórico da conta selecionada, seleção automática da primeira
  conta, recarga **conjunta** de saldo e extrato após registrar, filtro por forma de
  pagamento consultando a API, garantia de que o extrato é buscado **uma única vez** ao
  abrir a conta, alerta de API fora do ar e o convite a criar conta quando não há nenhuma.

## Melhorias futuras

- Filtro de período no histórico (a API já o aceita via query string).
- Totais de toda a conta, hoje calculados apenas sobre a página exibida — o caminho seria
  um endpoint de resumo na API.
- Atualização otimista com reconciliação, em vez de recarregar tudo após cada registro.
- Autenticação.
- Testes end-to-end (Playwright) cobrindo front + API em execução.
