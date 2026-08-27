import type {
  Conta,
  ContaCorrenteApi,
  Movimentacao,
  NovaConta,
  NovaMovimentacao,
  PaginaMovimentacoes,
  Saldo,
} from '../types';
import { ApiError } from './client';

/**
 * Repositorio em memoria usado apenas para demonstrar a tela sem o backend
 * (VITE_USE_MOCK=true). Reproduz as mesmas regras da API, inclusive o bloqueio
 * de saldo negativo, para que a UI possa ser validada de ponta a ponta.
 */
const TAMANHO_DA_PAGINA = 10;

interface ContaEmMemoria extends Conta {
  movimentacoes: Movimentacao[];
}

const contas: ContaEmMemoria[] = [
  {
    id: 'conta-demo',
    nome: 'Empresa Demonstracao LTDA',
    documento: '12345678000199',
    saldo: 0,
    criadaEm: new Date().toISOString(),
    movimentacoes: [],
  },
];

let sequencia = 0;

const atraso = () => new Promise((resolve) => setTimeout(resolve, 250));

function buscar(contaId: string): ContaEmMemoria {
  const conta = contas.find((c) => c.id === contaId);
  if (!conta) throw new ApiError(`Conta ${contaId} nao encontrada.`, 404);
  return conta;
}

function registrar(
  contaId: string,
  tipo: Movimentacao['tipo'],
  { valor, descricao }: NovaMovimentacao,
): Movimentacao {
  const conta = buscar(contaId);

  if (!(valor > 0)) {
    throw new ApiError('O valor da movimentacao deve ser maior que zero.', 400);
  }

  if (tipo === 'Saida' && valor > conta.saldo) {
    throw new ApiError(
      `Saldo insuficiente. Disponivel: ${conta.saldo.toFixed(2)}, solicitado: ${valor.toFixed(2)}.`,
      422,
    );
  }

  conta.saldo += tipo === 'Entrada' ? valor : -valor;

  const movimentacao: Movimentacao = {
    id: String(++sequencia),
    tipo,
    valor,
    descricao: descricao.trim(),
    dataHora: new Date().toISOString(),
    saldoApos: conta.saldo,
  };

  conta.movimentacoes.unshift(movimentacao);
  return movimentacao;
}

export const mockApi: ContaCorrenteApi = {
  async listarContas(): Promise<Conta[]> {
    await atraso();
    return contas.map(({ movimentacoes: _, ...conta }) => conta);
  },

  async criarConta(dados: NovaConta): Promise<Conta> {
    await atraso();

    const conta: ContaEmMemoria = {
      id: `conta-${contas.length + 1}`,
      nome: dados.nome,
      documento: dados.documento,
      saldo: 0,
      criadaEm: new Date().toISOString(),
      movimentacoes: [],
    };

    contas.push(conta);
    const { movimentacoes: _, ...semMovimentacoes } = conta;
    return semMovimentacoes;
  },

  async obterSaldo(contaId: string): Promise<Saldo> {
    await atraso();
    return { saldo: buscar(contaId).saldo, atualizadoEm: new Date().toISOString() };
  },

  async listarMovimentacoes(contaId: string, pagina = 1): Promise<PaginaMovimentacoes> {
    await atraso();

    const todas = buscar(contaId).movimentacoes;
    const inicio = (pagina - 1) * TAMANHO_DA_PAGINA;
    const totalDePaginas = Math.ceil(todas.length / TAMANHO_DA_PAGINA);

    return {
      itens: todas.slice(inicio, inicio + TAMANHO_DA_PAGINA),
      pagina,
      totalDeItens: todas.length,
      totalDePaginas,
      temProximaPagina: pagina < totalDePaginas,
    };
  },

  async registrarEntrada(contaId, dados) {
    await atraso();
    return registrar(contaId, 'Entrada', dados);
  },

  async registrarSaida(contaId, dados) {
    await atraso();
    return registrar(contaId, 'Saida', dados);
  },
};
