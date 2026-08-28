using FluentValidation;

namespace ContaCorrente.Api.Application.Contas.Commands.Movimentar;

/// <summary>
/// Valida apenas o <b>formato</b> do comando. A regra de negócio — saldo suficiente —
/// pertence ao domínio e não é verificada aqui: validar saldo neste ponto abriria uma
/// janela entre a checagem e a escrita, e duplicaria a regra em dois lugares.
/// </summary>
public sealed class MovimentarCommandValidator : AbstractValidator<MovimentarCommand>
{
    public MovimentarCommandValidator()
    {
        RuleFor(c => c.ContaId)
            .NotEmpty().WithMessage("ContaId é obrigatório.");

        RuleFor(c => c.Tipo)
            .IsInEnum().WithMessage("Tipo deve ser Credito (1) ou Debito (2).");

        RuleFor(c => c.Valor)
            .GreaterThan(0).WithMessage("Valor deve ser maior que zero.");

        RuleFor(c => c.Descricao)
            .MaximumLength(200).WithMessage("Descrição deve ter no máximo 200 caracteres.");

        // Opcional, mas se vier tem de ser um valor conhecido do enum. Sem esta regra,
        // um número fora da faixa entraria silenciosamente no banco.
        RuleFor(c => c.FormaPagamento)
            .IsInEnum().WithMessage("Forma de pagamento inválida.")
            .When(c => c.FormaPagamento.HasValue);
    }
}
