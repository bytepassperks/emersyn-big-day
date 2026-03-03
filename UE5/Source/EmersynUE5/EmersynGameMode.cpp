#include "EmersynGameMode.h"
#include "Engine/StaticMeshActor.h"
#include "MeshLoader.h"
#include "Engine/DirectionalLight.h"
#include "Engine/PointLight.h"
#include "Components/DirectionalLightComponent.h"
#include "Components/PointLightComponent.h"
#include "Kismet/GameplayStatics.h"
#include "GameFramework/PlayerController.h"
#include "Camera/CameraActor.h"
#include "Camera/CameraComponent.h"
#include "Engine/World.h"

// Include ALL mesh data headers
#include "MeshData/Mesh_bedroom_bed.h"
#include "MeshData/Mesh_bedroom_dresser.h"
#include "MeshData/Mesh_bedroom_bookshelf.h"
#include "MeshData/Mesh_bedroom_lamp.h"
#include "MeshData/Mesh_bedroom_rug.h"
#include "MeshData/Mesh_kitchen_table.h"
#include "MeshData/Mesh_kitchen_chair.h"
#include "MeshData/Mesh_kitchen_fridge.h"
#include "MeshData/Mesh_kitchen_stove.h"
#include "MeshData/Mesh_kitchen_counter.h"
#include "MeshData/Mesh_bathroom_tub.h"
#include "MeshData/Mesh_bathroom_sink.h"
#include "MeshData/Mesh_bathroom_mirror.h"
#include "MeshData/Mesh_bathroom_towelrack.h"
#include "MeshData/Mesh_livingroom_sofa.h"
#include "MeshData/Mesh_livingroom_coffeetable.h"
#include "MeshData/Mesh_livingroom_tv.h"
#include "MeshData/Mesh_livingroom_plant.h"
#include "MeshData/Mesh_garden_tree.h"
#include "MeshData/Mesh_garden_fence.h"
#include "MeshData/Mesh_garden_flowerbed.h"
#include "MeshData/Mesh_school_desk.h"
#include "MeshData/Mesh_school_chalkboard.h"
#include "MeshData/Mesh_school_backpack.h"
#include "MeshData/Mesh_arcade_cabinet.h"
#include "MeshData/Mesh_arcade_claw_machine.h"
#include "MeshData/Mesh_park_bench.h"
#include "MeshData/Mesh_park_fountain.h"
#include "MeshData/Mesh_park_lamppost.h"
#include "MeshData/Mesh_playground_slide.h"
#include "MeshData/Mesh_playground_swing.h"
#include "MeshData/Mesh_playground_sandbox.h"
#include "MeshData/Mesh_shop_counter.h"
#include "MeshData/Mesh_shop_shelf.h"
#include "MeshData/Mesh_shop_register.h"
#include "MeshData/Mesh_mall_escalator.h"
#include "MeshData/Mesh_mall_planter.h"
#include "MeshData/Mesh_amusement_carousel.h"
#include "MeshData/Mesh_amusement_ferriswheel.h"
#include "MeshData/Mesh_amusement_foodcart.h"
#include "MeshData/Mesh_emersyn.h"
#include "MeshData/Mesh_ava.h"
#include "MeshData/Mesh_leo.h"
#include "MeshData/Mesh_mia.h"
#include "MeshData/Mesh_cat.h"
#include "MeshData/Mesh_dog.h"

// ========= Sims-style color palette =========
namespace SC {
    // Woods
    const FLinearColor WoodLight(0.76f, 0.60f, 0.42f);
    const FLinearColor WoodMedium(0.55f, 0.35f, 0.17f);
    const FLinearColor WoodDark(0.36f, 0.20f, 0.09f);
    const FLinearColor WoodCherry(0.40f, 0.15f, 0.10f);
    // Fabrics
    const FLinearColor FabricPink(0.92f, 0.62f, 0.68f);
    const FLinearColor FabricBlue(0.45f, 0.62f, 0.82f);
    const FLinearColor FabricGreen(0.42f, 0.72f, 0.55f);
    const FLinearColor FabricPurple(0.62f, 0.42f, 0.75f);
    const FLinearColor FabricRed(0.82f, 0.28f, 0.28f);
    const FLinearColor FabricYellow(0.95f, 0.85f, 0.45f);
    const FLinearColor FabricOrange(0.95f, 0.60f, 0.30f);
    const FLinearColor FabricCream(0.96f, 0.94f, 0.88f);
    // Metals
    const FLinearColor MetalSilver(0.78f, 0.78f, 0.80f);
    const FLinearColor MetalGold(0.85f, 0.75f, 0.45f);
    const FLinearColor MetalBlack(0.15f, 0.15f, 0.18f);
    // Walls / floors
    const FLinearColor WallWhite(0.95f, 0.93f, 0.90f);
    const FLinearColor WallPink(0.95f, 0.85f, 0.88f);
    const FLinearColor WallBlue(0.82f, 0.88f, 0.95f);
    const FLinearColor WallGreen(0.82f, 0.92f, 0.85f);
    const FLinearColor WallYellow(0.98f, 0.95f, 0.82f);
    const FLinearColor FloorWood(0.65f, 0.45f, 0.28f);
    const FLinearColor FloorTile(0.88f, 0.88f, 0.85f);
    const FLinearColor FloorGrass(0.35f, 0.65f, 0.30f);
    const FLinearColor FloorSand(0.90f, 0.82f, 0.65f);
    const FLinearColor FloorConcrete(0.70f, 0.68f, 0.65f);
    // Sky
    const FLinearColor SkyTop(0.45f, 0.65f, 0.95f);
    const FLinearColor SkyBottom(0.75f, 0.88f, 1.00f);
    const FLinearColor SkyNightTop(0.08f, 0.08f, 0.22f);
    const FLinearColor SkyNightBot(0.15f, 0.12f, 0.30f);
    // Characters
    const FLinearColor SkinLight(0.92f, 0.78f, 0.65f);
    const FLinearColor SkinMedium(0.72f, 0.55f, 0.40f);
    const FLinearColor SkinDark(0.45f, 0.30f, 0.20f);
    const FLinearColor HairBlonde(0.85f, 0.72f, 0.42f);
    const FLinearColor HairBrown(0.35f, 0.22f, 0.12f);
    const FLinearColor HairBlack(0.10f, 0.08f, 0.08f);
    const FLinearColor HairRed(0.65f, 0.25f, 0.15f);
    const FLinearColor OutfitPink(0.95f, 0.55f, 0.65f);
    const FLinearColor OutfitBlue(0.35f, 0.50f, 0.80f);
    const FLinearColor OutfitGreen(0.30f, 0.65f, 0.40f);
    const FLinearColor OutfitYellow(0.95f, 0.82f, 0.30f);
    // Nature
    const FLinearColor TreeTrunk(0.45f, 0.30f, 0.15f);
    const FLinearColor TreeLeaves(0.25f, 0.55f, 0.22f);
    const FLinearColor FlowerPink(0.95f, 0.55f, 0.65f);
    const FLinearColor FlowerYellow(0.98f, 0.90f, 0.35f);
    const FLinearColor WaterBlue(0.40f, 0.65f, 0.90f);
    // Appliances
    const FLinearColor ApplianceWhite(0.92f, 0.92f, 0.90f);
    const FLinearColor ApplianceSteel(0.72f, 0.72f, 0.75f);
    const FLinearColor ScreenBlue(0.20f, 0.35f, 0.65f);
    const FLinearColor ScreenGlow(0.30f, 0.55f, 0.85f);
    // Misc
    const FLinearColor TileWhite(0.92f, 0.92f, 0.90f);
    const FLinearColor TileBlue(0.65f, 0.78f, 0.90f);
    const FLinearColor BookRed(0.72f, 0.18f, 0.18f);
    const FLinearColor BookGreen(0.18f, 0.52f, 0.28f);
    const FLinearColor BookBlue(0.18f, 0.28f, 0.62f);
    const FLinearColor ChalkGreen(0.22f, 0.42f, 0.28f);
    const FLinearColor ArcadePurple(0.50f, 0.20f, 0.70f);
    const FLinearColor ArcadeNeon(0.20f, 0.95f, 0.50f);
    const FLinearColor CarouselRed(0.85f, 0.25f, 0.25f);
    const FLinearColor CarouselGold(0.90f, 0.80f, 0.40f);
}

