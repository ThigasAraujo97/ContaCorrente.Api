import { useCallback, useEffect, useState } from 'react';
import { api, usandoMock } from './api';
import { Header } from './components/Header';
import { HistoricoTable } from './components/HistoricoTable';
import { MovimentacaoForm } from './components/MovimentacaoForm';
import { ResumoCard } from './components/ResumoCard';
import { SaldoCard } from './components/SaldoCard';
import { SeletorConta } from './components/SeletorConta';
import { useConta } from './hooks/useConta';
import type {
  Conta,
  ContaCorrenteApi,
  NovaConta,
  NovaMovimentacao,
  TipoMovimentacao,
} from './types';

interface AppProps {
  /** Injetavel para permitir testes com uma implementacao controlada da API. */
  cliente?: ContaCorrenteApi;
}

export function App({ cliente = api }: AppProps) {
  const [contas, setContas] = useState<Conta[]>([]);
  const [contaId, setContaId] = useState<string | null>(null);
  const [erroDeContas, setErroDeContas] = useState<string | null>(null);

  const carregarContas = useCallback(async () => {
    try {
      const lista = await cliente.listarContas();
      setContas(lista);
      setErroDeContas(null);

      // Seleciona a primeira conta para a tela nao abrir vazia.
      setContaId((atual) => atual ?? lista[0]?.id ?? null);
    } catch (e) {
      setErroDeContas(e instanceof Error ? e.message : 'Falha ao carregar as contas.');
    }
  }, [cliente]);

  useEffect(() => {
    void carregarContas();
  }, [carregarContas]);

  const {
    saldo,
    atualizadoEm,
    movimentacoes,
    pagina,
    filtro,
    aplicarFiltro,
    irParaPagina,
    carregando,
    enviando,
    erro,
    totalEntradas,
    totalSaidas,
    recarregar,
    registrar,
  } = useConta(cliente, contaId);

  async function handleRegistrar(tipo: TipoMovimentacao, dados: NovaMovimentacao) {
    await registrar(tipo, dados);
    // A lista de contas mostra o saldo de cada uma: precisa acompanhar.
    await carregarContas();
  }

  async function handleCriarConta(dados: NovaConta) {
    const conta = await cliente.criarConta(dados);
    await carregarContas();
    return conta;
  }

  const mensagemDeErro = erroDeContas ?? erro;

  return (
    <div className="pagina" id="topo">
      <Header />
      <div className="faixa-clara" />

      <main className="conteudo">
        <section className="hero">
          <h1 className="hero__titulo">Conta corrente empresarial</h1>
          <p className="hero__descricao">
            Registre entradas e saídas, acompanhe o saldo disponível e consulte o
            histórico completo das movimentações.
          </p>
        </section>

        {usandoMock && (
          <p className="aviso" role="status">
            Modo demonstração: os dados ficam apenas na memória do navegador. Defina
            <code> VITE_USE_MOCK=false </code> para consumir a API .NET.
          </p>
        )}

        {mensagemDeErro && (
          <div className="aviso aviso--erro" role="alert">
            <span>{mensagemDeErro}</span>
            <button
              type="button"
              className="botao botao--claro"
              onClick={() => void recarregar()}
            >
              Tentar novamente
            </button>
          </div>
        )}

        <div className="grade" id="saldo">
          <SeletorConta
            contas={contas}
            contaSelecionada={contaId}
            onSelecionar={setContaId}
            onCriar={handleCriarConta}
          />
          <SaldoCard saldo={saldo} atualizadoEm={atualizadoEm} carregando={carregando} />
          <ResumoCard
            totalEntradas={totalEntradas}
            totalSaidas={totalSaidas}
            quantidade={pagina?.totalDeItens ?? movimentacoes.length}
          />
        </div>

        {contaId ? (
          <>
            <div className="grade grade--formulario" id="nova-movimentacao">
              <MovimentacaoForm enviando={enviando} onRegistrar={handleRegistrar} />
            </div>

            <div id="historico">
              <HistoricoTable
                movimentacoes={movimentacoes}
                carregando={carregando}
                pagina={pagina}
                onTrocarPagina={irParaPagina}
                filtro={filtro}
                onAplicarFiltro={aplicarFiltro}
              />
            </div>
          </>
        ) : (
          <p className="aviso" role="status">
            Crie uma conta para começar a registrar movimentações.
          </p>
        )}
      </main>

      <footer className="rodape">
        Desafio técnico · API em C# / .NET com interface em React
      </footer>
    </div>
  );
}
