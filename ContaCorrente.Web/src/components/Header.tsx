const NAVEGACAO = [
  { texto: 'Movimentações', href: '#nova-movimentacao' },
  { texto: 'Saldo', href: '#saldo' },
  { texto: 'Histórico', href: '#historico' },
];

export function Header() {
  return (
    <header className="cabecalho">
      <div className="cabecalho__conteudo">
        <a className="logo" href="#topo" aria-label="Página inicial">
          act
          <span className="logo__pontos" aria-hidden="true">
            <i />
            <i />
          </span>
        </a>

        <nav className="menu" aria-label="Navegação principal">
          {NAVEGACAO.map((item) => (
            <a key={item.texto} className="menu__item" href={item.href}>
              {item.texto}
            </a>
          ))}
          <a className="menu__contato" href="#nova-movimentacao">
            Nova movimentação
          </a>
        </nav>
      </div>
    </header>
  );
}
