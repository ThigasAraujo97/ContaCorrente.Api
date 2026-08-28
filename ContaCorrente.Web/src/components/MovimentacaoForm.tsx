import { useState, type FormEvent } from 'react';
import {
  FORMAS_PAGAMENTO,
  type FormaPagamento,
  type NovaMovimentacao,
  type TipoMovimentacao,
} from '../types';
import { formatarMoeda, paraNumero } from '../utils/format';
import { rotuloFormaPagamento } from '../utils/rotulos';
import { Card } from './Card';
import { IconeMovimentacao } from './Icons';

interface MovimentacaoFormProps {
  enviando: boolean;
  onRegistrar: (tipo: TipoMovimentacao, dados: NovaMovimentacao) => Promise<void>;
}

const DESCRICAO_MAXIMA = 120;
const OPCOES: TipoMovimentacao[] = ['Entrada', 'Saida'];

const rotuloTipo = (tipo: TipoMovimentacao) => (tipo === 'Entrada' ? 'Entrada' : 'Saída');

export function MovimentacaoForm({ enviando, onRegistrar }: MovimentacaoFormProps) {
  const [tipo, setTipo] = useState<TipoMovimentacao>('Entrada');
  const [valor, setValor] = useState('');
  const [descricao, setDescricao] = useState('');
  const [formaPagamento, setFormaPagamento] = useState<FormaPagamento | ''>('');
  const [erro, setErro] = useState<string | null>(null);
  const [sucesso, setSucesso] = useState<string | null>(null);

  const valorNumerico = paraNumero(valor);
  const valorValido = Number.isFinite(valorNumerico) && valorNumerico > 0;

  async function handleSubmit(evento: FormEvent) {
    evento.preventDefault();
    setSucesso(null);

    // Validacao de formato no cliente. As regras de negocio (saldo insuficiente,
    // por exemplo) continuam sob responsabilidade da API, que e a fonte da verdade.
    if (!valorValido) {
      setErro('Informe um valor maior que zero.');
      return;
    }

    if (!descricao.trim()) {
      setErro('Informe uma descrição para a movimentação.');
      return;
    }

    setErro(null);

    try {
      await onRegistrar(tipo, {
        valor: Number(valorNumerico.toFixed(2)),
        descricao: descricao.trim(),
        // Campo opcional: string vazia vira undefined para nao enviar o
        // parametro, ja que a API distingue "nao informado" de um valor.
        formaPagamento: formaPagamento || undefined,
      });

      setValor('');
      setDescricao('');
      setFormaPagamento('');
      setSucesso(rotuloTipo(tipo) + ' registrada com sucesso.');
    } catch (e) {
      setErro(e instanceof Error ? e.message : 'Não foi possível registrar a movimentação.');
    }
  }

  return (
    <Card
      titulo="Nova movimentação"
      icone={<IconeMovimentacao />}
      acento="azul"
      className="card--formulario"
    >
      <form id="nova-movimentacao" className="formulario" onSubmit={handleSubmit} noValidate>
        <div className="alternador" role="radiogroup" aria-label="Tipo de movimentação">
          {OPCOES.map((opcao) => {
            const ativa = tipo === opcao;
            const classe = ativa
              ? 'alternador__opcao alternador__opcao--ativa-' + opcao.toLowerCase()
              : 'alternador__opcao';

            return (
              <button
                key={opcao}
                type="button"
                role="radio"
                aria-checked={ativa}
                className={classe}
                onClick={() => setTipo(opcao)}
              >
                {rotuloTipo(opcao)}
              </button>
            );
          })}
        </div>

        <label className="campo">
          <span className="campo__rotulo">Valor</span>
          <input
            className="campo__entrada"
            name="valor"
            inputMode="decimal"
            autoComplete="off"
            placeholder="0,00"
            value={valor}
            onChange={(e) => setValor(e.target.value)}
          />
          <span className="campo__auxiliar">
            {valorValido ? formatarMoeda(valorNumerico) : 'Ex.: 1.500,00'}
          </span>
        </label>

        <label className="campo">
          <span className="campo__rotulo">Descrição</span>
          <input
            className="campo__entrada"
            name="descricao"
            autoComplete="off"
            maxLength={DESCRICAO_MAXIMA}
            placeholder="Pagamento de fornecedor"
            value={descricao}
            onChange={(e) => setDescricao(e.target.value)}
          />
          <span className="campo__auxiliar">
            {descricao.length}/{DESCRICAO_MAXIMA}
          </span>
        </label>

        <label className="campo">
          <span className="campo__rotulo">Forma de pagamento</span>
          <select
            className="campo__entrada"
            name="formaPagamento"
            value={formaPagamento}
            onChange={(e) => setFormaPagamento(e.target.value as FormaPagamento | '')}
          >
            <option value="">Nao informar</option>
            {FORMAS_PAGAMENTO.map((forma) => (
              <option key={forma} value={forma}>
                {rotuloFormaPagamento(forma)}
              </option>
            ))}
          </select>
        </label>

        <button className="botao" type="submit" disabled={enviando}>
          {enviando ? 'Registrando...' : 'Registrar movimentação'}
        </button>

        {erro && (
          <p className="mensagem mensagem--erro" role="alert">
            {erro}
          </p>
        )}

        {sucesso && (
          <p className="mensagem mensagem--sucesso" role="status">
            {sucesso}
          </p>
        )}
      </form>
    </Card>
  );
}
