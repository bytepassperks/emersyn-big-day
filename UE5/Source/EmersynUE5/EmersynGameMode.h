#pragma once
#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "ProceduralMeshComponent.h"
#include "Components/PointLightComponent.h"
#include "Components/DirectionalLightComponent.h"
#include "Components/SkyLightComponent.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "Engine/PostProcessVolume.h"
#include "Engine/SkyLight.h"
#include "Components/TextRenderComponent.h"
#include "Camera/CameraActor.h"
#include "Engine/Texture2D.h"
#include "EmersynGameMode.generated.h"

UENUM()
enum class ETexturePattern : uint8
{
    WoodGrain, TileGrid, Wallpaper, Carpet, Grass, Concrete,
    Brick, Marble, Metal, Fabric, Sand, Water
};

UCLASS()
class EMERSYNUE5_API AEmersynGameMode : public AGameModeBase
{
    GENERATED_BODY()
public:
    AEmersynGameMode();
    virtual void InitGame(const FString& MapName, const FString& Options, FString& ErrorMessage) override;
    virtual void BeginPlay() override;
    virtual void Tick(float DeltaTime) override;

    // Room management - auto cycling
    void LoadRoom(const FString& RoomName);
    void ClearRoom();
    FString CurrentRoom;
    TArray<AActor*> RoomActors;
    TArray<FString> RoomList;
    int32 RoomIndex;
    float RoomTimer;
    float RoomDuration;

    // Material
    UPROPERTY() UMaterial* M_VertexColor;
    UPROPERTY() UMaterialInstanceDynamic* DefaultMID;

    // Camera
    UPROPERTY() ACameraActor* IsoCam;
    FVector CamStartPos, CamTargetPos;
    FRotator CamStartRot, CamTargetRot;
    float CamMoveAlpha;
    bool bCameraMoving;

    // Texture cache
    TMap<FString, UTexture2D*> TextureCache;

    // Noise
    float SimpleNoise(float X, float Y) const;
    float FBMNoise(float X, float Y, int32 Octaves) const;

    // Procedural texture fill functions
    void FillWoodGrain(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Accent);
    void FillTileGrid(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Grout);
    void FillWallpaper(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Pattern);
    void FillCarpet(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Fiber);
    void FillGrass(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Tip);
    void FillConcrete(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Speckle);
    void FillBrick(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Mortar);
    void FillMarble(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Vein);
    void FillMetal(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Highlight);
    void FillFabric(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Thread);
    void FillSand(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Grain);
    void FillWater(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Highlight);

    UTexture2D* GenerateProceduralTexture(ETexturePattern Pattern, FLinearColor BaseColor, FLinearColor AccentColor, int32 Size);
    UMaterialInstanceDynamic* CreateTexturedMaterial(UTexture2D* Texture, float Roughness = 0.5f, float Metallic = 0.f);

    // Normal-based shading helper (v21 NEW)
    FLinearColor ApplyDirectionalShading(FLinearColor BaseColor, FVector Normal, float AO = 1.0f) const;

    // Spawn geometry
    AActor* SpawnTexturedFloor(FVector Center, FVector Size, ETexturePattern Pattern, FLinearColor Base, FLinearColor Accent, float UVScale = 1.f);
    AActor* SpawnTexturedWall(FVector Start, FVector End, float Height, ETexturePattern Pattern, FLinearColor Base, FLinearColor Accent);
    AActor* SpawnTexturedCeiling(FVector Center, FVector Size, FLinearColor Color);
    AActor* SpawnTexturedBox(FVector Loc, FVector Scale, ETexturePattern Pattern, FLinearColor Base, FLinearColor Accent);

    // Sky
    void SpawnSky();

    // Mesh spawning
    AActor* SpawnMesh(const float* Verts, const float* Norms, const float* UVData,
        const int32* Tris, int32 NumVerts, int32 NumTris,
        FVector Location, FRotator Rotation, FVector Scale,
        ETexturePattern Pattern, FLinearColor Base, FLinearColor Accent, float Brightness = 1.0f);
    AActor* SpawnMeshVC(const float* Verts, const float* Norms, const float* UVData,
        const int32* Tris, int32 NumVerts, int32 NumTris,
        FVector Location, FRotator Rotation, FVector Scale,
        const FLinearColor& Tint, float Brightness = 1.0f);
    AActor* SpawnCharacterMesh(const FString& Name, FVector Location, FRotator Rotation,
        float InScale, const FLinearColor& SkinTint, const FLinearColor& OutfitTint);

    // Lighting
    void SpawnLight(FVector Loc, float Intensity, FLinearColor Color, float Radius);
    void SpawnDirectionalLight(FRotator Rot, float Intensity, FLinearColor Color);
    void SpawnSkyLight(float Intensity);
    void SpawnRoomLighting(FVector RoomCenter, FVector RoomSize);

    // Post-processing
    void SetupPostProcessing();

    // Text and camera
    void SpawnWorldText(const FString& Text, FVector Location, float Size, FLinearColor Color);
    void SpawnRoomLabel(const FString& Label);
    void SetupIsometricCamera(FVector RoomCenter, float Distance);

    // Room builders
    void BuildSplashScreen();
    void BuildMainMenu();
    void BuildBedroom();
    void BuildKitchen();
    void BuildBathroom();
    void BuildLivingRoom();
    void BuildGarden();
    void BuildSchool();
    void BuildShop();
    void BuildPlayground();
    void BuildPark();
    void BuildMall();
    void BuildArcade();
    void BuildAmusementPark();
};