// ========= Constructor =========
AEmersynGameMode::AEmersynGameMode()
{
    PrimaryActorTick.bCanEverTick = true;
    RoomIndex = 0;
    Timer = 0.0f;
    SplashDuration = 4.0f;
    RoomDuration = 8.0f;
    bInSplash = true;
    CurrentRoom = TEXT("Splash");

    static ConstructorHelpers::FObjectFinder<UMaterialInterface> MatFinder(
        TEXT("/Game/Materials/M_VertexColor"));
    if (MatFinder.Succeeded())
    {
        VertexColorMaterial = MatFinder.Object;
    }

    RoomNames = {
        TEXT("Bedroom"), TEXT("Kitchen"), TEXT("Bathroom"), TEXT("LivingRoom"),
        TEXT("Garden"), TEXT("School"), TEXT("Shop"), TEXT("Playground"),
        TEXT("Park"), TEXT("Mall"), TEXT("Arcade"), TEXT("AmusementPark")
    };
    RoomDisplayNames = {
        TEXT("Emersyn's Bedroom"), TEXT("Kitchen"), TEXT("Bathroom"),
        TEXT("Living Room"), TEXT("Garden"), TEXT("Preschool"),
        TEXT("Toy Shop"), TEXT("Playground"), TEXT("Park"),
        TEXT("Shopping Mall"), TEXT("Arcade"), TEXT("Amusement Park")
    };
}

// ========= InitGame =========
void AEmersynGameMode::InitGame(const FString& MapName, const FString& Options, FString& ErrorMessage)
{
    Super::InitGame(MapName, Options, ErrorMessage);
}

// ========= BeginPlay =========
void AEmersynGameMode::BeginPlay()
{
    Super::BeginPlay();
    // Destroy default floor
    TArray<AActor*> Found;
    UGameplayStatics::GetAllActorsOfClass(GetWorld(), AStaticMeshActor::StaticClass(), Found);
    for (AActor* A : Found) { if (A) A->Destroy(); }

    BuildSplashScreen();
}

// ========= Tick =========
void AEmersynGameMode::Tick(float DeltaSeconds)
{
    Super::Tick(DeltaSeconds);
    Timer += DeltaSeconds;

    if (bInSplash && Timer > SplashDuration)
    {
        bInSplash = false;
        Timer = 0.0f;
        RoomIndex = 0;
        LoadRoom(RoomNames[RoomIndex]);
    }
    else if (!bInSplash && Timer > RoomDuration)
    {
        Timer = 0.0f;
        RoomIndex = (RoomIndex + 1) % RoomNames.Num();
        LoadRoom(RoomNames[RoomIndex]);
    }
}

// ========= LoadRoom =========
void AEmersynGameMode::LoadRoom(const FString& RoomName)
{
    ClearRoom();
    CurrentRoom = RoomName;

    if (RoomName == TEXT("Bedroom")) BuildBedroom();
    else if (RoomName == TEXT("Kitchen")) BuildKitchen();
    else if (RoomName == TEXT("Bathroom")) BuildBathroom();
    else if (RoomName == TEXT("LivingRoom")) BuildLivingRoom();
    else if (RoomName == TEXT("Garden")) BuildGarden();
    else if (RoomName == TEXT("School")) BuildSchool();
    else if (RoomName == TEXT("Shop")) BuildShop();
    else if (RoomName == TEXT("Playground")) BuildPlayground();
    else if (RoomName == TEXT("Park")) BuildPark();
    else if (RoomName == TEXT("Mall")) BuildMall();
    else if (RoomName == TEXT("Arcade")) BuildArcade();
    else if (RoomName == TEXT("AmusementPark")) BuildAmusementPark();
}

// ========= ClearRoom =========
void AEmersynGameMode::ClearRoom()
{
    for (AActor* A : SpawnedActors)
    {
        if (A && IsValid(A)) A->Destroy();
    }
    SpawnedActors.Empty();
}

// ========= Isometric Camera Setup =========
void AEmersynGameMode::SetupIsometricCamera(FVector RoomCenter, float Distance)
{
    APlayerController* PC = GetWorld()->GetFirstPlayerController();
    if (!PC) return;

    // Sims-style isometric: 40 degree pitch, rotated 30 degrees
    float Pitch = -40.0f;
    float Yaw = 30.0f;
    FRotator CamRot(Pitch, Yaw, 0.0f);

    // Position camera based on rotation and distance
    FVector Offset = CamRot.Vector() * (-Distance);
    FVector CamLoc = RoomCenter + Offset;
    CamLoc.Z = FMath::Max(CamLoc.Z, RoomCenter.Z + Distance * 0.5f);

    ACameraActor* Cam = GetWorld()->SpawnActor<ACameraActor>(CamLoc, CamRot);
    if (Cam)
    {
        Cam->GetCameraComponent()->FieldOfView = 50.0f;
        PC->SetViewTarget(Cam);
        SpawnedActors.Add(Cam);
    }
}

// ========= Spawn Real 3D Mesh =========
AActor* AEmersynGameMode::SpawnMesh(
    const float* Verts, const float* Norms, const float* UVData,
    const int32* Tris, int32 NumVerts, int32 NumTris,
    FVector Location, FRotator Rotation, FVector Scale,
    const FLinearColor& Tint, float Brightness)
{
    AActor* A = GetWorld()->SpawnActor<AActor>(Location, Rotation);
    if (!A) return nullptr;

    UProceduralMeshComponent* PMC = NewObject<UProceduralMeshComponent>(A);
    PMC->RegisterComponent();
    PMC->AttachToComponent(A->GetRootComponent(), FAttachmentTransformRules::KeepRelativeTransform);
    PMC->SetWorldScale3D(Scale);

    if (VertexColorMaterial)
        PMC->SetMaterial(0, VertexColorMaterial);

    FMeshLoader::LoadMesh(PMC, 0, Verts, Norms, UVData, nullptr, Tris,
                          NumVerts, NumTris, Tint, Brightness);

    SpawnedActors.Add(A);
    return A;
}

// ========= Spawn Character Mesh =========
AActor* AEmersynGameMode::SpawnCharacterMesh(
    const FString& Name, FVector Location, FRotator Rotation,
    float Scale, const FLinearColor& SkinTint, const FLinearColor& OutfitTint)
{
    // Mix skin and outfit tints
    FLinearColor Tint;
    Tint.R = (SkinTint.R + OutfitTint.R) * 0.5f;
    Tint.G = (SkinTint.G + OutfitTint.G) * 0.5f;
    Tint.B = (SkinTint.B + OutfitTint.B) * 0.5f;
    Tint.A = 1.0f;

    FVector S(Scale, Scale, Scale);

    if (Name == TEXT("Emersyn"))
        return SpawnMesh(MeshData_EMERSYN::Vertices, MeshData_EMERSYN::Normals,
                         MeshData_EMERSYN::UVs, MeshData_EMERSYN::Triangles,
                         MeshData_EMERSYN::NumVertices, MeshData_EMERSYN::NumVertices,
                         Location, Rotation, S, Tint);
    else if (Name == TEXT("Ava"))
        return SpawnMesh(MeshData_AVA::Vertices, MeshData_AVA::Normals,
                         MeshData_AVA::UVs, MeshData_AVA::Triangles,
                         MeshData_AVA::NumVertices, MeshData_AVA::NumVertices,
                         Location, Rotation, S, Tint);
    else if (Name == TEXT("Leo"))
        return SpawnMesh(MeshData_LEO::Vertices, MeshData_LEO::Normals,
                         MeshData_LEO::UVs, MeshData_LEO::Triangles,
                         MeshData_LEO::NumVertices, MeshData_LEO::NumVertices,
                         Location, Rotation, S, Tint);
    else if (Name == TEXT("Mia"))
        return SpawnMesh(MeshData_MIA::Vertices, MeshData_MIA::Normals,
                         MeshData_MIA::UVs, MeshData_MIA::Triangles,
                         MeshData_MIA::NumVertices, MeshData_MIA::NumVertices,
                         Location, Rotation, S, Tint);
    else if (Name == TEXT("Cat"))
        return SpawnMesh(MeshData_CAT::Vertices, MeshData_CAT::Normals,
                         MeshData_CAT::UVs, MeshData_CAT::Triangles,
                         MeshData_CAT::NumVertices, MeshData_CAT::NumVertices,
                         Location, Rotation, S, Tint);
    else if (Name == TEXT("Dog"))
        return SpawnMesh(MeshData_DOG::Vertices, MeshData_DOG::Normals,
                         MeshData_DOG::UVs, MeshData_DOG::Triangles,
                         MeshData_DOG::NumVertices, MeshData_DOG::NumVertices,
                         Location, Rotation, S, Tint);

    return nullptr;
}

// ========= Environment Helpers =========
void AEmersynGameMode::SpawnSky(FLinearColor TopCol, FLinearColor BotCol)
{
    // Large sky sphere using gradient box
    SpawnGradientBox(FVector(0, 0, 500), FVector(3000, 3000, 1500), TopCol, BotCol);
}

