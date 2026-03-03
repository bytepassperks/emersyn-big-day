#include "EmersynGameMode.h"
#include "EmersynHUD.h"
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
    CurrentRoom = TEXT("Splash");
    RoomIndex = 0;
    Timer = 0.0f;
    SplashDuration = 3.0f;
    RoomDuration = 6.0f;
    bInSplash = true;
    PrimaryActorTick.bCanEverTick = true;
    HUDClass = AEmersynHUD::StaticClass();

    RoomNames.Add(TEXT("MainMenu"));
    RoomNames.Add(TEXT("Bedroom"));
    RoomNames.Add(TEXT("Kitchen"));
    RoomNames.Add(TEXT("LivingRoom"));
    RoomNames.Add(TEXT("Bathroom"));
    RoomNames.Add(TEXT("Garden"));
    RoomNames.Add(TEXT("School"));
    RoomNames.Add(TEXT("Shop"));
    RoomNames.Add(TEXT("Playground"));
    RoomNames.Add(TEXT("Park"));
    RoomNames.Add(TEXT("Mall"));
    RoomNames.Add(TEXT("Arcade"));
    RoomNames.Add(TEXT("AmusementPark"));

    RoomDisplayNames.Add(TEXT("EMERSYN'S BIG DAY"));
    RoomDisplayNames.Add(TEXT("Emersyn's Bedroom"));
    RoomDisplayNames.Add(TEXT("Kitchen"));
    RoomDisplayNames.Add(TEXT("Living Room"));
    RoomDisplayNames.Add(TEXT("Bathroom"));
    RoomDisplayNames.Add(TEXT("Garden"));
    RoomDisplayNames.Add(TEXT("School"));
    RoomDisplayNames.Add(TEXT("Toy Shop"));
    RoomDisplayNames.Add(TEXT("Playground"));
    RoomDisplayNames.Add(TEXT("Park"));
    RoomDisplayNames.Add(TEXT("Shopping Mall"));
    RoomDisplayNames.Add(TEXT("Arcade"));
    RoomDisplayNames.Add(TEXT("Amusement Park"));
}

void AEmersynGameMode::InitGame(const FString& MapName, const FString& Options, FString& ErrorMessage)
{
    Super::InitGame(MapName, Options, ErrorMessage);
    UE_LOG(LogTemp, Log, TEXT("EmersynGameMode v8: InitGame - Custom M_SolidColor material"));
}

void AEmersynGameMode::BeginPlay()
{
    Super::BeginPlay();
    UE_LOG(LogTemp, Log, TEXT("EmersynGameMode v8: BeginPlay - Starting splash"));

    // Setup HUD
    UWorld* World = GetWorld();
    if (World)
    {
        APlayerController* PC = World->GetFirstPlayerController();
        if (PC)
        {
            AEmersynHUD* HUD = Cast<AEmersynHUD>(PC->GetHUD());
            if (HUD)
            {
                HUD->bShowSplash = true;
                HUD->SplashAlpha = 1.0f;
                HUD->RoomDisplayName = TEXT("");
            }
        }
    }

    BuildSplashScreen();
}

void AEmersynGameMode::Tick(float DeltaSeconds)
{
    Super::Tick(DeltaSeconds);
    Timer += DeltaSeconds;

    // Update HUD
    UWorld* World = GetWorld();
    AEmersynHUD* HUD = nullptr;
    if (World)
    {
        APlayerController* PC = World->GetFirstPlayerController();
        if (PC) HUD = Cast<AEmersynHUD>(PC->GetHUD());
    }

    if (bInSplash && Timer >= SplashDuration)
    {
        bInSplash = false;
        Timer = 0.0f;
        RoomIndex = 0;
        if (HUD)
        {
            HUD->bShowSplash = false;
            HUD->RoomDisplayName = RoomDisplayNames[RoomIndex];
        }
        LoadRoom(RoomNames[RoomIndex]);
    }
    else if (!bInSplash && Timer >= RoomDuration)
    {
        Timer = 0.0f;
        RoomIndex = (RoomIndex + 1) % RoomNames.Num();
        if (HUD)
        {
            HUD->RoomDisplayName = RoomDisplayNames[RoomIndex];
        }
        LoadRoom(RoomNames[RoomIndex]);
    }
    else if (bInSplash && HUD)
    {
        // Fade splash near end
        float FadeStart = SplashDuration * 0.7f;
        if (Timer > FadeStart)
        {
            HUD->SplashAlpha = 1.0f - (Timer - FadeStart) / (SplashDuration - FadeStart);
        }
    }
}

void AEmersynGameMode::LoadRoom(const FString& RoomName)
{
    ClearRoom();
    CurrentRoom = RoomName;
    BuildCurrentRoom();
}

void AEmersynGameMode::ClearRoom()
{
    for (AActor* Actor : SpawnedActors)
    {
        if (Actor && IsValid(Actor))
        {
            Actor->Destroy();
        }
    }
    SpawnedActors.Empty();
}

