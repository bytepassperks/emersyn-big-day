#pragma once
#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "ProceduralMeshComponent.h"
#include "Components/TextRenderComponent.h"
#include "Engine/Texture2D.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "EmersynGameMode.generated.h"

// Texture pattern types for procedural generation
enum class ETexturePattern : uint8
{
    WoodGrain,
    TileGrid,
    Wallpaper,
    Carpet,
    Grass,
    Concrete,
    Brick,
    Marble,
    Metal,
    Fabric,
    Sand,
    Water
};

UCLASS()
class EMERSYNUE5_API AEmersynGameMode : public AGameModeBase
{
    GENERATED_BODY()
public:
    AEmersynGameMode();
    virtual void BeginPlay() override;
    virtual void InitGame(const FString& MapName, const FString& Options, FString& ErrorMessage) override;
    virtual void Tick(float DeltaSeconds) override;

    UFUNCTION(BlueprintCallable)
    void LoadRoom(const FString& RoomName);

private:
    // === TEXTURE SYSTEM ===
    UTexture2D* GenerateProceduralTexture(ETexturePattern Pattern, FLinearColor BaseColor, FLinearColor AccentColor, int32 Size = 256);
    void FillWoodGrain(TArray<FColor>& Pixels, int32 W, int32 H, FLinearColor Base, FLinearColor Accent);
    void FillTileGrid(TArray<FColor>& Pixels, int32 W, int32 H, FLinearColor Base, FLinearColor Grout);
    void FillWallpaper(TArray<FColor>& Pixels, int32 W, int32 H, FLinearColor Base, FLinearColor Pattern);
    void FillCarpet(TArray<FColor>& Pixels, int32 W, int32 H, FLinearColor Base, FLinearColor Fiber);
    void FillGrass(TArray<FColor>& Pixels, int32 W, int32 H, FLinearColor Base, FLinearColor Tip);
    void FillConcrete(TArray<FColor>& Pixels, int32 W, int32 H, FLinearColor Base, FLinearColor Speckle);
    void FillBrick(TArray<FColor>& Pixels, int32 W, int32 H, FLinearColor Base, FLinearColor Mortar);
    void FillMarble(TArray<FColor>& Pixels, int32 W, int32 H, FLinearColor Base, FLinearColor Vein);
    void FillMetal(TArray<FColor>& Pixels, int32 W, int32 H, FLinearColor Base, FLinearColor Highlight);
    void FillFabric(TArray<FColor>& Pixels, int32 W, int32 H, FLinearColor Base, FLinearColor Thread);
    void FillSand(TArray<FColor>& Pixels, int32 W, int32 H, FLinearColor Base, FLinearColor Grain);
    void FillWater(TArray<FColor>& Pixels, int32 W, int32 H, FLinearColor Base, FLinearColor Highlight);

    // Noise helpers
    float SimpleNoise(float X, float Y) const;
    float FBMNoise(float X, float Y, int32 Octaves) const;

    // === MATERIAL SYSTEM ===
    UMaterialInstanceDynamic* CreateTexturedMaterial(UTexture2D* Texture, float Roughness = 0.7f, float Metallic = 0.0f);

    // === TEXTURED MESH SPAWNING ===
    AActor* SpawnTexturedFloor(FVector Center, FVector Size, ETexturePattern Pattern, FLinearColor Base, FLinearColor Accent, float UVScale = 1.0f);
    AActor* SpawnTexturedWall(FVector Start, FVector End, float Height, ETexturePattern Pattern, FLinearColor Base, FLinearColor Accent);
    AActor* SpawnTexturedCeiling(FVector Center, FVector Size, FLinearColor Color);
    AActor* SpawnTexturedBox(FVector Loc, FVector Scale, ETexturePattern Pattern, FLinearColor Base, FLinearColor Accent);

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

    // Spawn real 3D mesh from compiled data
    AActor* SpawnMesh(const float* Verts, const float* Norms, const float* UVData,
                      const int32* Tris, int32 NumVerts, int32 NumTris,
                      FVector Location, FRotator Rotation, FVector Scale,
                      ETexturePattern Pattern, FLinearColor Base, FLinearColor Accent, float Brightness = 1.0f);

    // Fallback vertex-color spawn
    AActor* SpawnMeshVC(const float* Verts, const float* Norms, const float* UVData,
                        const int32* Tris, int32 NumVerts, int32 NumTris,
                        FVector Location, FRotator Rotation, FVector Scale,
                        const FLinearColor& Tint, float Brightness = 1.0f);

    // Environment helpers
    void SpawnSky();
    void SpawnRoomLabel(const FString& Label);

    // Character spawner
    AActor* SpawnCharacterMesh(const FString& Name, FVector Location, FRotator Rotation,
                               float Scale, const FLinearColor& SkinTint,
                               const FLinearColor& OutfitTint);

    // Lighting
    void SpawnLight(FVector Loc, float Intensity, FLinearColor Color, float Radius = 500.f);
    void SpawnDirectionalLight(FRotator Rot, float Intensity, FLinearColor Color);
    void SpawnSkyLight(float Intensity);
    void SpawnRoomLighting(FVector RoomCenter, FVector RoomSize);

    // Post-processing
    void SetupPostProcessing();

    // HUD / UI
    void SpawnWorldText(const FString& Text, FVector Location, float Size, FLinearColor Color);

    // Camera
    void SetupIsometricCamera(FVector RoomCenter, float Distance);

    // Room management
    void ClearRoom();

    UPROPERTY() FString CurrentRoom;
    UPROPERTY() TArray<AActor*> RoomActors;
    UPROPERTY() ACameraActor* IsoCam;

    // Camera interpolation
    FVector CamTargetPos;
    FRotator CamTargetRot;
    bool bCameraMoving;
    float CamMoveAlpha;
    FVector CamStartPos;
    FRotator CamStartRot;

    // Texture cache to avoid regenerating
    UPROPERTY() TMap<FString, UTexture2D*> TextureCache;

    // Base material reference
    UPROPERTY() UMaterial* M_VertexColor;
    UPROPERTY() UMaterialInstanceDynamic* DefaultMID;
};