void AEmersynGameMode::SpawnFloor(FVector Size, FLinearColor Col1, FLinearColor Col2, bool bChecker)
{
    if (bChecker)
    {
        // Checkerboard floor
        float TileSize = 40.0f;
        int32 NX = FMath::CeilToInt(Size.X * 2.0f / TileSize);
        int32 NY = FMath::CeilToInt(Size.Y * 2.0f / TileSize);
        for (int32 x = 0; x < NX; x++)
        {
            for (int32 y = 0; y < NY; y++)
            {
                FLinearColor C = ((x + y) % 2 == 0) ? Col1 : Col2;
                FVector Loc(-Size.X + x * TileSize + TileSize/2, -Size.Y + y * TileSize + TileSize/2, 0);
                SpawnBox(Loc, FVector(TileSize/2, TileSize/2, 1), C);
            }
        }
    }
    else
    {
        SpawnBox(FVector(0, 0, -1), Size, Col1);
    }
}

void AEmersynGameMode::SpawnWalls(FVector RoomSize, float WallHeight, FLinearColor Col)
{
    float HW = WallHeight / 2.0f;
    // Back wall
    SpawnBox(FVector(0, -RoomSize.Y, HW), FVector(RoomSize.X, 5, HW), Col);
    // Left wall
    SpawnBox(FVector(-RoomSize.X, 0, HW), FVector(5, RoomSize.Y, HW), Col);
    // Right wall (shorter for view)
    SpawnBox(FVector(RoomSize.X, 0, HW), FVector(5, RoomSize.Y, HW), Col * 0.9f);
}

void AEmersynGameMode::SpawnRoomLabel(const FString& Label)
{
    // No-op for now (HUD handles labels)
}

// ========= Lighting =========
void AEmersynGameMode::SpawnLight(FVector Loc, float Intensity, FLinearColor Color)
{
    AActor* A = GetWorld()->SpawnActor<AActor>(Loc, FRotator::ZeroRotator);
    if (A)
    {
        UPointLightComponent* PL = NewObject<UPointLightComponent>(A);
        PL->RegisterComponent();
        PL->AttachToComponent(A->GetRootComponent(), FAttachmentTransformRules::KeepRelativeTransform);
        PL->SetIntensity(Intensity);
        PL->SetLightColor(Color);
        PL->SetAttenuationRadius(800.0f);
        SpawnedActors.Add(A);
    }
}

void AEmersynGameMode::SpawnDirectionalLight(FRotator Rot, float Intensity, FLinearColor Color)
{
    AActor* A = GetWorld()->SpawnActor<AActor>(FVector(0,0,500), Rot);
    if (A)
    {
        UDirectionalLightComponent* DL = NewObject<UDirectionalLightComponent>(A);
        DL->RegisterComponent();
        DL->AttachToComponent(A->GetRootComponent(), FAttachmentTransformRules::KeepRelativeTransform);
        DL->SetIntensity(Intensity);
        DL->SetLightColor(Color);
        SpawnedActors.Add(A);
    }
}

// ========= Primitive Mesh Generators =========
void AEmersynGameMode::GenerateBoxMesh(UProceduralMeshComponent* PMC, FVector HE, FLinearColor Col)
{
    TArray<FVector> V;
    TArray<int32> T;
    TArray<FVector> N;
    TArray<FVector2D> UV;
    TArray<FLinearColor> C;

    auto AddFace = [&](FVector P0, FVector P1, FVector P2, FVector P3, FVector Norm, FLinearColor FaceCol) {
        int32 Base = V.Num();
        V.Append({P0, P1, P2, P3});
        N.Append({Norm, Norm, Norm, Norm});
        UV.Append({FVector2D(0,0), FVector2D(1,0), FVector2D(1,1), FVector2D(0,1)});
        C.Append({FaceCol, FaceCol, FaceCol, FaceCol});
        T.Append({Base, Base+1, Base+2, Base, Base+2, Base+3});
    };

    float X = HE.X, Y = HE.Y, Z = HE.Z;
    // Key light shading per face
    FVector KL = FVector(0.5f, -0.3f, 0.7071f).GetSafeNormal();
    auto Shade = [&](FVector FN) -> FLinearColor {
        float D = FMath::Max(0.f, FVector::DotProduct(FN, KL));
        float TL = FMath::Max(0.f, FN.Z);
        float L = FMath::Clamp(0.3f + D*0.4f + TL*0.3f, 0.f, 1.3f);
        return FLinearColor(Col.R*L, Col.G*L, Col.B*L, 1.f);
    };

    // Top
    AddFace(FVector(-X,-Y,Z), FVector(X,-Y,Z), FVector(X,Y,Z), FVector(-X,Y,Z),
            FVector(0,0,1), Shade(FVector(0,0,1)));
    // Bottom
    AddFace(FVector(-X,Y,-Z), FVector(X,Y,-Z), FVector(X,-Y,-Z), FVector(-X,-Y,-Z),
            FVector(0,0,-1), Shade(FVector(0,0,-1)));
    // Front
    AddFace(FVector(-X,-Y,-Z), FVector(X,-Y,-Z), FVector(X,-Y,Z), FVector(-X,-Y,Z),
            FVector(0,-1,0), Shade(FVector(0,-1,0)));
    // Back
    AddFace(FVector(X,Y,-Z), FVector(-X,Y,-Z), FVector(-X,Y,Z), FVector(X,Y,Z),
            FVector(0,1,0), Shade(FVector(0,1,0)));
    // Right
    AddFace(FVector(X,-Y,-Z), FVector(X,Y,-Z), FVector(X,Y,Z), FVector(X,-Y,Z),
            FVector(1,0,0), Shade(FVector(1,0,0)));
    // Left
    AddFace(FVector(-X,Y,-Z), FVector(-X,-Y,-Z), FVector(-X,-Y,Z), FVector(-X,Y,Z),
            FVector(-1,0,0), Shade(FVector(-1,0,0)));

    PMC->CreateMeshSection_LinearColor(0, V, T, N, UV, C, TArray<FProcMeshTangent>(), false);
}

void AEmersynGameMode::GenerateGradientBoxMesh(UProceduralMeshComponent* PMC, FVector HE, FLinearColor Top, FLinearColor Bot)
{
    TArray<FVector> V;
    TArray<int32> T;
    TArray<FVector> N;
    TArray<FVector2D> UV;
    TArray<FLinearColor> C;

    auto AddFaceGrad = [&](FVector P0, FVector P1, FVector P2, FVector P3, FVector Norm,
                           FLinearColor C0, FLinearColor C1, FLinearColor C2, FLinearColor C3) {
        int32 Base = V.Num();
        V.Append({P0, P1, P2, P3});
        N.Append({Norm, Norm, Norm, Norm});
        UV.Append({FVector2D(0,0), FVector2D(1,0), FVector2D(1,1), FVector2D(0,1)});
        C.Append({C0, C1, C2, C3});
        T.Append({Base, Base+1, Base+2, Base, Base+2, Base+3});
    };

    float X = HE.X, Y = HE.Y, Z = HE.Z;
    // Top face all Top color
    AddFaceGrad(FVector(-X,-Y,Z), FVector(X,-Y,Z), FVector(X,Y,Z), FVector(-X,Y,Z),
                FVector(0,0,1), Top, Top, Top, Top);
    // Bottom face all Bot color
    AddFaceGrad(FVector(-X,Y,-Z), FVector(X,Y,-Z), FVector(X,-Y,-Z), FVector(-X,-Y,-Z),
                FVector(0,0,-1), Bot, Bot, Bot, Bot);
    // Front: bottom to top gradient
    AddFaceGrad(FVector(-X,-Y,-Z), FVector(X,-Y,-Z), FVector(X,-Y,Z), FVector(-X,-Y,Z),
                FVector(0,-1,0), Bot, Bot, Top, Top);
    // Back
    AddFaceGrad(FVector(X,Y,-Z), FVector(-X,Y,-Z), FVector(-X,Y,Z), FVector(X,Y,Z),
                FVector(0,1,0), Bot, Bot, Top, Top);
    // Right
    AddFaceGrad(FVector(X,-Y,-Z), FVector(X,Y,-Z), FVector(X,Y,Z), FVector(X,-Y,Z),
                FVector(1,0,0), Bot, Bot, Top, Top);
    // Left
    AddFaceGrad(FVector(-X,Y,-Z), FVector(-X,-Y,-Z), FVector(-X,-Y,Z), FVector(-X,Y,Z),
                FVector(-1,0,0), Bot, Bot, Top, Top);

    PMC->CreateMeshSection_LinearColor(0, V, T, N, UV, C, TArray<FProcMeshTangent>(), false);
}

