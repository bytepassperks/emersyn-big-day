// v13: Sims Mobile-quality detailed furniture with vertex color gradients
// Built on v12 proven pipeline: ProceduralMeshComponent + M_VertexColor material
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

// Sims Mobile-inspired color palette (warm, inviting tones)
namespace SimsColors {
    // Wood tones
    const FLinearColor WoodDark(0.35f, 0.22f, 0.12f);
    const FLinearColor WoodMed(0.55f, 0.38f, 0.22f);
    const FLinearColor WoodLight(0.75f, 0.58f, 0.38f);
    const FLinearColor WoodPine(0.72f, 0.62f, 0.42f);
    
    // Fabric colors
    const FLinearColor FabricPink(0.95f, 0.65f, 0.72f);
    const FLinearColor FabricBlue(0.55f, 0.68f, 0.88f);
    const FLinearColor FabricGreen(0.55f, 0.78f, 0.55f);
    const FLinearColor FabricPurple(0.68f, 0.52f, 0.82f);
    const FLinearColor FabricYellow(0.95f, 0.88f, 0.55f);
    const FLinearColor FabricWhite(0.95f, 0.93f, 0.90f);
    const FLinearColor FabricRed(0.85f, 0.28f, 0.25f);
    
    // Wall colors
    const FLinearColor WallCream(0.95f, 0.92f, 0.85f);
    const FLinearColor WallPink(0.95f, 0.85f, 0.88f);
    const FLinearColor WallBlue(0.82f, 0.88f, 0.95f);
    const FLinearColor WallGreen(0.85f, 0.92f, 0.85f);
    const FLinearColor WallYellow(0.98f, 0.95f, 0.82f);
    const FLinearColor WallLavender(0.88f, 0.82f, 0.95f);
    
    // Floor colors
    const FLinearColor FloorWood(0.72f, 0.55f, 0.35f);
    const FLinearColor FloorTile(0.85f, 0.88f, 0.90f);
    const FLinearColor FloorGrass(0.35f, 0.65f, 0.28f);
    const FLinearColor FloorSand(0.88f, 0.82f, 0.68f);
    const FLinearColor FloorConcrete(0.72f, 0.72f, 0.72f);
    
    // Metal & glass
    const FLinearColor MetalSilver(0.78f, 0.78f, 0.82f);
    const FLinearColor MetalBlack(0.15f, 0.15f, 0.18f);
    const FLinearColor GlassBlue(0.72f, 0.82f, 0.92f);
    const FLinearColor Ceramic(0.95f, 0.95f, 0.97f);
    
    // Nature
    const FLinearColor LeafGreen(0.25f, 0.55f, 0.18f);
    const FLinearColor LeafDark(0.15f, 0.38f, 0.12f);
    const FLinearColor TrunkBrown(0.42f, 0.28f, 0.15f);
    
    // School/office
    const FLinearColor SchoolGold(0.35f, 0.55f, 0.85f);
    
    // Skin tones
    const FLinearColor SkinLight(0.95f, 0.82f, 0.72f);
    const FLinearColor SkinMed(0.85f, 0.68f, 0.52f);
    const FLinearColor SkinTan(0.78f, 0.58f, 0.42f);
    
    // Hair colors
    const FLinearColor HairBlonde(0.88f, 0.75f, 0.42f);
    const FLinearColor HairBrown(0.35f, 0.22f, 0.12f);
    const FLinearColor HairBlack(0.12f, 0.10f, 0.08f);
    const FLinearColor HairPink(0.92f, 0.52f, 0.65f);
    
    // Sky gradients
    const FLinearColor SkyTop(0.25f, 0.45f, 0.75f);
    const FLinearColor SkyBot(0.72f, 0.85f, 0.95f);
    const FLinearColor SkyNightTop(0.08f, 0.05f, 0.15f);
    const FLinearColor SkyNightBot(0.18f, 0.12f, 0.25f);
}

AEmersynGameMode::AEmersynGameMode()
{
    CurrentRoom = TEXT("Splash");
    RoomIndex = 0;
    Timer = 0.0f;
    SplashDuration = 3.0f;
    RoomDuration = 8.0f;
    bInSplash = true;
    PrimaryActorTick.bCanEverTick = true;
    HUDClass = AEmersynHUD::StaticClass();

    // Material loading - CRITICAL fallback chain
    static ConstructorHelpers::FObjectFinder<UMaterialInterface> VMat(TEXT("/Game/Materials/M_VertexColor"));
    if (VMat.Succeeded()) {
        VertexColorMaterial = VMat.Object;
    } else {
        static ConstructorHelpers::FObjectFinder<UMaterialInterface> VMat2(TEXT("/Engine/EngineMaterials/VertexColorViewMode_ColorOnly"));
        if (VMat2.Succeeded()) VertexColorMaterial = VMat2.Object;
        else {
            static ConstructorHelpers::FObjectFinder<UMaterialInterface> VMat3(TEXT("/Engine/EngineMaterials/VertexColorMaterial"));
            if (VMat3.Succeeded()) VertexColorMaterial = VMat3.Object;
            else if (GEngine) VertexColorMaterial = GEngine->VertexColorMaterial;
        }
    }

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
    RoomDisplayNames.Add(TEXT("Shop"));
    RoomDisplayNames.Add(TEXT("Playground"));
    RoomDisplayNames.Add(TEXT("Park"));
    RoomDisplayNames.Add(TEXT("Mall"));
    RoomDisplayNames.Add(TEXT("Arcade"));
    RoomDisplayNames.Add(TEXT("Amusement Park"));
}

void AEmersynGameMode::BeginPlay()
{
    Super::BeginPlay();
    
    // CRITICAL: Destroy all default map geometry (removes WorldGridMaterial checkerboard floor)
    TArray<AActor*> AllActors;
    UGameplayStatics::GetAllActorsOfClass(GetWorld(), AStaticMeshActor::StaticClass(), AllActors);
    for (AActor* Actor : AllActors) {
        Actor->Destroy();
    }
    
    BuildCurrentRoom();
}

void AEmersynGameMode::InitGame(const FString& MapName, const FString& Options, FString& ErrorMessage)
{
    Super::InitGame(MapName, Options, ErrorMessage);
}

void AEmersynGameMode::Tick(float DeltaSeconds)
{
    Super::Tick(DeltaSeconds);
    
    Timer += DeltaSeconds;
    
    if (bInSplash && Timer >= SplashDuration) {
        bInSplash = false;
        Timer = 0.0f;
        RoomIndex = 0;
        CurrentRoom = RoomNames[RoomIndex];
        
        AEmersynHUD* HUD = Cast<AEmersynHUD>(UGameplayStatics::GetPlayerController(GetWorld(), 0)->GetHUD());
        if (HUD) {
            HUD->bShowSplash = false;
            HUD->RoomDisplayName = RoomDisplayNames[RoomIndex];
        }
        
        BuildCurrentRoom();
    }
    else if (!bInSplash && Timer >= RoomDuration) {
        Timer = 0.0f;
        RoomIndex = (RoomIndex + 1) % RoomNames.Num();
        CurrentRoom = RoomNames[RoomIndex];
        
        AEmersynHUD* HUD = Cast<AEmersynHUD>(UGameplayStatics::GetPlayerController(GetWorld(), 0)->GetHUD());
        if (HUD) {
            HUD->RoomDisplayName = RoomDisplayNames[RoomIndex];
        }
        
        BuildCurrentRoom();
    }
    
    if (bInSplash) {
        float Alpha = 1.0f - (Timer / SplashDuration);
        AEmersynHUD* HUD = Cast<AEmersynHUD>(UGameplayStatics::GetPlayerController(GetWorld(), 0)->GetHUD());
        if (HUD) HUD->SplashAlpha = Alpha;
    }
}

