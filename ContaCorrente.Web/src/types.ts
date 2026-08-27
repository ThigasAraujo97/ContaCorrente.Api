/**
 * Vocabulario da interface. A API fala 'Credito'/'Debito'; a tela fala
 * 'Entrada'/'Saida', que e o termo do enunciado e o que o usuario entende.
 * A traducao entre os dois acontece num unico lugar: api/httpApi.ts.
 */
export type TipoMovimentacao = 'Entrada' | 'Saida';

export interface Conta {
  id: string;
  nome: string;
  documento: string;
  saldo: number;
  criadaEm: string;
}

export interface Movimentacao {
  id: string;
  tipo: TipoMovimentacao;
  valor: number;
  descricao: string;
  dataHora: string;
  /** Saldo da conta logo apos a movimentacao, para auditoria do extrato. */
  saldoApos?: number;
}

export interface Saldo {
  saldo: number;
  atualizadoEm?: string;
}

export interface NovaMovimentacao {
  valor: number;
  descricao: string;
}

export interface NovaConta {
  nome: string;
  documento: string;
}

/** Envelope paginado do historico. O extrato cresce sem limite. */
export interface PaginaMovimentacoes {
  itens: Movimentacao[];
  pagina: number;
  totalDeItens: number;
  totalDePaginas: number;
  temProximaPagina: boolean;
}

/**
 * Contrato consumido pela tela. Tanto o cliente HTTP quanto o mock de
 * demonstracao implementam esta interface, entao a UI nao sabe qual esta ativo.
 */
export interface ContaCorrenteApi {
  listarContas(): Promise<Conta[]>;
  criarConta(dados: NovaConta): Promise<Conta>;
  obterSaldo(contaId: string): Promise<Saldo>;
  listarMovimentacoes(contaId: string, pagina?: number): Promise<PaginaMovimentacoes>;
  registrarEntrada(contaId: string, dados: NovaMovimentacao): Promise<Movimentacao>;
  registrarSaida(contaId: string, dados: NovaMovimentacao): Promise<Movimentacao>;
}
