import type { Movimentacao, PaginaMovimentacoes } from '../types';
import { formatarDataHora, formatarMoeda } from '../utils/format';
import { Card } from './Card';
import { IconeHistorico } from './Icons';

interface HistoricoTableProps {
  movimentacoes: Movimentacao[];
  carregando: boolean;
  pagina?: PaginaMovimentacoes | null;
  onTrocarPagina?: (pagina: number) => void;
}

export function HistoricoTable({
  movimentacoes,
  carregando,
  pagina = null,
  onTrocarPagina,
}: HistoricoTableProps) {
  const vazio = !carregando && movimentacoes.length === 0;
  const paginado = pagina !== null && pagina.totalDePaginas > 1 && onTrocarPagina;

  return (
    <Card
      titulo="Histórico de movimentações"
      icone={<IconeHistorico />}
      acento="roxo"
      className="card--largo"
    >
      {carregando && <p className="estado-vazio">Carregando movimentações...</p>}

      {vazio && (
        <p className="estado-vazio">Nenhuma movimentação registrada até o momento.</p>
      )}

      {!carregando && movimentacoes.length > 0 && (
        <div className="tabela-rolagem">
          <table className="tabela">
            <thead>
              <tr>
                <th scope="col">Data</th>
                <th scope="col">Descrição</th>
                <th scope="col">Tipo</th>
                <th scope="col" className="alinhado-direita">
                  Valor
                </th>
                <th scope="col" className="alinhado-direita">
                  Saldo após
                </th>
              </tr>
            </thead>
            <tbody>
              {movimentacoes.map((m) => {
                const entrada = m.tipo === 'Entrada';

                return (
                  <tr key={m.id}>
                    <td>{formatarDataHora(m.dataHora)}</td>
                    <td>{m.descricao}</td>
                    <td>
                      <span className={'etiqueta etiqueta--' + m.tipo.toLowerCase()}>
                        {entrada ? 'Entrada' : 'Saída'}
                      </span>
                    </td>
                    <td
                      className={
                        'alinhado-direita ' + (entrada ? 'texto-entrada' : 'texto-saida')
                      }
                    >
                      {entrada ? '+' : '−'} {formatarMoeda(m.valor)}
                    </td>
                    <td className="alinhado-direita">
                      {m.saldoApos === undefined ? '—' : formatarMoeda(m.saldoApos)}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {paginado && (
        <nav className="paginacao" aria-label="Paginação do histórico">
          <button
            type="button"
            className="botao botao--claro"
            onClick={() => onTrocarPagina(pagina.pagina - 1)}
            disabled={pagina.pagina <= 1}
          >
            Anterior
          </button>

          <span className="paginacao__posicao">
            Página {pagina.pagina} de {pagina.totalDePaginas} · {pagina.totalDeItens}{' '}
            movimentações
          </span>

          <button
            type="button"
            className="botao botao--claro"
            onClick={() => onTrocarPagina(pagina.pagina + 1)}
            disabled={!pagina.temProximaPagina}
          >
            Próxima
          </button>
        </nav>
      )}
    </Card>
  );
}