void AEmersynGameMode::LoadRoom(const FString& RoomName)
{
    CurrentRoom = RoomName;
    BuildCurrentRoom();
}

void AEmersynGameMode::BuildCurrentRoom()
{
    ClearRoom();
    
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
}

void AEmersynGameMode::ClearRoom()
{
    for (AActor* Actor : SpawnedActors) {
        if (Actor) Actor->Destroy();
    }
    SpawnedActors.Empty();
}

// ============================================================
// PRIMITIVE SPAWNERS (ProceduralMeshComponent + Vertex Colors)
// ============================================================

AActor* AEmersynGameMode::SpawnBox(FVector Loc, FVector Scale, FLinearColor Color)
{
    AActor* Actor = GetWorld()->SpawnActor<AActor>(AActor::StaticClass(), FTransform(FRotator::ZeroRotator, Loc));
    UProceduralMeshComponent* PMC = NewObject<UProceduralMeshComponent>(Actor);
    PMC->RegisterComponent();
    Actor->SetRootComponent(PMC);
    GenerateBoxMesh(PMC, Scale, Color);
    if (VertexColorMaterial) PMC->SetMaterial(0, VertexColorMaterial);
    SpawnedActors.Add(Actor);
    return Actor;
}

AActor* AEmersynGameMode::SpawnGradientBox(FVector Loc, FVector Scale, FLinearColor TopCol, FLinearColor BotCol)
{
    AActor* Actor = GetWorld()->SpawnActor<AActor>(AActor::StaticClass(), FTransform(FRotator::ZeroRotator, Loc));
    UProceduralMeshComponent* PMC = NewObject<UProceduralMeshComponent>(Actor);
    PMC->RegisterComponent();
    Actor->SetRootComponent(PMC);
    GenerateGradientBoxMesh(PMC, Scale, TopCol, BotCol);
    if (VertexColorMaterial) PMC->SetMaterial(0, VertexColorMaterial);
    SpawnedActors.Add(Actor);
    return Actor;
}

AActor* AEmersynGameMode::SpawnSphere(FVector Loc, float Radius, FLinearColor Color)
{
    AActor* Actor = GetWorld()->SpawnActor<AActor>(AActor::StaticClass(), FTransform(FRotator::ZeroRotator, Loc));
    UProceduralMeshComponent* PMC = NewObject<UProceduralMeshComponent>(Actor);
    PMC->RegisterComponent();
    Actor->SetRootComponent(PMC);
    GenerateSphereMesh(PMC, Radius, 16, Color);
    if (VertexColorMaterial) PMC->SetMaterial(0, VertexColorMaterial);
    SpawnedActors.Add(Actor);
    return Actor;
}

AActor* AEmersynGameMode::SpawnCylinder(FVector Loc, FVector Scale, FLinearColor Color)
{
    AActor* Actor = GetWorld()->SpawnActor<AActor>(AActor::StaticClass(), FTransform(FRotator::ZeroRotator, Loc));
    UProceduralMeshComponent* PMC = NewObject<UProceduralMeshComponent>(Actor);
    PMC->RegisterComponent();
    Actor->SetRootComponent(PMC);
    GenerateCylinderMesh(PMC, Scale.X, Scale.Z, 16, Color);
    if (VertexColorMaterial) PMC->SetMaterial(0, VertexColorMaterial);
    SpawnedActors.Add(Actor);
    return Actor;
}

// ============================================================
// MESH GENERATORS
// ============================================================

void AEmersynGameMode::GenerateBoxMesh(UProceduralMeshComponent* PMC, FVector HE, FLinearColor Col)
{
    TArray<FVector> V;
    TArray<int32> T;
    TArray<FVector> N;
    TArray<FVector2D> UV;
    TArray<FLinearColor> VC;

    V = {
        FVector(-HE.X,-HE.Y,-HE.Z), FVector(HE.X,-HE.Y,-HE.Z), FVector(HE.X,HE.Y,-HE.Z), FVector(-HE.X,HE.Y,-HE.Z),
        FVector(-HE.X,-HE.Y,HE.Z), FVector(HE.X,-HE.Y,HE.Z), FVector(HE.X,HE.Y,HE.Z), FVector(-HE.X,HE.Y,HE.Z),
        FVector(-HE.X,-HE.Y,-HE.Z), FVector(-HE.X,HE.Y,-HE.Z), FVector(-HE.X,HE.Y,HE.Z), FVector(-HE.X,-HE.Y,HE.Z),
        FVector(HE.X,-HE.Y,-HE.Z), FVector(HE.X,HE.Y,-HE.Z), FVector(HE.X,HE.Y,HE.Z), FVector(HE.X,-HE.Y,HE.Z),
        FVector(-HE.X,-HE.Y,-HE.Z), FVector(HE.X,-HE.Y,-HE.Z), FVector(HE.X,-HE.Y,HE.Z), FVector(-HE.X,-HE.Y,HE.Z),
        FVector(-HE.X,HE.Y,-HE.Z), FVector(HE.X,HE.Y,-HE.Z), FVector(HE.X,HE.Y,HE.Z), FVector(-HE.X,HE.Y,HE.Z)
    };
    T = {0,2,1, 0,3,2, 4,5,6, 4,6,7, 8,9,10, 8,10,11, 12,14,13, 12,15,14, 16,17,18, 16,18,19, 20,22,21, 20,23,22};
    N = {
        FVector(0,0,-1), FVector(0,0,-1), FVector(0,0,-1), FVector(0,0,-1),
        FVector(0,0,1), FVector(0,0,1), FVector(0,0,1), FVector(0,0,1),
        FVector(-1,0,0), FVector(-1,0,0), FVector(-1,0,0), FVector(-1,0,0),
        FVector(1,0,0), FVector(1,0,0), FVector(1,0,0), FVector(1,0,0),
        FVector(0,-1,0), FVector(0,-1,0), FVector(0,-1,0), FVector(0,-1,0),
        FVector(0,1,0), FVector(0,1,0), FVector(0,1,0), FVector(0,1,0)
    };
    UV.SetNumUninitialized(24);
    for (int i=0; i<24; i++) UV[i] = FVector2D(0,0);
    VC.SetNumUninitialized(24);
    for (int i=0; i<24; i++) VC[i] = Col;

    PMC->CreateMeshSection_LinearColor(0, V, T, N, UV, VC, TArray<FProcMeshTangent>(), true);
}

