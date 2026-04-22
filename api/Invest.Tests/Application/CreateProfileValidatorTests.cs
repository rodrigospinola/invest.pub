using FluentAssertions;
using Invest.Application.Commands.Profile;
using Invest.Application.Validators;

namespace Invest.Tests.Application;

public class CreateProfileValidatorTests
{
    private readonly CreateProfileValidator _sut = new();

    // =========================================================
    // Valid cases
    // =========================================================

    [Fact]
    public void Validate_PerfilConservadorValorValido_IsValid()
    {
        var command = new CreateProfileCommand(Guid.NewGuid(), "conservador", 1000m, false, null);
        var result = _sut.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PerfilModeradoValorValido_IsValid()
    {
        var command = new CreateProfileCommand(Guid.NewGuid(), "moderado", 50000m, false, null);
        var result = _sut.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PerfilArrojadoValorMinimo_IsValid()
    {
        var command = new CreateProfileCommand(Guid.NewGuid(), "arrojado", 0.01m, false, null);
        var result = _sut.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PerfilCaseInsensitive_IsValid()
    {
        // The validator uses ToLower() so mixed-case should work
        var command = new CreateProfileCommand(Guid.NewGuid(), "Conservador", 1000m, false, null);
        var result = _sut.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    // =========================================================
    // Invalid perfil
    // =========================================================

    [Fact]
    public void Validate_PerfilVazio_TemErroNoPerfil()
    {
        var command = new CreateProfileCommand(Guid.NewGuid(), "", 1000m, false, null);
        var result = _sut.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Perfil");
    }

    [Fact]
    public void Validate_PerfilInvalido_TemErroNoPerfil()
    {
        var command = new CreateProfileCommand(Guid.NewGuid(), "invalido", 1000m, false, null);
        var result = _sut.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Perfil");
    }

    [Fact]
    public void Validate_PerfilNulo_TemErroNoPerfil()
    {
        // Must() is guarded with .When(x => !IsNullOrEmpty), so null goes to NotEmpty which returns a validation error.
        var command = new CreateProfileCommand(Guid.NewGuid(), null!, 1000m, false, null);
        var result = _sut.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Perfil");
    }

    // =========================================================
    // Invalid ValorTotal
    // =========================================================

    [Fact]
    public void Validate_ValorTotalZero_TemErroNoValorTotal()
    {
        var command = new CreateProfileCommand(Guid.NewGuid(), "conservador", 0m, false, null);
        var result = _sut.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ValorTotal");
    }

    [Fact]
    public void Validate_ValorTotalNegativo_TemErroNoValorTotal()
    {
        var command = new CreateProfileCommand(Guid.NewGuid(), "conservador", -1m, false, null);
        var result = _sut.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ValorTotal");
    }
}
