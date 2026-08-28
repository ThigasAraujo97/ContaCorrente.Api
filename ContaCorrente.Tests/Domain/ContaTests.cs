using ContaCorrente.Api.Domain;
using ContaCorrente.Api.Domain.Exceptions;
using FluentAssertions;

namespace ContaCorrente.Tests.Domain;

/// <summary>
/// Testes da regra de negócio pura — sem banco, sem HTTP, sem DI.
/// </summary>
public class ContaTests
{
    private static Conta NovaConta() => new("Empresa Exemplo LTDA", "12345678000199");

    private static Conta ContaComSaldo(decimal saldo)
    {
        var conta = NovaConta();
        conta.Creditar(saldo);
        return conta;
    }

    [Fact]
    public void ContaNova_ComecaComSaldoZero()
    {
        NovaConta().Saldo.Should().Be(0m);
    }

    [Fact]
    public void Creditar_ComValorPositivo_AumentaSaldo()
    {
        var conta = NovaConta();

        conta.Creditar(150.50m);
        conta.Creditar(49.50m);

        conta.Saldo.Should().Be(200m);
        conta.Movimentacoes.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Creditar_ComValorNaoPositivo_LancaValorInvalido(decimal valor)
    {
        var conta = NovaConta();

        var acao = () => conta.Creditar(valor);

        acao.Should().Throw<ValorInvalidoException>();
        conta.Saldo.Should().Be(0m);
        conta.Movimentacoes.Should().BeEmpty();
    }

    [Fact]
    public void Debitar_ComSaldoSuficiente_ReduzSaldo()
    {
        var conta = ContaComSaldo(1000m);

        conta.Debitar(300m);

        conta.Saldo.Should().Be(700m);
    }

    [Fact]
    public void Debitar_AcimaDoSaldo_LancaSaldoInsuficienteENaoAlteraEstado()
    {
        var conta = ContaComSaldo(100m);

        var acao = () => conta.Debitar(100.01m);

        acao.Should().Throw<SaldoInsuficienteException>()
            .Which.SaldoDisponivel.Should().Be(100m);

        // O requisito central: a tentativa recusada não deixa rastro nem move o saldo.
        conta.Saldo.Should().Be(100m);
        conta.Movimentacoes.Should().HaveCount(1, "apenas o crédito inicial deve constar");
    }

    [Fact]
    public void Debitar_ExatamenteOSaldo_ZeraSaldoESucede()
    {
        var conta = ContaComSaldo(250.75m);

        conta.Debitar(250.75m);

        conta.Saldo.Should().Be(0m);
    }

    [Fact]
    public void Debitar_ComContaZerada_LancaSaldoInsuficiente()
    {
        var conta = NovaConta();

        var acao = () => conta.Debitar(0.01m);

        acao.Should().Throw<SaldoInsuficienteException>();
    }

    [Fact]
    public void Movimentacao_RegistraSaldoResultanteCorreto()
    {
        var conta = NovaConta();

        var credito = conta.Creditar(1000m, "Aporte inicial");
        var debito = conta.Debitar(250m, "Pagamento fornecedor");

        credito.Tipo.Should().Be(TipoMovimentacao.Credito);
        credito.Valor.Should().Be(1000m);
        credito.SaldoResultante.Should().Be(1000m);
        credito.Descricao.Should().Be("Aporte inicial");

        debito.Tipo.Should().Be(TipoMovimentacao.Debito);
        debito.Valor.Should().Be(250m, "o valor é sempre positivo; o sinal vem do tipo");
        debito.SaldoResultante.Should().Be(750m);
    }

    [Fact]
    public void Movimentar_TrocaVersao_ParaQueOTokenDeConcorrenciaFuncione()
    {
        var conta = NovaConta();
        var versaoInicial = conta.Versao;

        conta.Creditar(10m);

        conta.Versao.Should().NotBe(versaoInicial);
    }

    [Fact]
    public void Movimentacoes_SaoExpostasSomenteLeitura()
    {
        var conta = ContaComSaldo(10m);

        conta.Movimentacoes.Should().BeAssignableTo<IReadOnlyCollection<Movimentacao>>();
        conta.Movimentacoes.Should().NotBeAssignableTo<List<Movimentacao>>(
            "a coleção interna não pode ser manipulada de fora do agregado");
    }

    [Fact]
    public void Creditar_ComFormaPagamento_RegistraNaMovimentacao()
    {
        var conta = NovaConta();

        var movimentacao = conta.Creditar(500m, "Venda", FormaPagamento.Pix);

        movimentacao.FormaPagamento.Should().Be(FormaPagamento.Pix);
    }

    [Fact]
    public void Debitar_ComFormaPagamento_RegistraNaMovimentacao()
    {
        var conta = ContaComSaldo(500m);

        var movimentacao = conta.Debitar(200m, "Fornecedor", FormaPagamento.Boleto);

        movimentacao.FormaPagamento.Should().Be(FormaPagamento.Boleto);
    }

    [Fact]
    public void Movimentar_SemInformarFormaPagamento_DeixaNulo()
    {
        var conta = NovaConta();

        // Nulo e diferente de um valor padrao: significa "nao informado", que e o caso
        // dos lancamentos anteriores a existencia do campo.
        conta.Creditar(100m).FormaPagamento.Should().BeNull();
    }

    [Fact]
    public void FormaPagamento_NaoInterfereNaRegraDeSaldo()
    {
        var conta = ContaComSaldo(100m);

        var acao = () => conta.Debitar(500m, "Tentativa", FormaPagamento.Pix);

        acao.Should().Throw<SaldoInsuficienteException>(
            "a forma de pagamento e apenas um atributo do lancamento, nao afeta a regra");
        conta.Saldo.Should().Be(100m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Construtor_ComNomeVazio_Rejeita(string nome)
    {
        var acao = () => new Conta(nome, "12345678000199");

        acao.Should().Throw<ArgumentException>();
    }
}