// ========= Primitive Spawners =========
AActor* AEmersynGameMode::SpawnBox(FVector Loc, FVector Scale, FLinearColor Col)
{
    AActor* A = GetWorld()->SpawnActor<AActor>(Loc, FRotator::ZeroRotator);
    if (!A) return nullptr;
    UProceduralMeshComponent* PMC = NewObject<UProceduralMeshComponent>(A);
    PMC->RegisterComponent();
    PMC->AttachToComponent(A->GetRootComponent(), FAttachmentTransformRules::KeepRelativeTransform);
    if (VertexColorMaterial) PMC->SetMaterial(0, VertexColorMaterial);
    GenerateBoxMesh(PMC, Scale, Col);
    SpawnedActors.Add(A);
    return A;
}

AActor* AEmersynGameMode::SpawnGradientBox(FVector Loc, FVector Scale, FLinearColor Top, FLinearColor Bot)
{
    AActor* A = GetWorld()->SpawnActor<AActor>(Loc, FRotator::ZeroRotator);
    if (!A) return nullptr;
    UProceduralMeshComponent* PMC = NewObject<UProceduralMeshComponent>(A);
    PMC->RegisterComponent();
    PMC->AttachToComponent(A->GetRootComponent(), FAttachmentTransformRules::KeepRelativeTransform);
    if (VertexColorMaterial) PMC->SetMaterial(0, VertexColorMaterial);
    GenerateGradientBoxMesh(PMC, Scale, Top, Bot);
    SpawnedActors.Add(A);
    return A;
}

// ========= Splash Screen =========
void AEmersynGameMode::BuildSplashScreen()
{
    SpawnSky(FLinearColor(0.95f, 0.75f, 0.85f), FLinearColor(1.0f, 0.90f, 0.95f));

    // Title background
    SpawnBox(FVector(0, 0, 200), FVector(300, 200, 5), FLinearColor(0.95f, 0.55f, 0.65f));
    // Subtitle bar
    SpawnBox(FVector(0, 0, 120), FVector(250, 30, 3), FLinearColor(1.0f, 0.85f, 0.45f));

    // Emersyn character in center
    SpawnCharacterMesh(TEXT("Emersyn"), FVector(0, 50, 0), FRotator(0, -20, 0), 1.2f,
                       SC::SkinLight, SC::OutfitPink);
    // Pets
    SpawnCharacterMesh(TEXT("Cat"), FVector(-80, 80, 0), FRotator(0, 15, 0), 0.6f,
                       FLinearColor(0.85f, 0.75f, 0.55f), FLinearColor(0.85f, 0.75f, 0.55f));
    SpawnCharacterMesh(TEXT("Dog"), FVector(80, 80, 0), FRotator(0, -15, 0), 0.6f,
                       FLinearColor(0.65f, 0.45f, 0.25f), FLinearColor(0.65f, 0.45f, 0.25f));

    SetupIsometricCamera(FVector(0, 0, 100), 500);
    SpawnDirectionalLight(FRotator(-45, 30, 0), 3.0f, FLinearColor(1.0f, 0.95f, 0.85f));
}

// ========= Main Menu =========
void AEmersynGameMode::BuildMainMenu()
{
    BuildSplashScreen();
}

// ========= BEDROOM =========
void AEmersynGameMode::BuildBedroom()
{
    FVector RoomSize(250, 250, 0);
    SpawnSky(SC::SkyTop, SC::SkyBottom);
    SpawnFloor(FVector(RoomSize.X, RoomSize.Y, 2), SC::FloorWood, SC::FloorWood * 0.9f, false);
    SpawnWalls(RoomSize, 200, SC::WallPink);
    SpawnDirectionalLight(FRotator(-45, 30, 0), 3.0f, FLinearColor(1.0f, 0.95f, 0.85f));
    SpawnLight(FVector(0, 0, 190), 1500.0f, FLinearColor(1.0f, 0.9f, 0.8f));

    // Real 3D bed
    SpawnMesh(MeshData_BEDROOM_BED::Vertices, MeshData_BEDROOM_BED::Normals,
              MeshData_BEDROOM_BED::UVs, MeshData_BEDROOM_BED::Triangles,
              MeshData_BEDROOM_BED::NumVertices, MeshData_BEDROOM_BED::NumVertices,
              FVector(50, -100, 0), FRotator(0, 0, 0), FVector(1.2f, 1.2f, 1.2f),
              SC::FabricPink, 1.1f);

    // Real 3D dresser
    SpawnMesh(MeshData_BEDROOM_DRESSER::Vertices, MeshData_BEDROOM_DRESSER::Normals,
              MeshData_BEDROOM_DRESSER::UVs, MeshData_BEDROOM_DRESSER::Triangles,
              MeshData_BEDROOM_DRESSER::NumVertices, MeshData_BEDROOM_DRESSER::NumVertices,
              FVector(-180, -50, 0), FRotator(0, 90, 0), FVector(1.0f, 1.0f, 1.0f),
              SC::WoodLight, 1.0f);

    // Real 3D bookshelf
    SpawnMesh(MeshData_BEDROOM_BOOKSHELF::Vertices, MeshData_BEDROOM_BOOKSHELF::Normals,
              MeshData_BEDROOM_BOOKSHELF::UVs, MeshData_BEDROOM_BOOKSHELF::Triangles,
              MeshData_BEDROOM_BOOKSHELF::NumVertices, MeshData_BEDROOM_BOOKSHELF::NumVertices,
              FVector(-180, 100, 0), FRotator(0, 90, 0), FVector(1.0f, 1.0f, 1.0f),
              SC::WoodMedium, 1.0f);

    // Real 3D lamp
    SpawnMesh(MeshData_BEDROOM_LAMP::Vertices, MeshData_BEDROOM_LAMP::Normals,
              MeshData_BEDROOM_LAMP::UVs, MeshData_BEDROOM_LAMP::Triangles,
              MeshData_BEDROOM_LAMP::NumVertices, MeshData_BEDROOM_LAMP::NumVertices,
              FVector(150, -150, 50), FRotator(0, 0, 0), FVector(1.0f, 1.0f, 1.0f),
              SC::FabricYellow, 1.2f);

    // Real 3D rug
    SpawnMesh(MeshData_BEDROOM_RUG::Vertices, MeshData_BEDROOM_RUG::Normals,
              MeshData_BEDROOM_RUG::UVs, MeshData_BEDROOM_RUG::Triangles,
              MeshData_BEDROOM_RUG::NumVertices, MeshData_BEDROOM_RUG::NumVertices,
              FVector(0, 50, 1), FRotator(0, 0, 0), FVector(1.5f, 1.5f, 1.0f),
              SC::FabricPurple, 0.9f);

    // Characters
    SpawnCharacterMesh(TEXT("Emersyn"), FVector(0, 80, 0), FRotator(0, -30, 0), 1.0f,
                       SC::SkinLight, SC::OutfitPink);
    SpawnCharacterMesh(TEXT("Cat"), FVector(80, -60, 35), FRotator(0, 45, 0), 0.5f,
                       FLinearColor(0.85f, 0.75f, 0.55f), FLinearColor(0.85f, 0.75f, 0.55f));

    SetupIsometricCamera(FVector(0, 0, 50), 700);
}

