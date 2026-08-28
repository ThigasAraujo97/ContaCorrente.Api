import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { App } from './App';
import type { Conta, ContaCorrenteApi, Movimentacao } from './types';

const conta: Conta = {
  id: 'conta-1',
  nome: 'Empresa Exemplo LTDA',
  documento: '12345678000199',
  saldo: 2500,
  criadaEm: '2026-08-27T12:00:00.000Z',
};

const movimentacao: Movimentacao = {
  id: '1',
  tipo: 'Entrada',
  valor: 2500,
  descricao: 'Aporte inicial',
  dataHora: '2026-08-27T12:00:00.000Z',
  saldoApos: 2500,
  formaPagamento: 'Pix',
};

/** Dublê da API: mantém a tela isolada de rede e do backend real. */
function criarApiFalsa(sobrescritas: Partial<ContaCorrenteApi> = {}): ContaCorrenteApi {
  return {
    listarContas: vi.fn().mockResolvedValue([conta]),
    criarConta: vi.fn().mockResolvedValue(conta),
    obterSaldo: vi.fn().mockResolvedValue({ saldo: 2500 }),
    listarMovimentacoes: vi.fn().mockResolvedValue({
      itens: [movimentacao],
      pagina: 1,
      totalDeItens: 1,
      totalDePaginas: 1,
      temProximaPagina: false,
    }),
    registrarEntrada: vi.fn().mockResolvedValue(movimentacao),
    registrarSaida: vi.fn().mockResolvedValue(movimentacao),
    ...sobrescritas,
  };
}

describe('App', () => {
  it('exibe o saldo e o histórico da conta selecionada', async () => {
    render(<App cliente={criarApiFalsa()} />);

    await waitFor(() => expect(screen.getByTestId('saldo')).toHaveTextContent('2.500,00'));
    expect(screen.getByText('Aporte inicial')).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: /Entrada/ })).toBeInTheDocument();
  });

  it('seleciona a primeira conta automaticamente', async () => {
    const cliente = criarApiFalsa();

    render(<App cliente={cliente} />);

    await waitFor(() => expect(cliente.obterSaldo).toHaveBeenCalledWith('conta-1'));
  });

  it('recarrega saldo e histórico juntos após registrar uma movimentação', async () => {
    const cliente = criarApiFalsa();
    const usuario = userEvent.setup();

    render(<App cliente={cliente} />);
    await waitFor(() => expect(cliente.listarMovimentacoes).toHaveBeenCalledTimes(1));

    await usuario.type(screen.getByPlaceholderText('0,00'), '300');
    await usuario.type(screen.getByPlaceholderText('Pagamento de fornecedor'), 'Serviço');
    await usuario.click(screen.getByRole('button', { name: /registrar movimenta/i }));

    expect(cliente.registrarEntrada).toHaveBeenCalledWith('conta-1', {
      valor: 300,
      descricao: 'Serviço',
    });

    // Os dois têm de ser recarregados; um sem o outro deixaria a tela inconsistente.
    await waitFor(() => expect(cliente.listarMovimentacoes).toHaveBeenCalledTimes(2));
    await waitFor(() => expect(cliente.obterSaldo).toHaveBeenCalledTimes(2));
  });

  it('exibe a forma de pagamento no extrato', async () => {
    render(<App cliente={criarApiFalsa()} />);

    // Busca dentro da tabela: "PIX" tambem aparece como opcao do filtro.
    const linha = await screen.findByRole('row', { name: /Aporte inicial/ });
    expect(within(linha).getByText('PIX')).toBeInTheDocument();
  });

  it('busca o extrato uma unica vez ao abrir a conta', async () => {
    const cliente = criarApiFalsa();

    render(<App cliente={cliente} />);

    await waitFor(() => expect(cliente.listarMovimentacoes).toHaveBeenCalled());
    // Guarda contra o efeito de reset de filtro disparar uma segunda consulta.
    expect(cliente.listarMovimentacoes).toHaveBeenCalledTimes(1);
  });

  it('consulta a API ao filtrar por forma de pagamento, e volta para a página 1', async () => {
    const cliente = criarApiFalsa();
    const usuario = userEvent.setup();

    render(<App cliente={cliente} />);
    await waitFor(() => expect(cliente.listarMovimentacoes).toHaveBeenCalledTimes(1));

    await usuario.selectOptions(
      screen.getByLabelText('Forma de pagamento', { selector: 'select[name="filtroFormaPagamento"]' }),
      'Boleto',
    );

    // O filtro e aplicado no servidor: filtrar em memoria erraria assim que o
    // extrato passasse de uma pagina.
    await waitFor(() =>
      expect(cliente.listarMovimentacoes).toHaveBeenLastCalledWith(
        'conta-1',
        expect.objectContaining({ formaPagamento: 'Boleto', pagina: 1 }),
      ),
    );
  });

  it('mostra alerta quando a API está indisponível', async () => {
    const cliente = criarApiFalsa({
      obterSaldo: vi.fn().mockRejectedValue(new Error('API fora do ar.')),
    });

    render(<App cliente={cliente} />);

    expect(await screen.findByRole('alert')).toHaveTextContent('API fora do ar.');
  });

  it('convida a criar uma conta quando não existe nenhuma', async () => {
    const cliente = criarApiFalsa({ listarContas: vi.fn().mockResolvedValue([]) });

    render(<App cliente={cliente} />);

    expect(await screen.findByText(/Crie uma conta para começar/)).toBeInTheDocument();
    expect(cliente.obterSaldo).not.toHaveBeenCalled();
  });
});
