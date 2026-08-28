import type { FormaPagamento, TipoMovimentacao } from '../types';

/**
 * Rotulos de exibicao. Os valores trafegados continuam sendo os da API; aqui so
 * mora como cada um aparece na tela, com acentuacao e capitalizacao corretas.
 */
const FORMAS: Record<FormaPagamento, string> = {
  Pix: 'PIX',
  Boleto: 'Boleto',
  CartaoCredito: 'Cartão de crédito',
  CartaoDebito: 'Cartão de débito',
  TransferenciaBancaria: 'Transferência',
  Dinheiro: 'Dinheiro',
};

export function rotuloFormaPagamento(forma: FormaPagamento | null | undefined): string | null {
  return forma ? FORMAS[forma] : null;
}

export function rotuloTipo(tipo: TipoMovimentacao): string {
  return tipo === 'Entrada' ? 'Entrada' : 'Saída';
}
