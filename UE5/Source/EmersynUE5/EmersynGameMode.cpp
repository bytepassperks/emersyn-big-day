// v12: Complete vertex color fix - destroy default map floor, all geometry colored
// Fixes: v11 showed colored walls but checkerboard floor (default map ground plane)
// Solution: Destroy default StaticMeshActors + larger PMC floors + sky background
#include "EmersynGameMode.h"
#include "Engine/Engine.h"
#include "Materials/Material.h"
#include "EmersynHUD.h"
#include "Engine/World.h"
#include "ProceduralMeshComponent.h"
#include "Engine/PointLight.h"
#include "Components/PointLightComponent.h"
#include "Engine/DirectionalLight.h"
#include "Components/DirectionalLightComponent.h"
#include "UObject/ConstructorHelpers.h"
#include "Kismet/GameplayStatics.h"
#include "Camera/CameraActor.h"
#include "Camera/CameraComponent.h"
#include "GameFramework/PlayerController.h"
#include "Engine/StaticMeshActor.h"

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
    UE_LOG(LogTemp, Log, TEXT("EmersynGameMode v12: InitGame - Full color fix"));
}

void AEmersynGameMode::BeginPlay()
{
    Super::BeginPlay();

    UWorld* World = GetWorld();

    // ======================================================================
    // v12 FIX #1: DESTROY ALL DEFAULT MAP ACTORS (floor, sky sphere, fog, etc.)
    // The default UE5 map includes a StaticMeshActor floor with WorldGridMaterial
    // (checkerboard pattern). We must destroy it so only our colored PMC geometry shows.
    // ======================================================================
    if (World)
    {
        TArray<AActor*> StaticMeshActors;
        UGameplayStatics::GetAllActorsOfClass(World, AStaticMeshActor::StaticClass(), StaticMeshActors);
        
        for (AActor* Actor : StaticMeshActors)
        {
            if (Actor && IsValid(Actor))
            {
                UE_LOG(LogTemp, Display, TEXT("v12: Destroying default map StaticMeshActor: %s"), *Actor->GetName());
                Actor->Destroy();
            }
        }
        
        UE_LOG(LogTemp, Display, TEXT("v12: Destroyed %d default StaticMeshActors"), StaticMeshActors.Num());
    }

    // ======================================================================
    // v12 FIX #2: Load vertex color material (same as v11 - working!)
    // ======================================================================
    VertexColorMaterial = nullptr;

    // Try 1: Our custom M_VertexColor created by commandlet (VertexColor->EmissiveColor, Unlit)
    VertexColorMaterial = Cast<UMaterialInterface>(StaticLoadObject(
        UMaterialInterface::StaticClass(), nullptr,
        TEXT("/Game/Materials/M_VertexColor")));

    if (VertexColorMaterial)
    {
        UE_LOG(LogTemp, Display, TEXT("v12: Custom M_VertexColor loaded from /Game/Materials/"));
    }

    // Try 2: Engine built-in vertex color material
    if (!VertexColorMaterial)
    {
        UE_LOG(LogTemp, Warning, TEXT("v12: Custom material not found, trying engine VertexColorViewMode_ColorOnly"));
        VertexColorMaterial = Cast<UMaterialInterface>(StaticLoadObject(
            UMaterialInterface::StaticClass(), nullptr,
            TEXT("/Engine/EngineDebugMaterials/VertexColorViewMode_ColorOnly")));
    }

    // Try 3: Another engine vertex color material
    if (!VertexColorMaterial)
    {
        VertexColorMaterial = Cast<UMaterialInterface>(StaticLoadObject(
            UMaterialInterface::StaticClass(), nullptr,
            TEXT("/Engine/EngineDebugMaterials/VertexColorMaterial")));
    }

    // Try 4: Runtime engine reference
    if (!VertexColorMaterial && GEngine)
    {
        VertexColorMaterial = GEngine->VertexColorViewModeMaterial_ColorOnly;
    }

    if (VertexColorMaterial)
    {
        UE_LOG(LogTemp, Display, TEXT("v12: Vertex color material loaded: %s"), *VertexColorMaterial->GetName());
    }
    else
    {
        UE_LOG(LogTemp, Error, TEXT("v12: FAILED to load ANY vertex color material!"));
    }

    UE_LOG(LogTemp, Log, TEXT("EmersynGameMode v12: BeginPlay - Starting splash"));

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
    UE_LOG(LogTemp, Log, TEXT("v12 Building room: %s"), *CurrentRoom);

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
// v12: PROCEDURAL MESH GENERATION WITH VERTEX COLORS
// =============================================================================

void AEmersynGameMode::GenerateBoxMesh(UProceduralMeshComponent* PMC, FVector HalfExtent, FLinearColor Color)
{
    TArray<FVector> Vertices;
    TArray<int32> Triangles;
    TArray<FVector> Normals;
    TArray<FVector2D> UV0;
    TArray<FLinearColor> VertexColors;
    TArray<FProcMeshTangent> Tangents;

    float X = HalfExtent.X;
    float Y = HalfExtent.Y;
    float Z = HalfExtent.Z;

    // 6 faces, 4 vertices each = 24 vertices
    // Front face (-Y)
    Vertices.Add(FVector(-X, -Y, -Z)); Vertices.Add(FVector(X, -Y, -Z)); 
    Vertices.Add(FVector(X, -Y, Z));  Vertices.Add(FVector(-X, -Y, Z));
    // Back face (+Y)
    Vertices.Add(FVector(X, Y, -Z));  Vertices.Add(FVector(-X, Y, -Z)); 
    Vertices.Add(FVector(-X, Y, Z));  Vertices.Add(FVector(X, Y, Z));
    // Left face (-X)
    Vertices.Add(FVector(-X, Y, -Z));  Vertices.Add(FVector(-X, -Y, -Z)); 
    Vertices.Add(FVector(-X, -Y, Z));  Vertices.Add(FVector(-X, Y, Z));
    // Right face (+X)
    Vertices.Add(FVector(X, -Y, -Z)); Vertices.Add(FVector(X, Y, -Z)); 
    Vertices.Add(FVector(X, Y, Z));   Vertices.Add(FVector(X, -Y, Z));
    // Top face (+Z)
    Vertices.Add(FVector(-X, -Y, Z)); Vertices.Add(FVector(X, -Y, Z)); 
    Vertices.Add(FVector(X, Y, Z));   Vertices.Add(FVector(-X, Y, Z));
    // Bottom face (-Z)
    Vertices.Add(FVector(-X, Y, -Z));  Vertices.Add(FVector(X, Y, -Z)); 
    Vertices.Add(FVector(X, -Y, -Z));  Vertices.Add(FVector(-X, -Y, -Z));

    FVector FaceNormals[] = {
        FVector(0, -1, 0), FVector(0, 1, 0), FVector(-1, 0, 0),
        FVector(1, 0, 0), FVector(0, 0, 1), FVector(0, 0, -1)
    };

    for (int32 Face = 0; Face < 6; Face++)
    {
        int32 Base = Face * 4;
        Triangles.Add(Base); Triangles.Add(Base + 1); Triangles.Add(Base + 2);
        Triangles.Add(Base); Triangles.Add(Base + 2); Triangles.Add(Base + 3);

        for (int32 V = 0; V < 4; V++)
        {
            Normals.Add(FaceNormals[Face]);
            VertexColors.Add(Color);
            UV0.Add(FVector2D(V % 2, V / 2));
            Tangents.Add(FProcMeshTangent(1, 0, 0));
        }
    }

    PMC->CreateMeshSection_LinearColor(0, Vertices, Triangles, Normals, UV0, VertexColors, Tangents, false);
}

void AEmersynGameMode::GenerateSphereMesh(UProceduralMeshComponent* PMC, float Radius, int32 Segments, FLinearColor Color)
{
    TArray<FVector> Vertices;
    TArray<int32> Triangles;
    TArray<FVector> Normals;
    TArray<FVector2D> UV0;
    TArray<FLinearColor> VertexColors;
    TArray<FProcMeshTangent> Tangents;

    int32 Rings = Segments;
    int32 Sectors = Segments;

    for (int32 Ring = 0; Ring <= Rings; Ring++)
    {
        float Phi = PI * Ring / Rings;
        for (int32 Sector = 0; Sector <= Sectors; Sector++)
        {
            float Theta = 2.0f * PI * Sector / Sectors;
            
            FVector Pos;
            Pos.X = Radius * FMath::Sin(Phi) * FMath::Cos(Theta);
            Pos.Y = Radius * FMath::Sin(Phi) * FMath::Sin(Theta);
            Pos.Z = Radius * FMath::Cos(Phi);

            FVector Normal = Pos.GetSafeNormal();
            
            Vertices.Add(Pos);
            Normals.Add(Normal);
            VertexColors.Add(Color);
            UV0.Add(FVector2D((float)Sector / Sectors, (float)Ring / Rings));
            Tangents.Add(FProcMeshTangent(FVector(-FMath::Sin(Theta), FMath::Cos(Theta), 0), false));
        }
    }

    for (int32 Ring = 0; Ring < Rings; Ring++)
    {
        for (int32 Sector = 0; Sector < Sectors; Sector++)
        {
            int32 Current = Ring * (Sectors + 1) + Sector;
            int32 Next = Current + Sectors + 1;

            Triangles.Add(Current);
            Triangles.Add(Next);
            Triangles.Add(Current + 1);

            Triangles.Add(Current + 1);
            Triangles.Add(Next);
            Triangles.Add(Next + 1);
        }
    }

    PMC->CreateMeshSection_LinearColor(0, Vertices, Triangles, Normals, UV0, VertexColors, Tangents, false);
}

void AEmersynGameMode::GenerateCylinderMesh(UProceduralMeshComponent* PMC, float Radius, float HalfHeight, int32 Segments, FLinearColor Color)
{
    TArray<FVector> Vertices;
    TArray<int32> Triangles;
    TArray<FVector> Normals;
    TArray<FVector2D> UV0;
    TArray<FLinearColor> VertexColors;
    TArray<FProcMeshTangent> Tangents;

    // Side vertices
    for (int32 i = 0; i <= Segments; i++)
    {
        float Angle = 2.0f * PI * i / Segments;
        float CosA = FMath::Cos(Angle);
        float SinA = FMath::Sin(Angle);
        FVector Normal(CosA, SinA, 0);

        Vertices.Add(FVector(CosA * Radius, SinA * Radius, -HalfHeight));
        Normals.Add(Normal);
        VertexColors.Add(Color);
        UV0.Add(FVector2D((float)i / Segments, 1.0f));
        Tangents.Add(FProcMeshTangent(FVector(-SinA, CosA, 0), false));

        Vertices.Add(FVector(CosA * Radius, SinA * Radius, HalfHeight));
        Normals.Add(Normal);
        VertexColors.Add(Color);
        UV0.Add(FVector2D((float)i / Segments, 0.0f));
        Tangents.Add(FProcMeshTangent(FVector(-SinA, CosA, 0), false));
    }

    for (int32 i = 0; i < Segments; i++)
    {
        int32 Base = i * 2;
        Triangles.Add(Base); Triangles.Add(Base + 1); Triangles.Add(Base + 2);
        Triangles.Add(Base + 2); Triangles.Add(Base + 1); Triangles.Add(Base + 3);
    }

    // Top cap
    int32 TopCenter = Vertices.Num();
    Vertices.Add(FVector(0, 0, HalfHeight));
    Normals.Add(FVector(0, 0, 1));
    VertexColors.Add(Color);
    UV0.Add(FVector2D(0.5f, 0.5f));
    Tangents.Add(FProcMeshTangent(FVector(1, 0, 0), false));

    for (int32 i = 0; i <= Segments; i++)
    {
        float Angle = 2.0f * PI * i / Segments;
        Vertices.Add(FVector(FMath::Cos(Angle) * Radius, FMath::Sin(Angle) * Radius, HalfHeight));
        Normals.Add(FVector(0, 0, 1));
        VertexColors.Add(Color);
        UV0.Add(FVector2D(FMath::Cos(Angle) * 0.5f + 0.5f, FMath::Sin(Angle) * 0.5f + 0.5f));
        Tangents.Add(FProcMeshTangent(FVector(1, 0, 0), false));
    }
    for (int32 i = 0; i < Segments; i++)
    {
        Triangles.Add(TopCenter);
        Triangles.Add(TopCenter + 1 + i);
        Triangles.Add(TopCenter + 2 + i);
    }

    // Bottom cap
    int32 BotCenter = Vertices.Num();
    Vertices.Add(FVector(0, 0, -HalfHeight));
    Normals.Add(FVector(0, 0, -1));
    VertexColors.Add(Color);
    UV0.Add(FVector2D(0.5f, 0.5f));
    Tangents.Add(FProcMeshTangent(FVector(1, 0, 0), false));

    for (int32 i = 0; i <= Segments; i++)
    {
        float Angle = 2.0f * PI * i / Segments;
        Vertices.Add(FVector(FMath::Cos(Angle) * Radius, FMath::Sin(Angle) * Radius, -HalfHeight));
        Normals.Add(FVector(0, 0, -1));
        VertexColors.Add(Color);
        UV0.Add(FVector2D(FMath::Cos(Angle) * 0.5f + 0.5f, FMath::Sin(Angle) * 0.5f + 0.5f));
        Tangents.Add(FProcMeshTangent(FVector(1, 0, 0), false));
    }
    for (int32 i = 0; i < Segments; i++)
    {
        Triangles.Add(BotCenter);
        Triangles.Add(BotCenter + 2 + i);
        Triangles.Add(BotCenter + 1 + i);
    }

    PMC->CreateMeshSection_LinearColor(0, Vertices, Triangles, Normals, UV0, VertexColors, Tangents, false);
}

// =============================================================================
// SPAWN FUNCTIONS - All using ProceduralMeshComponent with vertex colors
// =============================================================================

AActor* AEmersynGameMode::SpawnBox(FVector Location, FVector Scale, FLinearColor Color)
{
    UWorld* World = GetWorld();
    if (!World) return nullptr;
    
    AActor* Actor = World->SpawnActor<AActor>(AActor::StaticClass(), Location, FRotator::ZeroRotator);
    if (!Actor) return nullptr;
    
    UProceduralMeshComponent* PMC = NewObject<UProceduralMeshComponent>(Actor, TEXT("ProceduralMesh"));
    PMC->RegisterComponent();
    Actor->SetRootComponent(PMC);
    
    FVector HalfExtent = Scale * 50.0f;
    GenerateBoxMesh(PMC, HalfExtent, Color);
    
    // v12: ALWAYS assign vertex color material
    if (VertexColorMaterial)
    {
        PMC->SetMaterial(0, VertexColorMaterial);
    }
    
    SpawnedActors.Add(Actor);
    return Actor;
}

AActor* AEmersynGameMode::SpawnSphere(FVector Location, float Radius, FLinearColor Color)
{
    UWorld* World = GetWorld();
    if (!World) return nullptr;
    
    AActor* Actor = World->SpawnActor<AActor>(AActor::StaticClass(), Location, FRotator::ZeroRotator);
    if (!Actor) return nullptr;
    
    UProceduralMeshComponent* PMC = NewObject<UProceduralMeshComponent>(Actor, TEXT("ProceduralMesh"));
    PMC->RegisterComponent();
    Actor->SetRootComponent(PMC);
    
    float ActualRadius = Radius * 50.0f;
    GenerateSphereMesh(PMC, ActualRadius, 12, Color);
    
    if (VertexColorMaterial)
    {
        PMC->SetMaterial(0, VertexColorMaterial);
    }
    
    SpawnedActors.Add(Actor);
    return Actor;
}

AActor* AEmersynGameMode::SpawnCylinder(FVector Location, FVector Scale, FLinearColor Color)
{
    UWorld* World = GetWorld();
    if (!World) return nullptr;
    
    AActor* Actor = World->SpawnActor<AActor>(AActor::StaticClass(), Location, FRotator::ZeroRotator);
    if (!Actor) return nullptr;
    
    UProceduralMeshComponent* PMC = NewObject<UProceduralMeshComponent>(Actor, TEXT("ProceduralMesh"));
    PMC->RegisterComponent();
    Actor->SetRootComponent(PMC);
    
    float ActualRadius = Scale.X * 50.0f;
    float ActualHalfHeight = Scale.Z * 50.0f;
    GenerateCylinderMesh(PMC, ActualRadius, ActualHalfHeight, 16, Color);
    
    if (VertexColorMaterial)
    {
        PMC->SetMaterial(0, VertexColorMaterial);
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
    SpawnSphere(Location + FVector(0, 0, 140 * S), 0.45f * S, SkinColor);
    SpawnSphere(Location + FVector(0, 0, 165 * S), 0.48f * S, HairColor);
    SpawnBox(Location + FVector(0, 0, 80 * S), FVector(0.4f, 0.25f, 0.6f) * S, OutfitColor);
    SpawnCylinder(Location + FVector(35 * S, 0, 90 * S), FVector(0.1f, 0.1f, 0.35f) * S, SkinColor);
    SpawnCylinder(Location + FVector(-35 * S, 0, 90 * S), FVector(0.1f, 0.1f, 0.35f) * S, SkinColor);
    SpawnCylinder(Location + FVector(12 * S, 0, 25 * S), FVector(0.12f, 0.12f, 0.3f) * S, OutfitColor * 0.8f);
    SpawnCylinder(Location + FVector(-12 * S, 0, 25 * S), FVector(0.12f, 0.12f, 0.3f) * S, OutfitColor * 0.8f);
    SpawnBox(Location + FVector(12 * S, 0, 5 * S), FVector(0.12f, 0.15f, 0.06f) * S, FLinearColor(0.3f, 0.2f, 0.15f));
    SpawnBox(Location + FVector(-12 * S, 0, 5 * S), FVector(0.12f, 0.15f, 0.06f) * S, FLinearColor(0.3f, 0.2f, 0.15f));
    SpawnSphere(Location + FVector(10 * S, -20 * S, 145 * S), 0.06f * S, FLinearColor(0.1f, 0.1f, 0.15f));
    SpawnSphere(Location + FVector(-10 * S, -20 * S, 145 * S), 0.06f * S, FLinearColor(0.1f, 0.1f, 0.15f));
}

void AEmersynGameMode::SpawnPet(FVector Location, FLinearColor BodyColor, FLinearColor AccentColor, const FString& Name, float Scale)
{
    float S = Scale;
    SpawnSphere(Location + FVector(0, 0, 25 * S), 0.4f * S, BodyColor);
    SpawnSphere(Location + FVector(30 * S, 0, 40 * S), 0.3f * S, BodyColor);
    SpawnSphere(Location + FVector(40 * S, 12 * S, 55 * S), 0.1f * S, AccentColor);
    SpawnSphere(Location + FVector(40 * S, -12 * S, 55 * S), 0.1f * S, AccentColor);
    SpawnCylinder(Location + FVector(-35 * S, 0, 35 * S), FVector(0.05f, 0.05f, 0.2f) * S, BodyColor);
    SpawnCylinder(Location + FVector(15 * S, 12 * S, 8 * S), FVector(0.06f, 0.06f, 0.15f) * S, BodyColor);
    SpawnCylinder(Location + FVector(15 * S, -12 * S, 8 * S), FVector(0.06f, 0.06f, 0.15f) * S, BodyColor);
    SpawnCylinder(Location + FVector(-15 * S, 12 * S, 8 * S), FVector(0.06f, 0.06f, 0.15f) * S, BodyColor);
    SpawnCylinder(Location + FVector(-15 * S, -12 * S, 8 * S), FVector(0.06f, 0.06f, 0.15f) * S, BodyColor);
    SpawnSphere(Location + FVector(40 * S, -8 * S, 45 * S), 0.05f * S, FLinearColor(0.1f, 0.1f, 0.1f));
    SpawnSphere(Location + FVector(40 * S, 8 * S, 45 * S), 0.05f * S, FLinearColor(0.1f, 0.1f, 0.1f));
    SpawnSphere(Location + FVector(48 * S, 0, 40 * S), 0.04f * S, AccentColor);
}

// =============================================================================
// v12: ROOM BUILDERS - All with sky background + enlarged floors
// Every room now spawns: sky background box + colored floor + walls + furniture
// =============================================================================

// Helper: Spawn a sky-colored background that covers the entire viewport
void AEmersynGameMode::SpawnSkyBackground(FLinearColor SkyColor)
{
    // Huge boxes to fill any void visible to camera
    SpawnBox(FVector(0, 0, -100), FVector(200, 200, 0.5f), SkyColor * 0.3f);   // Ground extension (darker)
    SpawnBox(FVector(0, 2000, 500), FVector(200, 0.5f, 40), SkyColor);          // Far back wall (sky)
    SpawnBox(FVector(0, 0, 2000), FVector(200, 200, 0.5f), SkyColor * 0.9f);    // Ceiling/sky above
}

// ==================== SPLASH ====================
void AEmersynGameMode::BuildSplashScreen()
{
    SetupCamera(FVector(0, -500, 200), FRotator(-10, 0, 0));

    SpawnSkyBackground(FLinearColor(0.15f, 0.1f, 0.3f));

    // v12: HUGE floor - ensures no checkerboard visible anywhere
    SpawnBox(FVector(0, 0, -10), FVector(50, 50, 0.2f), FLinearColor(0.08f, 0.05f, 0.15f));
    SpawnBox(FVector(0, 200, 200), FVector(50, 0.1f, 8), FLinearColor(0.12f, 0.08f, 0.2f));

    // Rainbow columns
    SpawnBox(FVector(-300, 0, 300), FVector(0.8f, 0.1f, 1.2f), FLinearColor(1.0f, 0.4f, 0.6f));
    SpawnBox(FVector(-200, 0, 300), FVector(0.8f, 0.1f, 1.2f), FLinearColor(0.95f, 0.6f, 0.2f));
    SpawnBox(FVector(-100, 0, 300), FVector(0.8f, 0.1f, 1.2f), FLinearColor(0.95f, 0.85f, 0.2f));
    SpawnBox(FVector(0, 0, 300), FVector(0.8f, 0.1f, 1.2f), FLinearColor(0.3f, 0.85f, 0.4f));
    SpawnBox(FVector(100, 0, 300), FVector(0.8f, 0.1f, 1.2f), FLinearColor(0.3f, 0.7f, 0.95f));
    SpawnBox(FVector(200, 0, 300), FVector(0.8f, 0.1f, 1.2f), FLinearColor(0.7f, 0.4f, 0.95f));
    SpawnBox(FVector(300, 0, 300), FVector(0.8f, 0.1f, 1.2f), FLinearColor(0.95f, 0.4f, 0.8f));

    // Stars/particles
    SpawnSphere(FVector(-400, -50, 450), 0.2f, FLinearColor(1.0f, 0.95f, 0.3f));
    SpawnSphere(FVector(400, -50, 420), 0.15f, FLinearColor(1.0f, 0.8f, 0.9f));
    SpawnSphere(FVector(-200, -50, 480), 0.12f, FLinearColor(0.8f, 0.9f, 1.0f));
    SpawnSphere(FVector(300, -50, 500), 0.18f, FLinearColor(0.9f, 0.7f, 1.0f));
    SpawnSphere(FVector(0, -50, 520), 0.25f, FLinearColor(1.0f, 0.6f, 0.7f));

    SpawnCharacter(FVector(0, 50, 50), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(1.0f, 0.5f, 0.7f), TEXT("Emersyn"), 1.5f);

    SpawnBox(FVector(0, 0, 150), FVector(4.0f, 0.1f, 0.15f), FLinearColor(0.95f, 0.85f, 0.3f));

    SpawnLight(FVector(0, -300, 400), 8000.0f, FLinearColor(0.95f, 0.8f, 0.9f));
    SpawnLight(FVector(-300, -200, 300), 3000.0f, FLinearColor(0.6f, 0.4f, 0.9f));
    SpawnLight(FVector(300, -200, 300), 3000.0f, FLinearColor(0.9f, 0.4f, 0.6f));
}

// ==================== MAIN MENU ====================
void AEmersynGameMode::BuildMainMenu()
{
    SetupCamera(FVector(0, -800, 400), FRotator(-20, 0, 0));

    SpawnSkyBackground(FLinearColor(0.6f, 0.8f, 1.0f));

    SpawnBox(FVector(0, 0, -10), FVector(50, 50, 0.2f), FLinearColor(0.95f, 0.85f, 0.88f));
    SpawnBox(FVector(0, 500, 250), FVector(50, 0.1f, 10), FLinearColor(0.92f, 0.82f, 0.9f));

    SpawnBox(FVector(-400, 200, 100), FVector(1.2f, 0.1f, 0.8f), FLinearColor(1.0f, 0.7f, 0.8f));
    SpawnBox(FVector(-200, 200, 100), FVector(1.2f, 0.1f, 0.8f), FLinearColor(0.95f, 0.9f, 0.6f));
    SpawnBox(FVector(0, 200, 100), FVector(1.2f, 0.1f, 0.8f), FLinearColor(0.7f, 0.85f, 0.95f));
    SpawnBox(FVector(200, 200, 100), FVector(1.2f, 0.1f, 0.8f), FLinearColor(0.9f, 0.85f, 0.75f));
    SpawnBox(FVector(400, 200, 100), FVector(1.2f, 0.1f, 0.8f), FLinearColor(0.6f, 0.9f, 0.65f));

    SpawnCharacter(FVector(-300, -100, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(1.0f, 0.5f, 0.7f), TEXT("Emersyn"));
    SpawnCharacter(FVector(-100, -50, 0), FLinearColor(0.92f, 0.78f, 0.65f), FLinearColor(0.15f, 0.1f, 0.08f), FLinearColor(0.6f, 0.3f, 0.9f), TEXT("Ava"));
    SpawnCharacter(FVector(100, -80, 0), FLinearColor(0.85f, 0.7f, 0.55f), FLinearColor(0.3f, 0.2f, 0.1f), FLinearColor(0.2f, 0.6f, 0.9f), TEXT("Leo"));
    SpawnCharacter(FVector(300, -40, 0), FLinearColor(0.9f, 0.75f, 0.6f), FLinearColor(0.7f, 0.3f, 0.15f), FLinearColor(0.3f, 0.85f, 0.5f), TEXT("Mia"));

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

    SpawnSkyBackground(FLinearColor(0.7f, 0.6f, 0.9f));

    SpawnBox(FVector(0, 0, -10), FVector(30, 30, 0.2f), FLinearColor(0.95f, 0.82f, 0.85f));
    SpawnBox(FVector(0, 400, 200), FVector(30, 0.1f, 8), FLinearColor(0.88f, 0.82f, 0.95f));
    SpawnBox(FVector(-400, 0, 200), FVector(0.1f, 30, 8), FLinearColor(0.9f, 0.84f, 0.96f));
    SpawnBox(FVector(400, 0, 200), FVector(0.1f, 30, 8), FLinearColor(0.9f, 0.84f, 0.96f));

    SpawnBox(FVector(-200, 200, 30), FVector(1.8f, 1.2f, 0.3f), FLinearColor(1.0f, 0.7f, 0.8f));
    SpawnBox(FVector(-200, 200, 50), FVector(1.6f, 1.0f, 0.15f), FLinearColor(0.98f, 0.95f, 0.97f));
    SpawnBox(FVector(-200, 280, 65), FVector(0.5f, 0.3f, 0.1f), FLinearColor(0.95f, 0.85f, 0.95f));
    SpawnBox(FVector(-200, 310, 100), FVector(1.8f, 0.08f, 0.8f), FLinearColor(0.95f, 0.75f, 0.85f));

    SpawnBox(FVector(250, 300, 50), FVector(0.8f, 0.4f, 1.0f), FLinearColor(0.95f, 0.88f, 0.92f));
    SpawnBox(FVector(250, 310, 130), FVector(0.5f, 0.05f, 0.5f), FLinearColor(0.8f, 0.85f, 0.9f));

    SpawnBox(FVector(-50, 280, 25), FVector(0.35f, 0.35f, 0.5f), FLinearColor(0.92f, 0.85f, 0.88f));
    SpawnSphere(FVector(-50, 280, 72), 0.12f, FLinearColor(1.0f, 0.95f, 0.8f));

    SpawnBox(FVector(0, 0, 1), FVector(2.5f, 2.0f, 0.02f), FLinearColor(0.85f, 0.65f, 0.75f));

    SpawnBox(FVector(350, -100, 75), FVector(0.4f, 0.8f, 1.5f), FLinearColor(0.92f, 0.85f, 0.8f));
    SpawnBox(FVector(200, -200, 20), FVector(0.6f, 0.4f, 0.4f), FLinearColor(0.95f, 0.6f, 0.3f));

    SpawnCharacter(FVector(50, -100, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(1.0f, 0.5f, 0.7f), TEXT("Emersyn"));
    SpawnPet(FVector(-100, -150, 0), FLinearColor(0.9f, 0.6f, 0.3f), FLinearColor(0.95f, 0.8f, 0.6f), TEXT("Cat"));

    SpawnDirectionalLight(FRotator(-45, 20, 0), 3.0f, FLinearColor(1.0f, 0.95f, 0.9f));
    SpawnLight(FVector(0, 0, 350), 5000.0f, FLinearColor(1.0f, 0.9f, 0.95f));
    SpawnLight(FVector(-200, 200, 100), 2000.0f, FLinearColor(1.0f, 0.85f, 0.9f));
}

// ==================== KITCHEN ====================
void AEmersynGameMode::BuildKitchen()
{
    SetupCamera(FVector(0, -600, 300), FRotator(-20, 0, 0));

    SpawnSkyBackground(FLinearColor(0.9f, 0.85f, 0.7f));

    SpawnBox(FVector(0, 0, -10), FVector(30, 30, 0.2f), FLinearColor(0.95f, 0.92f, 0.85f));
    SpawnBox(FVector(0, 400, 200), FVector(30, 0.1f, 8), FLinearColor(0.98f, 0.95f, 0.88f));
    SpawnBox(FVector(-400, 0, 200), FVector(0.1f, 30, 8), FLinearColor(0.95f, 0.92f, 0.85f));

    SpawnBox(FVector(-350, 350, 50), FVector(0.6f, 3.0f, 1.0f), FLinearColor(0.92f, 0.88f, 0.82f));
    SpawnBox(FVector(-350, 350, 110), FVector(0.55f, 2.8f, 0.05f), FLinearColor(0.85f, 0.82f, 0.78f));

    SpawnBox(FVector(0, 100, 50), FVector(1.2f, 0.8f, 1.0f), FLinearColor(0.85f, 0.5f, 0.25f));
    SpawnBox(FVector(0, 100, 110), FVector(1.3f, 0.9f, 0.03f), FLinearColor(0.92f, 0.88f, 0.8f));

    SpawnBox(FVector(200, 350, 45), FVector(0.7f, 0.5f, 0.9f), FLinearColor(0.92f, 0.92f, 0.95f));
    SpawnBox(FVector(200, 350, 100), FVector(0.6f, 0.4f, 0.03f), FLinearColor(0.3f, 0.3f, 0.32f));

    SpawnSphere(FVector(-100, 100, 125), 0.08f, FLinearColor(0.95f, 0.2f, 0.15f));
    SpawnSphere(FVector(-50, 100, 125), 0.06f, FLinearColor(0.2f, 0.85f, 0.2f));
    SpawnSphere(FVector(50, 100, 125), 0.07f, FLinearColor(0.95f, 0.85f, 0.1f));

    SpawnCharacter(FVector(100, -100, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(0.95f, 0.85f, 0.3f), TEXT("Emersyn"));
    SpawnCharacter(FVector(-150, -50, 0), FLinearColor(0.9f, 0.75f, 0.6f), FLinearColor(0.7f, 0.3f, 0.15f), FLinearColor(0.3f, 0.85f, 0.5f), TEXT("Mia"));

    SpawnDirectionalLight(FRotator(-40, 30, 0), 3.5f, FLinearColor(1.0f, 0.98f, 0.92f));
    SpawnLight(FVector(0, 100, 300), 6000.0f, FLinearColor(1.0f, 0.97f, 0.9f));
}

// ==================== BATHROOM ====================
void AEmersynGameMode::BuildBathroom()
{
    SetupCamera(FVector(0, -450, 300), FRotator(-25, 0, 0));

    SpawnSkyBackground(FLinearColor(0.7f, 0.85f, 0.95f));

    SpawnBox(FVector(0, 0, -10), FVector(30, 30, 0.2f), FLinearColor(0.85f, 0.92f, 0.95f));
    SpawnBox(FVector(0, 300, 200), FVector(30, 0.1f, 8), FLinearColor(0.82f, 0.9f, 0.95f));
    SpawnBox(FVector(-300, 0, 200), FVector(0.1f, 30, 8), FLinearColor(0.85f, 0.92f, 0.96f));

    SpawnBox(FVector(-200, 200, 40), FVector(1.0f, 0.6f, 0.4f), FLinearColor(0.95f, 0.95f, 0.97f));
    SpawnBox(FVector(150, 250, 30), FVector(0.5f, 0.3f, 0.6f), FLinearColor(0.92f, 0.92f, 0.95f));
    SpawnBox(FVector(150, 250, 70), FVector(0.4f, 0.25f, 0.15f), FLinearColor(0.85f, 0.88f, 0.92f));

    SpawnBox(FVector(0, 295, 150), FVector(0.8f, 0.03f, 0.6f), FLinearColor(0.9f, 0.92f, 0.95f));

    SpawnSphere(FVector(-150, -100, 40), 0.15f, FLinearColor(0.95f, 0.85f, 0.2f));

    SpawnCharacter(FVector(50, -80, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(0.7f, 0.85f, 0.95f), TEXT("Emersyn"));

    SpawnDirectionalLight(FRotator(-50, 20, 0), 3.0f, FLinearColor(0.95f, 0.97f, 1.0f));
    SpawnLight(FVector(0, 0, 300), 5000.0f, FLinearColor(0.95f, 0.97f, 1.0f));
}

// ==================== LIVING ROOM ====================
void AEmersynGameMode::BuildLivingRoom()
{
    SetupCamera(FVector(0, -700, 350), FRotator(-20, 0, 0));

    SpawnSkyBackground(FLinearColor(0.85f, 0.75f, 0.6f));

    SpawnBox(FVector(0, 0, -10), FVector(40, 40, 0.2f), FLinearColor(0.85f, 0.75f, 0.6f));
    SpawnBox(FVector(0, 500, 250), FVector(40, 0.1f, 10), FLinearColor(0.92f, 0.88f, 0.82f));
    SpawnBox(FVector(-500, 0, 250), FVector(0.1f, 40, 10), FLinearColor(0.9f, 0.86f, 0.8f));
    SpawnBox(FVector(500, 0, 250), FVector(0.1f, 40, 10), FLinearColor(0.9f, 0.86f, 0.8f));

    SpawnBox(FVector(-200, 300, 40), FVector(2.0f, 0.8f, 0.4f), FLinearColor(0.6f, 0.35f, 0.2f));
    SpawnBox(FVector(-200, 300, 80), FVector(1.8f, 0.6f, 0.3f), FLinearColor(0.85f, 0.5f, 0.3f));

    SpawnBox(FVector(200, 100, 30), FVector(1.5f, 0.8f, 0.3f), FLinearColor(0.95f, 0.92f, 0.88f));

    SpawnBox(FVector(0, 480, 100), FVector(2.0f, 0.1f, 1.5f), FLinearColor(0.15f, 0.15f, 0.18f));
    SpawnBox(FVector(0, 0, 1), FVector(3.0f, 2.5f, 0.02f), FLinearColor(0.7f, 0.55f, 0.4f));

    SpawnCharacter(FVector(-100, -100, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(1.0f, 0.5f, 0.7f), TEXT("Emersyn"));
    SpawnCharacter(FVector(150, -50, 0), FLinearColor(0.85f, 0.7f, 0.55f), FLinearColor(0.3f, 0.2f, 0.1f), FLinearColor(0.2f, 0.6f, 0.9f), TEXT("Leo"));
    SpawnPet(FVector(0, -180, 0), FLinearColor(0.7f, 0.5f, 0.3f), FLinearColor(0.4f, 0.25f, 0.15f), TEXT("Dog"));

    SpawnDirectionalLight(FRotator(-40, 25, 0), 3.0f, FLinearColor(1.0f, 0.95f, 0.88f));
    SpawnLight(FVector(0, 0, 400), 8000.0f, FLinearColor(1.0f, 0.95f, 0.9f));
}

// ==================== GARDEN ====================
void AEmersynGameMode::BuildGarden()
{
    SetupCamera(FVector(0, -800, 400), FRotator(-20, 0, 0));

    SpawnSkyBackground(FLinearColor(0.5f, 0.75f, 1.0f));

    SpawnBox(FVector(0, 0, -10), FVector(60, 60, 0.2f), FLinearColor(0.35f, 0.65f, 0.25f));

    SpawnCylinder(FVector(-300, 200, 80), FVector(0.3f, 0.3f, 1.6f), FLinearColor(0.5f, 0.35f, 0.2f));
    SpawnSphere(FVector(-300, 200, 220), 1.2f, FLinearColor(0.2f, 0.65f, 0.15f));
    SpawnCylinder(FVector(200, 300, 60), FVector(0.25f, 0.25f, 1.2f), FLinearColor(0.45f, 0.3f, 0.18f));
    SpawnSphere(FVector(200, 300, 170), 0.9f, FLinearColor(0.25f, 0.7f, 0.2f));

    SpawnSphere(FVector(-100, 100, 15), 0.15f, FLinearColor(0.95f, 0.3f, 0.35f));
    SpawnSphere(FVector(-80, 120, 12), 0.12f, FLinearColor(0.95f, 0.8f, 0.2f));
    SpawnSphere(FVector(-120, 90, 14), 0.13f, FLinearColor(0.95f, 0.4f, 0.7f));
    SpawnSphere(FVector(100, -50, 12), 0.12f, FLinearColor(0.7f, 0.3f, 0.95f));
    SpawnSphere(FVector(120, -30, 14), 0.14f, FLinearColor(0.95f, 0.6f, 0.2f));

    SpawnBox(FVector(-400, -200, 15), FVector(0.6f, 1.5f, 0.3f), FLinearColor(0.6f, 0.4f, 0.25f));
    SpawnBox(FVector(-400, -200, 35), FVector(0.55f, 1.4f, 0.1f), FLinearColor(0.3f, 0.55f, 0.2f));

    SpawnCharacter(FVector(0, -150, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(0.6f, 0.85f, 0.4f), TEXT("Emersyn"));
    SpawnPet(FVector(150, -100, 0), FLinearColor(0.95f, 0.95f, 0.92f), FLinearColor(0.95f, 0.7f, 0.75f), TEXT("Bunny"));

    SpawnDirectionalLight(FRotator(-45, 30, 0), 5.0f, FLinearColor(1.0f, 0.98f, 0.9f));
    SpawnLight(FVector(0, 0, 500), 10000.0f, FLinearColor(1.0f, 0.97f, 0.92f));
}

// ==================== SCHOOL ====================
void AEmersynGameMode::BuildSchool()
{
    SetupCamera(FVector(0, -600, 300), FRotator(-15, 0, 0));

    SpawnSkyBackground(FLinearColor(0.75f, 0.82f, 0.7f));

    SpawnBox(FVector(0, 0, -10), FVector(40, 40, 0.2f), FLinearColor(0.82f, 0.78f, 0.7f));
    SpawnBox(FVector(0, 500, 200), FVector(40, 0.1f, 8), FLinearColor(0.85f, 0.88f, 0.82f));

    SpawnBox(FVector(0, 450, 120), FVector(2.5f, 0.05f, 1.2f), FLinearColor(0.2f, 0.45f, 0.25f));

    SpawnBox(FVector(-200, 200, 35), FVector(0.6f, 0.4f, 0.35f), FLinearColor(0.7f, 0.55f, 0.35f));
    SpawnBox(FVector(-100, 200, 35), FVector(0.6f, 0.4f, 0.35f), FLinearColor(0.7f, 0.55f, 0.35f));
    SpawnBox(FVector(100, 200, 35), FVector(0.6f, 0.4f, 0.35f), FLinearColor(0.7f, 0.55f, 0.35f));
    SpawnBox(FVector(200, 200, 35), FVector(0.6f, 0.4f, 0.35f), FLinearColor(0.7f, 0.55f, 0.35f));

    SpawnCharacter(FVector(-150, 50, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(0.3f, 0.5f, 0.85f), TEXT("Emersyn"));
    SpawnCharacter(FVector(0, 80, 0), FLinearColor(0.92f, 0.78f, 0.65f), FLinearColor(0.15f, 0.1f, 0.08f), FLinearColor(0.3f, 0.5f, 0.85f), TEXT("Ava"));
    SpawnCharacter(FVector(150, 50, 0), FLinearColor(0.85f, 0.7f, 0.55f), FLinearColor(0.3f, 0.2f, 0.1f), FLinearColor(0.3f, 0.5f, 0.85f), TEXT("Leo"));
    SpawnCharacter(FVector(350, 350, 0), FLinearColor(0.88f, 0.72f, 0.58f), FLinearColor(0.4f, 0.25f, 0.15f), FLinearColor(0.6f, 0.3f, 0.2f), TEXT("Teacher"), 1.15f);

    SpawnDirectionalLight(FRotator(-40, 20, 0), 3.0f, FLinearColor(1.0f, 0.97f, 0.92f));
    SpawnLight(FVector(0, 200, 350), 6000.0f, FLinearColor(1.0f, 0.98f, 0.95f));
}

// ==================== SHOP ====================
void AEmersynGameMode::BuildShop()
{
    SetupCamera(FVector(0, -600, 350), FRotator(-20, 0, 0));

    SpawnSkyBackground(FLinearColor(0.9f, 0.8f, 0.7f));

    SpawnBox(FVector(0, 0, -10), FVector(40, 40, 0.2f), FLinearColor(0.92f, 0.88f, 0.82f));
    SpawnBox(FVector(0, 500, 250), FVector(40, 0.1f, 10), FLinearColor(0.95f, 0.9f, 0.82f));

    SpawnBox(FVector(-350, 200, 60), FVector(0.5f, 2.0f, 1.2f), FLinearColor(0.85f, 0.7f, 0.5f));
    SpawnBox(FVector(350, 200, 60), FVector(0.5f, 2.0f, 1.2f), FLinearColor(0.85f, 0.7f, 0.5f));

    SpawnSphere(FVector(-340, 100, 130), 0.12f, FLinearColor(0.95f, 0.4f, 0.5f));
    SpawnSphere(FVector(-340, 200, 130), 0.1f, FLinearColor(0.4f, 0.7f, 0.95f));
    SpawnSphere(FVector(-340, 300, 130), 0.11f, FLinearColor(0.95f, 0.85f, 0.2f));
    SpawnSphere(FVector(340, 150, 130), 0.12f, FLinearColor(0.3f, 0.85f, 0.45f));
    SpawnSphere(FVector(340, 250, 130), 0.1f, FLinearColor(0.95f, 0.6f, 0.8f));

    SpawnBox(FVector(0, 400, 50), FVector(1.5f, 0.4f, 1.0f), FLinearColor(0.85f, 0.6f, 0.4f));

    SpawnCharacter(FVector(-50, -100, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(0.95f, 0.7f, 0.8f), TEXT("Emersyn"));
    SpawnCharacter(FVector(100, -50, 0), FLinearColor(0.9f, 0.75f, 0.6f), FLinearColor(0.7f, 0.3f, 0.15f), FLinearColor(0.3f, 0.85f, 0.5f), TEXT("Mia"));
    SpawnCharacter(FVector(0, 380, 0), FLinearColor(0.88f, 0.72f, 0.58f), FLinearColor(0.5f, 0.4f, 0.3f), FLinearColor(0.6f, 0.5f, 0.8f), TEXT("Shopkeeper"), 1.1f);

    SpawnDirectionalLight(FRotator(-40, 25, 0), 3.0f, FLinearColor(1.0f, 0.97f, 0.92f));
    SpawnLight(FVector(0, 200, 400), 8000.0f, FLinearColor(1.0f, 0.95f, 0.9f));
}

// ==================== PLAYGROUND ====================
void AEmersynGameMode::BuildPlayground()
{
    SetupCamera(FVector(0, -800, 400), FRotator(-20, 0, 0));

    SpawnSkyBackground(FLinearColor(0.5f, 0.75f, 1.0f));

    SpawnBox(FVector(0, 0, -10), FVector(60, 60, 0.2f), FLinearColor(0.45f, 0.7f, 0.35f));

    SpawnBox(FVector(-300, 200, 100), FVector(0.3f, 0.3f, 2.0f), FLinearColor(0.95f, 0.3f, 0.35f));
    SpawnBox(FVector(-300, 250, 120), FVector(0.6f, 1.0f, 0.05f), FLinearColor(0.95f, 0.85f, 0.2f));

    SpawnBox(FVector(200, 100, 150), FVector(2.0f, 0.05f, 0.05f), FLinearColor(0.5f, 0.5f, 0.55f));
    SpawnCylinder(FVector(100, 100, 75), FVector(0.08f, 0.08f, 1.5f), FLinearColor(0.5f, 0.5f, 0.55f));
    SpawnCylinder(FVector(300, 100, 75), FVector(0.08f, 0.08f, 1.5f), FLinearColor(0.5f, 0.5f, 0.55f));
    SpawnBox(FVector(200, 100, 50), FVector(0.3f, 0.15f, 0.03f), FLinearColor(0.85f, 0.6f, 0.3f));

    SpawnBox(FVector(0, -200, 10), FVector(1.5f, 1.5f, 0.2f), FLinearColor(0.95f, 0.88f, 0.65f));

    SpawnCharacter(FVector(-100, -50, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(0.95f, 0.5f, 0.7f), TEXT("Emersyn"));
    SpawnCharacter(FVector(100, 50, 0), FLinearColor(0.92f, 0.78f, 0.65f), FLinearColor(0.15f, 0.1f, 0.08f), FLinearColor(0.6f, 0.3f, 0.9f), TEXT("Ava"));
    SpawnPet(FVector(200, -100, 0), FLinearColor(0.7f, 0.5f, 0.3f), FLinearColor(0.4f, 0.25f, 0.15f), TEXT("Dog"));

    SpawnDirectionalLight(FRotator(-45, 30, 0), 5.0f, FLinearColor(1.0f, 0.98f, 0.92f));
    SpawnLight(FVector(0, 0, 500), 10000.0f, FLinearColor(1.0f, 0.97f, 0.9f));
}

// ==================== PARK ====================
void AEmersynGameMode::BuildPark()
{
    SetupCamera(FVector(0, -900, 450), FRotator(-20, 0, 0));

    SpawnSkyBackground(FLinearColor(0.5f, 0.75f, 1.0f));

    SpawnBox(FVector(0, 0, -10), FVector(80, 80, 0.2f), FLinearColor(0.4f, 0.68f, 0.3f));

    SpawnBox(FVector(200, 300, 2), FVector(2.0f, 1.5f, 0.05f), FLinearColor(0.3f, 0.6f, 0.85f));

    SpawnCylinder(FVector(-400, 300, 80), FVector(0.3f, 0.3f, 1.6f), FLinearColor(0.5f, 0.35f, 0.2f));
    SpawnSphere(FVector(-400, 300, 220), 1.5f, FLinearColor(0.15f, 0.6f, 0.12f));
    SpawnCylinder(FVector(400, -200, 60), FVector(0.25f, 0.25f, 1.2f), FLinearColor(0.45f, 0.3f, 0.18f));
    SpawnSphere(FVector(400, -200, 160), 1.0f, FLinearColor(0.2f, 0.65f, 0.15f));

    SpawnBox(FVector(-200, -100, 25), FVector(1.0f, 0.3f, 0.05f), FLinearColor(0.55f, 0.38f, 0.22f));
    SpawnBox(FVector(-200, -115, 40), FVector(1.0f, 0.05f, 0.3f), FLinearColor(0.55f, 0.38f, 0.22f));

    SpawnBox(FVector(0, 0, 1), FVector(1.0f, 10.0f, 0.02f), FLinearColor(0.75f, 0.7f, 0.6f));

    SpawnCharacter(FVector(0, -200, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(0.4f, 0.75f, 0.95f), TEXT("Emersyn"));
    SpawnCharacter(FVector(150, -150, 0), FLinearColor(0.85f, 0.7f, 0.55f), FLinearColor(0.3f, 0.2f, 0.1f), FLinearColor(0.95f, 0.5f, 0.2f), TEXT("Leo"));
    SpawnPet(FVector(-100, -250, 0), FLinearColor(0.7f, 0.5f, 0.3f), FLinearColor(0.4f, 0.25f, 0.15f), TEXT("Dog"));

    SpawnDirectionalLight(FRotator(-40, 35, 0), 5.0f, FLinearColor(1.0f, 0.95f, 0.85f));
    SpawnLight(FVector(0, 0, 600), 12000.0f, FLinearColor(1.0f, 0.97f, 0.9f));
}

// ==================== MALL ====================
void AEmersynGameMode::BuildMall()
{
    SetupCamera(FVector(0, -700, 400), FRotator(-20, 0, 0));

    SpawnSkyBackground(FLinearColor(0.85f, 0.82f, 0.78f));

    SpawnBox(FVector(0, 0, -10), FVector(50, 50, 0.2f), FLinearColor(0.9f, 0.88f, 0.85f));
    SpawnBox(FVector(0, 600, 300), FVector(50, 0.1f, 12), FLinearColor(0.92f, 0.9f, 0.88f));
    SpawnBox(FVector(-600, 0, 300), FVector(0.1f, 50, 12), FLinearColor(0.88f, 0.85f, 0.82f));
    SpawnBox(FVector(600, 0, 300), FVector(0.1f, 50, 12), FLinearColor(0.88f, 0.85f, 0.82f));

    SpawnBox(FVector(-400, 500, 100), FVector(1.5f, 0.1f, 2.0f), FLinearColor(0.95f, 0.5f, 0.6f));
    SpawnBox(FVector(-100, 500, 100), FVector(1.5f, 0.1f, 2.0f), FLinearColor(0.5f, 0.8f, 0.95f));
    SpawnBox(FVector(200, 500, 100), FVector(1.5f, 0.1f, 2.0f), FLinearColor(0.6f, 0.9f, 0.5f));

    SpawnCylinder(FVector(0, 200, 30), FVector(1.0f, 1.0f, 0.3f), FLinearColor(0.8f, 0.82f, 0.85f));
    SpawnCylinder(FVector(0, 200, 60), FVector(0.3f, 0.3f, 0.5f), FLinearColor(0.82f, 0.84f, 0.88f));
    SpawnSphere(FVector(0, 200, 90), 0.15f, FLinearColor(0.5f, 0.7f, 0.95f));

    SpawnCharacter(FVector(-150, -100, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(0.95f, 0.4f, 0.7f), TEXT("Emersyn"));
    SpawnCharacter(FVector(100, -50, 0), FLinearColor(0.92f, 0.78f, 0.65f), FLinearColor(0.15f, 0.1f, 0.08f), FLinearColor(0.85f, 0.5f, 0.85f), TEXT("Ava"));
    SpawnCharacter(FVector(0, -150, 0), FLinearColor(0.9f, 0.75f, 0.6f), FLinearColor(0.7f, 0.3f, 0.15f), FLinearColor(0.3f, 0.85f, 0.5f), TEXT("Mia"));

    SpawnDirectionalLight(FRotator(-40, 30, 0), 3.0f, FLinearColor(1.0f, 0.97f, 0.92f));
    SpawnLight(FVector(0, 200, 500), 10000.0f, FLinearColor(1.0f, 0.98f, 0.95f));
}

// ==================== ARCADE ====================
void AEmersynGameMode::BuildArcade()
{
    SetupCamera(FVector(0, -600, 350), FRotator(-20, 0, 0));

    SpawnSkyBackground(FLinearColor(0.1f, 0.05f, 0.2f));

    SpawnBox(FVector(0, 0, -10), FVector(30, 30, 0.2f), FLinearColor(0.15f, 0.1f, 0.25f));
    SpawnBox(FVector(0, 400, 200), FVector(30, 0.1f, 8), FLinearColor(0.12f, 0.08f, 0.2f));

    FLinearColor CabinetColors[] = {
        FLinearColor(0.95f, 0.2f, 0.3f), FLinearColor(0.2f, 0.3f, 0.95f),
        FLinearColor(0.15f, 0.9f, 0.3f), FLinearColor(0.95f, 0.8f, 0.1f),
        FLinearColor(0.9f, 0.3f, 0.9f), FLinearColor(0.1f, 0.85f, 0.9f)
    };
    for (int32 i = 0; i < 6; i++)
    {
        float X = (i < 3) ? -250.0f : 250.0f;
        float Y = -200.0f + (i % 3) * 200.0f;
        SpawnBox(FVector(X, Y, 70), FVector(0.4f, 0.3f, 1.3f), CabinetColors[i]);
        SpawnBox(FVector(X + (i < 3 ? 22 : -22), Y, 100), FVector(0.02f, 0.2f, 0.3f), FLinearColor(0.1f, 0.15f, 0.2f));
    }

    SpawnBox(FVector(0, 300, 80), FVector(0.5f, 0.5f, 1.5f), FLinearColor(0.95f, 0.6f, 0.7f));
    SpawnSphere(FVector(-10, 290, 60), 0.06f, FLinearColor(0.95f, 0.5f, 0.5f));
    SpawnSphere(FVector(10, 310, 58), 0.05f, FLinearColor(0.5f, 0.7f, 0.95f));
    SpawnSphere(FVector(5, 295, 62), 0.07f, FLinearColor(0.95f, 0.9f, 0.3f));

    SpawnCharacter(FVector(0, -100, 0), FLinearColor(0.85f, 0.7f, 0.55f), FLinearColor(0.3f, 0.2f, 0.1f), FLinearColor(0.95f, 0.5f, 0.2f), TEXT("Leo"));
    SpawnCharacter(FVector(-100, 50, 0), FLinearColor(0.95f, 0.82f, 0.72f), FLinearColor(0.85f, 0.55f, 0.2f), FLinearColor(0.6f, 0.3f, 0.9f), TEXT("Emersyn"), 0.95f);

    SpawnLight(FVector(-200, -200, 280), 2000.0f, FLinearColor(0.95f, 0.1f, 0.5f));
    SpawnLight(FVector(200, 200, 280), 2000.0f, FLinearColor(0.1f, 0.5f, 0.95f));
    SpawnLight(FVector(0, 0, 300), 3000.0f, FLinearColor(0.7f, 0.3f, 0.9f));
}

// ==================== AMUSEMENT PARK ====================
void AEmersynGameMode::BuildAmusementPark()
{
    SetupCamera(FVector(0, -800, 450), FRotator(-20, 0, 0));

    SpawnSkyBackground(FLinearColor(0.5f, 0.7f, 1.0f));

    SpawnBox(FVector(0, 0, -10), FVector(80, 80, 0.2f), FLinearColor(0.65f, 0.6f, 0.52f));

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

    SpawnCylinder(FVector(300, 200, 20), FVector(1.5f, 1.5f, 0.2f), FLinearColor(0.95f, 0.85f, 0.35f));
    SpawnCylinder(FVector(300, 200, 100), FVector(0.15f, 0.15f, 1.0f), FLinearColor(0.85f, 0.6f, 0.25f));
    SpawnSphere(FVector(350, 250, 50), 0.12f, FLinearColor(0.95f, 0.95f, 0.92f));
    SpawnSphere(FVector(250, 150, 60), 0.12f, FLinearColor(0.85f, 0.6f, 0.4f));

    SpawnBox(FVector(-200, 600, 50), FVector(0.8f, 0.5f, 1.0f), FLinearColor(0.95f, 0.55f, 0.2f));
    SpawnBox(FVector(-200, 575, 110), FVector(0.9f, 0.6f, 0.04f), FLinearColor(0.95f, 0.3f, 0.3f));

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
