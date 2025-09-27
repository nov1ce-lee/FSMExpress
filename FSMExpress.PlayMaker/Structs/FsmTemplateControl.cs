using FSMExpress.Common.Assets;
using FSMExpress.Common.Document;
using FSMExpress.Common.Interfaces;

namespace FSMExpress.PlayMaker.Structs;
public class FsmTemplateControl : IFsmPlaymakerValuePreviewer
{
    public NamedAssetPPtr Target { get; set; }
    public List<FsmVarOverride> InputVariables { get; set; }
    public List<FsmVarOverride> OutputVariables { get; set; }

    public FsmDocumentNodeDataFieldKind FieldKind => FsmDocumentNodeDataFieldKind.Object;

    public FsmTemplateControl()
    {
        Target = new NamedAssetPPtr();
        InputVariables = [];
        OutputVariables = [];
    }

    public FsmTemplateControl(IAssetField field)
    {
        if (field.Exists("target"))
            Target = field.GetValue<NamedAssetPPtr>("target");
        else
            Target = new NamedAssetPPtr();

        if (field.Exists("inputVariables"))
            InputVariables = field.GetValueArray("inputVariables", x => new FsmVarOverride(x));
        else
            InputVariables = [];

        if (field.Exists("outputVariables"))
            OutputVariables = field.GetValueArray("outputVariables", x => new FsmVarOverride(x));
        else
            OutputVariables = [];
    }

    public override string ToString()
    {
        return Target.ToString();
    }
}
