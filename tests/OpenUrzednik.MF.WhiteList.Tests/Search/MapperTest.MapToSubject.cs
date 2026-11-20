using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Dto;
using OpenUrzednik.MF.WhiteList.Search;
using OpenUrzednik.MF.WhiteList.Tests.Fakes;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Search;

public sealed partial class MapperTest
{
    [Fact]
    public void MapToSubject_EveryField_MapsEach()
    {
        // Arrange
        var dto = new EntityDtoFaker().WithConstantSeed().Generate();

        // Act
        var subject = Mapper.MapToSubject(dto);

        // Assert
        subject.Name.ShouldBe(dto.Name);
        subject.Nip.ShouldBe(dto.Nip);
        subject.VatStatus.ShouldNotBeNull();
        subject.VatStatus.ShouldBe(Mapper.MapToVatStatus(dto.StatusVat));
        subject.Regon.ShouldBe(dto.Regon);
        subject.Krs.ShouldBe(dto.Krs);
        subject.ResidenceAddress.ShouldBe(dto.ResidenceAddress);
        subject.WorkingAddress.ShouldBe(dto.WorkingAddress);
        subject.Representatives.ShouldBe(dto.Representatives!.Select(Person));
        subject.AuthorizedClerks.ShouldBe(dto.AuthorizedClerks!.Select(Person));
        subject.Partners.ShouldBe(dto.Partners!.Select(Person));
        subject.RegistrationLegalDate.ShouldBe(dto.RegistrationLegalDate!.Value.ToPublicDate());
        subject.RegistrationDenialDate.ShouldBe(dto.RegistrationDenialDate!.Value.ToPublicDate());
        subject.RegistrationDenialBasis.ShouldBe(dto.RegistrationDenialBasis);
        subject.RestorationDate.ShouldBe(dto.RestorationDate!.Value.ToPublicDate());
        subject.RestorationBasis.ShouldBe(dto.RestorationBasis);
        subject.RemovalDate.ShouldBe(dto.RemovalDate!.Value.ToPublicDate());
        subject.RemovalBasis.ShouldBe(dto.RemovalBasis);
        subject.ExemptionSmeDate.ShouldBe(dto.ExemptionSmeDate!.Value.ToPublicDate());
        subject.AccountNumbers.ShouldBe(dto.AccountNumbers!);
        subject.HasVirtualAccounts.ShouldBe(dto.HasVirtualAccounts);
    }

    [Fact]
    public void MapToSubject_Dates_ReturnsMidnightUnspecified()
    {
        // Arrange
        var date = DateOnly.FromDateTime(new DateTime(2017, 11, 26));
        var dto = new EntityDto
        {
            Name = "A",
            RegistrationLegalDate = date,
            RegistrationDenialDate = date,
            RestorationDate = date,
            RemovalDate = date,
            ExemptionSmeDate = date,
        };

        // Act
        var subject = Mapper.MapToSubject(dto);

        // Assert
        DateTime?[] dates = [subject.RegistrationLegalDate, subject.RegistrationDenialDate, subject.RestorationDate, subject.RemovalDate, subject.ExemptionSmeDate];
        foreach (var value in dates)
        {
            value.ShouldBe(new DateTime(2017, 11, 26));
            value!.Value.Kind.ShouldBe(DateTimeKind.Unspecified);
        }
    }

    [Fact]
    public void MapToSubject_OnlyAName_LeavesTheRestEmpty()
    {
        // Act
        var subject = Mapper.MapToSubject(Minimal());

        // Assert
        subject.ShouldBe(new Subject("MINISTERSTWO FINANSÓW"));
        subject.Nip.ShouldBeNull();
        subject.VatStatus.ShouldBeNull();
        subject.RegistrationLegalDate.ShouldBeNull();
        subject.RegistrationDenialDate.ShouldBeNull();
        subject.RestorationDate.ShouldBeNull();
        subject.RemovalDate.ShouldBeNull();
        subject.ExemptionSmeDate.ShouldBeNull();
        subject.HasVirtualAccounts.ShouldBeNull();
        subject.Representatives.ShouldBeEmpty();
        subject.AuthorizedClerks.ShouldBeEmpty();
        subject.Partners.ShouldBeEmpty();
        subject.AccountNumbers.ShouldBeEmpty();
    }

    [Fact]
    public void MapToSubject_Lists_ReturnsReadOnlyLists()
    {
        // Arrange
        var dto = new EntityDtoFaker().WithConstantSeed().Generate();

        // Act
        var subject = Mapper.MapToSubject(dto);

        // Assert: an app can't cast them back to arrays and change them
        subject.Representatives.ShouldBeOfType<System.Collections.ObjectModel.ReadOnlyCollection<SubjectPerson>>();
        subject.AuthorizedClerks.ShouldBeOfType<System.Collections.ObjectModel.ReadOnlyCollection<SubjectPerson>>();
        subject.Partners.ShouldBeOfType<System.Collections.ObjectModel.ReadOnlyCollection<SubjectPerson>>();
        subject.AccountNumbers.ShouldBeOfType<System.Collections.ObjectModel.ReadOnlyCollection<string>>();
    }

    [Theory]
    [InlineData("", null)] // the test host's empty basis
    [InlineData(null, null)] // production's empty basis
    [InlineData(" ", " ")]
    [InlineData("Art. 96 ust. 9", "Art. 96 ust. 9")]
    public void MapToSubject_Basis_EmptyTextIsNull(string? basis, string? expected)
    {
        // Arrange
        var dto = new EntityDto { Name = "A", RegistrationDenialBasis = basis, RestorationBasis = basis, RemovalBasis = basis };

        // Act
        var subject = Mapper.MapToSubject(dto);

        // Assert
        subject.RegistrationDenialBasis.ShouldBe(expected);
        subject.RestorationBasis.ShouldBe(expected);
        subject.RemovalBasis.ShouldBe(expected);
    }

    [Fact]
    public void MapToSubject_NullName_ThrowsMissingName()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToSubject(new EntityDto { Name = null! }))
            .Message.ShouldBe("MF White List API response is missing 'subject.name'.");
    }

    public static TheoryData<string> PersonLists => ["representatives", "authorizedClerks", "partners"];

    [Theory]
    [MemberData(nameof(PersonLists))]
    public void MapToSubject_NullPerson_ThrowsMissingPerson(string list)
    {
        // Arrange
        EntityPersonDto?[] persons = [new EntityPersonDto { LastName = "Kowalski" }, null];
        var dto = list switch
        {
            "representatives" => new EntityDto { Name = "A", Representatives = persons },
            "authorizedClerks" => new EntityDto { Name = "A", AuthorizedClerks = persons },
            _ => new EntityDto { Name = "A", Partners = persons },
        };

        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToSubject(dto)).Message.ShouldBe($"MF White List API response is missing '{list}[]'.");
    }

    [Fact]
    public void MapToSubject_NullAccountNumber_ThrowsMissingAccountNumber()
    {
        // Arrange
        var dto = new EntityDto { Name = "A", AccountNumbers = ["59101010100075912222000000", null] };

        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToSubject(dto))
            .Message.ShouldBe("MF White List API response is missing 'accountNumbers[]'.");
    }

    [Fact]
    public void MapToSubject_UnknownStatus_Throws()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToSubject(new EntityDto { Name = "A", StatusVat = "Aktywny" }))
            .Message.ShouldBe("MF White List API response has an unknown 'statusVat'.");
    }

    private static SubjectPerson Person(EntityPersonDto? dto) => new(dto!.CompanyName, dto.FirstName, dto.LastName, dto.Nip);
}
