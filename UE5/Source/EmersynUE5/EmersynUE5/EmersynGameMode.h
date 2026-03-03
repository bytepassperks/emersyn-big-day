#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "EmersynGameMode.generated.h"

UCLASS()
class EMERSYNUE5_API AEmersynGameMode : public AGameModeBase
{
    GENERATED_BODY()

public:
    AEmersynGameMode();

    virtual void BeginPlay() override;
    virtual void InitGame(const FString& MapName, const FString& Options, FString& ErrorMessage) override;

    UFUNCTION(BlueprintCallable, Category = "Scene")
    void LoadRoom(const FString& RoomName);

    UFUNCTION(BlueprintCallable, Category = "Scene")
    void BuildCurrentRoom();

private:
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
    void BuildMainMenu();

    void SpawnFloor(FVector Location, FVector Scale, FLinearColor Color);
    void SpawnWall(FVector Location, FRotator Rotation, FVector Scale, FLinearColor Color);
    void SpawnFurniture(FVector Location, FVector Scale, FLinearColor Color, const FString& Label);
    void SpawnLight(FVector Location, float Intensity, FLinearColor Color);

    UPROPERTY()
    FString CurrentRoom;

    UPROPERTY()
    TArray<AActor*> SpawnedActors;
};
