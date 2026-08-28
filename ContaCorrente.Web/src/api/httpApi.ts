import type {
  Conta,
  ContaCorrenteApi,
  FiltroHistorico,
  FormaPagamento,
  Movimentacao,
  NovaConta,
  NovaMovimentacao,
  PaginaMovimentacoes,
  Saldo,
  TipoMovimentacao,
} from '../types';
import { request } from './client';

const TAMANHO_DA_PAGINA = 10;

/** Como a API nomeia os tipos. A tela usa Entrada/Saida. */
type TipoNaApi = 'Credito' | 'Debito';

interface MovimentacaoDaApi {
  id: string;
  contaId: string;
  tipo: TipoNaApi;
  valor: number;
  saldoResultante: number;
  descricao: string | null;
  formaPagamento: FormaPagamento | null;
  ocorridaEm: string;
}

interface PaginaDaApi {
  itens: MovimentacaoDaApi[];
  pagina: number;
  tamanho: number;
  totalDeItens: number;
  totalDePaginas: number;
  temProximaPagina: boolean;
}

interface SaldoDaApi {
  contaId: string;
  saldo: number;
  atualizadoEm: string;
}

const paraApi = (tipo: TipoMovimentacao): TipoNaApi =>
  tipo === 'Entrada' ? 'Credito' : 'Debito';

const paraTela = (tipo: TipoNaApi): TipoMovimentacao =>
  tipo === 'Credito' ? 'Entrada' : 'Saida';

/**
 * Unico ponto de traducao entre o formato da API e o modelo da tela. Manter a
 * conversao aqui e o que permite trocar nomes ou formatos no backend sem tocar
 * em nenhum componente.
 */
function converterMovimentacao(m: MovimentacaoDaApi): Movimentacao {
  return {
    id: m.id,
    tipo: paraTela(m.tipo),
    valor: m.valor,
    descricao: m.descricao ?? '',
    dataHora: m.ocorridaEm,
    saldoApos: m.saldoResultante,
    formaPagamento: m.formaPagamento,
  };
}

function movimentar(
  contaId: string,
  tipo: TipoMovimentacao,
  { valor, descricao, formaPagamento }: NovaMovimentacao,
): Promise<Movimentacao> {
  return request<MovimentacaoDaApi>(`/api/contas/${contaId}/movimentacoes`, {
    method: 'POST',
    body: JSON.stringify({ tipo: paraApi(tipo), valor, descricao, formaPagamento }),
  }).then(converterMovimentacao);
}

/**
 * Monta a query string do historico. Filtro ausente nao vira parametro vazio:
 * a API distingue "sem filtro" de "filtro com valor em branco".
 */
function montarConsulta(filtro: FiltroHistorico): string {
  const parametros = new URLSearchParams({
    pagina: String(filtro.pagina ?? 1),
    tamanho: String(TAMANHO_DA_PAGINA),
  });

  if (filtro.tipo) parametros.set('tipo', paraApi(filtro.tipo));
  if (filtro.formaPagamento) parametros.set('formaPagamento', filtro.formaPagamento);

  return parametros.toString();
}

export const httpApi: ContaCorrenteApi = {
  listarContas: () => request<Conta[]>('/api/contas'),

  criarConta: (dados: NovaConta) =>
    request<Conta>('/api/contas', {
      method: 'POST',
      body: JSON.stringify(dados),
    }),

  obterSaldo: (contaId: string) =>
    request<SaldoDaApi>(`/api/contas/${contaId}/saldo`).then(
      (s): Saldo => ({ saldo: s.saldo, atualizadoEm: s.atualizadoEm }),
    ),

  listarMovimentacoes: (contaId: string, filtro: FiltroHistorico = {}) =>
    request<PaginaDaApi>(
      `/api/contas/${contaId}/movimentacoes?${montarConsulta(filtro)}`,
    ).then(
      (p): PaginaMovimentacoes => ({
        itens: p.itens.map(converterMovimentacao),
        pagina: p.pagina,
        totalDeItens: p.totalDeItens,
        totalDePaginas: p.totalDePaginas,
        temProximaPagina: p.temProximaPagina,
      }),
    ),

  registrarEntrada: (contaId, dados) => movimentar(contaId, 'Entrada', dados),

  registrarSaida: (contaId, dados) => movimentar(contaId, 'Saida', dados),
};
