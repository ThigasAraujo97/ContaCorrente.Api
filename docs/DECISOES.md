# Decisões técnicas

Registro curto de cada decisão relevante, o que foi descartado e por quê. Serve de apoio
para a conversa sobre a solução.

---

## 1. SQLite como banco

**Decisão.** EF Core com SQLite em arquivo, migrations aplicadas no startup.

**Por quê.** O avaliador clona e roda `dotnet run` — sem Docker, sem instalar servidor, sem
passo de banco. Ainda assim é um banco relacional de verdade, com transações reais.

**Descartado.** `EF Core InMemory` não suporta transações — inviabilizaria justamente o
argumento central de consistência. SQL Server em Docker seria mais próximo de produção ao
custo de exigir Docker rodando para qualquer teste.

**Consequência.** WAL é habilitado no startup para leituras não bloquearem durante escritas.
Trocar por PostgreSQL é uma linha no `Program.cs`, mas exigiria revisar o conversor de
centavos e o token de concorrência.

---

## 2. A regra de negócio mora no domínio

**Decisão.** `Conta.Debitar` valida o saldo e lança `SaldoInsuficienteException`. O handler
não tem um `if` sobre saldo.

**Por quê.** É a única forma de garantir que a regra não seja contornada. `Conta` tem
setters privados, construtor privado para o EF e expõe `Movimentacoes` como
`IReadOnlyCollection`: não existe caminho que altere o saldo sem passar por `Creditar` ou
`Debitar`, e cada um deles registra o lançamento correspondente na mesma operação.

**Descartado.** Validar no controller ou no validator do comando. Além de duplicar a regra,
abriria uma janela entre a checagem e a escrita — exatamente o bug que o teste de
concorrência procura.

---

## 3. Saldo materializado + token de concorrência

**Decisão.** `Conta.Saldo` é coluna própria, não `SUM()` das movimentações. `Conta.Versao`
(um `Guid` trocado a cada movimentação) é marcado `IsConcurrencyToken()`.

**Por quê.** Consulta de saldo em O(1), sem varrer o extrato. O token faz o `UPDATE` levar
`WHERE Versao = <valor lido>`: se outra requisição movimentou a conta no intervalo, zero
linhas são afetadas e o EF lança `DbUpdateConcurrencyException`. O dispatcher tenta uma vez
mais; persistindo o conflito, a resposta é `409`.

**Custo.** O saldo pode, em tese, divergir do extrato. Mitigação: cada `Movimentacao` guarda
`SaldoResultante`, então a reconciliação é sempre possível — e há um teste que a verifica
após 30 movimentações concorrentes.

**Descartado.** Calcular o saldo por `SUM()` a cada leitura. Mais simples e sempre correto,
mas degrada conforme o extrato cresce e ainda assim precisaria de bloqueio para evitar a
corrida entre a soma e a inserção.

---

## 4. Dinheiro em centavos

**Decisão.** `decimal` no domínio; persistido como `long` de centavos por um
`ValueConverter`.

**Por quê.** O SQLite não tem tipo decimal nativo — o provider cairia em `REAL` (ponto
flutuante), e comparar ou somar dinheiro em ponto flutuante é defeito garantido. Com
`INTEGER`, toda aritmética no banco é exata, e o domínio segue trabalhando com `decimal`.

**Consequência.** Valores são arredondados para 2 casas na gravação
(`MidpointRounding.AwayFromZero`). Um sistema que precisasse de mais casas — câmbio, juros —
exigiria revisar essa escolha.

---

## 5. CQRS com dispatcher próprio, sem MediatR

**Decisão.** `ICommand<T>` / `IQuery<T>` com handlers próprios e um `Dispatcher` de ~150
linhas resolvendo o handler pelo tipo concreto da mensagem.

**Por quê.** O fluxo controller → command → handler separa escrita de leitura e deixa cada
operação num arquivo com nome próprio. O dispatcher é curto o bastante para ser lido inteiro
e explicado linha a linha — não há mágica escondida.

