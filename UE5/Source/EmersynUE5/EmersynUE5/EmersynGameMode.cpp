#include "EmersynGameMode.h"
#include "Engine/World.h"
#include "Engine/StaticMeshActor.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/PointLight.h"
#include "Components/PointLightComponent.h"
#include "Engine/DirectionalLight.h"
#include "Components/DirectionalLightComponent.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "UObject/ConstructorHelpers.h"
#include "Kismet/GameplayStatics.h"
#include "Camera/CameraActor.h"
#include "Camera/CameraComponent.h"
#include "GameFramework/PlayerController.h"

AEmersynGameMode::AEmersynGameMode()
{
    CurrentRoom = TEXT("MainMenu");
}

void AEmersynGameMode::InitGame(const FString& MapName, const FString& Options, FString& ErrorMessage)
{
    Super::InitGame(MapName, Options, ErrorMessage);
    UE_LOG(LogTemp, Log, TEXT("EmersynGameMode: InitGame - Map: %s"), *MapName);
}

void AEmersynGameMode::BeginPlay()
{
    Super::BeginPlay();
    UE_LOG(LogTemp, Log, TEXT("EmersynGameMode: BeginPlay - Building room: %s"), *CurrentRoom);
    
    UWorld* World = GetWorld();
    if (World)
    {
        ACameraActor* Camera = World->SpawnActor<ACameraActor>(ACameraActor::StaticClass(), FVector(0, -600, 400), FRotator(-30, 0, 0));
        if (Camera)
        {
            APlayerController* PC = World->GetFirstPlayerController();
            if (PC)
            {
                PC->SetViewTarget(Camera);
            }
        }
    }
    
    BuildCurrentRoom();
}

void AEmersynGameMode::LoadRoom(const FString& RoomName)
{
    for (AActor* Actor : SpawnedActors)
    {
        if (Actor && IsValid(Actor))
        {
            Actor->Destroy();
        }
    }
    SpawnedActors.Empty();
    
    CurrentRoom = RoomName;
    BuildCurrentRoom();
}

void AEmersynGameMode::BuildCurrentRoom()
{
    UE_LOG(LogTemp, Log, TEXT("Building room: %s"), *CurrentRoom);
    
    if (CurrentRoom == TEXT("MainMenu")) BuildMainMenu();
    else if (CurrentRoom == TEXT("Bedroom")) BuildBedroom();
    else if (CurrentRoom == TEXT("Kitchen")) BuildKitchen();
    else if (CurrentRoom == TEXT("Bathroom")) BuildBathroom();
    else if (CurrentRoom == TEXT("LivingRoom")) BuildLivingRoom();
    else if (CurrentRoom == TEXT("Garden")) BuildGarden();
    else if (CurrentRoom == TEXT("School")) BuildSchool();
    else if (CurrentRoom == TEXT("Shop")) BuildShop();
    else if (CurrentRoom == TEXT("Playground")) BuildPlayground();
    else if (CurrentRoom == TEXT("Park")) BuildPark();
    else if (CurrentRoom == TEXT("Mall")) BuildMall();
    else if (CurrentRoom == TEXT("Arcade")) BuildArcade();
    else if (CurrentRoom == TEXT("AmusementPark")) BuildAmusementPark();
    else BuildMainMenu();
}

void AEmersynGameMode::SpawnFloor(FVector Location, FVector Scale, FLinearColor Color)
{
    UWorld* World = GetWorld();
    if (!World) return;
    
    AStaticMeshActor* Floor = World->SpawnActor<AStaticMeshActor>(AStaticMeshActor::StaticClass(), Location, FRotator::ZeroRotator);
    if (!Floor) return;
    
    UStaticMeshComponent* MeshComp = Floor->GetStaticMeshComponent();
    UStaticMesh* PlaneMesh = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Plane.Plane"));
    if (MeshComp && PlaneMesh)
    {
        MeshComp->SetStaticMesh(PlaneMesh);
        Floor->SetActorScale3D(Scale);
        
        UMaterialInstanceDynamic* Mat = UMaterialInstanceDynamic::Create(
            LoadObject<UMaterial>(nullptr, TEXT("/Engine/BasicShapes/BasicShapeMaterial.BasicShapeMaterial")), 
            Floor);
        if (Mat)
        {
            Mat->SetVectorParameterValue(TEXT("Color"), Color);
            MeshComp->SetMaterial(0, Mat);
        }
    }
    SpawnedActors.Add(Floor);
}