// ========= KITCHEN =========
void AEmersynGameMode::BuildKitchen()
{
    FVector RoomSize(280, 280, 0);
    SpawnSky(SC::SkyTop, SC::SkyBottom);
    SpawnFloor(FVector(RoomSize.X, RoomSize.Y, 2), SC::FloorTile, SC::TileBlue, true);
    SpawnWalls(RoomSize, 200, SC::WallYellow);
    SpawnDirectionalLight(FRotator(-45, 30, 0), 3.0f, FLinearColor(1.0f, 0.95f, 0.85f));
    SpawnLight(FVector(0, 0, 190), 2000.0f, FLinearColor(1.0f, 0.95f, 0.90f));

    // Table
    SpawnMesh(MeshData_KITCHEN_TABLE::Vertices, MeshData_KITCHEN_TABLE::Normals,
              MeshData_KITCHEN_TABLE::UVs, MeshData_KITCHEN_TABLE::Triangles,
              MeshData_KITCHEN_TABLE::NumVertices, MeshData_KITCHEN_TABLE::NumVertices,
              FVector(0, 0, 0), FRotator(0, 0, 0), FVector(1.2f, 1.2f, 1.2f),
              SC::WoodLight, 1.0f);

    // Chairs around table
    SpawnMesh(MeshData_KITCHEN_CHAIR::Vertices, MeshData_KITCHEN_CHAIR::Normals,
              MeshData_KITCHEN_CHAIR::UVs, MeshData_KITCHEN_CHAIR::Triangles,
              MeshData_KITCHEN_CHAIR::NumVertices, MeshData_KITCHEN_CHAIR::NumVertices,
              FVector(60, 0, 0), FRotator(0, -90, 0), FVector(1.0f, 1.0f, 1.0f),
              SC::WoodMedium, 1.0f);

    SpawnMesh(MeshData_KITCHEN_CHAIR::Vertices, MeshData_KITCHEN_CHAIR::Normals,
              MeshData_KITCHEN_CHAIR::UVs, MeshData_KITCHEN_CHAIR::Triangles,
              MeshData_KITCHEN_CHAIR::NumVertices, MeshData_KITCHEN_CHAIR::NumVertices,
              FVector(-60, 0, 0), FRotator(0, 90, 0), FVector(1.0f, 1.0f, 1.0f),
              SC::WoodMedium, 1.0f);

    // Fridge
    SpawnMesh(MeshData_KITCHEN_FRIDGE::Vertices, MeshData_KITCHEN_FRIDGE::Normals,
              MeshData_KITCHEN_FRIDGE::UVs, MeshData_KITCHEN_FRIDGE::Triangles,
              MeshData_KITCHEN_FRIDGE::NumVertices, MeshData_KITCHEN_FRIDGE::NumVertices,
              FVector(-200, -180, 0), FRotator(0, 0, 0), FVector(1.2f, 1.2f, 1.2f),
              SC::ApplianceWhite, 1.0f);

    // Stove
    SpawnMesh(MeshData_KITCHEN_STOVE::Vertices, MeshData_KITCHEN_STOVE::Normals,
              MeshData_KITCHEN_STOVE::UVs, MeshData_KITCHEN_STOVE::Triangles,
              MeshData_KITCHEN_STOVE::NumVertices, MeshData_KITCHEN_STOVE::NumVertices,
              FVector(-200, -60, 0), FRotator(0, 0, 0), FVector(1.1f, 1.1f, 1.1f),
              SC::ApplianceSteel, 1.0f);

    // Counter
    SpawnMesh(MeshData_KITCHEN_COUNTER::Vertices, MeshData_KITCHEN_COUNTER::Normals,
              MeshData_KITCHEN_COUNTER::UVs, MeshData_KITCHEN_COUNTER::Triangles,
              MeshData_KITCHEN_COUNTER::NumVertices, MeshData_KITCHEN_COUNTER::NumVertices,
              FVector(-200, 80, 0), FRotator(0, 0, 0), FVector(1.2f, 1.2f, 1.0f),
              SC::WoodLight, 1.0f);

    // Character
    SpawnCharacterMesh(TEXT("Mia"), FVector(0, 80, 0), FRotator(0, 180, 0), 1.0f,
                       SC::SkinMedium, SC::OutfitGreen);

    SetupIsometricCamera(FVector(0, 0, 50), 750);
}

// ========= BATHROOM =========
void AEmersynGameMode::BuildBathroom()
{
    FVector RoomSize(220, 220, 0);
    SpawnSky(SC::SkyTop, SC::SkyBottom);
    SpawnFloor(FVector(RoomSize.X, RoomSize.Y, 2), SC::TileWhite, SC::TileBlue, true);
    SpawnWalls(RoomSize, 200, SC::WallBlue);
    SpawnDirectionalLight(FRotator(-50, 20, 0), 2.5f, FLinearColor(0.95f, 0.95f, 1.0f));
    SpawnLight(FVector(0, 0, 190), 1500.0f, FLinearColor(0.95f, 0.95f, 1.0f));

    SpawnMesh(MeshData_BATHROOM_TUB::Vertices, MeshData_BATHROOM_TUB::Normals,
              MeshData_BATHROOM_TUB::UVs, MeshData_BATHROOM_TUB::Triangles,
              MeshData_BATHROOM_TUB::NumVertices, MeshData_BATHROOM_TUB::NumVertices,
              FVector(50, -100, 0), FRotator(0, 0, 0), FVector(1.5f, 1.5f, 1.5f),
              SC::TileWhite, 1.0f);

    SpawnMesh(MeshData_BATHROOM_SINK::Vertices, MeshData_BATHROOM_SINK::Normals,
              MeshData_BATHROOM_SINK::UVs, MeshData_BATHROOM_SINK::Triangles,
              MeshData_BATHROOM_SINK::NumVertices, MeshData_BATHROOM_SINK::NumVertices,
              FVector(-150, 0, 50), FRotator(0, 90, 0), FVector(1.0f, 1.0f, 1.0f),
              SC::TileWhite, 1.0f);

    SpawnMesh(MeshData_BATHROOM_MIRROR::Vertices, MeshData_BATHROOM_MIRROR::Normals,
              MeshData_BATHROOM_MIRROR::UVs, MeshData_BATHROOM_MIRROR::Triangles,
              MeshData_BATHROOM_MIRROR::NumVertices, MeshData_BATHROOM_MIRROR::NumVertices,
              FVector(-155, 0, 120), FRotator(0, 90, 0), FVector(0.8f, 0.8f, 0.8f),
              SC::MetalSilver, 1.1f);

    SpawnMesh(MeshData_BATHROOM_TOWELRACK::Vertices, MeshData_BATHROOM_TOWELRACK::Normals,
              MeshData_BATHROOM_TOWELRACK::UVs, MeshData_BATHROOM_TOWELRACK::Triangles,
              MeshData_BATHROOM_TOWELRACK::NumVertices, MeshData_BATHROOM_TOWELRACK::NumVertices,
              FVector(150, 50, 80), FRotator(0, -90, 0), FVector(0.8f, 0.8f, 0.8f),
              SC::MetalSilver, 1.0f);

    SpawnCharacterMesh(TEXT("Emersyn"), FVector(0, 50, 0), FRotator(0, 0, 0), 1.0f,
                       SC::SkinLight, SC::OutfitBlue);

    SetupIsometricCamera(FVector(0, 0, 50), 650);
}

// ========= LIVING ROOM =========
void AEmersynGameMode::BuildLivingRoom()
{
    FVector RoomSize(300, 300, 0);
    SpawnSky(SC::SkyTop, SC::SkyBottom);
    SpawnFloor(FVector(RoomSize.X, RoomSize.Y, 2), SC::FloorWood, SC::FloorWood * 0.85f, false);
    SpawnWalls(RoomSize, 200, SC::WallWhite);
    SpawnDirectionalLight(FRotator(-45, 30, 0), 3.0f, FLinearColor(1.0f, 0.95f, 0.85f));
    SpawnLight(FVector(0, 0, 190), 2000.0f, FLinearColor(1.0f, 0.95f, 0.90f));

    SpawnMesh(MeshData_LIVINGROOM_SOFA::Vertices, MeshData_LIVINGROOM_SOFA::Normals,
              MeshData_LIVINGROOM_SOFA::UVs, MeshData_LIVINGROOM_SOFA::Triangles,
              MeshData_LIVINGROOM_SOFA::NumVertices, MeshData_LIVINGROOM_SOFA::NumVertices,
              FVector(0, -150, 0), FRotator(0, 0, 0), FVector(1.3f, 1.3f, 1.3f),
              SC::FabricBlue, 1.0f);

    SpawnMesh(MeshData_LIVINGROOM_COFFEETABLE::Vertices, MeshData_LIVINGROOM_COFFEETABLE::Normals,
              MeshData_LIVINGROOM_COFFEETABLE::UVs, MeshData_LIVINGROOM_COFFEETABLE::Triangles,
              MeshData_LIVINGROOM_COFFEETABLE::NumVertices, MeshData_LIVINGROOM_COFFEETABLE::NumVertices,
              FVector(0, -50, 0), FRotator(0, 0, 0), FVector(1.2f, 1.2f, 1.0f),
              SC::WoodMedium, 1.0f);

    SpawnMesh(MeshData_LIVINGROOM_TV::Vertices, MeshData_LIVINGROOM_TV::Normals,
              MeshData_LIVINGROOM_TV::UVs, MeshData_LIVINGROOM_TV::Triangles,
              MeshData_LIVINGROOM_TV::NumVertices, MeshData_LIVINGROOM_TV::NumVertices,
              FVector(0, 200, 80), FRotator(0, 180, 0), FVector(1.5f, 1.5f, 1.5f),
              SC::MetalBlack, 1.0f);

    SpawnMesh(MeshData_LIVINGROOM_PLANT::Vertices, MeshData_LIVINGROOM_PLANT::Normals,
              MeshData_LIVINGROOM_PLANT::UVs, MeshData_LIVINGROOM_PLANT::Triangles,
              MeshData_LIVINGROOM_PLANT::NumVertices, MeshData_LIVINGROOM_PLANT::NumVertices,
              FVector(200, -200, 0), FRotator(0, 0, 0), FVector(1.0f, 1.0f, 1.0f),
              SC::TreeLeaves, 1.0f);

    SpawnCharacterMesh(TEXT("Leo"), FVector(-50, 0, 0), FRotator(0, 30, 0), 1.0f,
                       SC::SkinLight, SC::OutfitBlue);
    SpawnCharacterMesh(TEXT("Dog"), FVector(100, -80, 0), FRotator(0, -20, 0), 0.6f,
                       FLinearColor(0.65f, 0.45f, 0.25f), FLinearColor(0.65f, 0.45f, 0.25f));

    SetupIsometricCamera(FVector(0, 0, 50), 800);
}