O ponto técnico: em `Send<TResult>(ICommand<TResult>)` só `TResult` é conhecido em
compilação; o tipo do comando só existe em runtime. O invoker genérico é fechado por
reflection **uma vez por tipo** e guardado num `ConcurrentDictionary`, então a partir da
segunda chamada é despacho virtual comum.

**Descartado.** MediatR — padrão de mercado e reconhecível, mas passou a exigir licença
comercial em 2025, e traz um pipeline muito maior do que este escopo pede.

---

## 6. Pipeline de dois passos, não behaviors encadeados

**Decisão.** `Send` executa validação → transação → handler. `Ask` não faz nem uma nem outra.

**Por quê.** Tira o boilerplate dos handlers (nenhum abre transação ou chama `SaveChanges`)
sem virar um framework. A validação roda **antes** da transação: comando inválido não chega
a abrir transação — e há um teste que garante isso.

**Descartado.** Uma lista de behaviors componível ao estilo MediatR. É a evolução natural
quando surgir o terceiro ou quarto passo transversal; com dois passos e cinco handlers, o
código explícito é mais legível.

---

## 7. Sem repositório sobre o DbContext

**Decisão.** Os handlers usam `ContaCorrenteDbContext` diretamente.

**Por quê.** O `DbContext` já **é** Unit of Work + Repository. Envolvê-lo em
`IContaRepository`/`IUnitOfWork` acrescentaria indireção sem desacoplar nada de real — e os
testes usam SQLite de verdade, não mocks de repositório, o que dá muito mais confiança.

---

## 8. `ValueGeneratedNever()` nas chaves primárias

**Decisão.** `Conta.Id` e `Movimentacao.Id` são declarados `ValueGeneratedNever()`.

**Por quê.** Não é cosmético — sem isso a aplicação **não funciona**. O domínio gera o `Guid`
no construtor. Por padrão o EF assume que gera chaves `Guid` ele mesmo; ao descobrir a
movimentação pela coleção do agregado e ver a chave já preenchida, conclui "linha existente"
e emite `UPDATE` em vez de `INSERT`. O `UPDATE` não afeta nenhuma linha, e o EF reporta isso
como `DbUpdateConcurrencyException` — um erro que aponta para o lugar errado.

Foi exatamente o que aconteceu na primeira execução da suíte: 14 testes falhando com
conflito de concorrência em operações que não tinham concorrência alguma.

**Alternativa descartada.** Chamar `db.Movimentacoes.Add(...)` no handler. Resolve, mas só
naquele caminho: qualquer código futuro que movimente pelo domínio voltaria a quebrar. A
correção no mapeamento vale para todos os caminhos.

---

## 9. Datas sempre em UTC com sufixo `Z`

**Decisão.** Um `UtcDateTimeConverter` aplicado por convenção a todo `DateTime` do modelo.

**Por quê.** O SQLite não guarda fuso: na leitura o EF devolve `DateTimeKind.Unspecified` e o
`System.Text.Json` serializa sem o `Z`. O JavaScript interpreta data sem `Z` como horário
**local** — o extrato apareceria deslocado pelo fuso do navegador. O defeito é silencioso:
só aparece fora do UTC.

Há um teste de integração que falha se o `Z` sumir da resposta.

---

## 10. Uma rota para entrada e saída

**Decisão.** `POST /api/contas/{id}/movimentacoes` com `tipo` no corpo, em vez de
`/creditos` e `/debitos`.

**Por quê.** As duas operações compartilham validação, transação, formato de resposta e
lugar no extrato. O que muda é uma linha: qual método do domínio é chamado. Duas rotas
duplicariam tudo em volta dessa única diferença.

---

## 11. Vocabulário da tela separado do da API

