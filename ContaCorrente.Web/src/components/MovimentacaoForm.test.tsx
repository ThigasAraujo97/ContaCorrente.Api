import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { MovimentacaoForm } from './MovimentacaoForm';

function montar(onRegistrar = vi.fn().mockResolvedValue(undefined)) {
  render(<MovimentacaoForm enviando={false} onRegistrar={onRegistrar} />);
  return { onRegistrar, usuario: userEvent.setup() };
}

const botaoRegistrar = () => screen.getByRole('button', { name: /registrar movimenta/i });

describe('MovimentacaoForm', () => {
  it('nao envia quando o valor e zero', async () => {
    const { onRegistrar, usuario } = montar();

    await usuario.type(screen.getByPlaceholderText('0,00'), '0');
    await usuario.click(botaoRegistrar());

    expect(onRegistrar).not.toHaveBeenCalled();
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Informe um valor maior que zero.',
    );
  });

  it('nao envia sem descricao', async () => {
    const { onRegistrar, usuario } = montar();

    await usuario.type(screen.getByPlaceholderText('0,00'), '100');
    await usuario.click(botaoRegistrar());

    expect(onRegistrar).not.toHaveBeenCalled();
    expect(await screen.findByRole('alert')).toHaveTextContent('Informe uma descrição');
  });

  it('envia uma entrada com valor no formato brasileiro', async () => {
    const { onRegistrar, usuario } = montar();

    await usuario.type(screen.getByPlaceholderText('0,00'), '1.500,50');
    await usuario.type(screen.getByPlaceholderText('Pagamento de fornecedor'), 'Venda');
    await usuario.click(botaoRegistrar());

    expect(onRegistrar).toHaveBeenCalledWith('Entrada', {
      valor: 1500.5,
      descricao: 'Venda',
    });
    expect(await screen.findByRole('status')).toHaveTextContent('Entrada registrada');
  });

  it('envia uma saida quando o tipo e alternado', async () => {
    const { onRegistrar, usuario } = montar();

    await usuario.click(screen.getByRole('radio', { name: 'Saída' }));
    await usuario.type(screen.getByPlaceholderText('0,00'), '200');
    await usuario.type(screen.getByPlaceholderText('Pagamento de fornecedor'), 'Aluguel');
    await usuario.click(botaoRegistrar());

    expect(onRegistrar).toHaveBeenCalledWith('Saida', { valor: 200, descricao: 'Aluguel' });
  });

  it('exibe a mensagem de erro devolvida pela API', async () => {
    const onRegistrar = vi
      .fn()
      .mockRejectedValue(new Error('Saldo insuficiente para realizar a saída.'));
    const { usuario } = montar(onRegistrar);

    await usuario.click(screen.getByRole('radio', { name: 'Saída' }));
    await usuario.type(screen.getByPlaceholderText('0,00'), '999');
    await usuario.type(screen.getByPlaceholderText('Pagamento de fornecedor'), 'Retirada');
    await usuario.click(botaoRegistrar());

    expect(await screen.findByRole('alert')).toHaveTextContent('Saldo insuficiente');
  });
});