void AEmersynGameMode::GenerateGradientBoxMesh(UProceduralMeshComponent* PMC, FVector HE, FLinearColor Top, FLinearColor Bot)
{
    TArray<FVector> V;
    TArray<int32> T;
    TArray<FVector> N;
    TArray<FVector2D> UV;
    TArray<FLinearColor> VC;

    V = {
        FVector(-HE.X,-HE.Y,-HE.Z), FVector(HE.X,-HE.Y,-HE.Z), FVector(HE.X,HE.Y,-HE.Z), FVector(-HE.X,HE.Y,-HE.Z),
        FVector(-HE.X,-HE.Y,HE.Z), FVector(HE.X,-HE.Y,HE.Z), FVector(HE.X,HE.Y,HE.Z), FVector(-HE.X,HE.Y,HE.Z),
        FVector(-HE.X,-HE.Y,-HE.Z), FVector(-HE.X,HE.Y,-HE.Z), FVector(-HE.X,HE.Y,HE.Z), FVector(-HE.X,-HE.Y,HE.Z),
        FVector(HE.X,-HE.Y,-HE.Z), FVector(HE.X,HE.Y,-HE.Z), FVector(HE.X,HE.Y,HE.Z), FVector(HE.X,-HE.Y,HE.Z),
        FVector(-HE.X,-HE.Y,-HE.Z), FVector(HE.X,-HE.Y,-HE.Z), FVector(HE.X,-HE.Y,HE.Z), FVector(-HE.X,-HE.Y,HE.Z),
        FVector(-HE.X,HE.Y,-HE.Z), FVector(HE.X,HE.Y,-HE.Z), FVector(HE.X,HE.Y,HE.Z), FVector(-HE.X,HE.Y,HE.Z)
    };
    T = {0,2,1, 0,3,2, 4,5,6, 4,6,7, 8,9,10, 8,10,11, 12,14,13, 12,15,14, 16,17,18, 16,18,19, 20,22,21, 20,23,22};
    N = {
        FVector(0,0,-1), FVector(0,0,-1), FVector(0,0,-1), FVector(0,0,-1),
        FVector(0,0,1), FVector(0,0,1), FVector(0,0,1), FVector(0,0,1),
        FVector(-1,0,0), FVector(-1,0,0), FVector(-1,0,0), FVector(-1,0,0),
        FVector(1,0,0), FVector(1,0,0), FVector(1,0,0), FVector(1,0,0),
        FVector(0,-1,0), FVector(0,-1,0), FVector(0,-1,0), FVector(0,-1,0),
        FVector(0,1,0), FVector(0,1,0), FVector(0,1,0), FVector(0,1,0)
    };
    UV.SetNumUninitialized(24);
    for (int i=0; i<24; i++) UV[i] = FVector2D(0,0);
    
    // Gradient: bottom faces darker, top faces brighter
    VC.SetNumUninitialized(24);
    for (int i=0; i<4; i++) VC[i] = Bot;      // Bottom face
    for (int i=4; i<8; i++) VC[i] = Top;      // Top face
    for (int i=8; i<12; i++) VC[i] = FMath::Lerp(Bot, Top, 0.5f);  // Left
    for (int i=12; i<16; i++) VC[i] = FMath::Lerp(Bot, Top, 0.5f); // Right
    for (int i=16; i<20; i++) VC[i] = Bot;    // Front
    for (int i=20; i<24; i++) VC[i] = Top;    // Back

    PMC->CreateMeshSection_LinearColor(0, V, T, N, UV, VC, TArray<FProcMeshTangent>(), true);
}

void AEmersynGameMode::GenerateSphereMesh(UProceduralMeshComponent* PMC, float R, int32 Seg, FLinearColor Col)
{
    TArray<FVector> V;
    TArray<int32> T;
    TArray<FVector> N;
    TArray<FVector2D> UV;
    TArray<FLinearColor> VC;

    for (int lat=0; lat<=Seg; lat++) {
        float theta = PI * lat / Seg;
        float sinT = FMath::Sin(theta);
        float cosT = FMath::Cos(theta);
        for (int lon=0; lon<=Seg; lon++) {
            float phi = 2 * PI * lon / Seg;
            float x = R * sinT * FMath::Cos(phi);
            float y = R * sinT * FMath::Sin(phi);
            float z = R * cosT;
            V.Add(FVector(x, y, z));
            N.Add(FVector(x, y, z).GetSafeNormal());
            UV.Add(FVector2D((float)lon/Seg, (float)lat/Seg));
            VC.Add(Col);
        }
    }

    for (int lat=0; lat<Seg; lat++) {
        for (int lon=0; lon<Seg; lon++) {
            int c = lat * (Seg+1) + lon;
            int n = c + Seg + 1;
            T.Add(c); T.Add(n); T.Add(c+1);
            T.Add(c+1); T.Add(n); T.Add(n+1);
        }
    }

    PMC->CreateMeshSection_LinearColor(0, V, T, N, UV, VC, TArray<FProcMeshTangent>(), true);
}

void AEmersynGameMode::GenerateCylinderMesh(UProceduralMeshComponent* PMC, float R, float HH, int32 Seg, FLinearColor Col)
{
    TArray<FVector> V;
    TArray<int32> T;
    TArray<FVector> N;
    TArray<FVector2D> UV;
    TArray<FLinearColor> VC;

    // Side vertices
    for (int i=0; i<=Seg; i++) {
        float a = 2*PI*i/Seg;
        float x = R * FMath::Cos(a);
        float y = R * FMath::Sin(a);
        V.Add(FVector(x, y, -HH));
        N.Add(FVector(x, y, 0).GetSafeNormal());
        UV.Add(FVector2D((float)i/Seg, 0));
        VC.Add(Col);
        V.Add(FVector(x, y, HH));
        N.Add(FVector(x, y, 0).GetSafeNormal());
        UV.Add(FVector2D((float)i/Seg, 1));
        VC.Add(Col);
    }

    // Side triangles
    for (int i=0; i<Seg; i++) {
        int b = i*2;
        T.Add(b); T.Add(b+2); T.Add(b+1);
        T.Add(b+1); T.Add(b+2); T.Add(b+3);
    }

    PMC->CreateMeshSection_LinearColor(0, V, T, N, UV, VC, TArray<FProcMeshTangent>(), true);
}

// ============================================================
// DETAILED FURNITURE BUILDERS
// ============================================================

void AEmersynGameMode::SpawnBed(FVector L, FLinearColor Frame, FLinearColor Sheet, FLinearColor Pillow)
{
    // Frame (dark gradient)
    SpawnGradientBox(L + FVector(0,0,15), FVector(120,80,15), Frame * 1.1f, Frame * 0.7f);
    // Mattress (lighter gradient)
    SpawnGradientBox(L + FVector(0,0,35), FVector(115,75,10), Sheet * 1.15f, Sheet * 0.85f);
    // Headboard
    SpawnGradientBox(L + FVector(0,-85,50), FVector(120,5,30), Frame * 1.1f, Frame * 0.8f);
    // Pillows
    SpawnBox(L + FVector(-40,-50,50), FVector(25,15,8), Pillow);
    SpawnBox(L + FVector(40,-50,50), FVector(25,15,8), Pillow);
    // Legs (4 corners)
    SpawnBox(L + FVector(-100,-70,0), FVector(5,5,15), Frame * 0.6f);
    SpawnBox(L + FVector(100,-70,0), FVector(5,5,15), Frame * 0.6f);
    SpawnBox(L + FVector(-100,70,0), FVector(5,5,15), Frame * 0.6f);
    SpawnBox(L + FVector(100,70,0), FVector(5,5,15), Frame * 0.6f);
}

void AEmersynGameMode::SpawnSofa(FVector L, FLinearColor Fabric, FLinearColor Cushion)
{
    // Base frame
    SpawnGradientBox(L + FVector(0,0,25), FVector(150,40,20), Fabric * 0.75f, Fabric * 0.55f);
    // Backrest
    SpawnGradientBox(L + FVector(0,-35,60), FVector(150,8,35), Fabric * 1.05f, Fabric * 0.85f);
    // Left armrest
    SpawnGradientBox(L + FVector(-140,0,45), FVector(10,40,40), Fabric * 0.95f, Fabric * 0.7f);
    // Right armrest
    SpawnGradientBox(L + FVector(140,0,45), FVector(10,40,40), Fabric * 0.95f, Fabric * 0.7f);
    // Cushions (3)
    SpawnGradientBox(L + FVector(-80,0,45), FVector(35,35,12), Cushion * 1.2f, Cushion * 0.9f);
    SpawnGradientBox(L + FVector(0,0,45), FVector(35,35,12), Cushion * 1.2f, Cushion * 0.9f);
    SpawnGradientBox(L + FVector(80,0,45), FVector(35,35,12), Cushion * 1.2f, Cushion * 0.9f);
}

