using UnrealBuildTool;
using System.Collections.Generic;

public class EmersynUE5Target : TargetRules
{
    public EmersynUE5Target(TargetInfo Target) : base(Target)
    {
        Type = TargetType.Game;
        DefaultBuildSettings = BuildSettingsVersion.V4;
        IncludeOrderVersion = EngineIncludeOrderVersion.Unreal5_4;
        ExtraModuleNames.AddRange(new string[] { "EmersynUE5" });
    }
}
