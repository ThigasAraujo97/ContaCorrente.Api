import type { ReactNode } from 'react';

type Acento = 'lima' | 'azul' | 'ciano' | 'roxo';

interface CardProps {
  titulo: string;
  icone: ReactNode;
  acento?: Acento;
  className?: string;
  children: ReactNode;
}

/**
 * Card escuro com faixa de destaque na base, reproduzindo o padrao visual
 * dos cards de "Solucoes inovadoras" da referencia.
 */
export function Card({
  titulo,
  icone,
  acento = 'azul',
  className = '',
  children,
}: CardProps) {
  return (
    <section className={`card card--${acento} ${className}`.trim()}>
      <div className="card__conteudo">
        <div className="card__icone">{icone}</div>
        <div className="card__corpo">{children}</div>
        <h2 className="card__titulo">{titulo}</h2>
      </div>
      <span className="card__acento" aria-hidden="true" />
    </section>
  );
}
