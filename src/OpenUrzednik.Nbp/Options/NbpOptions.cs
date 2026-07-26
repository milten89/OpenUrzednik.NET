namespace OpenUrzednik.Nbp.Options;

public class NbpOptions
{
    public string ApiUrl { get; set; } = "https://api.nbp.pl/api/";

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}
