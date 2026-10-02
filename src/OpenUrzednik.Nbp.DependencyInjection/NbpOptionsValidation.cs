using Microsoft.Extensions.Options;

using OpenUrzednik.Nbp.Options;

namespace OpenUrzednik.Nbp.DependencyInjection;

/// <summary>
/// Applies the clients' own checks (<see cref="NbpOptions.Validate"/>) to the configured options, so invalid settings fail at startup.
/// </summary>
internal sealed class NbpOptionsValidation : IValidateOptions<NbpOptions>
{
    public ValidateOptionsResult Validate(string? name, NbpOptions options)
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