void AEmersynGameMode::SpawnWall(FVector Location, FRotator Rotation, FVector Scale, FLinearColor Color)
{
    UWorld* World = GetWorld();
    if (!World) return;
    
    AStaticMeshActor* Wall = World->SpawnActor<AStaticMeshActor>(AStaticMeshActor::StaticClass(), Location, Rotation);
    if (!Wall) return;
    
    UStaticMeshComponent* MeshComp = Wall->GetStaticMeshComponent();
    UStaticMesh* CubeMesh = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cube.Cube"));
    if (MeshComp && CubeMesh)
    {
        MeshComp->SetStaticMesh(CubeMesh);
        Wall->SetActorScale3D(Scale);
        
        UMaterialInstanceDynamic* Mat = UMaterialInstanceDynamic::Create(
            LoadObject<UMaterial>(nullptr, TEXT("/Engine/BasicShapes/BasicShapeMaterial.BasicShapeMaterial")), 
            Wall);
        if (Mat)
        {
            Mat->SetVectorParameterValue(TEXT("Color"), Color);
            MeshComp->SetMaterial(0, Mat);
        }
    }
    SpawnedActors.Add(Wall);
}

void AEmersynGameMode::SpawnFurniture(FVector Location, FVector Scale, FLinearColor Color, const FString& Label)
{
    UWorld* World = GetWorld();
    if (!World) return;
    
    AStaticMeshActor* Furniture = World->SpawnActor<AStaticMeshActor>(AStaticMeshActor::StaticClass(), Location, FRotator::ZeroRotator);
    if (!Furniture) return;
    
    UStaticMeshComponent* MeshComp = Furniture->GetStaticMeshComponent();
    UStaticMesh* CubeMesh = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cube.Cube"));
    if (MeshComp && CubeMesh)
    {
        MeshComp->SetStaticMesh(CubeMesh);
        Furniture->SetActorScale3D(Scale);
        
        UMaterialInstanceDynamic* Mat = UMaterialInstanceDynamic::Create(
            LoadObject<UMaterial>(nullptr, TEXT("/Engine/BasicShapes/BasicShapeMaterial.BasicShapeMaterial")), 
            Furniture);
        if (Mat)
        {
            Mat->SetVectorParameterValue(TEXT("Color"), Color);
            MeshComp->SetMaterial(0, Mat);
        }
    }
    SpawnedActors.Add(Furniture);
}

void AEmersynGameMode::SpawnLight(FVector Location, float Intensity, FLinearColor Color)
{
    UWorld* World = GetWorld();
    if (!World) return;
    
    APointLight* Light = World->SpawnActor<APointLight>(APointLight::StaticClass(), Location, FRotator::ZeroRotator);
    if (Light)
    {
        UPointLightComponent* LightComp = Light->PointLightComponent;
        if (LightComp)
        {
            LightComp->SetIntensity(Intensity);
            LightComp->SetLightColor(Color);
            LightComp->SetAttenuationRadius(2000.0f);
        }
    }
    SpawnedActors.Add(Light);
}

// === ROOM BUILDERS ===

void AEmersynGameMode::BuildMainMenu()
{
    SpawnFloor(FVector(0, 0, 0), FVector(10, 10, 1), FLinearColor(0.9f, 0.85f, 0.8f));
    SpawnLight(FVector(0, 0, 500), 5000.0f, FLinearColor(1.0f, 0.95f, 0.9f));
    SpawnFurniture(FVector(0, 0, 200), FVector(3, 0.5f, 0.3f), FLinearColor(0.95f, 0.45f, 0.62f), TEXT("TitlePlatform"));
    SpawnFurniture(FVector(-300, 0, 50), FVector(0.5f, 0.5f, 1.0f), FLinearColor(0.2f, 0.55f, 0.8f), TEXT("Decor1"));
    SpawnFurniture(FVector(300, 0, 50), FVector(0.5f, 0.5f, 1.0f), FLinearColor(0.95f, 0.75f, 0.1f), TEXT("Decor2"));
}

