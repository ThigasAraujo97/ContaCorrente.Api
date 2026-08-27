import { useState, type FormEvent } from 'react';
import type { Conta, NovaConta } from '../types';
import { Card } from './Card';
import { IconeConta } from './Icons';

interface SeletorContaProps {
  contas: Conta[];
  contaSelecionada: string | null;
  onSelecionar: (contaId: string) => void;
  onCriar: (dados: NovaConta) => Promise<Conta>;
}

export function SeletorConta({
  contas,
  contaSelecionada,
  onSelecionar,
  onCriar,
}: SeletorContaProps) {
  const [abrindo, setAbrindo] = useState(false);
  const [nome, setNome] = useState('');
  const [documento, setDocumento] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  async function handleSubmit(evento: FormEvent) {
    evento.preventDefault();

    if (!nome.trim() || !documento.trim()) {
      setErro('Informe nome e documento da conta.');
      return;
    }

    setErro(null);
    setEnviando(true);

    try {
      const conta = await onCriar({ nome: nome.trim(), documento: documento.trim() });
      setNome('');
      setDocumento('');
      setAbrindo(false);
      onSelecionar(conta.id);
    } catch (e) {
      setErro(e instanceof Error ? e.message : 'Nao foi possivel criar a conta.');
    } finally {
      setEnviando(false);
    }
  }

  return (
    <Card titulo="Conta" icone={<IconeConta />} acento="ciano">
      {contas.length > 0 && (
        <label className="campo">
          <span className="campo__rotulo">Conta selecionada</span>
          <select
            className="campo__entrada"
            value={contaSelecionada ?? ''}
            onChange={(e) => onSelecionar(e.target.value)}
          >
            {contas.map((c) => (
              <option key={c.id} value={c.id}>
                {c.nome}
              </option>
            ))}
          </select>
        </label>
      )}

      {abrindo ? (
        <form className="formulario" onSubmit={handleSubmit} noValidate>
          <label className="campo">
            <span className="campo__rotulo">Nome</span>
            <input
              className="campo__entrada"
              name="nome"
              autoComplete="off"
              placeholder="Empresa Exemplo LTDA"
              value={nome}
              onChange={(e) => setNome(e.target.value)}
            />
          </label>

          <label className="campo">
            <span className="campo__rotulo">Documento</span>
            <input
              className="campo__entrada"
              name="documento"
              autoComplete="off"
              placeholder="12345678000199"
              value={documento}
              onChange={(e) => setDocumento(e.target.value)}
            />
          </label>

          <button className="botao" type="submit" disabled={enviando}>
            {enviando ? 'Criando...' : 'Criar conta'}
          </button>

          <button
            className="botao botao--claro"
            type="button"
            onClick={() => {
              setAbrindo(false);
              setErro(null);
            }}
            disabled={enviando}
          >
            Cancelar
          </button>

          {erro && (
            <p className="mensagem mensagem--erro" role="alert">
              {erro}
            </p>
          )}
        </form>
      ) : (
        <button className="botao botao--claro" type="button" onClick={() => setAbrindo(true)}>
          Nova conta
        </button>
      )}
    </Card>
  );
}
