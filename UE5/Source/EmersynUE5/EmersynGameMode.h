#pragma once
#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "ProceduralMeshComponent.h"
#include "EmersynGameMode.generated.h"

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
                      const FLinearColor& Tint, float Brightness = 1.0f);

    // Environment helpers
    void SpawnSky(FLinearColor TopCol, FLinearColor BotCol);
    void SpawnFloor(FVector Size, FLinearColor Col1, FLinearColor Col2, bool bCheckerboard);
    void SpawnWalls(FVector RoomSize, float WallHeight, FLinearColor Col);
    void SpawnRoomLabel(const FString& Label);

    // Primitive spawners (fallback)
    AActor* SpawnBox(FVector Loc, FVector Scale, FLinearColor Col);
    AActor* SpawnGradientBox(FVector Loc, FVector Scale, FLinearColor Top, FLinearColor Bot);

    // Character spawner using real mesh data
    AActor* SpawnCharacterMesh(const FString& Name, FVector Location, FRotator Rotation,
                               float Scale, const FLinearColor& SkinTint,
                               const FLinearColor& OutfitTint);

    // Lighting
    void SpawnLight(FVector Loc, float Intensity, FLinearColor Color);
    void SpawnDirectionalLight(FRotator Rot, float Intensity, FLinearColor Color);

    // Mesh generators for primitives
    void GenerateBoxMesh(UProceduralMeshComponent* PMC, FVector HE, FLinearColor Col);
    void GenerateGradientBoxMesh(UProceduralMeshComponent* PMC, FVector HE, FLinearColor Top, FLinearColor Bot);

    // Camera
    void SetupIsometricCamera(FVector RoomCenter, float Distance);

    // Room management
    void ClearRoom();

    UPROPERTY() FString CurrentRoom;
    UPROPERTY() TArray<AActor*> SpawnedActors;
    UPROPERTY() UMaterialInterface* VertexColorMaterial;

    TArray<FString> RoomNames;
    TArray<FString> RoomDisplayNames;
    int32 RoomIndex;
    float Timer;
    float SplashDuration;
    float RoomDuration;
    bool bInSplash;
};