void AEmersynGameMode::BuildBedroom()
{
    SpawnFloor(FVector(0, 0, 0), FVector(8, 8, 1), FLinearColor(0.72f, 0.52f, 0.32f));
    SpawnWall(FVector(0, -400, 150), FRotator::ZeroRotator, FVector(8, 0.1f, 3), FLinearColor(0.95f, 0.9f, 0.85f));
    SpawnWall(FVector(0, 400, 150), FRotator::ZeroRotator, FVector(8, 0.1f, 3), FLinearColor(0.95f, 0.9f, 0.85f));
    SpawnWall(FVector(-400, 0, 150), FRotator::ZeroRotator, FVector(0.1f, 8, 3), FLinearColor(0.9f, 0.85f, 0.95f));
    SpawnWall(FVector(400, 0, 150), FRotator::ZeroRotator, FVector(0.1f, 8, 3), FLinearColor(0.9f, 0.85f, 0.95f));
    SpawnFurniture(FVector(-200, -200, 30), FVector(2.0f, 1.2f, 0.6f), FLinearColor(0.95f, 0.7f, 0.8f), TEXT("Bed"));
    SpawnFurniture(FVector(-200, -280, 55), FVector(0.5f, 0.4f, 0.2f), FLinearColor(1.0f, 1.0f, 1.0f), TEXT("Pillow"));
    SpawnFurniture(FVector(200, -300, 50), FVector(1.0f, 0.5f, 1.0f), FLinearColor(0.6f, 0.4f, 0.25f), TEXT("Dresser"));
    SpawnFurniture(FVector(200, 200, 30), FVector(0.8f, 0.6f, 0.6f), FLinearColor(0.2f, 0.6f, 0.9f), TEXT("ToyBox"));
    SpawnFurniture(FVector(0, 0, 1), FVector(2.0f, 2.0f, 0.02f), FLinearColor(0.9f, 0.5f, 0.6f), TEXT("Rug"));
    SpawnFurniture(FVector(-100, -200, 30), FVector(0.4f, 0.4f, 0.6f), FLinearColor(0.5f, 0.35f, 0.2f), TEXT("Nightstand"));
    SpawnFurniture(FVector(-395, 0, 200), FVector(0.05f, 1.5f, 1.2f), FLinearColor(0.7f, 0.85f, 1.0f), TEXT("Window"));
    SpawnLight(FVector(0, 0, 280), 3000.0f, FLinearColor(1.0f, 0.9f, 0.8f));
    SpawnLight(FVector(-100, -200, 100), 1000.0f, FLinearColor(1.0f, 0.95f, 0.85f));
}

void AEmersynGameMode::BuildKitchen()
{
    SpawnFloor(FVector(0, 0, 0), FVector(8, 8, 1), FLinearColor(0.85f, 0.85f, 0.85f));
    SpawnWall(FVector(0, -400, 150), FRotator::ZeroRotator, FVector(8, 0.1f, 3), FLinearColor(0.95f, 0.95f, 0.9f));
    SpawnWall(FVector(0, 400, 150), FRotator::ZeroRotator, FVector(8, 0.1f, 3), FLinearColor(0.95f, 0.95f, 0.9f));
    SpawnWall(FVector(-400, 0, 150), FRotator::ZeroRotator, FVector(0.1f, 8, 3), FLinearColor(0.95f, 0.95f, 0.9f));
    SpawnWall(FVector(400, 0, 150), FRotator::ZeroRotator, FVector(0.1f, 8, 3), FLinearColor(0.95f, 0.95f, 0.9f));
    SpawnFurniture(FVector(-300, 0, 45), FVector(0.6f, 4.0f, 0.9f), FLinearColor(0.8f, 0.75f, 0.7f), TEXT("Counter"));
    SpawnFurniture(FVector(-300, -150, 45), FVector(0.6f, 0.8f, 0.9f), FLinearColor(0.3f, 0.3f, 0.3f), TEXT("Stove"));
    SpawnFurniture(FVector(300, -300, 80), FVector(0.8f, 0.6f, 1.6f), FLinearColor(0.9f, 0.9f, 0.95f), TEXT("Fridge"));
    SpawnFurniture(FVector(100, 100, 35), FVector(1.5f, 1.0f, 0.05f), FLinearColor(0.65f, 0.45f, 0.3f), TEXT("Table"));
    SpawnFurniture(FVector(40, 100, 20), FVector(0.3f, 0.3f, 0.4f), FLinearColor(0.55f, 0.35f, 0.2f), TEXT("Chair1"));
    SpawnFurniture(FVector(160, 100, 20), FVector(0.3f, 0.3f, 0.4f), FLinearColor(0.55f, 0.35f, 0.2f), TEXT("Chair2"));
    SpawnFurniture(FVector(-300, 200, 45), FVector(0.5f, 0.6f, 0.1f), FLinearColor(0.7f, 0.75f, 0.8f), TEXT("Sink"));
    SpawnLight(FVector(0, 0, 280), 4000.0f, FLinearColor(1.0f, 1.0f, 0.95f));
}