// ========= GARDEN =========
void AEmersynGameMode::BuildGarden()
{
    SpawnSky(SC::SkyTop, SC::SkyBottom);
    SpawnFloor(FVector(500, 500, 2), SC::FloorGrass, SC::FloorGrass * 0.9f, false);
    SpawnDirectionalLight(FRotator(-40, 45, 0), 4.0f, FLinearColor(1.0f, 0.95f, 0.80f));

    // Trees
    SpawnMesh(MeshData_GARDEN_TREE::Vertices, MeshData_GARDEN_TREE::Normals,
              MeshData_GARDEN_TREE::UVs, MeshData_GARDEN_TREE::Triangles,
              MeshData_GARDEN_TREE::NumVertices, MeshData_GARDEN_TREE::NumVertices,
              FVector(-200, -200, 0), FRotator(0, 0, 0), FVector(2.0f, 2.0f, 2.0f),
              SC::TreeLeaves, 1.0f);
    SpawnMesh(MeshData_GARDEN_TREE::Vertices, MeshData_GARDEN_TREE::Normals,
              MeshData_GARDEN_TREE::UVs, MeshData_GARDEN_TREE::Triangles,
              MeshData_GARDEN_TREE::NumVertices, MeshData_GARDEN_TREE::NumVertices,
              FVector(200, -150, 0), FRotator(0, 60, 0), FVector(1.5f, 1.5f, 1.5f),
              SC::TreeLeaves, 0.9f);

    // Fence
    SpawnMesh(MeshData_GARDEN_FENCE::Vertices, MeshData_GARDEN_FENCE::Normals,
              MeshData_GARDEN_FENCE::UVs, MeshData_GARDEN_FENCE::Triangles,
              MeshData_GARDEN_FENCE::NumVertices, MeshData_GARDEN_FENCE::NumVertices,
              FVector(0, -350, 0), FRotator(0, 0, 0), FVector(3.0f, 1.0f, 1.0f),
              SC::WoodLight, 1.0f);

    // Flowerbeds
    SpawnMesh(MeshData_GARDEN_FLOWERBED::Vertices, MeshData_GARDEN_FLOWERBED::Normals,
              MeshData_GARDEN_FLOWERBED::UVs, MeshData_GARDEN_FLOWERBED::Triangles,
              MeshData_GARDEN_FLOWERBED::NumVertices, MeshData_GARDEN_FLOWERBED::NumVertices,
              FVector(-100, 100, 0), FRotator(0, 0, 0), FVector(1.5f, 1.5f, 1.0f),
              SC::FlowerPink, 1.0f);
    SpawnMesh(MeshData_GARDEN_FLOWERBED::Vertices, MeshData_GARDEN_FLOWERBED::Normals,
              MeshData_GARDEN_FLOWERBED::UVs, MeshData_GARDEN_FLOWERBED::Triangles,
              MeshData_GARDEN_FLOWERBED::NumVertices, MeshData_GARDEN_FLOWERBED::NumVertices,
              FVector(100, 100, 0), FRotator(0, 45, 0), FVector(1.5f, 1.5f, 1.0f),
              SC::FlowerYellow, 1.0f);

    // Characters
    SpawnCharacterMesh(TEXT("Emersyn"), FVector(0, 0, 0), FRotator(0, 0, 0), 1.0f,
                       SC::SkinLight, SC::OutfitYellow);
    SpawnCharacterMesh(TEXT("Ava"), FVector(80, 50, 0), FRotator(0, -45, 0), 1.0f,
                       SC::SkinMedium, SC::OutfitGreen);
    SpawnCharacterMesh(TEXT("Dog"), FVector(-60, -50, 0), FRotator(0, 30, 0), 0.7f,
                       FLinearColor(0.65f, 0.45f, 0.25f), FLinearColor(0.65f, 0.45f, 0.25f));

    SetupIsometricCamera(FVector(0, 0, 50), 900);
}

// ========= SCHOOL =========
void AEmersynGameMode::BuildSchool()
{
    FVector RoomSize(350, 300, 0);
    SpawnSky(SC::SkyTop, SC::SkyBottom);
    SpawnFloor(FVector(RoomSize.X, RoomSize.Y, 2), SC::FloorWood, SC::FloorWood * 0.9f, false);
    SpawnWalls(RoomSize, 220, SC::WallGreen);
    SpawnDirectionalLight(FRotator(-45, 30, 0), 3.0f, FLinearColor(1.0f, 0.95f, 0.85f));
    SpawnLight(FVector(0, 0, 210), 2000.0f, FLinearColor(1.0f, 1.0f, 0.95f));

    // Chalkboard
    SpawnMesh(MeshData_SCHOOL_CHALKBOARD::Vertices, MeshData_SCHOOL_CHALKBOARD::Normals,
              MeshData_SCHOOL_CHALKBOARD::UVs, MeshData_SCHOOL_CHALKBOARD::Triangles,
              MeshData_SCHOOL_CHALKBOARD::NumVertices, MeshData_SCHOOL_CHALKBOARD::NumVertices,
              FVector(0, -230, 100), FRotator(0, 0, 0), FVector(2.0f, 1.0f, 1.5f),
              SC::ChalkGreen, 1.0f);

    // Desks in rows
    for (int32 Row = 0; Row < 3; Row++)
    {
        for (int32 Col = 0; Col < 2; Col++)
        {
            FVector Loc(-100 + Col * 200, -50 + Row * 100, 0);
            SpawnMesh(MeshData_SCHOOL_DESK::Vertices, MeshData_SCHOOL_DESK::Normals,
                      MeshData_SCHOOL_DESK::UVs, MeshData_SCHOOL_DESK::Triangles,
                      MeshData_SCHOOL_DESK::NumVertices, MeshData_SCHOOL_DESK::NumVertices,
                      Loc, FRotator(0, 0, 0), FVector(0.8f, 0.8f, 0.8f),
                      SC::WoodLight, 1.0f);
        }
    }

    // Backpack near front desk
    SpawnMesh(MeshData_SCHOOL_BACKPACK::Vertices, MeshData_SCHOOL_BACKPACK::Normals,
              MeshData_SCHOOL_BACKPACK::UVs, MeshData_SCHOOL_BACKPACK::Triangles,
              MeshData_SCHOOL_BACKPACK::NumVertices, MeshData_SCHOOL_BACKPACK::NumVertices,
              FVector(-120, -50, 0), FRotator(0, 20, 0), FVector(0.6f, 0.6f, 0.6f),
              SC::FabricRed, 1.0f);

    // Characters
    SpawnCharacterMesh(TEXT("Emersyn"), FVector(-100, 50, 0), FRotator(0, 0, 0), 1.0f,
                       SC::SkinLight, SC::OutfitPink);
    SpawnCharacterMesh(TEXT("Leo"), FVector(100, 50, 0), FRotator(0, 0, 0), 1.0f,
                       SC::SkinLight, SC::OutfitBlue);

    SetupIsometricCamera(FVector(0, 0, 60), 800);
}

// ========= SHOP =========
void AEmersynGameMode::BuildShop()
{
    FVector RoomSize(300, 300, 0);
    SpawnSky(SC::SkyTop, SC::SkyBottom);
    SpawnFloor(FVector(RoomSize.X, RoomSize.Y, 2), SC::FloorTile, SC::FloorTile * 0.9f, false);
    SpawnWalls(RoomSize, 220, SC::WallYellow);
    SpawnDirectionalLight(FRotator(-45, 30, 0), 3.0f, FLinearColor(1.0f, 0.95f, 0.85f));
    SpawnLight(FVector(0, 0, 210), 2500.0f, FLinearColor(1.0f, 0.98f, 0.95f));

    // Shelves
    SpawnMesh(MeshData_SHOP_SHELF::Vertices, MeshData_SHOP_SHELF::Normals,
              MeshData_SHOP_SHELF::UVs, MeshData_SHOP_SHELF::Triangles,
              MeshData_SHOP_SHELF::NumVertices, MeshData_SHOP_SHELF::NumVertices,
              FVector(-200, -100, 0), FRotator(0, 90, 0), FVector(1.0f, 1.0f, 1.2f),
              SC::WoodLight, 1.0f);
    SpawnMesh(MeshData_SHOP_SHELF::Vertices, MeshData_SHOP_SHELF::Normals,
              MeshData_SHOP_SHELF::UVs, MeshData_SHOP_SHELF::Triangles,
              MeshData_SHOP_SHELF::NumVertices, MeshData_SHOP_SHELF::NumVertices,
              FVector(-200, 100, 0), FRotator(0, 90, 0), FVector(1.0f, 1.0f, 1.2f),
              SC::WoodMedium, 1.0f);

    // Counter
    SpawnMesh(MeshData_SHOP_COUNTER::Vertices, MeshData_SHOP_COUNTER::Normals,
              MeshData_SHOP_COUNTER::UVs, MeshData_SHOP_COUNTER::Triangles,
              MeshData_SHOP_COUNTER::NumVertices, MeshData_SHOP_COUNTER::NumVertices,
              FVector(150, 0, 0), FRotator(0, -90, 0), FVector(1.2f, 1.2f, 1.0f),
              SC::WoodDark, 1.0f);

    // Register
    SpawnMesh(MeshData_SHOP_REGISTER::Vertices, MeshData_SHOP_REGISTER::Normals,
              MeshData_SHOP_REGISTER::UVs, MeshData_SHOP_REGISTER::Triangles,
              MeshData_SHOP_REGISTER::NumVertices, MeshData_SHOP_REGISTER::NumVertices,
              FVector(150, 0, 60), FRotator(0, -90, 0), FVector(0.5f, 0.5f, 0.5f),
              SC::MetalSilver, 1.1f);

    SpawnCharacterMesh(TEXT("Emersyn"), FVector(0, 50, 0), FRotator(0, 0, 0), 1.0f,
                       SC::SkinLight, SC::OutfitPink);

    SetupIsometricCamera(FVector(0, 0, 60), 800);
}

