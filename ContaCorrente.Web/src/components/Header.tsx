const NAVEGACAO = ['Movimentações', 'Saldo', 'Histórico'];

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
            <a key={item} className="menu__item" href="#topo">
              {item}
            </a>
          ))}
          <a className="menu__contato" href="#nova-movimentacao">
            Nova movimentação
          </a>
          <span className="menu__idioma">
            <span aria-hidden="true">🌐</span> Português
          </span>
        </nav>
      </div>
    </header>
  );
}
