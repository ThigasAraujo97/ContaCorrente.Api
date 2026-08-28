import {
  FORMAS_PAGAMENTO,
  type FiltroHistorico,
  type FormaPagamento,
  type TipoMovimentacao,
} from '../types';
import { rotuloFormaPagamento } from '../utils/rotulos';

interface FiltrosHistoricoProps {
  filtro: FiltroHistorico;
  onAplicar: (filtro: FiltroHistorico) => void;
}

/**
 * Filtros do extrato. Cada mudanca dispara uma nova consulta a API — filtrar em
 * memoria daria resultado errado, porque a tela so tem a pagina atual em maos.
 */
export function FiltrosHistorico({ filtro, onAplicar }: FiltrosHistoricoProps) {
  const temFiltroAtivo = Boolean(filtro.tipo || filtro.formaPagamento);

  return (
    <div className="filtros" role="group" aria-label="Filtros do histórico">
      <label className="filtros__campo">
        <span className="campo__rotulo">Tipo</span>
        <select
          className="campo__entrada"
          name="filtroTipo"
          value={filtro.tipo ?? ''}
          onChange={(e) =>
            onAplicar({
              ...filtro,
              tipo: (e.target.value || undefined) as TipoMovimentacao | undefined,
            })
          }
        >
          <option value="">Todos</option>
          <option value="Entrada">Entrada</option>
          <option value="Saida">Saída</option>
        </select>
      </label>

      <label className="filtros__campo">
        <span className="campo__rotulo">Forma de pagamento</span>
        <select
          className="campo__entrada"
          name="filtroFormaPagamento"
          value={filtro.formaPagamento ?? ''}
          onChange={(e) =>
            onAplicar({
              ...filtro,
              formaPagamento: (e.target.value || undefined) as FormaPagamento | undefined,
            })
          }
        >
          <option value="">Todas</option>
          {FORMAS_PAGAMENTO.map((forma) => (
            <option key={forma} value={forma}>
              {rotuloFormaPagamento(forma)}
            </option>
          ))}
        </select>
      </label>

      {temFiltroAtivo && (
        <button
          type="button"
          className="botao botao--claro filtros__limpar"
          onClick={() => onAplicar({ pagina: 1 })}
        >
          Limpar filtros
        </button>
      )}
    </div>
  );
}
