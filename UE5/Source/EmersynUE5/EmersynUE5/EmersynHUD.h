#pragma once

#include "CoreMinimal.h"
#include "GameFramework/HUD.h"
#include "EmersynHUD.generated.h"

UCLASS()
class EMERSYNUE5_API AEmersynHUD : public AHUD
{
    GENERATED_BODY()

public:
    AEmersynHUD();
    virtual void DrawHUD() override;

    FString RoomDisplayName;
    bool bShowSplash;
    float SplashAlpha;
};