void AEmersynGameMode::BuildBathroom()
{
    SpawnFloor(FVector(0, 0, 0), FVector(6, 6, 1), FLinearColor(0.8f, 0.9f, 0.95f));
    SpawnWall(FVector(0, -300, 150), FRotator::ZeroRotator, FVector(6, 0.1f, 3), FLinearColor(0.85f, 0.92f, 0.95f));
    SpawnWall(FVector(0, 300, 150), FRotator::ZeroRotator, FVector(6, 0.1f, 3), FLinearColor(0.85f, 0.92f, 0.95f));
    SpawnWall(FVector(-300, 0, 150), FRotator::ZeroRotator, FVector(0.1f, 6, 3), FLinearColor(0.85f, 0.92f, 0.95f));
    SpawnWall(FVector(300, 0, 150), FRotator::ZeroRotator, FVector(0.1f, 6, 3), FLinearColor(0.85f, 0.92f, 0.95f));
    SpawnFurniture(FVector(-200, -100, 30), FVector(1.8f, 0.8f, 0.6f), FLinearColor(0.95f, 0.95f, 1.0f), TEXT("Bathtub"));
    SpawnFurniture(FVector(200, -200, 25), FVector(0.4f, 0.5f, 0.5f), FLinearColor(0.95f, 0.95f, 0.95f), TEXT("Toilet"));
    SpawnFurniture(FVector(200, 100, 40), FVector(0.6f, 0.8f, 0.8f), FLinearColor(0.85f, 0.85f, 0.9f), TEXT("Vanity"));
    SpawnFurniture(FVector(295, 100, 150), FVector(0.02f, 0.6f, 0.8f), FLinearColor(0.7f, 0.8f, 0.9f), TEXT("Mirror"));
    SpawnFurniture(FVector(-50, -100, 1), FVector(0.8f, 0.5f, 0.02f), FLinearColor(0.5f, 0.8f, 0.9f), TEXT("BathMat"));
    SpawnLight(FVector(0, 0, 280), 3500.0f, FLinearColor(1.0f, 1.0f, 1.0f));
}

void AEmersynGameMode::BuildLivingRoom()
{
    SpawnFloor(FVector(0, 0, 0), FVector(10, 10, 1), FLinearColor(0.75f, 0.55f, 0.35f));
    SpawnWall(FVector(0, -500, 150), FRotator::ZeroRotator, FVector(10, 0.1f, 3), FLinearColor(0.95f, 0.92f, 0.88f));
    SpawnWall(FVector(0, 500, 150), FRotator::ZeroRotator, FVector(10, 0.1f, 3), FLinearColor(0.95f, 0.92f, 0.88f));
    SpawnWall(FVector(-500, 0, 150), FRotator::ZeroRotator, FVector(0.1f, 10, 3), FLinearColor(0.95f, 0.92f, 0.88f));
    SpawnWall(FVector(500, 0, 150), FRotator::ZeroRotator, FVector(0.1f, 10, 3), FLinearColor(0.95f, 0.92f, 0.88f));
    SpawnFurniture(FVector(0, -350, 30), FVector(2.5f, 0.8f, 0.6f), FLinearColor(0.6f, 0.3f, 0.2f), TEXT("Sofa"));
    SpawnFurniture(FVector(0, 350, 80), FVector(2.0f, 0.05f, 1.2f), FLinearColor(0.1f, 0.1f, 0.12f), TEXT("TV"));
    SpawnFurniture(FVector(0, 350, 25), FVector(2.2f, 0.4f, 0.5f), FLinearColor(0.5f, 0.35f, 0.2f), TEXT("TVStand"));
    SpawnFurniture(FVector(0, -100, 20), FVector(1.2f, 0.6f, 0.05f), FLinearColor(0.6f, 0.4f, 0.25f), TEXT("CoffeeTable"));
    SpawnFurniture(FVector(-400, 0, 80), FVector(0.3f, 1.5f, 1.6f), FLinearColor(0.55f, 0.38f, 0.22f), TEXT("Bookshelf"));
    SpawnFurniture(FVector(0, -100, 1), FVector(3.0f, 2.0f, 0.02f), FLinearColor(0.8f, 0.6f, 0.4f), TEXT("Rug"));
    SpawnLight(FVector(0, 0, 280), 4000.0f, FLinearColor(1.0f, 0.95f, 0.88f));
    SpawnLight(FVector(-300, -200, 150), 1500.0f, FLinearColor(1.0f, 0.9f, 0.8f));
}

