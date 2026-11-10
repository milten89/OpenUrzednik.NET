using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.MF.WhiteList.Dto;
using OpenUrzednik.Privacy;

namespace OpenUrzednik.MF.WhiteList.Common;

/// <summary>
/// Turns the MF White List API's error responses into the provider's error types (ADR-0023's error table): it reads the API's
/// <c>{"message", "code"}</c> body and keeps the code in <see cref="IWhiteListErrorCode.WhiteListCode"/>. Statuses the table doesn't name
/// fall back to the shared layer's default mapping.
/// </summary>
internal static class WhiteListErrorMapping
{
    // The codes of a 400 that asks to repeat the query while the list is updated.
    internal const string DatabaseUpdatedCode = "WL-195";
    internal const string UpdateInProgressCode = "WL-196";

    // The code of a 500 for an entity whose historical status the system can't determine.
    internal const string HistoricalStatusUnknownCode = "WL-197";

    // The shortest identifier the API echoes is a 9-digit REGON; NIP has 10 digits, a long REGON 14, an account number 26.
    internal const int MinIdentifierDigits = 9;

    // The prefix of the API's own codes (WL-100 to WL-197).
    private const string CodePrefix = "WL-";

    private const HttpStatusCode TooManyRequests = (HttpStatusCode)429;

    /// <summary>
    /// Maps a non-success response; <see langword="null"/> for a status the table doesn't name (the shared default mapping applies).
    /// </summary>
    /// <param name="context">The response.</param>
    /// <param name="timeProvider">The client's clock, for the midnight estimate of a 429 without <c>Retry-After</c>.</param>
    internal static async Task<OpenUrzednikError?> MapAsync(ErrorResponseContext context, TimeProvider timeProvider)
    {
        var statusCode = context.StatusCode;
        if (statusCode != HttpStatusCode.BadRequest && statusCode != TooManyRequests && statusCode < HttpStatusCode.InternalServerError)
            return null;

        var body = Parse(await context.ReadMessageAsync().ConfigureAwait(false));
        var code = SanitizeCode(body?.Code);
        var detail = Detail(code, body?.Message);
        var path = context.RequestPath;

        if (statusCode == TooManyRequests)
        {
            // The ministry documents no Retry-After; an IP over the limit "may" be blocked until midnight (ADR-0023).
            var retryAfter = context.RetryAfter ?? WarsawCalendar.UntilNextMidnight(timeProvider);
            return new WhiteListRateLimitExceededError(
                $"MF White List API rate limit hit on {path}{detail}", retryAfter, statusCode, code);
        }

        if (statusCode == HttpStatusCode.BadRequest)
        {
            if (code == DatabaseUpdatedCode || code == UpdateInProgressCode)
                return new WhiteListServiceUnavailableError(
                    $"MF White List API asked to repeat the request to {path} later{detail}", statusCode, code);

            return new WhiteListBadRequestError($"MF White List API rejected the request to {path}{detail}", statusCode, code);
        }

        if (statusCode == HttpStatusCode.InternalServerError && code == HistoricalStatusUnknownCode)
            return new WhiteListUnknownError(
                $"MF White List API can't determine the historical status for {path}{detail}", statusCode, code);

        return new WhiteListServiceUnavailableError(
            $"MF White List API is unavailable for {path}, status {((int)statusCode).ToString(CultureInfo.InvariantCulture)}{detail}",
            statusCode, code);
    }

    /// <summary>
    /// Replaces every run of at least <see cref="MinIdentifierDigits"/> digits with its masked form (<see cref="SensitiveValue.Mask"/>), so an
    /// identifier the server's message echoes never reaches messages or telemetry (ADR-0003).
    /// </summary>
    internal static string MaskIdentifiers(string text)
    {
        StringBuilder? masked = null;
        var copied = 0;
        var start = 0;
        // Each non-digit, and the end of the text, closes the run of digits [start, index), which may be empty. One loop with no inner
        // skip, so that mutation testing has no equivalent mutant here (ADR-0020).
        for (var index = 0; index <= text.Length; index++)
        {
            if (index < text.Length && IsDigit(text[index]))
                continue;

            var length = index - start;
            if (length >= MinIdentifierDigits)
            {
                masked ??= new StringBuilder(text.Length);
                masked.Append(text, copied, start - copied).Append(SensitiveValue.Mask(text.Substring(start, length)));
                copied = index;
            }

            start = index + 1;
        }

        if (masked is null)
            return text;

        return masked.Append(text, copied, text.Length - copied).ToString();
    }

    /// <summary>
    /// The server's code when it has a documented shape: <c>WL-</c> and three digits (<c>WL-115</c>), or WL2's single upper-case letter
    /// (<c>I</c>, <c>D</c>, <c>X</c>); <see langword="null"/> for anything else, which is never echoed into messages, <c>WhiteListCode</c> or
    /// metadata, so a proxy's or a changed API's text can't carry an identifier or other data there.
    /// </summary>
    internal static string? SanitizeCode(string? code)
    {
        if (code is null)
            return null;

        var length = code.Length;
        if (length == 1)
            return IsUpperCaseLetter(code[0]) ? code : null;

        if (length == CodePrefix.Length + 3 && code.StartsWith(CodePrefix, StringComparison.Ordinal)
            && IsDigit(code[3]) && IsDigit(code[4]) && IsDigit(code[5]))
            return code;

        return null;
    }

    // ASCII digits only: the API sends identifiers as ASCII.
    private static bool IsDigit(char value) => (uint)(value - '0') <= 9;

    private static bool IsUpperCaseLetter(char value) => (uint)(value - 'A') <= 'Z' - 'A';

    // The body is the API's JSON; anything else (no body, a proxy's HTML page, a body cut at the 500 characters the executor reads) has no code.
    private static ErrorDto? Parse(string? body)
    {
        if (body is null)
            return null;

        // One return after the try, so that mutation testing has no equivalent mutant in the catch block (ADR-0020).
        ErrorDto? dto = null;
        try
        {
            dto = JsonSerializer.Deserialize(body, WhiteListJsonContext.Default.ErrorDto);
        }
        catch (JsonException)
        {
            // Not the API's JSON: the answer has no code.
        }

        return dto;
    }

    // ": WL-115 Nieprawidłowy NIP." with the identifiers in the message masked, or "." without code and message.
    private static string Detail(string? code, string? message)
    {
        if (message is not null)
            message = MaskIdentifiers(message);

        if (code is null)
            return message is null ? "." : $": {message}";

        return message is null ? $": {code}" : $": {code} {message}";
    }
}
