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

    // Environment
    void SpawnSkyBackground(FLinearColor TopColor, FLinearColor BottomColor);
    void SpawnRoomShell(FVector Size, FLinearColor FloorCol, FLinearColor WallCol, FLinearColor CeilCol);

    // Primitive spawners (PMC + vertex colors)
    AActor* SpawnBox(FVector Loc, FVector Scale, FLinearColor Color);
    AActor* SpawnGradientBox(FVector Loc, FVector Scale, FLinearColor TopCol, FLinearColor BotCol);
    AActor* SpawnSphere(FVector Loc, float Radius, FLinearColor Color);
    AActor* SpawnCylinder(FVector Loc, FVector Scale, FLinearColor Color);

    // Detailed furniture
    void SpawnBed(FVector L, FLinearColor Frame, FLinearColor Sheet, FLinearColor Pillow);
    void SpawnSofa(FVector L, FLinearColor Fabric, FLinearColor Cushion);
    void SpawnTable(FVector L, FLinearColor Top, FLinearColor Leg);
    void SpawnChair(FVector L, FLinearColor Col);
    void SpawnBookshelf(FVector L, FLinearColor Wood);
    void SpawnTV(FVector L);
    void SpawnStove(FVector L);
    void SpawnFridge(FVector L);
    void SpawnSink(FVector L);
    void SpawnBathtub(FVector L);
    void SpawnToilet(FVector L);
    void SpawnLamp(FVector L, FLinearColor Shade);
    void SpawnRug(FVector L, FLinearColor C1, FLinearColor C2, FVector Size);
    void SpawnPlant(FVector L, float S = 1.0f);
    void SpawnTree(FVector L, float S = 1.0f);
    void SpawnSwing(FVector L);
    void SpawnSlide(FVector L);
    void SpawnBench(FVector L);
    void SpawnShopShelf(FVector L, FLinearColor Col);
    void SpawnArcadeMachine(FVector L, FLinearColor Col);
    void SpawnDesk(FVector L, FLinearColor Col);
    void SpawnWindow(FVector L, FLinearColor Frame, FLinearColor Glass);
    void SpawnPainting(FVector L, FLinearColor Frame, FLinearColor Canvas);
    void SpawnFountain(FVector L);
    void SpawnFerrisWheel(FVector L);
    void SpawnCarousel(FVector L);
    void SpawnCounter(FVector L, FLinearColor Col);
    void SpawnCabinet(FVector L, FLinearColor Col);

    // Characters & pets
    void SpawnCharacter(FVector Loc, FLinearColor Skin, FLinearColor Hair, FLinearColor Outfit, const FString& Name, float Scale = 1.0f);
    void SpawnPet(FVector Loc, FLinearColor Body, FLinearColor Accent, const FString& Name, float Scale = 0.6f);

    // Lighting actors
    void SpawnLight(FVector Loc, float Intensity, FLinearColor Color);
    void SpawnDirectionalLight(FRotator Rot, float Intensity, FLinearColor Color);

    // Mesh generators
    void GenerateBoxMesh(UProceduralMeshComponent* PMC, FVector HE, FLinearColor Col);
    void GenerateGradientBoxMesh(UProceduralMeshComponent* PMC, FVector HE, FLinearColor Top, FLinearColor Bot);
    void GenerateSphereMesh(UProceduralMeshComponent* PMC, float Radius, int32 Seg, FLinearColor Col);
    void GenerateCylinderMesh(UProceduralMeshComponent* PMC, float Radius, float HH, int32 Seg, FLinearColor Col);

    // Utilities
    void ClearRoom();
    void SetupCamera(FVector Loc, FRotator Rot);

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
