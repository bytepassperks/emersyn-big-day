using UnrealBuildTool;
using System.Collections.Generic;

public class EmersynUE5EditorTarget : TargetRules
{
    public EmersynUE5EditorTarget(TargetInfo Target) : base(Target)
    {
        Type = TargetType.Editor;
        DefaultBuildSettings = BuildSettingsVersion.V4;
        IncludeOrderVersion = EngineIncludeOrderVersion.Unreal5_4;
        ExtraModuleNames.AddRange(new string[] { "EmersynUE5" });
    }
}
