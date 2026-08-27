import { formatarMoeda } from '../utils/format';
import { Card } from './Card';
import { IconeResumo } from './Icons';

interface ResumoCardProps {
  totalEntradas: number;
  totalSaidas: number;
  quantidade: number;
}

export function ResumoCard({ totalEntradas, totalSaidas, quantidade }: ResumoCardProps) {
  return (
    <Card titulo="Resumo do período" icone={<IconeResumo />} acento="ciano">
      <dl className="resumo">
        <div className="resumo__linha">
          <dt>Entradas</dt>
          <dd className="texto-entrada">{formatarMoeda(totalEntradas)}</dd>
        </div>
        <div className="resumo__linha">
          <dt>Saídas</dt>
          <dd className="texto-saida">{formatarMoeda(totalSaidas)}</dd>
        </div>
        <div className="resumo__linha">
          <dt>Movimentações</dt>
          <dd>{quantidade}</dd>
        </div>
      </dl>
    </Card>
  );
}
