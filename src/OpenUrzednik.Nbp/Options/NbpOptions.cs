namespace OpenUrzednik.Nbp.Options;

public class NbpOptions
{
    public const string DefaultApiUrl = "https://api.nbp.pl/api/";

    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    public string ApiUrl { get; set; } = DefaultApiUrl;

    public TimeSpan Timeout { get; set; } = DefaultTimeout;
}