void AEmersynGameMode::BuildGarden()
{
    SpawnFloor(FVector(0, 0, 0), FVector(15, 15, 1), FLinearColor(0.3f, 0.7f, 0.2f));
    SpawnFurniture(FVector(0, 0, 1), FVector(1.0f, 8.0f, 0.02f), FLinearColor(0.75f, 0.65f, 0.5f), TEXT("Path"));
    SpawnFurniture(FVector(-300, -200, 15), FVector(1.5f, 1.0f, 0.3f), FLinearColor(0.4f, 0.6f, 0.2f), TEXT("FlowerBed1"));
    SpawnFurniture(FVector(300, -200, 15), FVector(1.5f, 1.0f, 0.3f), FLinearColor(0.4f, 0.6f, 0.2f), TEXT("FlowerBed2"));
    SpawnFurniture(FVector(-300, -200, 40), FVector(0.2f, 0.2f, 0.5f), FLinearColor(0.95f, 0.3f, 0.4f), TEXT("Flower1"));
    SpawnFurniture(FVector(-250, -180, 40), FVector(0.2f, 0.2f, 0.5f), FLinearColor(0.95f, 0.8f, 0.2f), TEXT("Flower2"));
    SpawnFurniture(FVector(300, -200, 40), FVector(0.2f, 0.2f, 0.5f), FLinearColor(0.6f, 0.3f, 0.9f), TEXT("Flower3"));
    SpawnFurniture(FVector(-400, 300, 50), FVector(0.3f, 0.3f, 2.0f), FLinearColor(0.45f, 0.3f, 0.15f), TEXT("TreeTrunk"));
    SpawnFurniture(FVector(-400, 300, 150), FVector(1.5f, 1.5f, 1.0f), FLinearColor(0.2f, 0.6f, 0.15f), TEXT("TreeCanopy"));
    SpawnWall(FVector(0, -750, 40), FRotator::ZeroRotator, FVector(15, 0.05f, 0.8f), FLinearColor(0.9f, 0.9f, 0.85f));
    SpawnFurniture(FVector(100, -100, 10), FVector(0.2f, 0.15f, 0.2f), FLinearColor(0.3f, 0.6f, 0.8f), TEXT("WateringCan"));
    SpawnLight(FVector(0, 0, 800), 8000.0f, FLinearColor(1.0f, 0.98f, 0.9f));
}

void AEmersynGameMode::BuildSchool()
{
    SpawnFloor(FVector(0, 0, 0), FVector(10, 10, 1), FLinearColor(0.8f, 0.8f, 0.75f));
    SpawnWall(FVector(0, -500, 150), FRotator::ZeroRotator, FVector(10, 0.1f, 3), FLinearColor(0.95f, 0.95f, 0.9f));
    SpawnWall(FVector(0, 500, 150), FRotator::ZeroRotator, FVector(10, 0.1f, 3), FLinearColor(0.95f, 0.95f, 0.9f));
    SpawnWall(FVector(-500, 0, 150), FRotator::ZeroRotator, FVector(0.1f, 10, 3), FLinearColor(0.9f, 0.95f, 0.9f));
    SpawnWall(FVector(500, 0, 150), FRotator::ZeroRotator, FVector(0.1f, 10, 3), FLinearColor(0.9f, 0.95f, 0.9f));
    SpawnFurniture(FVector(0, 490, 150), FVector(3.0f, 0.05f, 1.5f), FLinearColor(0.15f, 0.3f, 0.15f), TEXT("Blackboard"));
    SpawnFurniture(FVector(0, 300, 35), FVector(1.5f, 0.6f, 0.7f), FLinearColor(0.6f, 0.4f, 0.25f), TEXT("TeacherDesk"));
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 2; j++)
        {
            float x = -200.0f + i * 200.0f;
            float y = -200.0f + j * 200.0f;
            SpawnFurniture(FVector(x, y, 25), FVector(0.6f, 0.4f, 0.5f), FLinearColor(0.65f, 0.5f, 0.35f), TEXT("StudentDesk"));
        }
    }
    SpawnLight(FVector(0, 0, 280), 5000.0f, FLinearColor(1.0f, 1.0f, 1.0f));
}