void AEmersynGameMode::SpawnTable(FVector L, FLinearColor Top, FLinearColor Leg)
{
    // Tabletop (gradient top brighter)
    SpawnGradientBox(L + FVector(0,0,70), FVector(80,60,5), Top * 1.15f, Top * 0.95f);
    // 4 legs
    SpawnBox(L + FVector(-70,-50,35), FVector(5,5,35), Leg * 0.75f);
    SpawnBox(L + FVector(70,-50,35), FVector(5,5,35), Leg * 0.75f);
    SpawnBox(L + FVector(-70,50,35), FVector(5,5,35), Leg * 0.75f);
    SpawnBox(L + FVector(70,50,35), FVector(5,5,35), Leg * 0.75f);
}

void AEmersynGameMode::SpawnChair(FVector L, FLinearColor Col)
{
    // Seat
    SpawnGradientBox(L + FVector(0,0,45), FVector(25,25,5), Col * 1.1f, Col * 0.85f);
    // Backrest
    SpawnGradientBox(L + FVector(0,-22,70), FVector(25,5,25), Col * 1.05f, Col * 0.8f);
    // 4 legs
    SpawnBox(L + FVector(-20,-20,22), FVector(3,3,22), Col * 0.7f);
    SpawnBox(L + FVector(20,-20,22), FVector(3,3,22), Col * 0.7f);
    SpawnBox(L + FVector(-20,20,22), FVector(3,3,22), Col * 0.7f);
    SpawnBox(L + FVector(20,20,22), FVector(3,3,22), Col * 0.7f);
}

void AEmersynGameMode::SpawnBookshelf(FVector L, FLinearColor Wood)
{
    // Frame (tall vertical)
    SpawnGradientBox(L + FVector(0,0,100), FVector(60,15,100), Wood * 1.05f, Wood * 0.75f);
    // 4 shelves
    SpawnBox(L + FVector(0,0,50), FVector(58,14,3), Wood * 0.9f);
    SpawnBox(L + FVector(0,0,100), FVector(58,14,3), Wood * 0.9f);
    SpawnBox(L + FVector(0,0,150), FVector(58,14,3), Wood * 0.9f);
    SpawnBox(L + FVector(0,0,190), FVector(58,14,3), Wood * 0.9f);
    // Books (colored boxes)
    SpawnBox(L + FVector(-40,0,58), FVector(8,12,18), SimsColors::FabricRed);
    SpawnBox(L + FVector(-20,0,58), FVector(8,12,20), SimsColors::FabricBlue);
    SpawnBox(L + FVector(0,0,58), FVector(8,12,16), SimsColors::FabricGreen);
    SpawnBox(L + FVector(20,0,58), FVector(8,12,22), SimsColors::FabricYellow);
    SpawnBox(L + FVector(-30,0,108), FVector(8,12,17), SimsColors::FabricPurple);
    SpawnBox(L + FVector(-10,0,108), FVector(8,12,19), SimsColors::FabricPink);
    SpawnBox(L + FVector(10,0,108), FVector(8,12,15), SimsColors::FabricBlue);
    SpawnBox(L + FVector(30,0,108), FVector(8,12,21), SimsColors::FabricGreen);
}

void AEmersynGameMode::SpawnTV(FVector L)
{
    // Screen (dark glass)
    SpawnBox(L + FVector(0,0,80), FVector(70,5,45), SimsColors::MetalBlack);
    // Stand base
    SpawnGradientBox(L + FVector(0,0,30), FVector(40,15,5), SimsColors::MetalSilver * 1.1f, SimsColors::MetalSilver * 0.8f);
    // Stand pole
    SpawnBox(L + FVector(0,0,55), FVector(5,5,25), SimsColors::MetalSilver * 0.9f);
}

void AEmersynGameMode::SpawnStove(FVector L)
{
    // Body
    SpawnGradientBox(L + FVector(0,0,45), FVector(50,40,45), SimsColors::Ceramic * 1.05f, SimsColors::Ceramic * 0.85f);
    // 4 burners
    SpawnBox(L + FVector(-20,-15,92), FVector(12,12,3), SimsColors::MetalBlack);
    SpawnBox(L + FVector(20,-15,92), FVector(12,12,3), SimsColors::MetalBlack);
    SpawnBox(L + FVector(-20,15,92), FVector(12,12,3), SimsColors::MetalBlack);
    SpawnBox(L + FVector(20,15,92), FVector(12,12,3), SimsColors::MetalBlack);
    // Oven door handle
    SpawnBox(L + FVector(0,42,65), FVector(40,3,3), SimsColors::MetalSilver);
}

void AEmersynGameMode::SpawnFridge(FVector L)
{
    // Body (tall gradient)
    SpawnGradientBox(L + FVector(0,0,100), FVector(45,40,100), SimsColors::Ceramic * 1.1f, SimsColors::Ceramic * 0.9f);
    // Handle (top door)
    SpawnBox(L + FVector(0,42,140), FVector(5,3,30), SimsColors::MetalSilver);
    // Handle (bottom door)
    SpawnBox(L + FVector(0,42,60), FVector(5,3,20), SimsColors::MetalSilver);
}

void AEmersynGameMode::SpawnSink(FVector L)
{
    // Counter
    SpawnGradientBox(L + FVector(0,0,75), FVector(60,35,5), SimsColors::FloorTile * 1.05f, SimsColors::FloorTile * 0.9f);
    // Basin (recessed)
    SpawnBox(L + FVector(0,0,68), FVector(25,25,8), SimsColors::Ceramic * 0.95f);
    // Faucet base
    SpawnCylinder(L + FVector(0,-30,85), FVector(4,4,10), SimsColors::MetalSilver);
    // Faucet spout
    SpawnBox(L + FVector(0,-10,95), FVector(3,20,3), SimsColors::MetalSilver);
}

void AEmersynGameMode::SpawnBathtub(FVector L)
{
    // Outer shell (gradient)
    SpawnGradientBox(L + FVector(0,0,30), FVector(90,45,30), SimsColors::Ceramic * 1.1f, SimsColors::Ceramic * 0.85f);
    // Inner basin (darker)
    SpawnBox(L + FVector(0,0,45), FVector(80,40,15), SimsColors::Ceramic * 0.75f);
    // Faucet
    SpawnCylinder(L + FVector(0,-45,70), FVector(5,5,10), SimsColors::MetalSilver);
}

void AEmersynGameMode::SpawnToilet(FVector L)
{
    // Bowl
    SpawnGradientBox(L + FVector(0,0,30), FVector(25,35,20), SimsColors::Ceramic * 1.1f, SimsColors::Ceramic * 0.9f);
    // Tank
    SpawnGradientBox(L + FVector(0,-30,55), FVector(25,20,25), SimsColors::Ceramic * 1.05f, SimsColors::Ceramic * 0.85f);
    // Lid
    SpawnBox(L + FVector(0,0,52), FVector(26,36,3), SimsColors::Ceramic * 1.15f);
}

void AEmersynGameMode::SpawnLamp(FVector L, FLinearColor Shade)
{
    // Base
    SpawnCylinder(L + FVector(0,0,10), FVector(12,12,10), SimsColors::WoodDark);
    // Pole
    SpawnCylinder(L + FVector(0,0,50), FVector(3,3,40), SimsColors::MetalSilver * 0.85f);
    // Shade
    SpawnCylinder(L + FVector(0,0,95), FVector(20,20,15), Shade);
}