// ========= PLAYGROUND =========
void AEmersynGameMode::BuildPlayground()
{
    SpawnSky(SC::SkyTop, SC::SkyBottom);
    SpawnFloor(FVector(500, 500, 2), SC::FloorSand, SC::FloorSand * 0.95f, false);
    SpawnDirectionalLight(FRotator(-40, 45, 0), 4.0f, FLinearColor(1.0f, 0.95f, 0.80f));

    SpawnMesh(MeshData_PLAYGROUND_SLIDE::Vertices, MeshData_PLAYGROUND_SLIDE::Normals,
              MeshData_PLAYGROUND_SLIDE::UVs, MeshData_PLAYGROUND_SLIDE::Triangles,
              MeshData_PLAYGROUND_SLIDE::NumVertices, MeshData_PLAYGROUND_SLIDE::NumVertices,
              FVector(-120, -100, 0), FRotator(0, 0, 0), FVector(1.5f, 1.5f, 1.5f),
              SC::FabricRed, 1.0f);

    SpawnMesh(MeshData_PLAYGROUND_SWING::Vertices, MeshData_PLAYGROUND_SWING::Normals,
              MeshData_PLAYGROUND_SWING::UVs, MeshData_PLAYGROUND_SWING::Triangles,
              MeshData_PLAYGROUND_SWING::NumVertices, MeshData_PLAYGROUND_SWING::NumVertices,
              FVector(120, -100, 0), FRotator(0, 0, 0), FVector(1.5f, 1.5f, 1.5f),
              SC::MetalSilver, 1.0f);

    SpawnMesh(MeshData_PLAYGROUND_SANDBOX::Vertices, MeshData_PLAYGROUND_SANDBOX::Normals,
              MeshData_PLAYGROUND_SANDBOX::UVs, MeshData_PLAYGROUND_SANDBOX::Triangles,
              MeshData_PLAYGROUND_SANDBOX::NumVertices, MeshData_PLAYGROUND_SANDBOX::NumVertices,
              FVector(0, 100, 0), FRotator(0, 0, 0), FVector(1.2f, 1.2f, 0.8f),
              SC::FloorSand, 0.9f);

    SpawnCharacterMesh(TEXT("Emersyn"), FVector(-50, 0, 0), FRotator(0, 30, 0), 1.0f,
                       SC::SkinLight, SC::OutfitPink);
    SpawnCharacterMesh(TEXT("Mia"), FVector(50, 0, 0), FRotator(0, -30, 0), 1.0f,
                       SC::SkinMedium, SC::OutfitYellow);

    SetupIsometricCamera(FVector(0, 0, 50), 900);
}

// ========= PARK =========
void AEmersynGameMode::BuildPark()
{
    SpawnSky(SC::SkyTop, SC::SkyBottom);
    SpawnFloor(FVector(600, 600, 2), SC::FloorGrass, SC::FloorGrass * 0.85f, false);
    // Path
    SpawnBox(FVector(0, 0, 1), FVector(50, 400, 1), SC::FloorConcrete);
    SpawnDirectionalLight(FRotator(-35, 50, 0), 4.5f, FLinearColor(1.0f, 0.95f, 0.80f));

    SpawnMesh(MeshData_PARK_BENCH::Vertices, MeshData_PARK_BENCH::Normals,
              MeshData_PARK_BENCH::UVs, MeshData_PARK_BENCH::Triangles,
              MeshData_PARK_BENCH::NumVertices, MeshData_PARK_BENCH::NumVertices,
              FVector(-100, -50, 0), FRotator(0, 0, 0), FVector(1.0f, 1.0f, 1.0f),
              SC::WoodMedium, 1.0f);

    SpawnMesh(MeshData_PARK_FOUNTAIN::Vertices, MeshData_PARK_FOUNTAIN::Normals,
              MeshData_PARK_FOUNTAIN::UVs, MeshData_PARK_FOUNTAIN::Triangles,
              MeshData_PARK_FOUNTAIN::NumVertices, MeshData_PARK_FOUNTAIN::NumVertices,
              FVector(0, 150, 0), FRotator(0, 0, 0), FVector(2.0f, 2.0f, 2.0f),
              SC::WaterBlue, 1.1f);

    SpawnMesh(MeshData_PARK_LAMPPOST::Vertices, MeshData_PARK_LAMPPOST::Normals,
              MeshData_PARK_LAMPPOST::UVs, MeshData_PARK_LAMPPOST::Triangles,
              MeshData_PARK_LAMPPOST::NumVertices, MeshData_PARK_LAMPPOST::NumVertices,
              FVector(100, -200, 0), FRotator(0, 0, 0), FVector(1.5f, 1.5f, 2.0f),
              SC::MetalBlack, 1.0f);

    // Trees
    SpawnMesh(MeshData_GARDEN_TREE::Vertices, MeshData_GARDEN_TREE::Normals,
              MeshData_GARDEN_TREE::UVs, MeshData_GARDEN_TREE::Triangles,
              MeshData_GARDEN_TREE::NumVertices, MeshData_GARDEN_TREE::NumVertices,
              FVector(-300, 200, 0), FRotator(0, 0, 0), FVector(2.5f, 2.5f, 2.5f),
              SC::TreeLeaves, 1.0f);
    SpawnMesh(MeshData_GARDEN_TREE::Vertices, MeshData_GARDEN_TREE::Normals,
              MeshData_GARDEN_TREE::UVs, MeshData_GARDEN_TREE::Triangles,
              MeshData_GARDEN_TREE::NumVertices, MeshData_GARDEN_TREE::NumVertices,
              FVector(300, -150, 0), FRotator(0, 90, 0), FVector(2.0f, 2.0f, 2.0f),
              SC::TreeLeaves, 0.95f);

    SpawnCharacterMesh(TEXT("Emersyn"), FVector(-50, 0, 0), FRotator(0, 0, 0), 1.0f,
                       SC::SkinLight, SC::OutfitPink);
    SpawnCharacterMesh(TEXT("Ava"), FVector(50, 50, 0), FRotator(0, -20, 0), 1.0f,
                       SC::SkinMedium, SC::OutfitGreen);
    SpawnCharacterMesh(TEXT("Dog"), FVector(0, -100, 0), FRotator(0, 45, 0), 0.7f,
                       FLinearColor(0.65f, 0.45f, 0.25f), FLinearColor(0.65f, 0.45f, 0.25f));

    SetupIsometricCamera(FVector(0, 0, 50), 1000);
}