void AEmersynGameMode::BuildShop()
{
    SpawnFloor(FVector(0, 0, 0), FVector(10, 10, 1), FLinearColor(0.85f, 0.82f, 0.78f));
    SpawnWall(FVector(0, -500, 150), FRotator::ZeroRotator, FVector(10, 0.1f, 3), FLinearColor(0.95f, 0.88f, 0.75f));
    SpawnWall(FVector(0, 500, 150), FRotator::ZeroRotator, FVector(10, 0.1f, 3), FLinearColor(0.95f, 0.88f, 0.75f));
    SpawnWall(FVector(-500, 0, 150), FRotator::ZeroRotator, FVector(0.1f, 10, 3), FLinearColor(0.95f, 0.88f, 0.75f));
    SpawnWall(FVector(500, 0, 150), FRotator::ZeroRotator, FVector(0.1f, 10, 3), FLinearColor(0.95f, 0.88f, 0.75f));
    SpawnFurniture(FVector(0, 350, 45), FVector(3.0f, 0.5f, 0.9f), FLinearColor(0.65f, 0.45f, 0.3f), TEXT("ShopCounter"));
    SpawnFurniture(FVector(-400, 0, 80), FVector(0.3f, 3.0f, 1.6f), FLinearColor(0.6f, 0.4f, 0.25f), TEXT("Shelf1"));
    SpawnFurniture(FVector(400, 0, 80), FVector(0.3f, 3.0f, 1.6f), FLinearColor(0.6f, 0.4f, 0.25f), TEXT("Shelf2"));
    SpawnFurniture(FVector(-400, -100, 130), FVector(0.2f, 0.2f, 0.2f), FLinearColor(0.95f, 0.3f, 0.3f), TEXT("Item1"));
    SpawnFurniture(FVector(-400, 0, 130), FVector(0.2f, 0.2f, 0.2f), FLinearColor(0.3f, 0.7f, 0.95f), TEXT("Item2"));
    SpawnFurniture(FVector(-400, 100, 130), FVector(0.2f, 0.2f, 0.2f), FLinearColor(0.95f, 0.85f, 0.2f), TEXT("Item3"));
    SpawnLight(FVector(0, 0, 280), 4500.0f, FLinearColor(1.0f, 0.98f, 0.92f));
}

void AEmersynGameMode::BuildPlayground()
{
    SpawnFloor(FVector(0, 0, 0), FVector(15, 15, 1), FLinearColor(0.35f, 0.65f, 0.25f));
    SpawnFurniture(FVector(0, 0, 1), FVector(6.0f, 6.0f, 0.02f), FLinearColor(0.75f, 0.6f, 0.4f), TEXT("DirtArea"));
    SpawnFurniture(FVector(-300, -200, 60), FVector(0.3f, 0.3f, 2.0f), FLinearColor(0.8f, 0.2f, 0.2f), TEXT("SlidePost"));
    SpawnFurniture(FVector(-300, -100, 50), FVector(0.4f, 1.5f, 0.05f), FLinearColor(0.95f, 0.85f, 0.1f), TEXT("Slide"));
    SpawnFurniture(FVector(200, -200, 100), FVector(0.1f, 0.1f, 2.0f), FLinearColor(0.4f, 0.4f, 0.5f), TEXT("SwingPole1"));
    SpawnFurniture(FVector(400, -200, 100), FVector(0.1f, 0.1f, 2.0f), FLinearColor(0.4f, 0.4f, 0.5f), TEXT("SwingPole2"));
    SpawnFurniture(FVector(300, -200, 200), FVector(2.2f, 0.1f, 0.1f), FLinearColor(0.4f, 0.4f, 0.5f), TEXT("SwingBar"));
    SpawnFurniture(FVector(300, -200, 30), FVector(0.3f, 0.2f, 0.05f), FLinearColor(0.2f, 0.5f, 0.8f), TEXT("SwingSeat"));
    SpawnFurniture(FVector(0, 300, 10), FVector(2.0f, 2.0f, 0.2f), FLinearColor(0.9f, 0.85f, 0.65f), TEXT("Sandbox"));
    SpawnLight(FVector(0, 0, 800), 8000.0f, FLinearColor(1.0f, 0.98f, 0.9f));
}