void AEmersynGameMode::SpawnRug(FVector L, FLinearColor C1, FLinearColor C2, FVector Size)
{
    // Flat rectangle on floor with gradient
    SpawnGradientBox(L + FVector(0,0,2), Size, C1, C2);
}

void AEmersynGameMode::SpawnPlant(FVector L, float S)
{
    // Pot
    SpawnCylinder(L + FVector(0,0,15*S), FVector(15*S,15*S,15*S), SimsColors::TrunkBrown);
    // Stem
    SpawnCylinder(L + FVector(0,0,40*S), FVector(3*S,3*S,25*S), SimsColors::LeafGreen);
    // Leaves (sphere)
    SpawnSphere(L + FVector(0,0,70*S), 20*S, SimsColors::LeafGreen);
}

void AEmersynGameMode::SpawnTree(FVector L, float S)
{
    // Trunk
    SpawnCylinder(L + FVector(0,0,80*S), FVector(15*S,15*S,80*S), SimsColors::TrunkBrown);
    // Canopy
    SpawnSphere(L + FVector(0,0,180*S), 60*S, SimsColors::LeafGreen);
    // Top accent
    SpawnSphere(L + FVector(0,0,220*S), 35*S, SimsColors::LeafDark);
}

void AEmersynGameMode::SpawnSwing(FVector L)
{
    // Left pole
    SpawnBox(L + FVector(-80,0,100), FVector(5,5,100), SimsColors::MetalSilver * 0.8f);
    // Right pole
    SpawnBox(L + FVector(80,0,100), FVector(5,5,100), SimsColors::MetalSilver * 0.8f);
    // Top bar
    SpawnBox(L + FVector(0,0,200), FVector(80,5,5), SimsColors::MetalSilver * 0.9f);
    // Seat
    SpawnBox(L + FVector(0,0,50), FVector(30,25,5), SimsColors::FabricRed);
    // Chains (visual only, 2 per side)
    SpawnCylinder(L + FVector(-15,0,125), FVector(2,2,75), SimsColors::MetalSilver * 0.7f);
    SpawnCylinder(L + FVector(15,0,125), FVector(2,2,75), SimsColors::MetalSilver * 0.7f);
}

void AEmersynGameMode::SpawnSlide(FVector L)
{
    // Platform
    SpawnGradientBox(L + FVector(0,-80,100), FVector(40,30,5), SimsColors::FabricYellow * 1.1f, SimsColors::FabricYellow * 0.85f);
    // Slide surface (angled plane approximated with boxes)
    for (int i=0; i<8; i++) {
        float z = 90 - i*12;
        float y = -60 + i*15;
        SpawnBox(L + FVector(0,y,z), FVector(38,12,3), SimsColors::FabricBlue * (1.0f - i*0.05f));
    }
    // Support poles
    SpawnBox(L + FVector(-30,-80,50), FVector(5,5,50), SimsColors::MetalSilver * 0.75f);
    SpawnBox(L + FVector(30,-80,50), FVector(5,5,50), SimsColors::MetalSilver * 0.75f);
}

void AEmersynGameMode::SpawnBench(FVector L)
{
    // Seat
    SpawnGradientBox(L + FVector(0,0,50), FVector(80,25,5), SimsColors::WoodMed * 1.15f, SimsColors::WoodMed * 0.9f);
    // Backrest
    SpawnGradientBox(L + FVector(0,-22,75), FVector(80,5,25), SimsColors::WoodMed * 1.1f, SimsColors::WoodMed * 0.85f);
    // Legs
    SpawnBox(L + FVector(-60,0,25), FVector(5,20,25), SimsColors::WoodDark * 0.8f);
    SpawnBox(L + FVector(60,0,25), FVector(5,20,25), SimsColors::WoodDark * 0.8f);
}

void AEmersynGameMode::SpawnShopShelf(FVector L, FLinearColor Col)
{
    // Frame
    SpawnGradientBox(L + FVector(0,0,100), FVector(70,20,100), Col * 1.05f, Col * 0.8f);
    // 3 shelves
    SpawnBox(L + FVector(0,0,60), FVector(68,18,3), Col * 0.95f);
    SpawnBox(L + FVector(0,0,110), FVector(68,18,3), Col * 0.95f);
    SpawnBox(L + FVector(0,0,160), FVector(68,18,3), Col * 0.95f);
    // Items (colored boxes)
    SpawnBox(L + FVector(-40,0,70), FVector(10,15,12), SimsColors::FabricRed);
    SpawnBox(L + FVector(-10,0,70), FVector(10,15,15), SimsColors::FabricBlue);
    SpawnBox(L + FVector(20,0,70), FVector(10,15,10), SimsColors::FabricGreen);
}

void AEmersynGameMode::SpawnArcadeMachine(FVector L, FLinearColor Col)
{
    // Cabinet body
    SpawnGradientBox(L + FVector(0,0,80), FVector(40,35,80), Col * 1.05f, Col * 0.75f);
    // Screen
    SpawnBox(L + FVector(0,-30,120), FVector(35,5,40), SimsColors::MetalBlack);
    // Control panel
    SpawnGradientBox(L + FVector(0,0,60), FVector(38,40,5), Col * 0.95f, Col * 0.7f);
    // Joystick
    SpawnCylinder(L + FVector(-15,5,70), FVector(4,4,10), SimsColors::FabricRed);
}

void AEmersynGameMode::SpawnDesk(FVector L, FLinearColor Col)
{
    // Tabletop
    SpawnGradientBox(L + FVector(0,0,70), FVector(90,50,5), Col * 1.15f, Col * 0.95f);
    // Drawer
    SpawnGradientBox(L + FVector(0,35,50), FVector(40,10,15), Col * 0.95f, Col * 0.75f);
    // Legs
    SpawnBox(L + FVector(-80,-40,35), FVector(5,5,35), Col * 0.7f);
    SpawnBox(L + FVector(80,-40,35), FVector(5,5,35), Col * 0.7f);
    SpawnBox(L + FVector(-80,40,35), FVector(5,5,35), Col * 0.7f);
    SpawnBox(L + FVector(80,40,35), FVector(5,5,35), Col * 0.7f);
}

void AEmersynGameMode::SpawnWindow(FVector L, FLinearColor Frame, FLinearColor Glass)
{
    // Frame (vertical)
    SpawnGradientBox(L + FVector(0,0,100), FVector(60,8,80), Frame * 1.05f, Frame * 0.85f);
    // Glass panes (4 quadrants)
    SpawnBox(L + FVector(-20,-5,120), FVector(25,3,30), Glass);
    SpawnBox(L + FVector(20,-5,120), FVector(25,3,30), Glass);
    SpawnBox(L + FVector(-20,-5,80), FVector(25,3,30), Glass);
    SpawnBox(L + FVector(20,-5,80), FVector(25,3,30), Glass);
}

void AEmersynGameMode::SpawnPainting(FVector L, FLinearColor Frame, FLinearColor Canvas)
{
    // Frame
    SpawnBox(L + FVector(0,0,100), FVector(50,5,60), Frame);
    // Canvas
    SpawnBox(L + FVector(0,-3,100), FVector(45,2,55), Canvas);
}

void AEmersynGameMode::SpawnFountain(FVector L)
{
    // Base (large)
    SpawnCylinder(L + FVector(0,0,20), FVector(60,60,20), SimsColors::FloorConcrete);
    // Pillar
    SpawnCylinder(L + FVector(0,0,60), FVector(15,15,40), SimsColors::Ceramic);
    // Top bowl
    SpawnCylinder(L + FVector(0,0,105), FVector(30,30,15), SimsColors::Ceramic * 1.1f);
    // Water (blue sphere at top)
    SpawnSphere(L + FVector(0,0,120), 12, SimsColors::GlassBlue);
}