**Decisão.** A API fala `Credito`/`Debito`; a interface fala **Entrada**/**Saída**. A
tradução acontece só em `ContaCorrente.Web/src/api/httpApi.ts`.

**Por quê.** "Entrada" e "Saída" são os termos do enunciado e o que o usuário entende;
"Crédito" e "Débito" são o vocabulário contábil do domínio. Concentrar a conversão num
arquivo significa que nenhum componente conhece o formato do backend.

---

## 12. Projeto único de produção

**Decisão.** Um projeto `ContaCorrente.Api` organizado em pastas (`Domain`, `Application`,
`Infrastructure`, `Api`), em vez de quatro assemblies.

**Por quê.** O enunciado pede explicitamente para evitar complexidade desnecessária. A
separação por pastas comunica a mesma intenção arquitetural; a separação física em
assemblies só ganha valor quando é preciso impedir referências indevidas em um time grande,
ou publicar as camadas separadamente.

---

## 13. FluentAssertions 6.x, não 7+

**Decisão.** Fixado em `6.12.2`.

**Por quê.** A versão 7 mudou para licença comercial. A 6.12.2 é Apache 2.0 e cobre tudo o
que a suíte usa. Mesma lógica que levou a descartar o MediatR.

---

## 14. `FormaPagamento` separada de `TipoMovimentacao`

**Decisão.** O meio de pagamento é um enum próprio — `Boleto`, `CartaoCredito`,
`CartaoDebito`, `Pix`, `TransferenciaBancaria`, `Dinheiro` — e não reaproveita os nomes
`Credito`/`Debito`.

**Por quê.** O pedido original falava em "Boleto/Credito/Debito/PIX". Mas `TipoMovimentacao`
já usa `Credito`/`Debito` no sentido contábil (entrada e saída de valor). Se a forma de
pagamento usasse os mesmos nomes, uma consulta como
`?tipo=Debito&formaPagamento=Debito` ficaria ilegível — dois `Debito` com significados
diferentes na mesma linha. Os nomes longos custam alguns caracteres e eliminam a ambiguidade.

São duas dimensões independentes: uma entrada pode vir por boleto ou por PIX, e uma saída
também. Modelá-las como um único campo perderia essa combinação.

**Nulo é significado, não ausência de cuidado.** O campo é opcional porque lançamentos
anteriores à introdução da coluna simplesmente não têm essa informação. O extrato é registro
histórico: preencher um valor padrão retroativo seria inventar dado. Por isso a migration é
aditiva e a coluna, `nullable` — nenhum registro existente precisou ser tocado.

**Onde a regra vive.** No `MovimentarCommandValidator`, com `IsInEnum().When(tem valor)`.
Sem isso, um inteiro fora da faixa entraria no banco em silêncio — o enum do C# não valida
o intervalo por conta própria.

---

## 15. Filtros do histórico num objeto de consulta

**Decisão.** `ObterHistoricoRequest` agrupa `pagina`, `tamanho`, `de`, `ate`, `tipo` e
`formaPagamento`. A action recebe `[FromQuery] ObterHistoricoRequest filtro` em vez de seis
parâmetros anotados.

**Por quê.** Com um `[FromQuery]` por parâmetro, a assinatura crescia a cada filtro novo e
já ocupava sete linhas. Agrupando, a action tem três parâmetros e **adicionar um filtro
passa a ser uma propriedade no objeto** — o controller não muda.

Ganho secundário: o objeto documenta cada filtro com XML comment, e isso aparece no Swagger.

**Detalhe de binding.** A classe usa propriedades `init` com valores padrão, não um record
posicional. O model binder do ASP.NET Core precisa de um construtor sem parâmetros para
preencher propriedades vindas da query string.

**Filtro é responsabilidade do servidor.** O front dispara uma nova consulta a cada mudança
de filtro, em vez de filtrar o array em memória. Filtrar no cliente só funcionaria sobre a
página já carregada — com 10 itens por página, o resultado seria simplesmente errado.
