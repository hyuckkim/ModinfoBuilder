abstract class FileStatus
{
    public abstract string StatusText { get; }

    public abstract bool FileExists { get; }
    public string FilePath { get; set; } = string.Empty;

}

class Changed : FileStatus
{
    private readonly string oldHash, newHash;
    public Changed(string FilePath, string oldHash, string newHash)
    {
        this.oldHash = oldHash;
        this.newHash = newHash;
        this.FilePath = FilePath;
    }
    public override string StatusText => $"{FilePath}의 해시를 수정했습니다 : {oldHash[..8]}.. -> {newHash[..8]}..\n";
    public override bool FileExists => true;
}

class Ignored : FileStatus
{
    public Ignored(string FilePath)
    {
        this.FilePath = FilePath;
    }
    public override string StatusText => string.Empty;
    public override bool FileExists => true;
}

class NotFound : FileStatus
{
    public NotFound(string CheckPath)
    {
        FilePath = CheckPath;
    }
    public override string StatusText => $"경고 : {FilePath}가 없습니다.\n";
    public override bool FileExists => false;
}