using AssetsTools.NET;

namespace FSMExpress.Common.Assets;
public class NamedAssetPPtr : AssetPPtr
{
    public string Name { get; set; }
    public string? BundleName { get; set; }

    public NamedAssetPPtr() : base()
    {
        Name = string.Empty;
    }

    public NamedAssetPPtr(int fileId, long pathId) : base(fileId, pathId)
    {
        Name = string.Empty;
    }

    public NamedAssetPPtr(string fileName, long pathId) : base(fileName, pathId)
    {
        Name = string.Empty;
    }

    public NamedAssetPPtr(string fileName, int fileId, long pathId) : base(fileName, fileId, pathId)
    {
        Name = string.Empty;
    }

    public NamedAssetPPtr(string fileName, int fileId, long pathId, string name) : base(fileName, fileId, pathId)
    {
        Name = name;
    }

    public override string ToString()
    {
        string fileText;
        if (HasFilePath())
            fileText = Path.GetFileName(FilePath);
        else
            fileText = FileId.ToString();

        if (BundleName is not null)
            fileText = $"{BundleName}:{fileText}";

        if (PathId == 0)
            return "PPtr(null)";
        else if (Name != string.Empty)
            return $"{Name}/PPtr({fileText},{PathId})";
        else
            return $"PPtr({fileText},{PathId})";
    }
}