void AEmersynGameMode::SpawnFerrisWheel(FVector L)
{
    // Central axis
    SpawnCylinder(L + FVector(0,0,150), FVector(10,10,150), SimsColors::MetalSilver * 0.8f);
    // Wheel rim (8 spokes approximated)
    for (int i=0; i<8; i++) {
        float angle = PI * 2 * i / 8;
        float x = 120 * FMath::Cos(angle);
        float z = 120 * FMath::Sin(angle) + 150;
        SpawnBox(L + FVector(x,0,z), FVector(5,5,120), SimsColors::FabricYellow * (0.8f + i*0.03f));
    }
    // Gondolas (8)
    for (int i=0; i<8; i++) {
        float angle = PI * 2 * i / 8;
        float x = 115 * FMath::Cos(angle);
        float z = 115 * FMath::Sin(angle) + 150;
        SpawnBox(L + FVector(x,0,z), FVector(15,20,20), SimsColors::FabricRed);
    }
}

void AEmersynGameMode::SpawnCarousel(FVector L)
{
    // Platform
    SpawnCylinder(L + FVector(0,0,10), FVector(100,100,10), SimsColors::FabricYellow);
    // Central pole
    SpawnCylinder(L + FVector(0,0,100), FVector(15,15,90), SimsColors::MetalSilver);
    // Roof
    SpawnCylinder(L + FVector(0,0,200), FVector(110,110,20), SimsColors::FabricRed);
    // 4 horses (simplified as colored boxes)
    SpawnBox(L + FVector(50,0,50), FVector(15,25,30), SimsColors::FabricWhite);
    SpawnBox(L + FVector(-50,0,50), FVector(15,25,30), SimsColors::FabricPink);
    SpawnBox(L + FVector(0,50,50), FVector(15,25,30), SimsColors::FabricBlue);
    SpawnBox(L + FVector(0,-50,50), FVector(15,25,30), SimsColors::FabricGreen);
}

void AEmersynGameMode::SpawnCounter(FVector L, FLinearColor Col)
{
    // Counter top
    SpawnGradientBox(L + FVector(0,0,90), FVector(100,40,5), Col * 1.15f, Col * 0.95f);
    // Base cabinet
    SpawnGradientBox(L + FVector(0,0,45), FVector(95,38,45), Col * 0.9f, Col * 0.7f);
}

void AEmersynGameMode::SpawnCabinet(FVector L, FLinearColor Col)
{
    // Box
    SpawnGradientBox(L + FVector(0,0,80), FVector(50,30,80), Col * 1.05f, Col * 0.8f);
    // 2 doors (visual hint)
    SpawnBox(L + FVector(-15,32,80), FVector(15,2,70), Col * 0.95f);
    SpawnBox(L + FVector(15,32,80), FVector(15,2,70), Col * 0.95f);
}

// ============================================================
// CHARACTER & PET SPAWNERS
// ============================================================

void AEmersynGameMode::SpawnCharacter(FVector Loc, FLinearColor Skin, FLinearColor Hair, FLinearColor Outfit, const FString& Name, float Scale)
{
    // Head (sphere)
    SpawnSphere(Loc + FVector(0,0,150*Scale), 20*Scale, Skin);
    // Hair (smaller sphere on top)
    SpawnSphere(Loc + FVector(0,0,170*Scale), 22*Scale, Hair);
    // Body (box)
    SpawnGradientBox(Loc + FVector(0,0,100*Scale), FVector(18*Scale,12*Scale,30*Scale), Outfit * 1.1f, Outfit * 0.85f);
    // Arms
    SpawnCylinder(Loc + FVector(-25*Scale,0,110*Scale), FVector(5*Scale,5*Scale,25*Scale), Skin * 0.95f);
    SpawnCylinder(Loc + FVector(25*Scale,0,110*Scale), FVector(5*Scale,5*Scale,25*Scale), Skin * 0.95f);
    // Legs
    SpawnCylinder(Loc + FVector(-10*Scale,0,40*Scale), FVector(6*Scale,6*Scale,35*Scale), Outfit * 0.8f);
    SpawnCylinder(Loc + FVector(10*Scale,0,40*Scale), FVector(6*Scale,6*Scale,35*Scale), Outfit * 0.8f);
}

void AEmersynGameMode::SpawnPet(FVector Loc, FLinearColor Body, FLinearColor Accent, const FString& Name, float Scale)
{
    // Body (horizontal box)
    SpawnGradientBox(Loc + FVector(0,0,30*Scale), FVector(25*Scale,15*Scale,15*Scale), Body * 1.1f, Body * 0.85f);
    // Head (sphere)
    SpawnSphere(Loc + FVector(30*Scale,0,35*Scale), 12*Scale, Body);
    // Ears (accent color)
    SpawnBox(Loc + FVector(35*Scale,-8*Scale,48*Scale), FVector(3*Scale,3*Scale,8*Scale), Accent);
    SpawnBox(Loc + FVector(35*Scale,8*Scale,48*Scale), FVector(3*Scale,3*Scale,8*Scale), Accent);
    // Legs (4)
    SpawnCylinder(Loc + FVector(-15*Scale,-10*Scale,15*Scale), FVector(3*Scale,3*Scale,15*Scale), Body * 0.75f);
    SpawnCylinder(Loc + FVector(-15*Scale,10*Scale,15*Scale), FVector(3*Scale,3*Scale,15*Scale), Body * 0.75f);
    SpawnCylinder(Loc + FVector(15*Scale,-10*Scale,15*Scale), FVector(3*Scale,3*Scale,15*Scale), Body * 0.75f);
    SpawnCylinder(Loc + FVector(15*Scale,10*Scale,15*Scale), FVector(3*Scale,3*Scale,15*Scale), Body * 0.75f);
}

// ============================================================
// LIGHTING
// ============================================================

void AEmersynGameMode::SpawnLight(FVector Loc, float Intensity, FLinearColor Color)
{
    APointLight* Light = GetWorld()->SpawnActor<APointLight>(APointLight::StaticClass(), FTransform(FRotator::ZeroRotator, Loc));
    if (Light && Light->PointLightComponent) {
        Light->PointLightComponent->SetIntensity(Intensity);
        Light->PointLightComponent->SetLightColor(Color);
        Light->PointLightComponent->SetAttenuationRadius(1000.0f);
    }
    SpawnedActors.Add(Light);
}

void AEmersynGameMode::SpawnDirectionalLight(FRotator Rot, float Intensity, FLinearColor Color)
{
    ADirectionalLight* Light = GetWorld()->SpawnActor<ADirectionalLight>(ADirectionalLight::StaticClass(), FTransform(Rot, FVector::ZeroVector));
    if (Light) {
        UDirectionalLightComponent* DLC = Cast<UDirectionalLightComponent>(Light->GetLightComponent());
        if (DLC) {
            DLC->SetIntensity(Intensity);
            DLC->SetLightColor(Color);
        }
    }
    SpawnedActors.Add(Light);
}

// ============================================================
// ENVIRONMENT BUILDERS
// ============================================================

void AEmersynGameMode::SpawnSkyBackground(FLinearColor TopColor, FLinearColor BottomColor)
{
    // Huge gradient sphere encompassing the entire scene
    SpawnGradientBox(FVector(0,0,0), FVector(8000,8000,4000), TopColor, BottomColor);
}

