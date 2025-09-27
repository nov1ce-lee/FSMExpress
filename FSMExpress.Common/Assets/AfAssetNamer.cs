using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace FSMExpress.Common.Assets;
public class AfAssetNamer(AssetsManager manager, AssetsFileInstance inst)
{
    public string? GetName(int fileId, long pathId)
    {
        if (fileId == 0 && pathId == 0)
            return null;

        var external = manager.GetExtAsset(inst, fileId, pathId, true, AssetReadFlags.SkipMonoBehaviourFields);

        var extInfo = external.info;
        var extFileInst = external.file;
        if (extFileInst is null)
            return null;

        var extFile = extFileInst.file;

        var classId = extInfo.GetTypeId(extFile);

        // try to get name from type tree
        if (extFile.Metadata.TypeTreeEnabled)
        {
            TypeTreeType ttType;
            if (classId == 0x72 || classId < 0)
                ttType = extFile.Metadata.FindTypeTreeTypeByScriptIndex(extInfo.GetScriptIndex(extFile));
            else
                ttType = extFile.Metadata.FindTypeTreeTypeByID(classId);

            if (ttType != null && ttType.Nodes.Count > 0)
            {
                var typeName = ttType.Nodes[0].GetTypeString(ttType.StringBufferBytes);
                var reader = extFile.Reader;
                if (ttType.Nodes.Count > 1 && ttType.Nodes[1].GetNameString(ttType.StringBufferBytes) == "m_Name")
                {
                    lock (extFileInst.LockReader)
                    {
                        reader.Position = extInfo.GetAbsoluteByteOffset(extFile);
                        string? assetName = reader.ReadCountStringInt32();
                        if (assetName == "")
                            assetName = null;

                        return assetName;
                    }
                }
                else if (typeName == "GameObject")
                {
                    lock (extFileInst.LockReader)
                    {
                        reader.Position = extInfo.GetAbsoluteByteOffset(extFile);
                        int size = reader.ReadInt32();
                        int componentSize = extFile.Header.Version > 0x10 ? 0x0c : 0x10;
                        reader.Position += size * componentSize;
                        reader.Position += 0x04;
                        return reader.ReadCountStringInt32();
                    }
                }
                else if (typeName == "MonoBehaviour")
                {
                    lock (extFileInst.LockReader)
                    {
                        reader.Position = extInfo.GetAbsoluteByteOffset(extFile);
                        reader.Position += 0x1c;
                        var assetName = reader.ReadCountStringInt32();
                        if (assetName == "")
                        {
                            assetName = GetMonoBehaviourNameFast(manager, extFileInst, extInfo);
                            if (assetName == "")
                                assetName = null;
                        }

                        return assetName;
                    }
                }
                return null;
            }
        }

        // try to get name from class database
        {
            var cldb = manager.ClassDatabase;
            var type = cldb?.FindAssetClassByID(classId);
            if (type == null || cldb == null)
            {
                return null;
            }

            var typeName = cldb.GetString(type.Name);
            var cldbNodes = type.GetPreferredNode(false).Children;
            if (cldbNodes.Count == 0)
            {
                return null;
            }

            var reader = extFile.Reader;
            if (cldbNodes.Count > 1 && cldb.GetString(cldbNodes[0].FieldName) == "m_Name")
            {
                lock (extFileInst.LockReader)
                {
                    reader.Position = extInfo.GetAbsoluteByteOffset(extFile);
                    string? assetName = reader.ReadCountStringInt32();
                    if (assetName == "")
                        assetName = null;

                    return assetName;
                }
            }
            else if (typeName == "GameObject")
            {
                lock (extFileInst.LockReader)
                {
                    reader.Position = extInfo.GetAbsoluteByteOffset(extFile);
                    int size = reader.ReadInt32();
                    int componentSize = extFile.Header.Version > 0x10 ? 0x0c : 0x10;
                    reader.Position += size * componentSize;
                    reader.Position += 0x04;
                    return reader.ReadCountStringInt32();
                }
            }
            else if (typeName == "MonoBehaviour")
            {
                lock (extFileInst.LockReader)
                {
                    reader.Position = extInfo.GetAbsoluteByteOffset(extFile);
                    reader.Position += 0x1c;
                    var assetName = reader.ReadCountStringInt32();
                    if (assetName == "")
                    {
                        assetName = GetMonoBehaviourNameFast(manager, extFileInst, extInfo);
                        if (assetName == "")
                            assetName = null;
                    }

                    return assetName;
                }
            }

            return null;
        }
    }

    private static string? GetMonoBehaviourNameFast(AssetsManager manager, AssetsFileInstance fileInst, AssetFileInfo info)
    {
        try
        {
            if (info.GetTypeId(fileInst.file) != (int)AssetClassID.MonoBehaviour && info.TypeId >= 0)
                return string.Empty;

            // attempt to skip monobehaviour fields. if that fails, we need to trim the rest off manually.
            var monoTemp = manager.GetTemplateBaseField(fileInst, info, AssetReadFlags.SkipMonoBehaviourFields).Clone();
            // trim off extra (needs a speedup. findindex isn't going to be the fastest.)
            var nameIndex = monoTemp.Children.FindIndex(monoTemp => monoTemp.Name == "m_Script");
            if (nameIndex != -1)
            {
                monoTemp.Children.RemoveRange(nameIndex + 1, monoTemp.Children.Count - (nameIndex + 1));
            }

            AssetTypeValueField? monoBf;
            lock (fileInst.LockReader)
            {
                monoBf = monoTemp.MakeValue(fileInst.file.Reader, info.GetAbsoluteByteOffset(fileInst.file));
            }

            if (monoBf == null)
                return null;

            var scriptBaseField = manager.GetExtAsset(fileInst, monoBf["m_Script"]).baseField;
            if (scriptBaseField == null)
                return null;

            return scriptBaseField["m_ClassName"].AsString;
        }
        catch
        {
            return null;
        }
    }

    public bool NameAssetPPtrFile(NamedAssetPPtr pptr)
    {
        var depInst = GetDepInstance(pptr);
        if (depInst is not null)
        {
            pptr.FilePath = depInst.path;
            if (depInst.parentBundle is not null)
            {
                pptr.BundleName = depInst.parentBundle.name;
            }
        }

        return !string.IsNullOrEmpty(pptr.FilePath);
    }

    private AssetsFileInstance? GetDepInstance(AssetPPtr pptr)
    {
        if (pptr.FileId == 0)
        {
            return inst;
        }

        int depIndex = pptr.FileId - 1;
        AssetsFileInstance depInst = inst.GetDependency(manager, depIndex);
        if (depInst != null)
        {
            return depInst;
        }

        return null;
    }
}
