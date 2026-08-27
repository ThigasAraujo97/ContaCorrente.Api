import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from './client';
import { httpApi } from './httpApi';

afterEach(() => {
  vi.restoreAllMocks();
});

function resposta(corpo: unknown, status = 200) {
  return new Response(JSON.stringify(corpo), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

function movimentacaoDaApi(tipo: 'Credito' | 'Debito') {
  return {
    id: 'mov-1',
    contaId: 'conta-1',
    tipo,
    valor: 150.5,
    saldoResultante: 849.5,
    descricao: 'Pagamento',
    ocorridaEm: '2026-08-27T12:00:00.0000000Z',
  };
}

describe('httpApi', () => {
  it('traduz Entrada da tela em Credito da API', async () => {
    const fetchSpy = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(resposta(movimentacaoDaApi('Credito'), 201));

    await httpApi.registrarEntrada('conta-1', { valor: 150.5, descricao: 'Pagamento' });

    const [url, init] = fetchSpy.mock.calls[0];
    expect(url).toBe('/api/contas/conta-1/movimentacoes');
    expect(JSON.parse(init!.body as string)).toEqual({
      tipo: 'Credito',
      valor: 150.5,
      descricao: 'Pagamento',
    });
  });

  it('traduz Saida da tela em Debito da API', async () => {
    const fetchSpy = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(resposta(movimentacaoDaApi('Debito'), 201));

    await httpApi.registrarSaida('conta-1', { valor: 150.5, descricao: 'Pagamento' });

    const enviado = JSON.parse(fetchSpy.mock.calls[0][1]!.body as string);
    expect(enviado.tipo).toBe('Debito');
  });

  it('converte a movimentação da API para o modelo da tela', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      resposta(movimentacaoDaApi('Debito'), 201),
    );

    const movimentacao = await httpApi.registrarSaida('conta-1', {
      valor: 150.5,
      descricao: 'Pagamento',
    });

    expect(movimentacao).toEqual({
      id: 'mov-1',
      tipo: 'Saida',
      valor: 150.5,
      descricao: 'Pagamento',
      dataHora: '2026-08-27T12:00:00.0000000Z',
      saldoApos: 849.5,
    });
  });

  it('converte a página de histórico preservando a paginação', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      resposta({
        itens: [movimentacaoDaApi('Credito')],
        pagina: 2,
        tamanho: 10,
        totalDeItens: 15,
        totalDePaginas: 2,
        temProximaPagina: false,
      }),
    );

    const pagina = await httpApi.listarMovimentacoes('conta-1', 2);

    expect(pagina.itens[0].tipo).toBe('Entrada');
    expect(pagina.totalDeItens).toBe(15);
    expect(pagina.temProximaPagina).toBe(false);
  });

  it('trata descrição nula da API como texto vazio', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      resposta({ ...movimentacaoDaApi('Credito'), descricao: null }, 201),
    );

    const movimentacao = await httpApi.registrarEntrada('conta-1', {
      valor: 10,
      descricao: '',
    });

    expect(movimentacao.descricao).toBe('');
  });

  it('transforma o 422 de saldo insuficiente na mensagem do domínio', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      resposta(
        {
          title: 'Saldo insuficiente',
          detail: 'Saldo insuficiente. Disponível: 100,00, solicitado: 5000,00.',
          status: 422,
        },
        422,
      ),
    );

    const erro = (await httpApi
      .registrarSaida('conta-1', { valor: 5000, descricao: 'Retirada' })
      .catch((e: unknown) => e)) as ApiError;

    expect(erro).toBeInstanceOf(ApiError);
    expect(erro.isSaldoInsuficiente).toBe(true);
    expect(erro.message).toBe(
      'Saldo insuficiente. Disponível: 100,00, solicitado: 5000,00.',
    );
  });

  it('junta os erros por campo de um 400 de validação', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      resposta(
        {
          title: 'Falha de validação',
          status: 400,
          errors: { Valor: ['Valor deve ser maior que zero.'] },
        },
        400,
      ),
    );

    const erro = (await httpApi
      .registrarEntrada('conta-1', { valor: 0, descricao: 'x' })
      .catch((e: unknown) => e)) as ApiError;

    expect(erro.status).toBe(400);
    expect(erro.message).toContain('Valor deve ser maior que zero.');
    expect(erro.errosPorCampo).toHaveProperty('Valor');
  });

  it('avisa quando a API está fora do ar em vez de estourar erro cru', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new TypeError('Failed to fetch'));

    const erro = (await httpApi.listarContas().catch((e: unknown) => e)) as ApiError;

    expect(erro).toBeInstanceOf(ApiError);
    expect(erro.status).toBe(0);
    expect(erro.message).toContain('API');
  });
});
