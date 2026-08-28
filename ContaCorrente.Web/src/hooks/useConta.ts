import { useCallback, useEffect, useMemo, useState } from 'react';
import { ApiError } from '../api';
import type {
  ContaCorrenteApi,
  FiltroHistorico,
  Movimentacao,
  NovaMovimentacao,
  PaginaMovimentacoes,
  TipoMovimentacao,
} from '../types';

interface EstadoConta {
  saldo: number;
  atualizadoEm?: string;
  movimentacoes: Movimentacao[];
  pagina: PaginaMovimentacoes | null;
  filtro: FiltroHistorico;
  aplicarFiltro: (filtro: FiltroHistorico) => void;
  irParaPagina: (pagina: number) => void;
  carregando: boolean;
  enviando: boolean;
  erro: string | null;
  totalEntradas: number;
  totalSaidas: number;
  recarregar: () => Promise<void>;
  registrar: (
    tipo: TipoMovimentacao,
    dados: NovaMovimentacao,
  ) => Promise<Movimentacao>;
}

function mensagem(erro: unknown): string {
  if (erro instanceof ApiError) return erro.message;
  if (erro instanceof Error) return erro.message;
  return 'Erro inesperado ao comunicar com a API.';
}

/**
 * Centraliza o estado da conta: saldo e historico vem da API (fonte da verdade)
 * e sao recarregados juntos apos cada movimentacao, garantindo que a tela nunca
 * exiba um saldo derivado localmente nem um extrato dessincronizado do saldo.
 *
 * A filtragem tambem e responsabilidade da API, nao do cliente: filtrar em
 * memoria so funcionaria sobre a pagina ja carregada, dando resultados errados
 * assim que o extrato passasse de uma pagina.
 */
export function useConta(
  api: ContaCorrenteApi,
  contaId: string | null,
): EstadoConta {
  const [saldo, setSaldo] = useState(0);
  const [atualizadoEm, setAtualizadoEm] = useState<string | undefined>();
  const [pagina, setPagina] = useState<PaginaMovimentacoes | null>(null);
  const [filtro, setFiltro] = useState<FiltroHistorico>({ pagina: 1 });
  const [carregando, setCarregando] = useState(true);
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  const carregar = useCallback(
    async (filtroDesejado: FiltroHistorico) => {
      if (!contaId) {
        setSaldo(0);
        setAtualizadoEm(undefined);
        setPagina(null);
        setCarregando(false);
        return;
      }

      setCarregando(true);
      try {
        const [resultadoSaldo, historico] = await Promise.all([
          api.obterSaldo(contaId),
          api.listarMovimentacoes(contaId, filtroDesejado),
        ]);

        setSaldo(resultadoSaldo.saldo);
        setAtualizadoEm(resultadoSaldo.atualizadoEm);
        setPagina(historico);
        setErro(null);
      } catch (e) {
        setErro(mensagem(e));
      } finally {
        setCarregando(false);
      }
    },
    [api, contaId],
  );

  // Trocar de conta zera os filtros: eles pertencem ao extrato exibido.
  // Devolver o proprio objeto quando ja esta no padrao evita uma segunda
  // consulta desnecessaria — trocar a referencia dispararia o efeito de carga
  // de novo, e o extrato seria buscado duas vezes por troca de conta.
  useEffect(() => {
    setFiltro((atual) =>
      atual.pagina === 1 && !atual.tipo && !atual.formaPagamento
        ? atual
        : { pagina: 1 },
    );
  }, [contaId]);

  useEffect(() => {
    void carregar(filtro);
  }, [carregar, filtro]);

  const recarregar = useCallback(() => carregar(filtro), [carregar, filtro]);

  /** Trocar de filtro sempre volta para a primeira pagina. */
  const aplicarFiltro = useCallback((novo: FiltroHistorico) => {
    setFiltro({ ...novo, pagina: 1 });
  }, []);

  const irParaPagina = useCallback((numero: number) => {
    setFiltro((atual) => ({ ...atual, pagina: numero }));
  }, []);

  const registrar = useCallback(
    async (tipo: TipoMovimentacao, dados: NovaMovimentacao) => {
      if (!contaId) throw new ApiError('Nenhuma conta selecionada.', 400);

      setEnviando(true);
      try {
        const movimentacao =
          tipo === 'Entrada'
            ? await api.registrarEntrada(contaId, dados)
            : await api.registrarSaida(contaId, dados);

        // Um lancamento novo entra no topo do extrato: volta para a pagina 1
        // para que ele fique visivel, preservando os filtros ativos.
        const primeiraPagina = { ...filtro, pagina: 1 };
        if (filtro.pagina !== 1) {
          setFiltro(primeiraPagina);
        } else {
          await carregar(primeiraPagina);
        }

        return movimentacao;
      } finally {
        setEnviando(false);
      }
    },
    [api, contaId, carregar, filtro],
  );

  const movimentacoes = useMemo(() => pagina?.itens ?? [], [pagina]);

  // Totais da pagina exibida. Para totais de toda a conta, o caminho seria um
  // endpoint de resumo na API em vez de somar no cliente.
  const { totalEntradas, totalSaidas } = useMemo(
    () =>
      movimentacoes.reduce(
        (acumulado, m) => {
          if (m.tipo === 'Entrada') acumulado.totalEntradas += m.valor;
          else acumulado.totalSaidas += m.valor;
          return acumulado;
        },
        { totalEntradas: 0, totalSaidas: 0 },
      ),
    [movimentacoes],
  );

  return {
    saldo,
    atualizadoEm,
    movimentacoes,
    pagina,
    filtro,
    aplicarFiltro,
    irParaPagina,
    carregando,
    enviando,
    erro,
    totalEntradas,
    totalSaidas,
    recarregar,
    registrar,
  };
}