void AEmersynGameMode::BuildPark()
{
    SpawnFloor(FVector(0, 0, 0), FVector(20, 20, 1), FLinearColor(0.3f, 0.65f, 0.2f));
    SpawnFurniture(FVector(0, 0, 1), FVector(1.0f, 12.0f, 0.02f), FLinearColor(0.7f, 0.6f, 0.45f), TEXT("MainPath"));
    SpawnFurniture(FVector(0, 0, 1), FVector(12.0f, 1.0f, 0.02f), FLinearColor(0.7f, 0.6f, 0.45f), TEXT("CrossPath"));
    SpawnFurniture(FVector(-500, -500, 50), FVector(0.3f, 0.3f, 2.0f), FLinearColor(0.45f, 0.3f, 0.15f), TEXT("Tree1Trunk"));
    SpawnFurniture(FVector(-500, -500, 170), FVector(1.5f, 1.5f, 1.2f), FLinearColor(0.15f, 0.55f, 0.12f), TEXT("Tree1Top"));
    SpawnFurniture(FVector(500, 500, 50), FVector(0.3f, 0.3f, 2.0f), FLinearColor(0.45f, 0.3f, 0.15f), TEXT("Tree2Trunk"));
    SpawnFurniture(FVector(500, 500, 170), FVector(1.5f, 1.5f, 1.2f), FLinearColor(0.15f, 0.55f, 0.12f), TEXT("Tree2Top"));
    SpawnFurniture(FVector(300, -300, 2), FVector(2.0f, 1.5f, 0.05f), FLinearColor(0.2f, 0.5f, 0.8f), TEXT("Pond"));
    SpawnFurniture(FVector(-200, 200, 20), FVector(1.0f, 0.3f, 0.4f), FLinearColor(0.5f, 0.35f, 0.2f), TEXT("Bench"));
    SpawnLight(FVector(0, 0, 1000), 10000.0f, FLinearColor(1.0f, 0.97f, 0.88f));
}

void AEmersynGameMode::BuildMall()
{
    SpawnFloor(FVector(0, 0, 0), FVector(12, 12, 1), FLinearColor(0.9f, 0.88f, 0.85f));
    SpawnWall(FVector(0, -600, 200), FRotator::ZeroRotator, FVector(12, 0.1f, 4), FLinearColor(0.92f, 0.9f, 0.88f));
    SpawnWall(FVector(0, 600, 200), FRotator::ZeroRotator, FVector(12, 0.1f, 4), FLinearColor(0.92f, 0.9f, 0.88f));
    SpawnWall(FVector(-600, 0, 200), FRotator::ZeroRotator, FVector(0.1f, 12, 4), FLinearColor(0.92f, 0.9f, 0.88f));
    SpawnWall(FVector(600, 0, 200), FRotator::ZeroRotator, FVector(0.1f, 12, 4), FLinearColor(0.92f, 0.9f, 0.88f));
    SpawnFurniture(FVector(-500, -200, 100), FVector(0.3f, 2.0f, 2.0f), FLinearColor(0.95f, 0.45f, 0.6f), TEXT("StoreFront1"));
    SpawnFurniture(FVector(-500, 200, 100), FVector(0.3f, 2.0f, 2.0f), FLinearColor(0.4f, 0.7f, 0.95f), TEXT("StoreFront2"));
    SpawnFurniture(FVector(500, -200, 100), FVector(0.3f, 2.0f, 2.0f), FLinearColor(0.95f, 0.85f, 0.3f), TEXT("StoreFront3"));
    SpawnFurniture(FVector(500, 200, 100), FVector(0.3f, 2.0f, 2.0f), FLinearColor(0.4f, 0.9f, 0.5f), TEXT("StoreFront4"));
    SpawnFurniture(FVector(0, 0, 20), FVector(1.5f, 1.5f, 0.4f), FLinearColor(0.7f, 0.75f, 0.8f), TEXT("Fountain"));
    SpawnFurniture(FVector(0, 0, 50), FVector(0.3f, 0.3f, 0.8f), FLinearColor(0.3f, 0.6f, 0.9f), TEXT("FountainWater"));
    SpawnLight(FVector(0, 0, 380), 6000.0f, FLinearColor(1.0f, 1.0f, 1.0f));
    SpawnLight(FVector(-300, 0, 200), 2000.0f, FLinearColor(0.95f, 0.9f, 0.85f));
    SpawnLight(FVector(300, 0, 200), 2000.0f, FLinearColor(0.95f, 0.9f, 0.85f));
}

