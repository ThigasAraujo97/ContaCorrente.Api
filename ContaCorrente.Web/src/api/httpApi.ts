import type {
  Conta,
  ContaCorrenteApi,
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
  };
}

function movimentar(
  contaId: string,
  tipo: TipoMovimentacao,
  { valor, descricao }: NovaMovimentacao,
): Promise<Movimentacao> {
  return request<MovimentacaoDaApi>(`/api/contas/${contaId}/movimentacoes`, {
    method: 'POST',
    body: JSON.stringify({ tipo: paraApi(tipo), valor, descricao }),
  }).then(converterMovimentacao);
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

  listarMovimentacoes: (contaId: string, pagina = 1) =>
    request<PaginaDaApi>(
      `/api/contas/${contaId}/movimentacoes?pagina=${pagina}&tamanho=${TAMANHO_DA_PAGINA}`,
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