void AEmersynGameMode::SpawnRoomShell(FVector Size, FLinearColor FloorCol, FLinearColor WallCol, FLinearColor CeilCol)
{
    float W = Size.X;
    float D = Size.Y;
    float H = Size.Z;
    
    // Floor (LARGE to cover any default ground)
    SpawnGradientBox(FVector(0,0,-10), FVector(W,D,5), FloorCol * 1.1f, FloorCol * 0.95f);
    
    // Walls (4 sides with gradient)
    SpawnGradientBox(FVector(-W,0,H/2), FVector(8,D,H/2), WallCol * 1.05f, WallCol * 0.85f);  // Left
    SpawnGradientBox(FVector(W,0,H/2), FVector(8,D,H/2), WallCol * 1.05f, WallCol * 0.85f);   // Right
    SpawnGradientBox(FVector(0,-D,H/2), FVector(W,8,H/2), WallCol * 1.05f, WallCol * 0.85f);  // Back
    SpawnGradientBox(FVector(0,D,H/2), FVector(W,8,H/2), WallCol * 1.05f, WallCol * 0.85f);   // Front
    
    // Ceiling
    SpawnBox(FVector(0,0,H), FVector(W,D,5), CeilCol);
}

void AEmersynGameMode::SetupCamera(FVector Loc, FRotator Rot)
{
    APlayerController* PC = UGameplayStatics::GetPlayerController(GetWorld(), 0);
    if (PC) {
        PC->SetViewTarget(PC->GetPawn());
        ACameraActor* Cam = GetWorld()->SpawnActor<ACameraActor>(ACameraActor::StaticClass(), FTransform(Rot, Loc));
        if (Cam) {
            PC->SetViewTarget(Cam);
            SpawnedActors.Add(Cam);
        }
    }
}

// ============================================================
// ROOM BUILDERS
// ============================================================

void AEmersynGameMode::BuildSplashScreen()
{
    SpawnSkyBackground(SimsColors::SkyNightTop, SimsColors::SkyNightBot);
    SetupCamera(FVector(0, -800, 400), FRotator(-20, 0, 0));
}

void AEmersynGameMode::BuildMainMenu()
{
    SpawnSkyBackground(SimsColors::SkyTop, SimsColors::SkyBot);
    
    // Title decoration (colorful boxes)
    SpawnBox(FVector(-200,0,150), FVector(30,30,30), SimsColors::FabricPink);
    SpawnSphere(FVector(200,0,150), 35, SimsColors::FabricBlue);
    SpawnBox(FVector(0,-150,100), FVector(25,25,25), SimsColors::FabricYellow);
    
    SetupCamera(FVector(0, -600, 300), FRotator(-15, 0, 0));
}

void AEmersynGameMode::BuildBedroom()
{
    SpawnSkyBackground(SimsColors::SkyTop, SimsColors::SkyBot);
    SpawnRoomShell(FVector(600,600,300), SimsColors::FloorWood, SimsColors::WallPink, SimsColors::WallCream);
    
    // Furniture
    SpawnBed(FVector(-200, -300, 0), SimsColors::WoodDark, SimsColors::FabricPink, SimsColors::FabricWhite);
    SpawnDesk(FVector(250, -250, 0), SimsColors::WoodMed);
    SpawnChair(FVector(250, -150, 0), SimsColors::FabricPurple);
    SpawnBookshelf(FVector(350, 200, 0), SimsColors::WoodLight);
    SpawnLamp(FVector(-350, -350, 0), SimsColors::FabricYellow);
    SpawnRug(FVector(0, 0, 0), SimsColors::FabricPink, SimsColors::FabricWhite, FVector(200,150,2));
    SpawnWindow(FVector(-600, 0, 100), SimsColors::WoodDark, SimsColors::GlassBlue);
    SpawnPainting(FVector(600, -200, 120), SimsColors::WoodDark, SimsColors::FabricBlue);
    
    // Character
    SpawnCharacter(FVector(100, 0, 0), SimsColors::SkinLight, SimsColors::HairBrown, SimsColors::FabricPink, TEXT("Emersyn"));
    
    SetupCamera(FVector(0, -900, 400), FRotator(-20, 0, 0));
}

void AEmersynGameMode::BuildKitchen()
{
    SpawnSkyBackground(SimsColors::SkyTop, SimsColors::SkyBot);
    SpawnRoomShell(FVector(600,600,300), SimsColors::FloorTile, SimsColors::WallYellow, SimsColors::WallCream);
    
    // Appliances
    SpawnStove(FVector(-250, -300, 0));
    SpawnFridge(FVector(-400, -300, 0));
    SpawnSink(FVector(0, -300, 0));
    SpawnCounter(FVector(200, -300, 0), SimsColors::WoodMed);
    
    // Dining
    SpawnTable(FVector(0, 150, 0), SimsColors::WoodLight, SimsColors::WoodDark);
    SpawnChair(FVector(-80, 80, 0), SimsColors::WoodMed);
    SpawnChair(FVector(80, 80, 0), SimsColors::WoodMed);
    SpawnChair(FVector(-80, 220, 0), SimsColors::WoodMed);
    SpawnChair(FVector(80, 220, 0), SimsColors::WoodMed);
    
    // Decor
    SpawnPlant(FVector(350, 200, 0), 0.8f);
    
    SetupCamera(FVector(0, -900, 400), FRotator(-20, 0, 0));
}

void AEmersynGameMode::BuildBathroom()
{
    SpawnSkyBackground(SimsColors::SkyTop, SimsColors::SkyBot);
    SpawnRoomShell(FVector(500,500,300), SimsColors::FloorTile, SimsColors::WallBlue, SimsColors::WallCream);
    
    SpawnBathtub(FVector(-200, -200, 0));
    SpawnToilet(FVector(200, -200, 0));
    SpawnSink(FVector(0, 200, 70));
    SpawnRug(FVector(0, 0, 0), SimsColors::FabricBlue, SimsColors::FabricWhite, FVector(150,150,2));
    
    SetupCamera(FVector(0, -750, 350), FRotator(-22, 0, 0));
}

void AEmersynGameMode::BuildLivingRoom()
{
    SpawnSkyBackground(SimsColors::SkyTop, SimsColors::SkyBot);
    SpawnRoomShell(FVector(700,700,300), SimsColors::FloorWood, SimsColors::WallCream, SimsColors::WallCream);
    
    SpawnSofa(FVector(0, 0, 0), SimsColors::FabricBlue, SimsColors::FabricYellow);
    SpawnTV(FVector(0, -400, 0));
    SpawnBookshelf(FVector(-400, 300, 0), SimsColors::WoodMed);
    SpawnLamp(FVector(-350, -350, 0), SimsColors::FabricWhite);
    SpawnRug(FVector(0, -100, 0), SimsColors::FabricRed, SimsColors::FabricYellow, FVector(250,200,2));
    SpawnPlant(FVector(350, 350, 0), 1.0f);
    
    // Pet
    SpawnPet(FVector(150, 50, 0), SimsColors::TrunkBrown, SimsColors::FabricWhite, TEXT("Dog"));
    
    SetupCamera(FVector(0, -1000, 450), FRotator(-20, 0, 0));
}

