using OpenUrzednik.Core;
using OpenUrzednik.Nbp.Validation;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Validation;

// ADR-0022: the errors of every NBP validator keep their metadata to JSON-shaped values (no DateOnly, no tuples).
public class ValidatorMetadataRuleTest
{
    private static readonly DateOnly Today = new(2026, 11, 8);

    public static TheoryData<string> Validators => ["currencyDate", "goldDate", "lastPublishedBefore", "dateRangeOrder", "dateRangeLength", "topCount", "iso4217"];

    [Theory]
    [MemberData(nameof(Validators))]
    public void Validate_Failure_ErrorFollowsTheMetadataRule(string validator)
    {
        // Act
        var result = validator switch
        {
            "currencyDate" => new CurrencyDateValidator("date", new DateOnly(1990, 1, 1), Today).Validate(),
            "goldDate" => new GoldDateValidator("date", new DateOnly(1990, 1, 1), Today).Validate(),
            "lastPublishedBefore" => new LastPublishedBeforeDateValidator("date", new DateOnly(1990, 1, 1), new DateOnly(2002, 1, 2), Today).Validate(),
            "dateRangeOrder" => new DateRangeValidator("from", "to", (new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1)), 93).Validate(),
            "dateRangeLength" => new DateRangeValidator("from", "to", (new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 1)), 93).Validate(),
            "topCount" => new TopCountValidator("topCount", 0).Validate(),
            "iso4217" => new Iso4217Validator("currency", "US").Validate(),
            _ => OpenUrzednikResult.Success(),
        };

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldFollowTheMetadataRule();
    }
}
