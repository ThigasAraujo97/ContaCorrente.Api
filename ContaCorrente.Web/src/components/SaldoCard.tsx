import { formatarDataHora, formatarMoeda } from '../utils/format';
import { Card } from './Card';
import { IconeSaldo } from './Icons';

interface SaldoCardProps {
  saldo: number;
  atualizadoEm?: string;
  carregando: boolean;
}

export function SaldoCard({ saldo, atualizadoEm, carregando }: SaldoCardProps) {
  const classeValor = saldo < 0 ? 'valor-destaque valor-destaque--negativo' : 'valor-destaque';

  return (
    <Card titulo="Saldo disponível" icone={<IconeSaldo />} acento="lima">
      <p className="rotulo">Conta empresarial</p>
      <p className={classeValor} data-testid="saldo">
        {carregando ? '—' : formatarMoeda(saldo)}
      </p>
      {atualizadoEm && !carregando && (
        <p className="rodape-card">Atualizado em {formatarDataHora(atualizadoEm)}</p>
      )}
    </Card>
  );
}