void AEmersynGameMode::BuildArcade()
{
    SpawnFloor(FVector(0, 0, 0), FVector(8, 8, 1), FLinearColor(0.15f, 0.1f, 0.2f));
    SpawnWall(FVector(0, -400, 150), FRotator::ZeroRotator, FVector(8, 0.1f, 3), FLinearColor(0.2f, 0.1f, 0.3f));
    SpawnWall(FVector(0, 400, 150), FRotator::ZeroRotator, FVector(8, 0.1f, 3), FLinearColor(0.2f, 0.1f, 0.3f));
    SpawnWall(FVector(-400, 0, 150), FRotator::ZeroRotator, FVector(0.1f, 8, 3), FLinearColor(0.2f, 0.1f, 0.3f));
    SpawnWall(FVector(400, 0, 150), FRotator::ZeroRotator, FVector(0.1f, 8, 3), FLinearColor(0.2f, 0.1f, 0.3f));
    SpawnFurniture(FVector(-250, -250, 60), FVector(0.5f, 0.4f, 1.2f), FLinearColor(0.95f, 0.2f, 0.2f), TEXT("ArcadeMachine1"));
    SpawnFurniture(FVector(-250, 0, 60), FVector(0.5f, 0.4f, 1.2f), FLinearColor(0.2f, 0.3f, 0.95f), TEXT("ArcadeMachine2"));
    SpawnFurniture(FVector(-250, 250, 60), FVector(0.5f, 0.4f, 1.2f), FLinearColor(0.2f, 0.9f, 0.3f), TEXT("ArcadeMachine3"));
    SpawnFurniture(FVector(250, -250, 60), FVector(0.5f, 0.4f, 1.2f), FLinearColor(0.9f, 0.8f, 0.1f), TEXT("ArcadeMachine4"));
    SpawnFurniture(FVector(250, 0, 60), FVector(0.5f, 0.4f, 1.2f), FLinearColor(0.9f, 0.4f, 0.9f), TEXT("ArcadeMachine5"));
    SpawnFurniture(FVector(250, 250, 60), FVector(0.5f, 0.4f, 1.2f), FLinearColor(0.1f, 0.8f, 0.9f), TEXT("ArcadeMachine6"));
    SpawnLight(FVector(-200, -200, 250), 1500.0f, FLinearColor(0.95f, 0.1f, 0.4f));
    SpawnLight(FVector(200, 200, 250), 1500.0f, FLinearColor(0.1f, 0.4f, 0.95f));
    SpawnLight(FVector(0, 0, 280), 2000.0f, FLinearColor(0.8f, 0.3f, 0.9f));
}

void AEmersynGameMode::BuildAmusementPark()
{
    SpawnFloor(FVector(0, 0, 0), FVector(20, 20, 1), FLinearColor(0.6f, 0.55f, 0.5f));
    SpawnFurniture(FVector(-500, 0, 50), FVector(0.5f, 0.5f, 1.0f), FLinearColor(0.7f, 0.2f, 0.2f), TEXT("FerrisBase"));
    SpawnFurniture(FVector(-500, 0, 250), FVector(3.0f, 0.1f, 3.0f), FLinearColor(0.8f, 0.3f, 0.3f), TEXT("FerrisWheel"));
    SpawnFurniture(FVector(300, 0, 30), FVector(2.0f, 2.0f, 0.3f), FLinearColor(0.95f, 0.85f, 0.3f), TEXT("CarouselBase"));
    SpawnFurniture(FVector(300, 0, 100), FVector(0.2f, 0.2f, 1.5f), FLinearColor(0.8f, 0.6f, 0.2f), TEXT("CarouselPole"));
    SpawnFurniture(FVector(300, 0, 170), FVector(2.2f, 2.2f, 0.1f), FLinearColor(0.95f, 0.3f, 0.5f), TEXT("CarouselTop"));
    SpawnFurniture(FVector(0, -500, 100), FVector(5.0f, 0.3f, 0.1f), FLinearColor(0.4f, 0.4f, 0.5f), TEXT("CoasterTrack1"));
    SpawnFurniture(FVector(0, -500, 50), FVector(0.2f, 0.2f, 1.0f), FLinearColor(0.5f, 0.5f, 0.6f), TEXT("CoasterSupport"));
    SpawnFurniture(FVector(0, 500, 50), FVector(1.5f, 0.8f, 1.0f), FLinearColor(0.95f, 0.6f, 0.2f), TEXT("FoodStand"));
    SpawnFurniture(FVector(-200, 600, 50), FVector(0.8f, 0.8f, 1.0f), FLinearColor(0.2f, 0.5f, 0.9f), TEXT("TicketBooth"));
    SpawnLight(FVector(0, 0, 800), 10000.0f, FLinearColor(1.0f, 0.95f, 0.88f));
    SpawnLight(FVector(-500, 0, 400), 3000.0f, FLinearColor(0.95f, 0.8f, 0.3f));
    SpawnLight(FVector(300, 0, 200), 3000.0f, FLinearColor(0.95f, 0.4f, 0.6f));
}
