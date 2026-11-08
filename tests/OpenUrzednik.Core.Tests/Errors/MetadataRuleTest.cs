using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;

namespace OpenUrzednik.Core.Tests.Errors;

// ADR-0022: every error Core creates keeps its metadata to JSON-shaped values.
public class MetadataRuleTest
{
    public static TheoryData<OpenUrzednikError> CoreErrors => new()
    {
        new BadRequestError("message"),
        new UnauthorizedError("message"),
        new NotFoundError("message"),
        new RequestTimeoutError("message", TimeSpan.FromSeconds(5), new TimeoutException()),
        new RateLimitExceededError("message", TimeSpan.FromSeconds(30)),
        new RateLimitExceededError("message", null),
        new ServiceUnavailableError("message", new InvalidOperationException()),
        new UnknownError("message"),
        new SerializationError("message"),
        new ConnectionFailedError("message"),
        new ValidationError("message", "rule", "name", 5),
        new ValidationError<DateTime>("message", "date", "date", new DateTime(2026, 1, 5)),
        new ValidationError<string>("message", "nip", "nip", "******7890"),
        new RangeValidationError<DateTime>("message", "dateRange", "from", new DateTime(2026, 1, 5), "to", new DateTime(2026, 6, 1)),
    };

    [Theory]
    [MemberData(nameof(CoreErrors))]
    public void Metadata_EveryCoreError_FollowsTheMetadataRule(OpenUrzednikError error)
    {
        // Assert
        error.ShouldFollowTheMetadataRule();
    }
}
