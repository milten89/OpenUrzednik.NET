using Bogus;

using OpenUrzednik.MF.WhiteList.Dto;

namespace OpenUrzednik.MF.WhiteList.Tests.Fakes;

// A subject with every field set, as a struck-off subject's entry has them; the identifiers are random digits, not real ones.
internal sealed class EntityDtoFaker : Faker<EntityDto>
{
    public EntityDtoFaker()
    {
        RuleFor(x => x.Name, f => f.Company.CompanyName());
        RuleFor(x => x.Nip, f => f.Random.ReplaceNumbers("##########"));
        RuleFor(x => x.StatusVat, f => f.PickRandom("Czynny", "Zwolniony", "Niezarejestrowany"));
        RuleFor(x => x.Regon, f => f.Random.ReplaceNumbers("#########"));
        RuleFor(x => x.Krs, f => f.Random.ReplaceNumbers("##########"));
        RuleFor(x => x.ResidenceAddress, f => f.Address.FullAddress());
        RuleFor(x => x.WorkingAddress, f => f.Address.FullAddress());
        RuleFor(x => x.Representatives, _ => new EntityPersonDtoFaker().Generate(2).ToArray());
        RuleFor(x => x.AuthorizedClerks, _ => new EntityPersonDtoFaker().Generate(1).ToArray());
        RuleFor(x => x.Partners, _ => new EntityPersonDtoFaker().Generate(3).ToArray());
        RuleFor(x => x.RegistrationLegalDate, f => DateOnly.FromDateTime(f.Date.Past(10)));
        RuleFor(x => x.RegistrationDenialDate, f => DateOnly.FromDateTime(f.Date.Past(10)));
        RuleFor(x => x.RegistrationDenialBasis, f => f.Lorem.Sentence());
        RuleFor(x => x.RestorationDate, f => DateOnly.FromDateTime(f.Date.Past(10)));
        RuleFor(x => x.RestorationBasis, f => f.Lorem.Sentence());
        RuleFor(x => x.RemovalDate, f => DateOnly.FromDateTime(f.Date.Past(10)));
        RuleFor(x => x.RemovalBasis, f => f.Lorem.Sentence());
        RuleFor(x => x.ExemptionSmeDate, f => DateOnly.FromDateTime(f.Date.Past(10)));
        RuleFor(x => x.AccountNumbers, f => [f.Random.ReplaceNumbers("##########################"), f.Random.ReplaceNumbers("##########################")]);
        RuleFor(x => x.HasVirtualAccounts, f => f.Random.Bool());
    }
}
