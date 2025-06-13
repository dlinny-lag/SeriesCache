namespace SeriesCache;

public enum OverwriteMode
{
    None = 0,
    Error = None,
    Skip,
    Replace,
}
public enum MissingMode
{
    None = 0,
    Error = None,
    Interpolate,
    Extrapolate,
    InterpolateAndExtrapolate
}

public class ReadWriteSettings
{
    public OverwriteMode OnOverwrite { get; init; }
    public MissingMode OnMiss { get; init; }
}
