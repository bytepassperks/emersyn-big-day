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

    UFUNCTION(BlueprintCallable, Category = "Scene")
    void LoadRoom(const FString& RoomName);

    UFUNCTION(BlueprintCallable, Category = "Scene")
    void BuildCurrentRoom();

private:
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

    // v12: Sky background helper
    void SpawnSkyBackground(FLinearColor SkyColor);

    // v9: ProceduralMeshComponent with vertex colors (bypasses material system)
    AActor* SpawnBox(FVector Location, FVector Scale, FLinearColor Color);
    AActor* SpawnSphere(FVector Location, float Radius, FLinearColor Color);
    AActor* SpawnCylinder(FVector Location, FVector Scale, FLinearColor Color);
    void SpawnLight(FVector Location, float Intensity, FLinearColor Color);
    void SpawnDirectionalLight(FRotator Rotation, float Intensity, FLinearColor Color);
    void SpawnCharacter(FVector Location, FLinearColor SkinColor, FLinearColor HairColor, FLinearColor OutfitColor, const FString& Name, float Scale = 1.0f);
    void SpawnPet(FVector Location, FLinearColor BodyColor, FLinearColor AccentColor, const FString& Name, float Scale = 0.6f);
    
    // v9: Procedural mesh generation helpers
    void GenerateBoxMesh(UProceduralMeshComponent* PMC, FVector HalfExtent, FLinearColor Color);
    void GenerateSphereMesh(UProceduralMeshComponent* PMC, float Radius, int32 Segments, FLinearColor Color);
    void GenerateCylinderMesh(UProceduralMeshComponent* PMC, float Radius, float HalfHeight, int32 Segments, FLinearColor Color);
    
    void ClearRoom();
    void SetupCamera(FVector Location, FRotator Rotation);

    UPROPERTY()
    FString CurrentRoom;

    UPROPERTY()
    TArray<AActor*> SpawnedActors;

    // v10: Vertex color material for ProceduralMeshComponent
    UPROPERTY()
    UMaterialInterface* VertexColorMaterial;

    TArray<FString> RoomNames;
    TArray<FString> RoomDisplayNames;
    int32 RoomIndex;
    float Timer;
    float SplashDuration;
    float RoomDuration;
    bool bInSplash;
};
