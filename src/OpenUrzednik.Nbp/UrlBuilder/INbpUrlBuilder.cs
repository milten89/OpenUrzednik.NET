namespace OpenUrzednik.Nbp.UrlBuilder;

public interface INbpUrlBuilder
{
    string Latest();
    string ForTopCount(int topCount);
    string Today();
    string ForDate(DateOnly date);
    string ForDateRange(DateOnly from, DateOnly to);
}