void AEmersynGameMode::BuildCurrentRoom()
{
    UE_LOG(LogTemp, Log, TEXT("v8 Building room: %s"), *CurrentRoom);

    if (CurrentRoom == TEXT("Splash")) BuildSplashScreen();
    else if (CurrentRoom == TEXT("MainMenu")) BuildMainMenu();
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

// =============================================================================
// v8 MakeMat: Custom M_SolidColor material (fixes Android checkerboard)
// 
// WHY PREVIOUS APPROACHES FAILED:
// v6: SetVectorParameterValue on BasicShapeMaterial → checkerboard (BSM's vector
//     parameter isn't properly connected in the compiled mobile shader)
// v7: Runtime UTexture2D + SetTextureParameterValue → checkerboard (BSM uses
//     TextureSample not TextureSampleParameter2D, so texture params are ignored)
//
// v8 FIX: Use a CUSTOM material (/Game/Materials/M_SolidColor) created via UE5
// editor Python script. This material has a VectorParameter "Color" node directly
// wired to BaseColor output in the material graph. The shader is compiled with
// this connection, so SetVectorParameterValue WILL work on Android mobile.
// =============================================================================
UMaterialInstanceDynamic* AEmersynGameMode::MakeMat(FLinearColor Color)
{
    // Build a cache key from the color
    FString ColorKey = FString::Printf(TEXT("%.3f_%.3f_%.3f_%.3f"), Color.R, Color.G, Color.B, Color.A);
    
    // Check material cache first
    if (UMaterialInstanceDynamic** CachedMat = MaterialCache.Find(ColorKey))
    {
        if (*CachedMat && IsValid(*CachedMat))
        {
            return *CachedMat;
        }
    }
    
    // Load our custom M_SolidColor material (created via UE5 editor Python script)
    // This material has a VectorParameter "Color" directly connected to BaseColor
    UMaterialInterface* BaseMat = LoadObject<UMaterialInterface>(nullptr, TEXT("/Game/Materials/M_SolidColor.M_SolidColor"));
    
    if (!BaseMat)
    {
        // Fallback 1: Try BasicShapeMaterial
        UE_LOG(LogTemp, Warning, TEXT("v8 MakeMat: M_SolidColor not found, trying BasicShapeMaterial"));
        BaseMat = LoadObject<UMaterialInterface>(nullptr, TEXT("/Engine/BasicShapes/BasicShapeMaterial.BasicShapeMaterial"));
    }
    if (!BaseMat)
    {
        // Fallback 2: Try DefaultMaterial
        UE_LOG(LogTemp, Warning, TEXT("v8 MakeMat: BasicShapeMaterial not found, trying DefaultMaterial"));
        BaseMat = LoadObject<UMaterialInterface>(nullptr, TEXT("/Engine/EngineMaterials/DefaultMaterial.DefaultMaterial"));
    }
    if (!BaseMat)
    {
        UE_LOG(LogTemp, Error, TEXT("v8 MakeMat: No base material available!"));
        return nullptr;
    }
    
    UMaterialInstanceDynamic* DynMat = UMaterialInstanceDynamic::Create(BaseMat, this);
    if (!DynMat)
    {
        UE_LOG(LogTemp, Error, TEXT("v8 MakeMat: Failed to create MID for %s"), *ColorKey);
        return nullptr;
    }
    
    // Set the "Color" vector parameter (this is the parameter name in M_SolidColor)
    DynMat->SetVectorParameterValue(TEXT("Color"), Color);
    
    // Also try BaseColor as fallback parameter name
    DynMat->SetVectorParameterValue(TEXT("BaseColor"), Color);
    
    // Cache the material
    MaterialCache.Add(ColorKey, DynMat);
    
    UE_LOG(LogTemp, Log, TEXT("v8 MakeMat: Created material for color %s using %s"), *ColorKey, *BaseMat->GetName());
    return DynMat;
}

AActor* AEmersynGameMode::SpawnBox(FVector Location, FVector Scale, FLinearColor Color)
{
    UWorld* World = GetWorld();
    if (!World) return nullptr;
    AStaticMeshActor* Actor = World->SpawnActor<AStaticMeshActor>(AStaticMeshActor::StaticClass(), Location, FRotator::ZeroRotator);
    if (!Actor) return nullptr;
    UStaticMeshComponent* Mesh = Actor->GetStaticMeshComponent();
    UStaticMesh* CubeMesh = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cube.Cube"));
    if (Mesh && CubeMesh)
    {
        Mesh->SetStaticMesh(CubeMesh);
        Mesh->SetMobility(EComponentMobility::Movable);
        Actor->SetActorScale3D(Scale);
        UMaterialInstanceDynamic* Mat = MakeMat(Color);
        if (Mat) Mesh->SetMaterial(0, Mat);
    }
    SpawnedActors.Add(Actor);
    return Actor;
}

AActor* AEmersynGameMode::SpawnSphere(FVector Location, float Radius, FLinearColor Color)
{
    UWorld* World = GetWorld();
    if (!World) return nullptr;
    AStaticMeshActor* Actor = World->SpawnActor<AStaticMeshActor>(AStaticMeshActor::StaticClass(), Location, FRotator::ZeroRotator);
    if (!Actor) return nullptr;
    UStaticMeshComponent* Mesh = Actor->GetStaticMeshComponent();
    UStaticMesh* SphereMesh = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Sphere.Sphere"));
    if (Mesh && SphereMesh)
    {
        Mesh->SetStaticMesh(SphereMesh);
        Mesh->SetMobility(EComponentMobility::Movable);
        Actor->SetActorScale3D(FVector(Radius));
        UMaterialInstanceDynamic* Mat = MakeMat(Color);
        if (Mat) Mesh->SetMaterial(0, Mat);
    }
    SpawnedActors.Add(Actor);
    return Actor;
}

AActor* AEmersynGameMode::SpawnCylinder(FVector Location, FVector Scale, FLinearColor Color)
{
    UWorld* World = GetWorld();
    if (!World) return nullptr;
    AStaticMeshActor* Actor = World->SpawnActor<AStaticMeshActor>(AStaticMeshActor::StaticClass(), Location, FRotator::ZeroRotator);
    if (!Actor) return nullptr;
    UStaticMeshComponent* Mesh = Actor->GetStaticMeshComponent();
    UStaticMesh* CylMesh = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cylinder.Cylinder"));
    if (Mesh && CylMesh)
    {
        Mesh->SetStaticMesh(CylMesh);
        Mesh->SetMobility(EComponentMobility::Movable);
        Actor->SetActorScale3D(Scale);
        UMaterialInstanceDynamic* Mat = MakeMat(Color);
        if (Mat) Mesh->SetMaterial(0, Mat);
    }
    SpawnedActors.Add(Actor);
    return Actor;
}

void AEmersynGameMode::SpawnLight(FVector Location, float Intensity, FLinearColor Color)
{
    UWorld* World = GetWorld();
    if (!World) return;
    APointLight* Light = World->SpawnActor<APointLight>(APointLight::StaticClass(), Location, FRotator::ZeroRotator);
    if (Light)
    {
        UPointLightComponent* LC = Light->PointLightComponent;
        if (LC)
        {
            LC->SetIntensity(Intensity);
            LC->SetLightColor(Color);
            LC->SetAttenuationRadius(3000.0f);
            LC->SetCastShadows(false);
        }
        SpawnedActors.Add(Light);
    }
}

void AEmersynGameMode::SpawnDirectionalLight(FRotator Rotation, float Intensity, FLinearColor Color)
{
    UWorld* World = GetWorld();
    if (!World) return;
    ADirectionalLight* Light = World->SpawnActor<ADirectionalLight>(ADirectionalLight::StaticClass(), FVector::ZeroVector, Rotation);
    if (Light)
    {
        ULightComponent* LC = Light->GetLightComponent();
        if (LC)
        {
            LC->SetIntensity(Intensity);
            LC->SetLightColor(Color);
            LC->SetCastShadows(false);
        }
        SpawnedActors.Add(Light);
    }
}

void AEmersynGameMode::SetupCamera(FVector Location, FRotator Rotation)
{
    UWorld* World = GetWorld();
    if (!World) return;
    ACameraActor* Camera = World->SpawnActor<ACameraActor>(ACameraActor::StaticClass(), Location, Rotation);
    if (Camera)
    {
        APlayerController* PC = World->GetFirstPlayerController();
        if (PC) PC->SetViewTarget(Camera);
        SpawnedActors.Add(Camera);
    }
}

void AEmersynGameMode::SpawnCharacter(FVector Location, FLinearColor SkinColor, FLinearColor HairColor, FLinearColor OutfitColor, const FString& Name, float Scale)
{
    float S = Scale;
    // Head
    SpawnSphere(Location + FVector(0, 0, 140 * S), 0.45f * S, SkinColor);
    // Hair
    SpawnSphere(Location + FVector(0, 0, 165 * S), 0.48f * S, HairColor);
    // Body / Outfit
    SpawnBox(Location + FVector(0, 0, 80 * S), FVector(0.4f, 0.25f, 0.6f) * S, OutfitColor);
    // Arms
    SpawnCylinder(Location + FVector(35 * S, 0, 90 * S), FVector(0.1f, 0.1f, 0.35f) * S, SkinColor);
    SpawnCylinder(Location + FVector(-35 * S, 0, 90 * S), FVector(0.1f, 0.1f, 0.35f) * S, SkinColor);
    // Legs
    SpawnCylinder(Location + FVector(12 * S, 0, 25 * S), FVector(0.12f, 0.12f, 0.3f) * S, OutfitColor * 0.8f);
    SpawnCylinder(Location + FVector(-12 * S, 0, 25 * S), FVector(0.12f, 0.12f, 0.3f) * S, OutfitColor * 0.8f);
    // Shoes
    SpawnBox(Location + FVector(12 * S, 0, 5 * S), FVector(0.12f, 0.15f, 0.06f) * S, FLinearColor(0.3f, 0.2f, 0.15f));
    SpawnBox(Location + FVector(-12 * S, 0, 5 * S), FVector(0.12f, 0.15f, 0.06f) * S, FLinearColor(0.3f, 0.2f, 0.15f));
    // Eyes (small dark spheres)
    SpawnSphere(Location + FVector(10 * S, -20 * S, 145 * S), 0.06f * S, FLinearColor(0.1f, 0.1f, 0.15f));
    SpawnSphere(Location + FVector(-10 * S, -20 * S, 145 * S), 0.06f * S, FLinearColor(0.1f, 0.1f, 0.15f));
}

void AEmersynGameMode::SpawnPet(FVector Location, FLinearColor BodyColor, FLinearColor AccentColor, const FString& Name, float Scale)
{
    float S = Scale;
    // Body
    SpawnSphere(Location + FVector(0, 0, 25 * S), 0.4f * S, BodyColor);
    // Head
    SpawnSphere(Location + FVector(30 * S, 0, 40 * S), 0.3f * S, BodyColor);
    // Ears
    SpawnSphere(Location + FVector(40 * S, 12 * S, 55 * S), 0.1f * S, AccentColor);
    SpawnSphere(Location + FVector(40 * S, -12 * S, 55 * S), 0.1f * S, AccentColor);
    // Tail
    SpawnCylinder(Location + FVector(-35 * S, 0, 35 * S), FVector(0.05f, 0.05f, 0.2f) * S, BodyColor);
    // Legs
    SpawnCylinder(Location + FVector(15 * S, 12 * S, 8 * S), FVector(0.06f, 0.06f, 0.15f) * S, BodyColor);
    SpawnCylinder(Location + FVector(15 * S, -12 * S, 8 * S), FVector(0.06f, 0.06f, 0.15f) * S, BodyColor);
    SpawnCylinder(Location + FVector(-15 * S, 12 * S, 8 * S), FVector(0.06f, 0.06f, 0.15f) * S, BodyColor);
    SpawnCylinder(Location + FVector(-15 * S, -12 * S, 8 * S), FVector(0.06f, 0.06f, 0.15f) * S, BodyColor);
    // Eyes
    SpawnSphere(Location + FVector(40 * S, -8 * S, 45 * S), 0.05f * S, FLinearColor(0.1f, 0.1f, 0.1f));
    SpawnSphere(Location + FVector(40 * S, 8 * S, 45 * S), 0.05f * S, FLinearColor(0.1f, 0.1f, 0.1f));
    // Nose
    SpawnSphere(Location + FVector(48 * S, 0, 40 * S), 0.04f * S, AccentColor);
}

// ==================== SPLASH ====================
void AEmersynGameMode::BuildSplashScreen()
{
    SetupCamera(FVector(0, -500, 200), FRotator(-10, 0, 0));

    // Dark gradient background
    SpawnBox(FVector(0, 0, 0), FVector(20, 20, 0.1f), FLinearColor(0.08f, 0.05f, 0.15f));
    SpawnBox(FVector(0, 200, 200), FVector(20, 0.1f, 4), FLinearColor(0.12f, 0.08f, 0.2f));

    // Title letters - large colorful blocks
    SpawnBox(FVector(-300, 0, 300), FVector(0.8f, 0.1f, 1.2f), FLinearColor(1.0f, 0.4f, 0.6f));
    SpawnBox(FVector(-200, 0, 300), FVector(0.8f, 0.1f, 1.2f), FLinearColor(0.95f, 0.6f, 0.2f));
    SpawnBox(FVector(-100, 0, 300), FVector(0.8f, 0.1f, 1.2f), FLinearColor(0.95f, 0.85f, 0.2f));
    SpawnBox(FVector(0, 0, 300), FVector(0.8f, 0.1f, 1.2f), FLinearColor(0.3f, 0.85f, 0.4f));
    SpawnBox(FVector(100, 0, 300), FVector(0.8f, 0.1f, 1.2f), FLinearColor(0.3f, 0.7f, 0.95f));
    SpawnBox(FVector(200, 0, 300), FVector(0.8f, 0.1f, 1.2f), FLinearColor(0.7f, 0.4f, 0.95f));
    SpawnBox(FVector(300, 0, 300), FVector(0.8f, 0.1f, 1.2f), FLinearColor(0.95f, 0.4f, 0.8f));

    // Decorative stars
    SpawnSphere(FVector(-400, -50, 450), 0.2f, FLinearColor(1.0f, 0.95f, 0.3f));
    SpawnSphere(FVector(400, -50, 420), 0.15f, FLinearColor(1.0f, 0.8f, 0.9f));
    SpawnSphere(FVector(-200, -50, 480), 0.12f, FLinearColor(0.8f, 0.9f, 1.0f));
    SpawnSphere(FVector(300, -50, 500), 0.18f, FLinearColor(0.9f, 0.7f, 1.0f));
    SpawnSphere(FVector(0, -50, 520), 0.25f, FLinearColor(1.0f, 0.6f, 0.7f));

    // Emersyn character in center
    SpawnCharacter(FVector(0, 50, 50), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(1.0f, 0.5f, 0.7f), TEXT("Emersyn"), 1.5f);

    // Subtitle bar
    SpawnBox(FVector(0, 0, 150), FVector(4.0f, 0.1f, 0.15f), FLinearColor(0.95f, 0.85f, 0.3f));

    // Ambient lights
    SpawnLight(FVector(0, -300, 400), 8000.0f, FLinearColor(0.95f, 0.8f, 0.9f));
    SpawnLight(FVector(-300, -200, 300), 3000.0f, FLinearColor(0.6f, 0.4f, 0.9f));
    SpawnLight(FVector(300, -200, 300), 3000.0f, FLinearColor(0.9f, 0.4f, 0.6f));
}

// ==================== MAIN MENU ====================
void AEmersynGameMode::BuildMainMenu()
{
    SetupCamera(FVector(0, -800, 400), FRotator(-20, 0, 0));

    // Floor - warm pink gradient
    SpawnBox(FVector(0, 0, -5), FVector(15, 15, 0.1f), FLinearColor(0.95f, 0.85f, 0.88f));
    // Background wall
    SpawnBox(FVector(0, 500, 250), FVector(15, 0.1f, 5), FLinearColor(0.92f, 0.82f, 0.9f));

    // Room door buttons
    SpawnBox(FVector(-400, 200, 100), FVector(1.2f, 0.1f, 0.8f), FLinearColor(1.0f, 0.7f, 0.8f));
    SpawnBox(FVector(-200, 200, 100), FVector(1.2f, 0.1f, 0.8f), FLinearColor(0.95f, 0.9f, 0.6f));
    SpawnBox(FVector(0, 200, 100), FVector(1.2f, 0.1f, 0.8f), FLinearColor(0.7f, 0.85f, 0.95f));
    SpawnBox(FVector(200, 200, 100), FVector(1.2f, 0.1f, 0.8f), FLinearColor(0.9f, 0.85f, 0.75f));
    SpawnBox(FVector(400, 200, 100), FVector(1.2f, 0.1f, 0.8f), FLinearColor(0.6f, 0.9f, 0.65f));
    SpawnBox(FVector(-400, 200, 250), FVector(1.2f, 0.1f, 0.8f), FLinearColor(0.95f, 0.88f, 0.65f));
    SpawnBox(FVector(-200, 200, 250), FVector(1.2f, 0.1f, 0.8f), FLinearColor(0.85f, 0.7f, 0.95f));
    SpawnBox(FVector(0, 200, 250), FVector(1.2f, 0.1f, 0.8f), FLinearColor(0.6f, 0.85f, 0.5f));
    SpawnBox(FVector(200, 200, 250), FVector(1.2f, 0.1f, 0.8f), FLinearColor(0.5f, 0.8f, 0.6f));
    SpawnBox(FVector(400, 200, 250), FVector(1.2f, 0.1f, 0.8f), FLinearColor(0.9f, 0.75f, 0.6f));

    // Characters
    SpawnCharacter(FVector(-300, -100, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(1.0f, 0.5f, 0.7f), TEXT("Emersyn"));
    SpawnCharacter(FVector(-100, -50, 0), FLinearColor(0.92f, 0.78f, 0.65f), FLinearColor(0.15f, 0.1f, 0.08f), FLinearColor(0.6f, 0.3f, 0.9f), TEXT("Ava"));
    SpawnCharacter(FVector(100, -80, 0), FLinearColor(0.85f, 0.7f, 0.55f), FLinearColor(0.3f, 0.2f, 0.1f), FLinearColor(0.2f, 0.6f, 0.9f), TEXT("Leo"));
    SpawnCharacter(FVector(300, -40, 0), FLinearColor(0.9f, 0.75f, 0.6f), FLinearColor(0.7f, 0.3f, 0.15f), FLinearColor(0.3f, 0.85f, 0.5f), TEXT("Mia"));

    // Pets
    SpawnPet(FVector(-200, -180, 0), FLinearColor(0.9f, 0.6f, 0.3f), FLinearColor(0.95f, 0.8f, 0.6f), TEXT("Cat"));
    SpawnPet(FVector(0, -200, 0), FLinearColor(0.7f, 0.5f, 0.3f), FLinearColor(0.4f, 0.25f, 0.15f), TEXT("Dog"));
    SpawnPet(FVector(200, -170, 0), FLinearColor(0.95f, 0.95f, 0.92f), FLinearColor(0.95f, 0.7f, 0.75f), TEXT("Bunny"));

    SpawnLight(FVector(0, -300, 500), 10000.0f, FLinearColor(1.0f, 0.95f, 0.9f));
    SpawnLight(FVector(-400, 0, 300), 3000.0f, FLinearColor(1.0f, 0.8f, 0.85f));
    SpawnLight(FVector(400, 0, 300), 3000.0f, FLinearColor(0.85f, 0.8f, 1.0f));
    SpawnDirectionalLight(FRotator(-45, 30, 0), 3.0f, FLinearColor(1.0f, 0.97f, 0.92f));
}

// ==================== BEDROOM ====================
void AEmersynGameMode::BuildBedroom()
{
    SetupCamera(FVector(0, -550, 350), FRotator(-25, 0, 0));

    SpawnBox(FVector(0, 0, -5), FVector(8, 8, 0.1f), FLinearColor(0.95f, 0.82f, 0.85f));
    SpawnBox(FVector(0, 400, 200), FVector(8, 0.1f, 4), FLinearColor(0.88f, 0.82f, 0.95f));
    SpawnBox(FVector(-400, 0, 200), FVector(0.1f, 8, 4), FLinearColor(0.9f, 0.84f, 0.96f));
    SpawnBox(FVector(400, 0, 200), FVector(0.1f, 8, 4), FLinearColor(0.9f, 0.84f, 0.96f));

    SpawnBox(FVector(-200, 200, 30), FVector(1.8f, 1.2f, 0.3f), FLinearColor(1.0f, 0.7f, 0.8f));
    SpawnBox(FVector(-200, 200, 50), FVector(1.6f, 1.0f, 0.15f), FLinearColor(0.98f, 0.95f, 0.97f));
    SpawnBox(FVector(-200, 280, 65), FVector(0.5f, 0.3f, 0.1f), FLinearColor(0.95f, 0.85f, 0.95f));
    SpawnBox(FVector(-200, 150, 58), FVector(1.4f, 0.7f, 0.08f), FLinearColor(0.85f, 0.6f, 0.8f));
    SpawnBox(FVector(-200, 310, 100), FVector(1.8f, 0.08f, 0.8f), FLinearColor(0.95f, 0.75f, 0.85f));

    SpawnBox(FVector(250, 300, 50), FVector(0.8f, 0.4f, 1.0f), FLinearColor(0.95f, 0.88f, 0.92f));
    SpawnSphere(FVector(250, 278, 60), 0.04f, FLinearColor(0.9f, 0.7f, 0.3f));
    SpawnSphere(FVector(250, 278, 40), 0.04f, FLinearColor(0.9f, 0.7f, 0.3f));
    SpawnBox(FVector(250, 310, 130), FVector(0.5f, 0.05f, 0.5f), FLinearColor(0.8f, 0.85f, 0.9f));

    SpawnBox(FVector(-50, 280, 25), FVector(0.35f, 0.35f, 0.5f), FLinearColor(0.92f, 0.85f, 0.88f));
    SpawnCylinder(FVector(-50, 280, 55), FVector(0.05f, 0.05f, 0.15f), FLinearColor(0.9f, 0.7f, 0.3f));
    SpawnSphere(FVector(-50, 280, 72), 0.12f, FLinearColor(1.0f, 0.95f, 0.8f));

    SpawnBox(FVector(0, 0, 1), FVector(2.5f, 2.0f, 0.02f), FLinearColor(0.85f, 0.65f, 0.75f));

    SpawnBox(FVector(350, -100, 75), FVector(0.4f, 0.8f, 1.5f), FLinearColor(0.92f, 0.85f, 0.8f));
    SpawnBox(FVector(350, -120, 120), FVector(0.05f, 0.15f, 0.2f), FLinearColor(0.9f, 0.3f, 0.4f));
    SpawnBox(FVector(350, -100, 120), FVector(0.05f, 0.12f, 0.18f), FLinearColor(0.3f, 0.6f, 0.9f));
    SpawnBox(FVector(350, -80, 120), FVector(0.05f, 0.13f, 0.2f), FLinearColor(0.4f, 0.85f, 0.5f));

    SpawnBox(FVector(200, -200, 20), FVector(0.6f, 0.4f, 0.4f), FLinearColor(0.95f, 0.6f, 0.3f));

    SpawnBox(FVector(150, 395, 250), FVector(1.0f, 0.02f, 0.8f), FLinearColor(0.7f, 0.85f, 0.95f));
    SpawnBox(FVector(150, 393, 250), FVector(1.05f, 0.01f, 0.03f), FLinearColor(0.95f, 0.92f, 0.88f));
    SpawnBox(FVector(150, 393, 290), FVector(1.05f, 0.01f, 0.03f), FLinearColor(0.95f, 0.92f, 0.88f));

    SpawnCharacter(FVector(50, 0, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(1.0f, 0.5f, 0.7f), TEXT("Emersyn"));
    SpawnPet(FVector(150, 50, 0), FLinearColor(0.9f, 0.6f, 0.3f), FLinearColor(0.95f, 0.8f, 0.6f), TEXT("Cat"));

    SpawnLight(FVector(0, 0, 350), 6000.0f, FLinearColor(1.0f, 0.92f, 0.88f));
    SpawnLight(FVector(-200, 200, 200), 2000.0f, FLinearColor(1.0f, 0.85f, 0.75f));
    SpawnDirectionalLight(FRotator(-45, 30, 0), 2.5f, FLinearColor(1.0f, 0.95f, 0.9f));
}

// ==================== KITCHEN ====================
void AEmersynGameMode::BuildKitchen()
{
    SetupCamera(FVector(0, -550, 350), FRotator(-25, 0, 0));

    SpawnBox(FVector(0, 0, -5), FVector(8, 8, 0.1f), FLinearColor(0.95f, 0.9f, 0.82f));
    SpawnBox(FVector(0, 400, 200), FVector(8, 0.1f, 4), FLinearColor(0.98f, 0.95f, 0.82f));
    SpawnBox(FVector(-400, 0, 200), FVector(0.1f, 8, 4), FLinearColor(0.97f, 0.94f, 0.83f));
    SpawnBox(FVector(400, 0, 200), FVector(0.1f, 8, 4), FLinearColor(0.97f, 0.94f, 0.83f));

    SpawnBox(FVector(-300, 300, 45), FVector(0.6f, 2.5f, 0.9f), FLinearColor(0.92f, 0.88f, 0.82f));
    SpawnBox(FVector(-300, 300, 92), FVector(0.65f, 2.6f, 0.04f), FLinearColor(0.85f, 0.82f, 0.78f));

    SpawnBox(FVector(-300, 150, 90), FVector(0.55f, 0.5f, 0.04f), FLinearColor(0.3f, 0.3f, 0.32f));
    SpawnCylinder(FVector(-290, 130, 93), FVector(0.08f, 0.08f, 0.01f), FLinearColor(0.2f, 0.2f, 0.22f));
    SpawnCylinder(FVector(-310, 130, 93), FVector(0.08f, 0.08f, 0.01f), FLinearColor(0.2f, 0.2f, 0.22f));
    SpawnCylinder(FVector(-290, 170, 93), FVector(0.06f, 0.06f, 0.01f), FLinearColor(0.2f, 0.2f, 0.22f));
    SpawnCylinder(FVector(-310, 170, 93), FVector(0.06f, 0.06f, 0.01f), FLinearColor(0.2f, 0.2f, 0.22f));

    SpawnBox(FVector(300, 300, 100), FVector(0.6f, 0.5f, 2.0f), FLinearColor(0.92f, 0.92f, 0.93f));
    SpawnBox(FVector(300, 273, 120), FVector(0.03f, 0.02f, 0.3f), FLinearColor(0.7f, 0.7f, 0.72f));

    SpawnBox(FVector(50, -50, 38), FVector(1.2f, 0.8f, 0.04f), FLinearColor(0.75f, 0.55f, 0.35f));
    SpawnCylinder(FVector(-8, -80, 18), FVector(0.04f, 0.04f, 0.35f), FLinearColor(0.65f, 0.45f, 0.28f));
    SpawnCylinder(FVector(108, -80, 18), FVector(0.04f, 0.04f, 0.35f), FLinearColor(0.65f, 0.45f, 0.28f));
    SpawnCylinder(FVector(-8, -20, 18), FVector(0.04f, 0.04f, 0.35f), FLinearColor(0.65f, 0.45f, 0.28f));
    SpawnCylinder(FVector(108, -20, 18), FVector(0.04f, 0.04f, 0.35f), FLinearColor(0.65f, 0.45f, 0.28f));

    SpawnBox(FVector(-60, -50, 22), FVector(0.3f, 0.3f, 0.04f), FLinearColor(0.85f, 0.6f, 0.35f));
    SpawnBox(FVector(-60, -50, 50), FVector(0.3f, 0.04f, 0.4f), FLinearColor(0.85f, 0.6f, 0.35f));
    SpawnBox(FVector(160, -50, 22), FVector(0.3f, 0.3f, 0.04f), FLinearColor(0.85f, 0.6f, 0.35f));
    SpawnBox(FVector(160, -50, 50), FVector(0.3f, 0.04f, 0.4f), FLinearColor(0.85f, 0.6f, 0.35f));

    SpawnCylinder(FVector(30, -40, 42), FVector(0.12f, 0.12f, 0.01f), FLinearColor(0.98f, 0.97f, 0.95f));
    SpawnCylinder(FVector(70, -60, 45), FVector(0.04f, 0.04f, 0.06f), FLinearColor(0.85f, 0.6f, 0.7f));

    SpawnCharacter(FVector(-150, 200, 0), FLinearColor(0.92f, 0.78f, 0.65f), FLinearColor(0.15f, 0.1f, 0.08f), FLinearColor(0.95f, 0.85f, 0.3f), TEXT("Ava"));

    SpawnLight(FVector(0, 0, 350), 5000.0f, FLinearColor(1.0f, 0.95f, 0.85f));
    SpawnLight(FVector(-300, 300, 200), 2000.0f, FLinearColor(1.0f, 0.9f, 0.8f));
    SpawnDirectionalLight(FRotator(-45, 30, 0), 2.5f, FLinearColor(1.0f, 0.97f, 0.9f));
}

// ==================== BATHROOM ====================
void AEmersynGameMode::BuildBathroom()
{
    SetupCamera(FVector(0, -450, 300), FRotator(-25, 0, 0));

    SpawnBox(FVector(0, 0, -5), FVector(6, 6, 0.1f), FLinearColor(0.85f, 0.92f, 0.97f));
    SpawnBox(FVector(0, 300, 175), FVector(6, 0.1f, 3.5f), FLinearColor(0.82f, 0.93f, 0.97f));
    SpawnBox(FVector(-300, 0, 175), FVector(0.1f, 6, 3.5f), FLinearColor(0.84f, 0.94f, 0.98f));
    SpawnBox(FVector(300, 0, 175), FVector(0.1f, 6, 3.5f), FLinearColor(0.84f, 0.94f, 0.98f));

    SpawnBox(FVector(-150, 200, 30), FVector(1.5f, 0.7f, 0.5f), FLinearColor(0.97f, 0.97f, 0.98f));
    SpawnBox(FVector(-150, 200, 40), FVector(1.3f, 0.55f, 0.1f), FLinearColor(0.6f, 0.85f, 0.95f));
    SpawnSphere(FVector(-180, 180, 50), 0.08f, FLinearColor(0.95f, 0.95f, 0.98f));
    SpawnSphere(FVector(-140, 210, 52), 0.06f, FLinearColor(0.93f, 0.93f, 0.97f));
    SpawnSphere(FVector(-160, 190, 53), 0.1f, FLinearColor(0.96f, 0.96f, 0.99f));

    SpawnBox(FVector(200, 280, 50), FVector(0.5f, 0.3f, 0.06f), FLinearColor(0.97f, 0.97f, 0.98f));
    SpawnCylinder(FVector(200, 280, 25), FVector(0.08f, 0.08f, 0.5f), FLinearColor(0.92f, 0.92f, 0.93f));
    SpawnCylinder(FVector(200, 295, 60), FVector(0.02f, 0.02f, 0.12f), FLinearColor(0.8f, 0.8f, 0.82f));
    SpawnBox(FVector(200, 298, 120), FVector(0.5f, 0.02f, 0.6f), FLinearColor(0.75f, 0.85f, 0.9f));

    SpawnBox(FVector(200, -100, 20), FVector(0.3f, 0.35f, 0.35f), FLinearColor(0.97f, 0.97f, 0.98f));
    SpawnBox(FVector(200, -120, 45), FVector(0.28f, 0.08f, 0.35f), FLinearColor(0.96f, 0.96f, 0.97f));

    SpawnBox(FVector(-280, -100, 100), FVector(0.02f, 0.5f, 0.02f), FLinearColor(0.8f, 0.8f, 0.82f));
    SpawnBox(FVector(-280, -100, 80), FVector(0.04f, 0.4f, 0.25f), FLinearColor(0.95f, 0.7f, 0.75f));

    SpawnSphere(FVector(-120, 220, 50), 0.06f, FLinearColor(0.98f, 0.92f, 0.2f));
    SpawnSphere(FVector(-115, 216, 52), 0.03f, FLinearColor(0.95f, 0.6f, 0.15f));

    SpawnBox(FVector(-150, 50, 1), FVector(1.0f, 0.6f, 0.02f), FLinearColor(0.7f, 0.88f, 0.95f));

    SpawnCharacter(FVector(0, -50, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(0.7f, 0.85f, 0.95f), TEXT("Emersyn"));

    SpawnLight(FVector(0, 0, 300), 5000.0f, FLinearColor(0.95f, 0.97f, 1.0f));
    SpawnLight(FVector(200, 280, 150), 1500.0f, FLinearColor(1.0f, 0.98f, 0.95f));
    SpawnDirectionalLight(FRotator(-50, 20, 0), 2.0f, FLinearColor(0.95f, 0.97f, 1.0f));
}

// ==================== LIVING ROOM ====================
void AEmersynGameMode::BuildLivingRoom()
{
    SetupCamera(FVector(0, -600, 350), FRotator(-22, 0, 0));

    SpawnBox(FVector(0, 0, -5), FVector(10, 10, 0.1f), FLinearColor(0.82f, 0.65f, 0.45f));
    SpawnBox(FVector(0, 500, 200), FVector(10, 0.1f, 4), FLinearColor(0.95f, 0.92f, 0.85f));
    SpawnBox(FVector(-500, 0, 200), FVector(0.1f, 10, 4), FLinearColor(0.94f, 0.91f, 0.84f));
    SpawnBox(FVector(500, 0, 200), FVector(0.1f, 10, 4), FLinearColor(0.94f, 0.91f, 0.84f));

    SpawnBox(FVector(-200, 300, 25), FVector(2.5f, 0.6f, 0.4f), FLinearColor(0.55f, 0.65f, 0.8f));
    SpawnBox(FVector(-200, 340, 55), FVector(2.5f, 0.15f, 0.5f), FLinearColor(0.5f, 0.6f, 0.75f));
    SpawnBox(FVector(-430, 310, 35), FVector(0.15f, 0.4f, 0.3f), FLinearColor(0.52f, 0.62f, 0.77f));
    SpawnBox(FVector(30, 310, 35), FVector(0.15f, 0.4f, 0.3f), FLinearColor(0.52f, 0.62f, 0.77f));
    SpawnBox(FVector(-300, 300, 42), FVector(0.35f, 0.35f, 0.08f), FLinearColor(0.95f, 0.7f, 0.4f));
    SpawnBox(FVector(-100, 300, 42), FVector(0.35f, 0.35f, 0.08f), FLinearColor(0.6f, 0.85f, 0.65f));

    SpawnBox(FVector(-200, 100, 22), FVector(1.0f, 0.5f, 0.03f), FLinearColor(0.65f, 0.45f, 0.3f));
    SpawnCylinder(FVector(-280, 80, 10), FVector(0.04f, 0.04f, 0.2f), FLinearColor(0.55f, 0.38f, 0.25f));
    SpawnCylinder(FVector(-120, 80, 10), FVector(0.04f, 0.04f, 0.2f), FLinearColor(0.55f, 0.38f, 0.25f));
    SpawnCylinder(FVector(-280, 120, 10), FVector(0.04f, 0.04f, 0.2f), FLinearColor(0.55f, 0.38f, 0.25f));
    SpawnCylinder(FVector(-120, 120, 10), FVector(0.04f, 0.04f, 0.2f), FLinearColor(0.55f, 0.38f, 0.25f));

    SpawnBox(FVector(350, 200, 120), FVector(0.05f, 1.8f, 1.0f), FLinearColor(0.12f, 0.12f, 0.15f));
    SpawnBox(FVector(350, 200, 30), FVector(0.3f, 1.5f, 0.5f), FLinearColor(0.4f, 0.35f, 0.3f));
    SpawnBox(FVector(348, 200, 120), FVector(0.02f, 1.6f, 0.85f), FLinearColor(0.3f, 0.5f, 0.8f));

    SpawnBox(FVector(-480, 100, 100), FVector(0.15f, 0.8f, 2.0f), FLinearColor(0.7f, 0.5f, 0.35f));
    SpawnBox(FVector(-480, 80, 130), FVector(0.04f, 0.12f, 0.2f), FLinearColor(0.9f, 0.3f, 0.3f));
    SpawnBox(FVector(-480, 100, 130), FVector(0.04f, 0.1f, 0.18f), FLinearColor(0.3f, 0.7f, 0.4f));
    SpawnBox(FVector(-480, 120, 130), FVector(0.04f, 0.14f, 0.22f), FLinearColor(0.4f, 0.5f, 0.9f));

    SpawnCylinder(FVector(300, -200, 50), FVector(0.03f, 0.03f, 1.0f), FLinearColor(0.75f, 0.6f, 0.4f));
    SpawnSphere(FVector(300, -200, 110), 0.2f, FLinearColor(1.0f, 0.95f, 0.8f));

    SpawnBox(FVector(-200, 150, 1), FVector(3.0f, 2.0f, 0.02f), FLinearColor(0.85f, 0.75f, 0.6f));

    SpawnCharacter(FVector(100, -100, 0), FLinearColor(0.85f, 0.7f, 0.55f), FLinearColor(0.3f, 0.2f, 0.1f), FLinearColor(0.2f, 0.6f, 0.9f), TEXT("Leo"));
    SpawnPet(FVector(200, -50, 0), FLinearColor(0.7f, 0.5f, 0.3f), FLinearColor(0.4f, 0.25f, 0.15f), TEXT("Dog"));

    SpawnLight(FVector(0, 100, 350), 6000.0f, FLinearColor(1.0f, 0.95f, 0.88f));
    SpawnLight(FVector(300, -200, 110), 1500.0f, FLinearColor(1.0f, 0.92f, 0.75f));
    SpawnLight(FVector(348, 200, 120), 1000.0f, FLinearColor(0.4f, 0.6f, 0.9f));
    SpawnDirectionalLight(FRotator(-45, 30, 0), 2.0f, FLinearColor(1.0f, 0.97f, 0.92f));
}

// ==================== GARDEN ====================
void AEmersynGameMode::BuildGarden()
{
    SetupCamera(FVector(0, -700, 400), FRotator(-20, 0, 0));

    SpawnBox(FVector(0, 0, -5), FVector(15, 15, 0.1f), FLinearColor(0.35f, 0.72f, 0.3f));
    SpawnBox(FVector(0, 800, 300), FVector(15, 0.1f, 6), FLinearColor(0.55f, 0.78f, 0.95f));
    SpawnSphere(FVector(-400, 700, 500), 1.5f, FLinearColor(0.98f, 0.98f, 1.0f));
    SpawnSphere(FVector(-300, 700, 520), 1.2f, FLinearColor(0.97f, 0.97f, 0.99f));
    SpawnSphere(FVector(300, 700, 480), 1.8f, FLinearColor(0.98f, 0.98f, 1.0f));
    SpawnSphere(FVector(400, 700, 500), 1.3f, FLinearColor(0.97f, 0.97f, 0.99f));

    SpawnBox(FVector(0, 0, 1), FVector(1.0f, 8.0f, 0.02f), FLinearColor(0.8f, 0.72f, 0.55f));

    SpawnBox(FVector(-300, -200, 10), FVector(1.5f, 1.0f, 0.15f), FLinearColor(0.45f, 0.3f, 0.2f));
    SpawnSphere(FVector(-350, -230, 25), 0.1f, FLinearColor(1.0f, 0.3f, 0.4f));
    SpawnSphere(FVector(-330, -200, 28), 0.08f, FLinearColor(0.95f, 0.85f, 0.2f));
    SpawnSphere(FVector(-300, -220, 26), 0.09f, FLinearColor(0.9f, 0.4f, 0.8f));
    SpawnSphere(FVector(-270, -190, 27), 0.1f, FLinearColor(0.4f, 0.6f, 0.95f));
    SpawnSphere(FVector(-250, -230, 25), 0.08f, FLinearColor(1.0f, 0.6f, 0.3f));

    SpawnBox(FVector(300, -200, 10), FVector(1.5f, 1.0f, 0.15f), FLinearColor(0.45f, 0.3f, 0.2f));
    SpawnSphere(FVector(250, -230, 25), 0.1f, FLinearColor(0.95f, 0.5f, 0.6f));
    SpawnSphere(FVector(280, -200, 28), 0.08f, FLinearColor(0.5f, 0.8f, 0.95f));
    SpawnSphere(FVector(310, -220, 26), 0.09f, FLinearColor(1.0f, 0.75f, 0.85f));
    SpawnSphere(FVector(340, -190, 27), 0.1f, FLinearColor(0.85f, 0.95f, 0.4f));

    SpawnCylinder(FVector(-500, 300, 80), FVector(0.25f, 0.25f, 1.6f), FLinearColor(0.5f, 0.35f, 0.2f));
    SpawnSphere(FVector(-500, 300, 200), 1.5f, FLinearColor(0.2f, 0.6f, 0.2f));
    SpawnSphere(FVector(-480, 310, 230), 1.0f, FLinearColor(0.25f, 0.65f, 0.22f));

    SpawnCylinder(FVector(500, 400, 80), FVector(0.2f, 0.2f, 1.4f), FLinearColor(0.55f, 0.38f, 0.22f));
    SpawnSphere(FVector(500, 400, 190), 1.3f, FLinearColor(0.22f, 0.62f, 0.2f));

    for (int32 i = -5; i <= 5; i++)
    {
        SpawnBox(FVector(i * 120.0f, 500, 30), FVector(0.06f, 0.06f, 0.5f), FLinearColor(0.9f, 0.85f, 0.75f));
    }
    SpawnBox(FVector(0, 500, 50), FVector(6.5f, 0.04f, 0.06f), FLinearColor(0.88f, 0.83f, 0.73f));
    SpawnBox(FVector(0, 500, 20), FVector(6.5f, 0.04f, 0.06f), FLinearColor(0.88f, 0.83f, 0.73f));

    SpawnBox(FVector(-200, 100, 22), FVector(0.8f, 0.25f, 0.04f), FLinearColor(0.6f, 0.42f, 0.28f));
    SpawnBox(FVector(-200, 115, 40), FVector(0.8f, 0.04f, 0.25f), FLinearColor(0.58f, 0.4f, 0.26f));

    SpawnCylinder(FVector(150, 100, 12), FVector(0.08f, 0.08f, 0.12f), FLinearColor(0.3f, 0.7f, 0.4f));

    SpawnSphere(FVector(-100, -100, 100), 0.05f, FLinearColor(0.95f, 0.6f, 0.8f));
    SpawnSphere(FVector(200, 50, 120), 0.04f, FLinearColor(0.6f, 0.8f, 0.95f));

    SpawnCharacter(FVector(0, -100, 0), FLinearColor(0.9f, 0.75f, 0.6f), FLinearColor(0.7f, 0.3f, 0.15f), FLinearColor(0.3f, 0.85f, 0.5f), TEXT("Mia"));
    SpawnPet(FVector(100, -50, 0), FLinearColor(0.95f, 0.95f, 0.92f), FLinearColor(0.95f, 0.7f, 0.75f), TEXT("Bunny"));

    SpawnDirectionalLight(FRotator(-50, 30, 0), 4.0f, FLinearColor(1.0f, 0.97f, 0.88f));
    SpawnLight(FVector(0, 0, 500), 8000.0f, FLinearColor(1.0f, 0.98f, 0.9f));
}

// ==================== SCHOOL ====================
void AEmersynGameMode::BuildSchool()
{
    SetupCamera(FVector(0, -500, 350), FRotator(-25, 0, 0));

    SpawnBox(FVector(0, 0, -5), FVector(8, 8, 0.1f), FLinearColor(0.78f, 0.62f, 0.42f));
    SpawnBox(FVector(0, 400, 200), FVector(8, 0.1f, 4), FLinearColor(0.97f, 0.94f, 0.85f));
    SpawnBox(FVector(-400, 0, 200), FVector(0.1f, 8, 4), FLinearColor(0.96f, 0.93f, 0.84f));
    SpawnBox(FVector(400, 0, 200), FVector(0.1f, 8, 4), FLinearColor(0.96f, 0.93f, 0.84f));

    SpawnBox(FVector(0, 395, 180), FVector(3.0f, 0.03f, 1.2f), FLinearColor(0.15f, 0.3f, 0.2f));
    SpawnBox(FVector(0, 393, 180), FVector(3.1f, 0.02f, 0.04f), FLinearColor(0.6f, 0.45f, 0.3f));
    SpawnBox(FVector(0, 393, 240), FVector(3.1f, 0.02f, 0.04f), FLinearColor(0.6f, 0.45f, 0.3f));
    SpawnBox(FVector(0, 390, 118), FVector(3.0f, 0.06f, 0.04f), FLinearColor(0.6f, 0.45f, 0.3f));
    SpawnBox(FVector(-20, 388, 121), FVector(0.08f, 0.02f, 0.02f), FLinearColor(0.95f, 0.95f, 0.92f));

    for (int32 row = 0; row < 3; row++)
    {
        for (int32 col = 0; col < 2; col++)
        {
            float X = -150.0f + col * 300.0f;
            float Y = -200.0f + row * 150.0f;
            SpawnBox(FVector(X, Y, 35), FVector(0.7f, 0.5f, 0.04f), FLinearColor(0.75f, 0.58f, 0.38f));
            SpawnCylinder(FVector(X - 30, Y - 20, 16), FVector(0.03f, 0.03f, 0.3f), FLinearColor(0.4f, 0.4f, 0.42f));
            SpawnCylinder(FVector(X + 30, Y + 20, 16), FVector(0.03f, 0.03f, 0.3f), FLinearColor(0.4f, 0.4f, 0.42f));
            SpawnBox(FVector(X, Y - 40, 20), FVector(0.25f, 0.25f, 0.04f), FLinearColor(0.3f, 0.5f, 0.8f));
        }
    }

    SpawnBox(FVector(0, 250, 40), FVector(1.2f, 0.5f, 0.7f), FLinearColor(0.55f, 0.4f, 0.28f));
    SpawnSphere(FVector(30, 240, 76), 0.06f, FLinearColor(0.9f, 0.15f, 0.15f));

    SpawnSphere(FVector(-300, 250, 80), 0.15f, FLinearColor(0.3f, 0.5f, 0.8f));
    SpawnCylinder(FVector(-300, 250, 55), FVector(0.03f, 0.03f, 0.2f), FLinearColor(0.5f, 0.4f, 0.3f));

    SpawnCylinder(FVector(300, 394, 300), FVector(0.2f, 0.2f, 0.02f), FLinearColor(0.95f, 0.93f, 0.88f));

    SpawnCharacter(FVector(0, 150, 0), FLinearColor(0.88f, 0.72f, 0.55f), FLinearColor(0.25f, 0.15f, 0.1f), FLinearColor(0.35f, 0.55f, 0.35f), TEXT("Teacher"));

    SpawnLight(FVector(0, 0, 350), 5000.0f, FLinearColor(1.0f, 0.97f, 0.92f));
    SpawnLight(FVector(0, 350, 200), 2000.0f, FLinearColor(1.0f, 0.95f, 0.88f));
    SpawnDirectionalLight(FRotator(-45, 20, 0), 2.0f, FLinearColor(1.0f, 0.98f, 0.93f));
}

// ==================== SHOP ====================
void AEmersynGameMode::BuildShop()
{
    SetupCamera(FVector(0, -500, 350), FRotator(-25, 0, 0));

    SpawnBox(FVector(0, 0, -5), FVector(8, 8, 0.1f), FLinearColor(0.92f, 0.88f, 0.82f));
    SpawnBox(FVector(0, 400, 200), FVector(8, 0.1f, 4), FLinearColor(0.97f, 0.88f, 0.9f));
    SpawnBox(FVector(-400, 0, 200), FVector(0.1f, 8, 4), FLinearColor(0.96f, 0.87f, 0.89f));
    SpawnBox(FVector(400, 0, 200), FVector(0.1f, 8, 4), FLinearColor(0.96f, 0.87f, 0.89f));

    SpawnBox(FVector(-380, 0, 80), FVector(0.15f, 3.0f, 0.04f), FLinearColor(0.9f, 0.82f, 0.75f));
    SpawnBox(FVector(-380, 0, 140), FVector(0.15f, 3.0f, 0.04f), FLinearColor(0.9f, 0.82f, 0.75f));
    SpawnBox(FVector(-380, 0, 200), FVector(0.15f, 3.0f, 0.04f), FLinearColor(0.9f, 0.82f, 0.75f));
    SpawnSphere(FVector(-375, -100, 90), 0.08f, FLinearColor(0.95f, 0.4f, 0.5f));
    SpawnBox(FVector(-375, -50, 88), FVector(0.06f, 0.06f, 0.1f), FLinearColor(0.4f, 0.6f, 0.95f));
    SpawnSphere(FVector(-375, 0, 90), 0.07f, FLinearColor(0.95f, 0.85f, 0.3f));
    SpawnBox(FVector(-375, 50, 88), FVector(0.08f, 0.05f, 0.08f), FLinearColor(0.5f, 0.85f, 0.5f));
    SpawnSphere(FVector(-375, -80, 150), 0.09f, FLinearColor(0.9f, 0.6f, 0.85f));
    SpawnBox(FVector(-375, -20, 148), FVector(0.07f, 0.07f, 0.1f), FLinearColor(0.95f, 0.7f, 0.3f));
    SpawnSphere(FVector(-375, 40, 150), 0.08f, FLinearColor(0.4f, 0.85f, 0.9f));

    SpawnBox(FVector(200, 300, 50), FVector(1.5f, 0.4f, 1.0f), FLinearColor(0.88f, 0.78f, 0.7f));
    SpawnBox(FVector(200, 290, 105), FVector(0.2f, 0.15f, 0.15f), FLinearColor(0.85f, 0.6f, 0.7f));

    SpawnBox(FVector(0, 0, 30), FVector(1.0f, 0.8f, 0.5f), FLinearColor(0.92f, 0.85f, 0.8f));
    SpawnSphere(FVector(-30, -20, 60), 0.1f, FLinearColor(0.95f, 0.7f, 0.8f));
    SpawnBox(FVector(30, 10, 58), FVector(0.08f, 0.08f, 0.12f), FLinearColor(0.7f, 0.85f, 0.95f));
    SpawnSphere(FVector(0, -10, 62), 0.12f, FLinearColor(0.85f, 0.55f, 0.3f));

    SpawnCharacter(FVector(200, 200, 0), FLinearColor(0.9f, 0.78f, 0.65f), FLinearColor(0.4f, 0.3f, 0.2f), FLinearColor(0.85f, 0.45f, 0.55f), TEXT("Shopkeeper"));

    SpawnLight(FVector(0, 0, 350), 5000.0f, FLinearColor(1.0f, 0.95f, 0.9f));
    SpawnLight(FVector(200, 300, 200), 2000.0f, FLinearColor(1.0f, 0.9f, 0.85f));
    SpawnDirectionalLight(FRotator(-45, 30, 0), 2.5f, FLinearColor(1.0f, 0.96f, 0.9f));
}

// ==================== PLAYGROUND ====================
void AEmersynGameMode::BuildPlayground()
{
    SetupCamera(FVector(0, -700, 400), FRotator(-20, 0, 0));

    SpawnBox(FVector(0, 0, -5), FVector(15, 15, 0.1f), FLinearColor(0.4f, 0.72f, 0.32f));
    SpawnBox(FVector(0, 0, -3), FVector(5, 5, 0.08f), FLinearColor(0.78f, 0.65f, 0.45f));
    SpawnBox(FVector(0, 800, 300), FVector(15, 0.1f, 6), FLinearColor(0.55f, 0.78f, 0.95f));

    SpawnCylinder(FVector(-300, 200, 100), FVector(0.12f, 0.12f, 2.0f), FLinearColor(0.9f, 0.25f, 0.3f));
    SpawnCylinder(FVector(-250, 200, 100), FVector(0.12f, 0.12f, 2.0f), FLinearColor(0.9f, 0.25f, 0.3f));
    SpawnBox(FVector(-275, 200, 200), FVector(0.6f, 0.6f, 0.05f), FLinearColor(0.95f, 0.85f, 0.3f));
    SpawnBox(FVector(-275, 80, 100), FVector(0.35f, 1.2f, 0.04f), FLinearColor(0.95f, 0.7f, 0.15f));

    SpawnCylinder(FVector(200, -100, 120), FVector(0.08f, 0.08f, 2.4f), FLinearColor(0.3f, 0.55f, 0.85f));
    SpawnCylinder(FVector(400, -100, 120), FVector(0.08f, 0.08f, 2.4f), FLinearColor(0.3f, 0.55f, 0.85f));
    SpawnBox(FVector(300, -100, 240), FVector(2.2f, 0.06f, 0.06f), FLinearColor(0.3f, 0.55f, 0.85f));
    SpawnBox(FVector(260, -100, 40), FVector(0.2f, 0.15f, 0.03f), FLinearColor(0.9f, 0.3f, 0.4f));
    SpawnBox(FVector(340, -100, 50), FVector(0.2f, 0.15f, 0.03f), FLinearColor(0.3f, 0.85f, 0.45f));

    SpawnBox(FVector(0, 300, 10), FVector(2.0f, 2.0f, 0.2f), FLinearColor(0.92f, 0.85f, 0.6f));
    SpawnBox(FVector(0, 300, 15), FVector(1.8f, 1.8f, 0.1f), FLinearColor(0.95f, 0.9f, 0.7f));
    SpawnCylinder(FVector(-20, 280, 25), FVector(0.1f, 0.1f, 0.12f), FLinearColor(0.92f, 0.85f, 0.6f));
    SpawnCylinder(FVector(20, 310, 23), FVector(0.08f, 0.08f, 0.1f), FLinearColor(0.9f, 0.83f, 0.58f));

    SpawnCylinder(FVector(-100, -200, 15), FVector(0.1f, 0.1f, 0.25f), FLinearColor(0.5f, 0.5f, 0.52f));
    SpawnBox(FVector(-100, -200, 35), FVector(1.5f, 0.15f, 0.04f), FLinearColor(0.4f, 0.75f, 0.85f));

    SpawnSphere(FVector(400, 300, 50), 1.0f, FLinearColor(0.95f, 0.5f, 0.6f));

    SpawnCharacter(FVector(50, -50, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(0.95f, 0.65f, 0.2f), TEXT("Emersyn"));
    SpawnCharacter(FVector(-150, 0, 0), FLinearColor(0.85f, 0.7f, 0.55f), FLinearColor(0.3f, 0.2f, 0.1f), FLinearColor(0.2f, 0.7f, 0.5f), TEXT("Leo"), 0.9f);

    SpawnDirectionalLight(FRotator(-50, 30, 0), 4.0f, FLinearColor(1.0f, 0.97f, 0.88f));
    SpawnLight(FVector(0, 0, 500), 8000.0f, FLinearColor(1.0f, 0.98f, 0.92f));
}

// ==================== PARK ====================
void AEmersynGameMode::BuildPark()
{
    SetupCamera(FVector(0, -700, 400), FRotator(-20, 0, 0));

    SpawnBox(FVector(0, 0, -5), FVector(20, 20, 0.1f), FLinearColor(0.32f, 0.7f, 0.28f));
    SpawnBox(FVector(0, 0, -2), FVector(1.0f, 12.0f, 0.05f), FLinearColor(0.78f, 0.68f, 0.52f));
    SpawnBox(FVector(0, 0, -2), FVector(12.0f, 1.0f, 0.05f), FLinearColor(0.78f, 0.68f, 0.52f));
    SpawnBox(FVector(0, 1000, 300), FVector(20, 0.1f, 6), FLinearColor(0.5f, 0.75f, 0.95f));

    SpawnCylinder(FVector(0, 0, 20), FVector(0.8f, 0.8f, 0.3f), FLinearColor(0.8f, 0.78f, 0.75f));
    SpawnCylinder(FVector(0, 0, 35), FVector(0.5f, 0.5f, 0.2f), FLinearColor(0.82f, 0.8f, 0.77f));
    SpawnCylinder(FVector(0, 0, 28), FVector(0.7f, 0.7f, 0.05f), FLinearColor(0.5f, 0.75f, 0.92f));
    SpawnSphere(FVector(0, 0, 55), 0.08f, FLinearColor(0.7f, 0.88f, 0.97f));

    SpawnCylinder(FVector(-500, -400, 80), FVector(0.3f, 0.3f, 1.6f), FLinearColor(0.5f, 0.35f, 0.2f));
    SpawnSphere(FVector(-500, -400, 220), 2.0f, FLinearColor(0.18f, 0.55f, 0.18f));
    SpawnCylinder(FVector(600, 500, 80), FVector(0.35f, 0.35f, 1.8f), FLinearColor(0.48f, 0.33f, 0.18f));
    SpawnSphere(FVector(600, 500, 240), 2.2f, FLinearColor(0.2f, 0.58f, 0.2f));
    SpawnCylinder(FVector(-400, 400, 70), FVector(0.25f, 0.25f, 1.4f), FLinearColor(0.52f, 0.37f, 0.22f));
    SpawnSphere(FVector(-400, 400, 190), 1.5f, FLinearColor(0.22f, 0.6f, 0.22f));

    SpawnBox(FVector(-200, 150, 22), FVector(0.8f, 0.25f, 0.04f), FLinearColor(0.55f, 0.4f, 0.25f));
    SpawnBox(FVector(-200, 165, 40), FVector(0.8f, 0.04f, 0.25f), FLinearColor(0.53f, 0.38f, 0.23f));
    SpawnBox(FVector(200, -150, 22), FVector(0.8f, 0.25f, 0.04f), FLinearColor(0.55f, 0.4f, 0.25f));
    SpawnBox(FVector(200, -135, 40), FVector(0.8f, 0.04f, 0.25f), FLinearColor(0.53f, 0.38f, 0.23f));

    SpawnBox(FVector(400, -300, -2), FVector(2.0f, 1.5f, 0.05f), FLinearColor(0.35f, 0.6f, 0.85f));
    SpawnCylinder(FVector(380, -280, 0), FVector(0.08f, 0.08f, 0.01f), FLinearColor(0.3f, 0.7f, 0.35f));
    SpawnCylinder(FVector(420, -310, 0), FVector(0.06f, 0.06f, 0.01f), FLinearColor(0.28f, 0.68f, 0.33f));

    for (int32 i = 0; i < 6; i++)
    {
        float Angle = i * 60.0f * PI / 180.0f;
        SpawnSphere(FVector(FMath::Cos(Angle) * 150.0f - 300.0f, FMath::Sin(Angle) * 150.0f + 200.0f, 10), 0.06f, FLinearColor(0.95f, 0.4f + i * 0.1f, 0.5f));
    }

    SpawnCharacter(FVector(-100, -100, 0), FLinearColor(0.9f, 0.75f, 0.6f), FLinearColor(0.7f, 0.3f, 0.15f), FLinearColor(0.4f, 0.75f, 0.9f), TEXT("Mia"));
    SpawnPet(FVector(0, -50, 0), FLinearColor(0.7f, 0.5f, 0.3f), FLinearColor(0.4f, 0.25f, 0.15f), TEXT("Dog"));

    SpawnDirectionalLight(FRotator(-50, 30, 0), 4.5f, FLinearColor(1.0f, 0.97f, 0.88f));
    SpawnLight(FVector(0, 0, 600), 10000.0f, FLinearColor(1.0f, 0.98f, 0.92f));
}

// ==================== MALL ====================
void AEmersynGameMode::BuildMall()
{
    SetupCamera(FVector(0, -600, 400), FRotator(-25, 0, 0));

    SpawnBox(FVector(0, 0, -5), FVector(12, 12, 0.1f), FLinearColor(0.95f, 0.93f, 0.9f));
    SpawnBox(FVector(0, 600, 250), FVector(12, 0.1f, 5), FLinearColor(0.97f, 0.95f, 0.93f));
    SpawnBox(FVector(-600, 0, 250), FVector(0.1f, 12, 5), FLinearColor(0.96f, 0.94f, 0.92f));
    SpawnBox(FVector(600, 0, 250), FVector(0.1f, 12, 5), FLinearColor(0.96f, 0.94f, 0.92f));

    SpawnBox(FVector(-580, -200, 120), FVector(0.12f, 2.0f, 2.0f), FLinearColor(0.95f, 0.6f, 0.7f));
    SpawnBox(FVector(-580, 200, 120), FVector(0.12f, 2.0f, 2.0f), FLinearColor(0.6f, 0.75f, 0.95f));
    SpawnBox(FVector(580, -200, 120), FVector(0.12f, 2.0f, 2.0f), FLinearColor(0.95f, 0.88f, 0.4f));
    SpawnBox(FVector(580, 200, 120), FVector(0.12f, 2.0f, 2.0f), FLinearColor(0.5f, 0.9f, 0.6f));

    SpawnBox(FVector(-580, -200, 230), FVector(0.08f, 1.5f, 0.3f), FLinearColor(1.0f, 0.5f, 0.6f));
    SpawnBox(FVector(-580, 200, 230), FVector(0.08f, 1.5f, 0.3f), FLinearColor(0.5f, 0.65f, 0.95f));
    SpawnBox(FVector(580, -200, 230), FVector(0.08f, 1.5f, 0.3f), FLinearColor(0.95f, 0.82f, 0.3f));
    SpawnBox(FVector(580, 200, 230), FVector(0.08f, 1.5f, 0.3f), FLinearColor(0.4f, 0.85f, 0.5f));

    SpawnCylinder(FVector(0, 0, 25), FVector(1.0f, 1.0f, 0.4f), FLinearColor(0.82f, 0.8f, 0.77f));
    SpawnCylinder(FVector(0, 0, 30), FVector(0.8f, 0.8f, 0.08f), FLinearColor(0.5f, 0.72f, 0.9f));
    SpawnCylinder(FVector(0, 0, 45), FVector(0.3f, 0.3f, 0.5f), FLinearColor(0.85f, 0.83f, 0.8f));
    SpawnSphere(FVector(0, 0, 70), 0.1f, FLinearColor(0.6f, 0.82f, 0.95f));

    SpawnBox(FVector(0, 400, 80), FVector(0.8f, 2.0f, 0.08f), FLinearColor(0.7f, 0.7f, 0.72f));
    SpawnBox(FVector(-40, 400, 90), FVector(0.04f, 2.0f, 0.15f), FLinearColor(0.6f, 0.6f, 0.62f));
    SpawnBox(FVector(40, 400, 90), FVector(0.04f, 2.0f, 0.15f), FLinearColor(0.6f, 0.6f, 0.62f));

    SpawnBox(FVector(-300, 0, 20), FVector(0.4f, 0.4f, 0.3f), FLinearColor(0.65f, 0.55f, 0.4f));
    SpawnSphere(FVector(-300, 0, 50), 0.3f, FLinearColor(0.25f, 0.6f, 0.25f));
    SpawnBox(FVector(300, 0, 20), FVector(0.4f, 0.4f, 0.3f), FLinearColor(0.65f, 0.55f, 0.4f));
    SpawnSphere(FVector(300, 0, 50), 0.3f, FLinearColor(0.25f, 0.6f, 0.25f));

    SpawnCharacter(FVector(-100, -200, 0), FLinearColor(0.92f, 0.78f, 0.65f), FLinearColor(0.15f, 0.1f, 0.08f), FLinearColor(0.85f, 0.5f, 0.85f), TEXT("Ava"));
    SpawnCharacter(FVector(100, -150, 0), FLinearColor(0.9f, 0.75f, 0.6f), FLinearColor(0.7f, 0.3f, 0.15f), FLinearColor(0.4f, 0.7f, 0.9f), TEXT("Mia"));

    SpawnLight(FVector(0, 0, 450), 8000.0f, FLinearColor(1.0f, 0.98f, 0.95f));
    SpawnLight(FVector(-400, 0, 250), 3000.0f, FLinearColor(0.95f, 0.9f, 0.85f));
    SpawnLight(FVector(400, 0, 250), 3000.0f, FLinearColor(0.95f, 0.9f, 0.85f));
    SpawnDirectionalLight(FRotator(-45, 30, 0), 2.5f, FLinearColor(1.0f, 0.98f, 0.95f));
}

// ==================== ARCADE ====================
void AEmersynGameMode::BuildArcade()
{
    SetupCamera(FVector(0, -450, 300), FRotator(-25, 0, 0));

    SpawnBox(FVector(0, 0, -5), FVector(8, 8, 0.1f), FLinearColor(0.1f, 0.08f, 0.15f));
    SpawnBox(FVector(0, 400, 175), FVector(8, 0.1f, 3.5f), FLinearColor(0.12f, 0.08f, 0.2f));
    SpawnBox(FVector(-400, 0, 175), FVector(0.1f, 8, 3.5f), FLinearColor(0.15f, 0.08f, 0.2f));
    SpawnBox(FVector(400, 0, 175), FVector(0.1f, 8, 3.5f), FLinearColor(0.15f, 0.08f, 0.2f));

    SpawnBox(FVector(0, 398, 300), FVector(7.5f, 0.02f, 0.06f), FLinearColor(0.95f, 0.1f, 0.5f));
    SpawnBox(FVector(0, 398, 50), FVector(7.5f, 0.02f, 0.06f), FLinearColor(0.1f, 0.5f, 0.95f));
    SpawnBox(FVector(-398, 0, 300), FVector(0.02f, 7.5f, 0.06f), FLinearColor(0.1f, 0.95f, 0.5f));
    SpawnBox(FVector(398, 0, 300), FVector(0.02f, 7.5f, 0.06f), FLinearColor(0.95f, 0.85f, 0.1f));

    FLinearColor CabinetColors[] = {
        FLinearColor(0.95f, 0.15f, 0.2f), FLinearColor(0.15f, 0.3f, 0.95f),
        FLinearColor(0.15f, 0.9f, 0.3f), FLinearColor(0.95f, 0.8f, 0.1f),
        FLinearColor(0.9f, 0.3f, 0.9f), FLinearColor(0.1f, 0.85f, 0.9f)
    };
    for (int32 i = 0; i < 6; i++)
    {
        float X = (i < 3) ? -250.0f : 250.0f;
        float Y = -200.0f + (i % 3) * 200.0f;
        SpawnBox(FVector(X, Y, 70), FVector(0.4f, 0.3f, 1.3f), CabinetColors[i]);
        SpawnBox(FVector(X + (i < 3 ? 22 : -22), Y, 100), FVector(0.02f, 0.2f, 0.3f), FLinearColor(0.1f, 0.15f, 0.2f));
        SpawnBox(FVector(X + (i < 3 ? 21 : -21), Y, 100), FVector(0.01f, 0.18f, 0.28f), CabinetColors[i] * 0.3f + FLinearColor(0.1f, 0.1f, 0.1f));
    }

    SpawnBox(FVector(0, 300, 80), FVector(0.5f, 0.5f, 1.5f), FLinearColor(0.95f, 0.6f, 0.7f));
    SpawnBox(FVector(0, 300, 110), FVector(0.45f, 0.45f, 0.7f), FLinearColor(0.7f, 0.85f, 0.9f));
    SpawnSphere(FVector(-10, 290, 60), 0.06f, FLinearColor(0.95f, 0.5f, 0.5f));
    SpawnSphere(FVector(10, 310, 58), 0.05f, FLinearColor(0.5f, 0.7f, 0.95f));
    SpawnSphere(FVector(5, 295, 62), 0.07f, FLinearColor(0.95f, 0.9f, 0.3f));

    SpawnBox(FVector(350, 300, 50), FVector(0.4f, 0.8f, 1.0f), FLinearColor(0.9f, 0.3f, 0.5f));

    SpawnBox(FVector(-150, -100, -2), FVector(0.8f, 0.8f, 0.02f), FLinearColor(0.15f, 0.1f, 0.25f));
    SpawnBox(FVector(150, 100, -2), FVector(0.8f, 0.8f, 0.02f), FLinearColor(0.2f, 0.1f, 0.2f));

    SpawnCharacter(FVector(0, -100, 0), FLinearColor(0.85f, 0.7f, 0.55f), FLinearColor(0.3f, 0.2f, 0.1f), FLinearColor(0.95f, 0.5f, 0.2f), TEXT("Leo"));
    SpawnCharacter(FVector(-100, 50, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(0.6f, 0.3f, 0.9f), TEXT("Emersyn"), 0.95f);

    SpawnLight(FVector(-200, -200, 280), 2000.0f, FLinearColor(0.95f, 0.1f, 0.5f));
    SpawnLight(FVector(200, 200, 280), 2000.0f, FLinearColor(0.1f, 0.5f, 0.95f));
    SpawnLight(FVector(0, 0, 300), 3000.0f, FLinearColor(0.7f, 0.3f, 0.9f));
    SpawnLight(FVector(-300, 200, 150), 1500.0f, FLinearColor(0.1f, 0.9f, 0.4f));
    SpawnLight(FVector(300, -200, 150), 1500.0f, FLinearColor(0.95f, 0.8f, 0.1f));
}

// ==================== AMUSEMENT PARK ====================
void AEmersynGameMode::BuildAmusementPark()
{
    SetupCamera(FVector(0, -800, 450), FRotator(-20, 0, 0));

    SpawnBox(FVector(0, 0, -5), FVector(20, 20, 0.1f), FLinearColor(0.65f, 0.6f, 0.52f));
    SpawnBox(FVector(0, 0, -2), FVector(1.2f, 15.0f, 0.05f), FLinearColor(0.75f, 0.68f, 0.58f));
    SpawnBox(FVector(0, 1000, 350), FVector(20, 0.1f, 7), FLinearColor(0.45f, 0.7f, 0.95f));
    SpawnSphere(FVector(-600, 900, 500), 2.0f, FLinearColor(0.98f, 0.85f, 0.75f));
    SpawnSphere(FVector(500, 900, 550), 2.5f, FLinearColor(0.97f, 0.82f, 0.72f));

    // Ferris wheel
    SpawnCylinder(FVector(-500, 300, 50), FVector(0.3f, 0.3f, 1.0f), FLinearColor(0.7f, 0.2f, 0.25f));
    float WheelRadius = 200.0f;
    FVector WheelCenter(-500, 300, 280);
    for (int32 i = 0; i < 12; i++)
    {
        float Angle = i * 30.0f * PI / 180.0f;
        FVector Pos = WheelCenter + FVector(0, FMath::Cos(Angle) * WheelRadius, FMath::Sin(Angle) * WheelRadius);
        SpawnBox(Pos, FVector(0.02f, 0.02f, 0.15f), FLinearColor(0.8f, 0.3f, 0.35f));
        SpawnBox(Pos - FVector(0, 0, 15), FVector(0.1f, 0.08f, 0.08f), FLinearColor(0.95f, 0.85f, 0.3f + i * 0.03f));
    }
    SpawnSphere(WheelCenter, 0.2f, FLinearColor(0.85f, 0.25f, 0.3f));

    // Carousel
    SpawnCylinder(FVector(300, 200, 20), FVector(1.5f, 1.5f, 0.2f), FLinearColor(0.95f, 0.85f, 0.35f));
    SpawnCylinder(FVector(300, 200, 100), FVector(0.15f, 0.15f, 1.0f), FLinearColor(0.85f, 0.6f, 0.25f));
    SpawnCylinder(FVector(300, 200, 120), FVector(1.6f, 1.6f, 0.08f), FLinearColor(0.95f, 0.4f, 0.55f));
    SpawnSphere(FVector(350, 250, 50), 0.12f, FLinearColor(0.95f, 0.95f, 0.92f));
    SpawnSphere(FVector(250, 150, 60), 0.12f, FLinearColor(0.85f, 0.6f, 0.4f));
    SpawnSphere(FVector(350, 150, 55), 0.12f, FLinearColor(0.7f, 0.7f, 0.72f));
    SpawnSphere(FVector(250, 250, 58), 0.12f, FLinearColor(0.4f, 0.3f, 0.25f));

    // Roller coaster
    SpawnBox(FVector(0, -400, 80), FVector(6.0f, 0.15f, 0.05f), FLinearColor(0.5f, 0.5f, 0.55f));
    SpawnBox(FVector(-200, -400, 120), FVector(2.0f, 0.15f, 0.05f), FLinearColor(0.5f, 0.5f, 0.55f));
    SpawnCylinder(FVector(-300, -400, 40), FVector(0.08f, 0.08f, 0.8f), FLinearColor(0.55f, 0.55f, 0.58f));
    SpawnCylinder(FVector(0, -400, 40), FVector(0.06f, 0.06f, 0.8f), FLinearColor(0.55f, 0.55f, 0.58f));
    SpawnCylinder(FVector(300, -400, 40), FVector(0.08f, 0.08f, 0.8f), FLinearColor(0.55f, 0.55f, 0.58f));

    // Food stands
    SpawnBox(FVector(-200, 600, 50), FVector(0.8f, 0.5f, 1.0f), FLinearColor(0.95f, 0.55f, 0.2f));
    SpawnBox(FVector(-200, 575, 110), FVector(0.9f, 0.6f, 0.04f), FLinearColor(0.95f, 0.3f, 0.3f));
    SpawnBox(FVector(200, 600, 35), FVector(0.5f, 0.3f, 0.6f), FLinearColor(0.85f, 0.9f, 0.95f));
    SpawnSphere(FVector(200, 580, 75), 0.1f, FLinearColor(0.95f, 0.7f, 0.8f));

    // Ticket booth
    SpawnBox(FVector(0, 700, 60), FVector(0.6f, 0.4f, 1.2f), FLinearColor(0.3f, 0.55f, 0.9f));
    SpawnBox(FVector(0, 700, 130), FVector(0.7f, 0.5f, 0.04f), FLinearColor(0.95f, 0.85f, 0.3f));

    // Balloons
    SpawnSphere(FVector(100, 0, 200), 0.1f, FLinearColor(0.95f, 0.2f, 0.3f));
    SpawnSphere(FVector(120, 10, 210), 0.1f, FLinearColor(0.3f, 0.7f, 0.95f));
    SpawnSphere(FVector(80, -10, 195), 0.1f, FLinearColor(0.95f, 0.85f, 0.2f));

    SpawnCharacter(FVector(-50, -100, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(0.95f, 0.4f, 0.6f), TEXT("Emersyn"));
    SpawnCharacter(FVector(100, -50, 0), FLinearColor(0.92f, 0.78f, 0.65f), FLinearColor(0.15f, 0.1f, 0.08f), FLinearColor(0.85f, 0.5f, 0.85f), TEXT("Ava"));
    SpawnPet(FVector(-150, -50, 0), FLinearColor(0.9f, 0.6f, 0.3f), FLinearColor(0.95f, 0.8f, 0.6f), TEXT("Cat"));

    SpawnDirectionalLight(FRotator(-40, 30, 0), 4.0f, FLinearColor(1.0f, 0.92f, 0.82f));
    SpawnLight(FVector(0, 0, 600), 10000.0f, FLinearColor(1.0f, 0.95f, 0.88f));
    SpawnLight(FVector(-500, 300, 300), 3000.0f, FLinearColor(0.95f, 0.8f, 0.4f));
    SpawnLight(FVector(300, 200, 150), 3000.0f, FLinearColor(0.95f, 0.5f, 0.6f));
}
