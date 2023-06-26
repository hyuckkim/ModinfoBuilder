internal record struct ModinfoRecord(int Changed, int Ignored, int NotFound, int Missed)
{
    public static implicit operator (int changed, int ignored, int notFound, int missed)(ModinfoRecord value)
    {
        return (value.Changed, value.Ignored, value.NotFound, value.Missed);
    }

    public static implicit operator ModinfoRecord((int changed, int ignored, int notFound, int missed) value)
    {
        return new ModinfoRecord(value.changed, value.ignored, value.notFound, value.missed);
    }
    public readonly ModinfoRecord AddFileStatus(FileStatus status) =>
        new(
            Changed + (status.GetType() == typeof(Changed) ? 1 : 0),
            Ignored + (status.GetType() == typeof(Ignored) ? 1 : 0),
            NotFound + (status.GetType() == typeof(NotFound) ? 1 : 0),
            Missed);
}