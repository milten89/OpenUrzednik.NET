using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.MF.WhiteList;
using OpenUrzednik.TestCommon;
using OpenUrzednik.Transport.Http;

using Shouldly;

namespace OpenUrzednik.Architecture.Tests;

// ADR-0021: every error's Metadata follows the JSON-shaped rule (TestCommon's ShouldFollowTheMetadataRule). One sample of each
// concrete public error type runs through it; a new error type needs a sample here, or the coverage test fails.
public class ErrorMetadataTest
{
    private static readonly DateTime Date = new(2026, 1, 5);

    private static readonly Dictionary<Type, Func<OpenUrzednikError>> Samples = new()
    {
        // Core
        [typeof(BadRequestError)] = () => new BadRequestError("Bad request."),
        [typeof(ConnectionFailedError)] = () => new ConnectionFailedError("Connection failed.", new IOException("Reset by peer.")),
        [typeof(NotFoundError)] = () => new NotFoundError("Not found."),
        [typeof(RateLimitExceededError)] = () => new RateLimitExceededError("Too many requests.", TimeSpan.FromSeconds(30)),
        [typeof(RequestTimeoutError)] = () => new RequestTimeoutError("Timed out.", TimeSpan.FromSeconds(10)),
        [typeof(SerializationError)] = () => new SerializationError("Bad payload.", new FormatException("Unexpected token.")),
        [typeof(ServiceUnavailableError)] = () => new ServiceUnavailableError("Unavailable."),
        [typeof(UnauthorizedError)] = () => new UnauthorizedError("Unauthorized."),
        [typeof(UnknownError)] = () => new UnknownError("Unknown."),
        [typeof(ValidationError)] = () => new ValidationError("Invalid.", "rule", "name", "value"),
        [typeof(ValidationError<>)] = () => new ValidationError<DateTime>("Invalid.", "rule", "date", Date),
        [typeof(RangeValidationError<>)] = () => new RangeValidationError<DateTime>("Invalid.", "rule", "from", Date, "to", Date.AddDays(1)),

        // OpenUrzednik.Transport.Http
        [typeof(HttpBadRequestError)] = () => new HttpBadRequestError("Bad request.", HttpStatusCode.BadRequest),
        [typeof(HttpNotFoundError)] = () => new HttpNotFoundError("Not found.", HttpStatusCode.NotFound),
        [typeof(HttpRateLimitExceededError)] = () => new HttpRateLimitExceededError("Too many requests.", TimeSpan.FromSeconds(30), (HttpStatusCode)429),
        [typeof(HttpRequestTimeoutError)] = () => new HttpRequestTimeoutError("Timed out.", HttpStatusCode.RequestTimeout),
        [typeof(HttpServiceUnavailableError)] = () => new HttpServiceUnavailableError("Unavailable.", HttpStatusCode.ServiceUnavailable),
        [typeof(HttpUnauthorizedError)] = () => new HttpUnauthorizedError("Unauthorized.", HttpStatusCode.Unauthorized),
        [typeof(HttpUnknownError)] = () => new HttpUnknownError("Unknown.", HttpStatusCode.InternalServerError),

        // OpenUrzednik.MF.WhiteList
        [typeof(WhiteListBadRequestError)] = () => new WhiteListBadRequestError("Bad request.", HttpStatusCode.BadRequest, "WL-113"),
        [typeof(WhiteListNotFoundError)] = () => new WhiteListNotFoundError("Not found.", "abc-123", new DateTimeOffset(Date, TimeSpan.FromHours(1))),
        [typeof(WhiteListQueryRejectedError)] = () => new WhiteListQueryRejectedError("Rejected.", "WL-115"),
        [typeof(WhiteListRateLimitExceededError)] = () => new WhiteListRateLimitExceededError("Too many requests.", TimeSpan.FromSeconds(30), (HttpStatusCode)429, "WL-190"),
        [typeof(WhiteListServiceInactiveError)] = () => new WhiteListServiceInactiveError("Inactive.", "WL-196"),
        [typeof(WhiteListServiceUnavailableError)] = () => new WhiteListServiceUnavailableError("Unavailable.", HttpStatusCode.ServiceUnavailable, "WL-191"),
        [typeof(WhiteListUnknownError)] = () => new WhiteListUnknownError("Unknown.", HttpStatusCode.InternalServerError, "WL-100"),
    };

    public static TheoryData<string> SampleTypes() => new(Samples.Keys.Select(Name));

    [Theory]
    [MemberData(nameof(SampleTypes))]
    public void Metadata_OfEveryErrorType_FollowsTheMetadataRule(string errorType)
    {
        // Arrange
        var sample = Samples.Single(sample => Name(sample.Key) == errorType);
        var error = sample.Value();

        // Act & Assert
        (error.GetType().IsGenericType ? error.GetType().GetGenericTypeDefinition() : error.GetType())
            .ShouldBe(sample.Key, "the sample builds a different type than its key");
        error.ShouldFollowTheMetadataRule();
    }

    [Fact]
    public void Samples_Always_CoverEveryConcretePublicErrorType()
    {
        // Act
        var missing = PublicSurface.Types()
            .Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(OpenUrzednikError)))
            .Where(type => !Samples.ContainsKey(type))
            .Select(Name)
            .ToList();

        // Assert
        missing.ShouldBeEmpty("add a sample of each new error type");
    }

    // An app reads an error's details by its type (`is WhiteListNotFoundError`, ADR-0021), and the samples above can only build
    // public types, so every error type is public.
    [Fact]
    public void ErrorType_InAnyPackage_IsPublic()
    {
        // Act
        var hidden = SrcAssemblies.All
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(OpenUrzednikError)))
            .Except(PublicSurface.Types())
            .Select(Name)
            .ToList();

        // Assert
        hidden.ShouldBeEmpty();
    }

    private static string Name(Type type) => type.FullName ?? type.Name;
}
