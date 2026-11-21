using Bogus;

using OpenUrzednik.MF.WhiteList.Dto;

namespace OpenUrzednik.MF.WhiteList.Tests.Fakes;

internal sealed class EntityPersonDtoFaker : Faker<EntityPersonDto>
{
    public EntityPersonDtoFaker()
    {
        RuleFor(x => x.CompanyName, f => f.Company.CompanyName());
        RuleFor(x => x.FirstName, f => f.Name.FirstName());
        RuleFor(x => x.LastName, f => f.Name.LastName());
        RuleFor(x => x.Nip, f => f.Random.ReplaceNumbers("##########"));
    }
}
