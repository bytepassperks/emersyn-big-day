using UnrealBuildTool;

public class EmersynUE5 : ModuleRules
{
    public EmersynUE5(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
        PublicDependencyModuleNames.AddRange(new string[] {
            "Core",
            "CoreUObject",
            "Engine",
            "InputCore"
        });
        PrivateDependencyModuleNames.AddRange(new string[] { });
    }
}
