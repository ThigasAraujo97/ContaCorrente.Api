using FluentValidation;

namespace ContaCorrente.Api.Application.Contas.Commands.CriarConta;

public sealed class CriarContaCommandValidator : AbstractValidator<CriarContaCommand>
{
    public CriarContaCommandValidator()
    {
        RuleFor(c => c.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150).WithMessage("Nome deve ter no máximo 150 caracteres.");

        RuleFor(c => c.Documento)
            .NotEmpty().WithMessage("Documento é obrigatório.")
            .MaximumLength(20).WithMessage("Documento deve ter no máximo 20 caracteres.");
    }
}