// ========= MALL =========
void AEmersynGameMode::BuildMall()
{
    FVector RoomSize(400, 400, 0);
    SpawnSky(SC::SkyTop, SC::SkyBottom);
    SpawnFloor(FVector(RoomSize.X, RoomSize.Y, 2), SC::FloorTile, SC::FloorTile * 0.92f, true);
    SpawnWalls(RoomSize, 250, SC::WallWhite);
    SpawnDirectionalLight(FRotator(-45, 30, 0), 3.5f, FLinearColor(1.0f, 0.98f, 0.95f));
    SpawnLight(FVector(-150, 0, 240), 2500.0f, FLinearColor(1.0f, 0.98f, 0.95f));
    SpawnLight(FVector(150, 0, 240), 2500.0f, FLinearColor(1.0f, 0.98f, 0.95f));

    SpawnMesh(MeshData_MALL_ESCALATOR::Vertices, MeshData_MALL_ESCALATOR::Normals,
              MeshData_MALL_ESCALATOR::UVs, MeshData_MALL_ESCALATOR::Triangles,
              MeshData_MALL_ESCALATOR::NumVertices, MeshData_MALL_ESCALATOR::NumVertices,
              FVector(0, -200, 0), FRotator(0, 0, 0), FVector(1.0f, 1.0f, 1.0f),
              SC::MetalSilver, 1.0f);

    SpawnMesh(MeshData_MALL_PLANTER::Vertices, MeshData_MALL_PLANTER::Normals,
              MeshData_MALL_PLANTER::UVs, MeshData_MALL_PLANTER::Triangles,
              MeshData_MALL_PLANTER::NumVertices, MeshData_MALL_PLANTER::NumVertices,
              FVector(-200, 100, 0), FRotator(0, 0, 0), FVector(1.5f, 1.5f, 1.0f),
              SC::TreeLeaves, 1.0f);
    SpawnMesh(MeshData_MALL_PLANTER::Vertices, MeshData_MALL_PLANTER::Normals,
              MeshData_MALL_PLANTER::UVs, MeshData_MALL_PLANTER::Triangles,
              MeshData_MALL_PLANTER::NumVertices, MeshData_MALL_PLANTER::NumVertices,
              FVector(200, 100, 0), FRotator(0, 0, 0), FVector(1.5f, 1.5f, 1.0f),
              SC::TreeLeaves, 0.95f);

    // Shop shelves in mall
    SpawnMesh(MeshData_SHOP_SHELF::Vertices, MeshData_SHOP_SHELF::Normals,
              MeshData_SHOP_SHELF::UVs, MeshData_SHOP_SHELF::Triangles,
              MeshData_SHOP_SHELF::NumVertices, MeshData_SHOP_SHELF::NumVertices,
              FVector(-300, 0, 0), FRotator(0, 90, 0), FVector(1.0f, 1.0f, 1.0f),
              SC::WoodLight, 1.0f);

    SpawnCharacterMesh(TEXT("Emersyn"), FVector(0, 50, 0), FRotator(0, 10, 0), 1.0f,
                       SC::SkinLight, SC::OutfitPink);
    SpawnCharacterMesh(TEXT("Mia"), FVector(80, 0, 0), FRotator(0, -30, 0), 1.0f,
                       SC::SkinMedium, SC::OutfitYellow);

    SetupIsometricCamera(FVector(0, 0, 80), 1000);
}

// ========= ARCADE =========
void AEmersynGameMode::BuildArcade()
{
    FVector RoomSize(300, 300, 0);
    SpawnSky(SC::SkyNightTop, SC::SkyNightBot);
    SpawnFloor(FVector(RoomSize.X, RoomSize.Y, 2), FLinearColor(0.12f, 0.08f, 0.18f),
               FLinearColor(0.15f, 0.10f, 0.22f), false);
    SpawnWalls(RoomSize, 220, FLinearColor(0.15f, 0.10f, 0.25f));
    SpawnLight(FVector(-100, 0, 210), 1500.0f, SC::ArcadeNeon);
    SpawnLight(FVector(100, 0, 210), 1500.0f, SC::ArcadePurple);

    SpawnMesh(MeshData_ARCADE_CABINET::Vertices, MeshData_ARCADE_CABINET::Normals,
              MeshData_ARCADE_CABINET::UVs, MeshData_ARCADE_CABINET::Triangles,
              MeshData_ARCADE_CABINET::NumVertices, MeshData_ARCADE_CABINET::NumVertices,
              FVector(-150, -150, 0), FRotator(0, 30, 0), FVector(1.2f, 1.2f, 1.2f),
              SC::ArcadePurple, 1.1f);
    SpawnMesh(MeshData_ARCADE_CABINET::Vertices, MeshData_ARCADE_CABINET::Normals,
              MeshData_ARCADE_CABINET::UVs, MeshData_ARCADE_CABINET::Triangles,
              MeshData_ARCADE_CABINET::NumVertices, MeshData_ARCADE_CABINET::NumVertices,
              FVector(150, -150, 0), FRotator(0, -30, 0), FVector(1.2f, 1.2f, 1.2f),
              SC::ArcadeNeon, 1.1f);

    SpawnMesh(MeshData_ARCADE_CLAW_MACHINE::Vertices, MeshData_ARCADE_CLAW_MACHINE::Normals,
              MeshData_ARCADE_CLAW_MACHINE::UVs, MeshData_ARCADE_CLAW_MACHINE::Triangles,
              MeshData_ARCADE_CLAW_MACHINE::NumVertices, MeshData_ARCADE_CLAW_MACHINE::NumVertices,
              FVector(0, 100, 0), FRotator(0, 0, 0), FVector(1.3f, 1.3f, 1.3f),
              SC::FabricYellow, 1.0f);

    SpawnCharacterMesh(TEXT("Emersyn"), FVector(-50, -30, 0), FRotator(0, 20, 0), 1.0f,
                       SC::SkinLight, SC::OutfitPink);
    SpawnCharacterMesh(TEXT("Leo"), FVector(50, -30, 0), FRotator(0, -20, 0), 1.0f,
                       SC::SkinLight, SC::OutfitBlue);

    SetupIsometricCamera(FVector(0, 0, 60), 800);
}

// ========= AMUSEMENT PARK =========
void AEmersynGameMode::BuildAmusementPark()
{
    SpawnSky(SC::SkyTop, SC::SkyBottom);
    SpawnFloor(FVector(600, 600, 2), SC::FloorConcrete, SC::FloorConcrete * 0.9f, false);
    SpawnDirectionalLight(FRotator(-35, 50, 0), 4.5f, FLinearColor(1.0f, 0.95f, 0.80f));

    SpawnMesh(MeshData_AMUSEMENT_CAROUSEL::Vertices, MeshData_AMUSEMENT_CAROUSEL::Normals,
              MeshData_AMUSEMENT_CAROUSEL::UVs, MeshData_AMUSEMENT_CAROUSEL::Triangles,
              MeshData_AMUSEMENT_CAROUSEL::NumVertices, MeshData_AMUSEMENT_CAROUSEL::NumVertices,
              FVector(-200, 0, 0), FRotator(0, 0, 0), FVector(1.5f, 1.5f, 1.5f),
              SC::CarouselRed, 1.0f);

    SpawnMesh(MeshData_AMUSEMENT_FERRISWHEEL::Vertices, MeshData_AMUSEMENT_FERRISWHEEL::Normals,
              MeshData_AMUSEMENT_FERRISWHEEL::UVs, MeshData_AMUSEMENT_FERRISWHEEL::Triangles,
              MeshData_AMUSEMENT_FERRISWHEEL::NumVertices, MeshData_AMUSEMENT_FERRISWHEEL::NumVertices,
              FVector(200, -100, 0), FRotator(0, 0, 0), FVector(2.0f, 2.0f, 2.0f),
              SC::MetalSilver, 1.0f);

    SpawnMesh(MeshData_AMUSEMENT_FOODCART::Vertices, MeshData_AMUSEMENT_FOODCART::Normals,
              MeshData_AMUSEMENT_FOODCART::UVs, MeshData_AMUSEMENT_FOODCART::Triangles,
              MeshData_AMUSEMENT_FOODCART::NumVertices, MeshData_AMUSEMENT_FOODCART::NumVertices,
              FVector(0, 200, 0), FRotator(0, -45, 0), FVector(1.2f, 1.2f, 1.2f),
              SC::FabricOrange, 1.0f);

    // Lamppost
    SpawnMesh(MeshData_PARK_LAMPPOST::Vertices, MeshData_PARK_LAMPPOST::Normals,
              MeshData_PARK_LAMPPOST::UVs, MeshData_PARK_LAMPPOST::Triangles,
              MeshData_PARK_LAMPPOST::NumVertices, MeshData_PARK_LAMPPOST::NumVertices,
              FVector(-50, -250, 0), FRotator(0, 0, 0), FVector(1.5f, 1.5f, 2.0f),
              SC::MetalBlack, 1.0f);

    SpawnCharacterMesh(TEXT("Emersyn"), FVector(0, 0, 0), FRotator(0, 0, 0), 1.0f,
                       SC::SkinLight, SC::OutfitPink);
    SpawnCharacterMesh(TEXT("Leo"), FVector(60, 30, 0), FRotator(0, -20, 0), 1.0f,
                       SC::SkinLight, SC::OutfitBlue);
    SpawnCharacterMesh(TEXT("Ava"), FVector(-60, 30, 0), FRotator(0, 20, 0), 1.0f,
                       SC::SkinMedium, SC::OutfitGreen);
    SpawnCharacterMesh(TEXT("Cat"), FVector(0, 80, 0), FRotator(0, 0, 0), 0.5f,
                       FLinearColor(0.85f, 0.75f, 0.55f), FLinearColor(0.85f, 0.75f, 0.55f));

    SetupIsometricCamera(FVector(0, 0, 80), 1100);
}
