namespace Turbo.Plugins
{
    public interface IPlayerDensityInfo
    {
        int MaxDensityRangeSupported { get; }
        int GetDensity(int range);
    }
}
