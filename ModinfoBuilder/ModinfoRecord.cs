internal record struct ModinfoRecord(int changed, int ignored, int notFound, int missed)
{
    public static implicit operator (int changed, int ignored, int notFound, int missed)(ModinfoRecord value)
    {
        return (value.changed, value.ignored, value.notFound, value.missed);
    }

    public static implicit operator ModinfoRecord((int changed, int ignored, int notFound, int missed) value)
    {
        return new ModinfoRecord(value.changed, value.ignored, value.notFound, value.missed);
    }
    public readonly ModinfoRecord AddFileStatus(FileStatus status) =>
        new(
            changed + (status.GetType() == typeof(Changed) ? 1 : 0),
            ignored + (status.GetType() == typeof(Ignored) ? 1 : 0),
            notFound + (status.GetType() == typeof(NotFound) ? 1 : 0),
            missed);
}