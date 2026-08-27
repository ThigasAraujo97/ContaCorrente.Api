import { describe, expect, it } from 'vitest';
import { formatarDataHora, formatarMoeda, paraNumero } from './format';

/** Intl usa espaco nao separavel; normaliza para facilitar a comparacao. */
const normalizar = (texto: string) => texto.replace(/[  ]/g, ' ');

describe('formatarMoeda', () => {
  it('formata valores em reais', () => {
    expect(normalizar(formatarMoeda(1500))).toBe('R$ 1.500,00');
    expect(normalizar(formatarMoeda(0))).toBe('R$ 0,00');
  });

  it('trata valores invalidos como zero', () => {
    expect(normalizar(formatarMoeda(Number.NaN))).toBe('R$ 0,00');
  });
});

describe('paraNumero', () => {
  it('aceita o formato brasileiro', () => {
    expect(paraNumero('1.500,25')).toBe(1500.25);
    expect(paraNumero('0,50')).toBe(0.5);
  });

  it('aceita o formato com ponto decimal', () => {
    expect(paraNumero('1500.25')).toBe(1500.25);
  });

  it('retorna NaN para texto vazio ou invalido', () => {
    expect(paraNumero('')).toBeNaN();
    expect(paraNumero('abc')).toBeNaN();
  });
});

describe('formatarDataHora', () => {
  it('retorna marcador quando a data e invalida', () => {
    expect(formatarDataHora('data-invalida')).toBe('--');
  });
});
