using FluentValidation;
using Invest.Application.Commands.Profile;
using Invest.Application.Common;
using Invest.Application.Queries.Profile;
using Invest.Application.Responses;
using Invest.Domain.Constants;
using Invest.Domain.Entities;
using Invest.Domain.Enums;
using Invest.Domain.Interfaces;
using Invest.Domain.Services;

namespace Invest.Application.Handlers;

public class ProfileHandler
{
    private readonly IUserProfileRepository _profileRepository;
    private readonly IUserSubStrategyRepository _subStrategyRepository;
    private readonly IUserAssetRepository _assetRepository;
    private readonly AllocationService _allocationService;
    private readonly IValidator<CreateProfileCommand> _createValidator;

    public ProfileHandler(
        IUserProfileRepository profileRepository,
        IUserSubStrategyRepository subStrategyRepository,
        IUserAssetRepository assetRepository,
        AllocationService allocationService,
        IValidator<CreateProfileCommand> createValidator)
    {
        _profileRepository = profileRepository;
        _subStrategyRepository = subStrategyRepository;
        _assetRepository = assetRepository;
        _allocationService = allocationService;
        _createValidator = createValidator;
    }

    public async Task<Result<ProfileResponse>> CreateProfileAsync(CreateProfileCommand command)
    {
        var validation = await _createValidator.ValidateAsync(command);
        if (!validation.IsValid)
        {
            var error = validation.Errors.First();
            return Result<ProfileResponse>.Failure("VALIDATION_ERROR", error.ErrorMessage, error.PropertyName);
        }

        var existing = await _profileRepository.GetByUserIdAsync(command.UserId);
        if (existing != null)
            return Result<ProfileResponse>.Failure("PROFILE_ALREADY_EXISTS", "Usuário já possui um perfil.");

        var perfil = ParsePerfil(command.Perfil);
        var faixa = _allocationService.DeterminarFaixa(command.ValorTotal);
        var profile = UserProfile.Create(command.UserId, perfil, command.ValorTotal, faixa,
            command.TemCarteiraExistente, command.CarteiraAnterior);

        await _profileRepository.AddAsync(profile);
        return Result<ProfileResponse>.Success(MapToResponse(profile));
    }

    public async Task<Result<ProfileResponse>> GetProfileAsync(GetProfileQuery query)
    {
        var profile = await _profileRepository.GetByUserIdAsync(query.UserId);
        if (profile == null)
            return Result<ProfileResponse>.Failure("PROFILE_NOT_FOUND", "Perfil não encontrado.");

        return Result<ProfileResponse>.Success(MapToResponse(profile));
    }

    public async Task<Result<UpdateProfileResponse>> UpdateProfileAsync(UpdateProfileCommand command)
    {
        var profile = await _profileRepository.GetByUserIdAsync(command.UserId);
        if (profile == null)
            return Result<UpdateProfileResponse>.Failure("PROFILE_NOT_FOUND", "Perfil não encontrado.");

        PerfilRisco? novoPerfil = command.Perfil != null ? ParsePerfil(command.Perfil) : null;
        var novoValor = command.ValorTotal ?? profile.ValorTotal;
        var novaFaixa = _allocationService.DeterminarFaixaComBuffer(novoValor, profile.Faixa);

        var mudouFaixa = profile.Update(novoPerfil, command.ValorTotal, novaFaixa);
        await _profileRepository.UpdateAsync(profile);

        var alocacao = _allocationService.ObterAlocacao(profile.Perfil, profile.ValorTotal);
        return Result<UpdateProfileResponse>.Success(new UpdateProfileResponse(
            profile.Id,
            profile.Perfil.ToString().ToLower(),
            profile.ValorTotal,
            MapFaixa(profile.Faixa),
            alocacao.Select(a => new AllocationClasseResponse(a.classe, a.percentual)).ToList(),
            mudouFaixa
        ));
    }

    public async Task<Result<ComparisonResponse>> ComparePortfolioAsync(ComparePortfolioCommand command)
    {
        var profile = await _profileRepository.GetByUserIdAsync(command.UserId);
        if (profile == null)
            return Result<ComparisonResponse>.Failure("PROFILE_NOT_FOUND", "Perfil não encontrado. Complete o onboarding primeiro.");

        var recomendado = _allocationService.ObterAlocacao(profile.Perfil, profile.ValorTotal)
            .ToDictionary(a => a.classe, a => a.percentual);

        var todasClasses = recomendado.Keys
            .Union(command.CarteiraAtual.Keys)
            .Distinct();

        var comparacao = todasClasses.Select(classe =>
        {
            var atual = command.CarteiraAtual.GetValueOrDefault(classe, 0m);
            var rec = recomendado.GetValueOrDefault(classe, 0m);
            return new ComparisonItemResponse(classe, atual, rec, atual - rec);
        }).ToList();

        return Result<ComparisonResponse>.Success(new ComparisonResponse(comparacao));
    }

    /// <summary>
    /// Remove perfil, sub-estratégia e todos os ativos do usuário.
    /// Após o reset o usuário volta ao onboarding.
    /// </summary>
    public async Task<Result<bool>> ResetProfileAsync(ResetProfileCommand command)
    {
        // Deleta ativos, sub-estratégia e perfil (nessa ordem para respeitar FK)
        await _assetRepository.DeleteAllByUserIdAsync(command.UserId);
        await _subStrategyRepository.DeleteByUserIdAsync(command.UserId);
        await _profileRepository.DeleteByUserIdAsync(command.UserId);

        return Result<bool>.Success(true);
    }

    public Result<AllocationResponse> GetAllocation(GetAllocationQuery query)
    {
        if (string.IsNullOrEmpty(query.Perfil) || query.Valor <= 0)
            return Result<AllocationResponse>.Failure("VALIDATION_ERROR", "Perfil e valor são obrigatórios.");

        var perfil = ParsePerfil(query.Perfil);
        var faixa = _allocationService.DeterminarFaixa(query.Valor);
        var alocacao = AllocationTargets.Get(faixa, perfil);

        return Result<AllocationResponse>.Success(new AllocationResponse(
            MapFaixa(faixa),
            query.Perfil.ToLower(),
            alocacao.Select(a => new AllocationClasseResponse(a.classe, a.percentual)).ToList()
        ));
    }

    private ProfileResponse MapToResponse(UserProfile profile)
    {
        var alocacao = _allocationService.ObterAlocacao(profile.Perfil, profile.ValorTotal);
        return new ProfileResponse(
            profile.Id,
            profile.UserId,
            profile.Perfil.ToString().ToLower(),
            profile.ValorTotal,
            MapFaixa(profile.Faixa),
            profile.TemCarteiraExistente,
            alocacao.Select(a => new AllocationClasseResponse(a.classe, a.percentual)).ToList(),
            profile.CreatedAt,
            profile.UpdatedAt
        );
    }

    private static PerfilRisco ParsePerfil(string perfil) => perfil.ToLower() switch
    {
        "conservador" => PerfilRisco.Conservador,
        "moderado" => PerfilRisco.Moderado,
        "arrojado" => PerfilRisco.Arrojado,
        _ => throw new ArgumentException($"Perfil inválido: {perfil}")
    };

    private static string MapFaixa(FaixaPatrimonio faixa) => faixa switch
    {
        FaixaPatrimonio.Ate10k => "ate_10k",
        FaixaPatrimonio.De10kA100k => "10k_100k",
        FaixaPatrimonio.Acima100k => "acima_100k",
        _ => throw new ArgumentOutOfRangeException()
    };
}
