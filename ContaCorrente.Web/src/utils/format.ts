const moeda = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
});

const dataHora = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});

export function formatarMoeda(valor: number): string {
  return moeda.format(Number.isFinite(valor) ? valor : 0);
}

export function formatarDataHora(iso: string): string {
  const data = new Date(iso);
  return Number.isNaN(data.getTime()) ? '--' : dataHora.format(data);
}

/**
 * Converte o texto digitado no campo de valor para numero.
 * Aceita "1234.56" e "1.234,56"; retorna NaN quando nao houver numero valido.
 */
export function paraNumero(texto: string): number {
  const limpo = texto.trim().replace(/\s/g, '');
  if (!limpo) return Number.NaN;

  const normalizado = limpo.includes(',')
    ? limpo.replace(/\./g, '').replace(',', '.')
    : limpo;

  return Number(normalizado);
}