void AEmersynGameMode::BuildGarden()
{
    SpawnSkyBackground(SimsColors::SkyTop, SimsColors::SkyBot);
    SpawnBox(FVector(0,0,-10), FVector(1500,1500,5), SimsColors::FloorGrass);
    
    // Trees
    SpawnTree(FVector(-400, -400, 0), 1.2f);
    SpawnTree(FVector(400, -400, 0), 1.0f);
    SpawnTree(FVector(-400, 400, 0), 1.1f);
    SpawnTree(FVector(400, 400, 0), 1.3f);
    
    // Plants
    SpawnPlant(FVector(-200, 0, 0), 1.0f);
    SpawnPlant(FVector(200, 0, 0), 1.0f);
    SpawnPlant(FVector(0, -200, 0), 0.9f);
    SpawnPlant(FVector(0, 200, 0), 1.1f);
    
    SpawnBench(FVector(0, 0, 0));
    SpawnFountain(FVector(0, 350, 0));
    
    SetupCamera(FVector(0, -1200, 500), FRotator(-18, 0, 0));
}

void AEmersynGameMode::BuildSchool()
{
    SpawnSkyBackground(SimsColors::SkyTop, SimsColors::SkyBot);
    SpawnRoomShell(FVector(800,700,300), SimsColors::FloorWood, SimsColors::WallGreen, SimsColors::WallCream);
    
    // Student desks
    SpawnDesk(FVector(-200, -200, 0), SimsColors::WoodLight);
    SpawnChair(FVector(-200, -100, 0), SimsColors::SchoolGold);
    SpawnDesk(FVector(0, -200, 0), SimsColors::WoodLight);
    SpawnChair(FVector(0, -100, 0), SimsColors::SchoolGold);
    SpawnDesk(FVector(200, -200, 0), SimsColors::WoodLight);
    SpawnChair(FVector(200, -100, 0), SimsColors::SchoolGold);
    
    // Teacher desk
    SpawnDesk(FVector(0, 350, 0), SimsColors::WoodDark);
    SpawnBookshelf(FVector(-450, 300, 0), SimsColors::WoodMed);
    
    // Characters
    SpawnCharacter(FVector(-200, -50, 0), SimsColors::SkinMed, SimsColors::HairBlack, SimsColors::FabricGreen, TEXT("Mia"), 0.9f);
    SpawnCharacter(FVector(0, 400, 0), SimsColors::SkinLight, SimsColors::HairBrown, SimsColors::FabricPurple, TEXT("Teacher"), 1.1f);
    
    SetupCamera(FVector(0, -1100, 450), FRotator(-20, 0, 0));
}

void AEmersynGameMode::BuildShop()
{
    SpawnSkyBackground(SimsColors::SkyTop, SimsColors::SkyBot);
    SpawnRoomShell(FVector(700,600,300), SimsColors::FloorTile, SimsColors::WallCream, SimsColors::WallCream);
    
    SpawnShopShelf(FVector(-300, -250, 0), SimsColors::WoodMed);
    SpawnShopShelf(FVector(0, -250, 0), SimsColors::WoodMed);
    SpawnShopShelf(FVector(300, -250, 0), SimsColors::WoodMed);
    SpawnCounter(FVector(0, 300, 0), SimsColors::WoodDark);
    SpawnLamp(FVector(-350, 350, 0), SimsColors::FabricWhite);
    
    // Shopkeeper
    SpawnCharacter(FVector(0, 350, 0), SimsColors::SkinTan, SimsColors::HairBlack, SimsColors::FabricWhite, TEXT("Shopkeeper"), 1.0f);
    
    SetupCamera(FVector(0, -950, 400), FRotator(-20, 0, 0));
}

void AEmersynGameMode::BuildPlayground()
{
    SpawnSkyBackground(SimsColors::SkyTop, SimsColors::SkyBot);
    SpawnBox(FVector(0,0,-10), FVector(1500,1500,5), SimsColors::FloorGrass);
    
    SpawnSwing(FVector(-300, 0, 0));
    SpawnSlide(FVector(300, 0, 0));
    SpawnBench(FVector(0, 400, 0));
    
    // Kids playing
    SpawnCharacter(FVector(-250, 50, 0), SimsColors::SkinLight, SimsColors::HairBlonde, SimsColors::FabricBlue, TEXT("Ava"), 0.9f);
    SpawnCharacter(FVector(250, 50, 0), SimsColors::SkinTan, SimsColors::HairBrown, SimsColors::FabricYellow, TEXT("Leo"), 0.9f);
    
    SetupCamera(FVector(0, -1200, 500), FRotator(-18, 0, 0));
}

void AEmersynGameMode::BuildPark()
{
    SpawnSkyBackground(SimsColors::SkyTop, SimsColors::SkyBot);
    SpawnBox(FVector(0,0,-10), FVector(2000,2000,5), SimsColors::FloorGrass);
    
    // Trees (scattered)
    SpawnTree(FVector(-500, -500, 0), 1.5f);
    SpawnTree(FVector(500, -500, 0), 1.2f);
    SpawnTree(FVector(-500, 500, 0), 1.3f);
    SpawnTree(FVector(500, 500, 0), 1.4f);
    SpawnTree(FVector(0, -700, 0), 1.1f);
    
    SpawnBench(FVector(-200, 0, 0));
    SpawnBench(FVector(200, 0, 0));
    SpawnFountain(FVector(0, 400, 0));
    SpawnLamp(FVector(-400, 400, 0), SimsColors::FabricWhite);
    
    SetupCamera(FVector(0, -1500, 600), FRotator(-18, 0, 0));
}

void AEmersynGameMode::BuildMall()
{
    SpawnSkyBackground(SimsColors::SkyTop, SimsColors::SkyBot);
    SpawnRoomShell(FVector(900,800,400), SimsColors::FloorTile, SimsColors::WallCream, SimsColors::WallCream);
    
    SpawnShopShelf(FVector(-400, -300, 0), SimsColors::WoodLight);
    SpawnShopShelf(FVector(0, -300, 0), SimsColors::WoodLight);
    SpawnShopShelf(FVector(400, -300, 0), SimsColors::WoodLight);
    SpawnBench(FVector(0, 200, 0));
    SpawnPlant(FVector(-500, 400, 0), 1.2f);
    SpawnLamp(FVector(500, 400, 0), SimsColors::FabricWhite);
    
    SetupCamera(FVector(0, -1300, 550), FRotator(-20, 0, 0));
}

void AEmersynGameMode::BuildArcade()
{
    SpawnSkyBackground(SimsColors::SkyTop, SimsColors::SkyBot);
    SpawnRoomShell(FVector(700,700,300), SimsColors::FloorConcrete, SimsColors::WallLavender, SimsColors::WallCream);
    
    SpawnArcadeMachine(FVector(-300, -200, 0), SimsColors::FabricRed);
    SpawnArcadeMachine(FVector(0, -200, 0), SimsColors::FabricBlue);
    SpawnArcadeMachine(FVector(300, -200, 0), SimsColors::FabricGreen);
    SpawnBench(FVector(0, 300, 0));
    
    // Kids at arcade
    SpawnCharacter(FVector(-300, -50, 0), SimsColors::SkinLight, SimsColors::HairPink, SimsColors::FabricPink, TEXT("Emersyn"), 0.9f);
    
    SetupCamera(FVector(0, -1000, 450), FRotator(-20, 0, 0));
}

void AEmersynGameMode::BuildAmusementPark()
{
    SpawnSkyBackground(SimsColors::SkyTop, SimsColors::SkyBot);
    SpawnBox(FVector(0,0,-10), FVector(2500,2500,5), SimsColors::FloorSand);
    
    SpawnFerrisWheel(FVector(-600, 0, 0));
    SpawnCarousel(FVector(600, 0, 0));
    SpawnBench(FVector(0, 600, 0));
    SpawnLamp(FVector(-800, 600, 0), SimsColors::FabricYellow);
    SpawnLamp(FVector(800, 600, 0), SimsColors::FabricYellow);
    
    SetupCamera(FVector(0, -2000, 700), FRotator(-18, 0, 0));
}
