using Microsoft.Extensions.Options;

namespace OpenUrzednik.MF.WhiteList.DependencyInjection;

/// <summary>
/// Applies the clients' own checks (<see cref="WhiteListOptions.Validate"/>) to the configured options, so invalid settings fail at startup.
/// </summary>
internal sealed class WhiteListOptionsValidation : IValidateOptions<WhiteListOptions>
{
    public ValidateOptionsResult Validate(string? name, WhiteListOptions options)
    {
        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (ArgumentException ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }
    }
}
