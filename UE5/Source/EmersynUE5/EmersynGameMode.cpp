// v24: Immersive fullscreen + minimal fuzz + faster splash, post-processing, geometry density, camera
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
#include "GameFramework/DefaultPawn.h"
#include "Engine/PostProcessVolume.h"
#include "Engine/SkyLight.h"
#include "Components/SkyLightComponent.h"
#include "Components/PostProcessComponent.h"
#include "Components/TextRenderComponent.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "Engine/Texture2D.h"
#include "TextureResource.h"

// Mesh data includes
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
    const FLinearColor WoodLight(0.82f, 0.62f, 0.38f);
    const FLinearColor WoodMedium(0.62f, 0.38f, 0.15f);
    const FLinearColor WoodDark(0.42f, 0.22f, 0.08f);
    const FLinearColor WoodCherry(0.50f, 0.15f, 0.08f);
    const FLinearColor FabricPink(1.0f, 0.45f, 0.60f);
    const FLinearColor FabricBlue(0.30f, 0.55f, 0.95f);
    const FLinearColor FabricGreen(0.25f, 0.85f, 0.45f);
    const FLinearColor FabricPurple(0.70f, 0.30f, 0.90f);
    const FLinearColor FabricRed(0.95f, 0.15f, 0.15f);
    const FLinearColor FabricYellow(1.0f, 0.90f, 0.25f);
    const FLinearColor FabricOrange(1.0f, 0.55f, 0.15f);
    const FLinearColor FabricCream(0.98f, 0.95f, 0.88f);
    const FLinearColor MetalSilver(0.78f, 0.78f, 0.80f);
    const FLinearColor MetalGold(0.85f, 0.75f, 0.45f);
    const FLinearColor MetalBlack(0.15f, 0.15f, 0.18f);
    const FLinearColor WallWhite(0.98f, 0.96f, 0.93f);
    const FLinearColor WallPink(1.0f, 0.60f, 0.75f);
    const FLinearColor WallBlue(0.55f, 0.75f, 1.0f);
    const FLinearColor WallGreen(0.50f, 0.95f, 0.65f);
    const FLinearColor WallYellow(1.0f, 0.92f, 0.50f);
    const FLinearColor FloorWood(0.72f, 0.50f, 0.28f);
    const FLinearColor FloorTile(0.92f, 0.92f, 0.88f);
    const FLinearColor FloorGrass(0.25f, 0.75f, 0.25f);
    const FLinearColor FloorSand(0.95f, 0.85f, 0.60f);
    const FLinearColor FloorConcrete(0.75f, 0.72f, 0.68f);
    const FLinearColor SkyTop(0.35f, 0.60f, 1.0f);
    const FLinearColor SkyBot(0.75f, 0.88f, 1.0f);
    const FLinearColor CeilingWhite(0.95f, 0.93f, 0.90f);
    const FLinearColor TileWhite(0.95f, 0.95f, 0.92f);
    const FLinearColor TileBlue(0.60f, 0.78f, 0.95f);
    const FLinearColor BrickRed(0.72f, 0.28f, 0.15f);
    const FLinearColor BrickMortar(0.85f, 0.82f, 0.75f);
    const FLinearColor MarbleWhite(0.95f, 0.93f, 0.90f);
    const FLinearColor MarbleVein(0.65f, 0.62f, 0.58f);
    const FLinearColor CarpetBeige(0.85f, 0.78f, 0.65f);
    const FLinearColor CarpetPurple(0.55f, 0.30f, 0.70f);
    const FLinearColor WPStripe1(0.90f, 0.85f, 0.75f);
    const FLinearColor WPStripe2(0.80f, 0.70f, 0.55f);
}

// ========= Constructor =========
AEmersynGameMode::AEmersynGameMode()
{
    PrimaryActorTick.bCanEverTick = true;
    DefaultPawnClass = nullptr;
    bCameraMoving = false;
    CamMoveAlpha = 0.f;
    IsoCam = nullptr;
    M_VertexColor = nullptr;
    DefaultMID = nullptr;
    RoomIndex = 0;
    RoomTimer = 0.f;
    RoomDuration = 6.f;
    RoomList.Add(TEXT("Splash"));
    RoomList.Add(TEXT("Bedroom"));
    RoomList.Add(TEXT("Kitchen"));
    RoomList.Add(TEXT("Bathroom"));
    RoomList.Add(TEXT("LivingRoom"));
    RoomList.Add(TEXT("Garden"));
    RoomList.Add(TEXT("School"));
    RoomList.Add(TEXT("Shop"));
    RoomList.Add(TEXT("Playground"));
    RoomList.Add(TEXT("Park"));
    RoomList.Add(TEXT("Mall"));
    RoomList.Add(TEXT("Arcade"));
    RoomList.Add(TEXT("AmusementPark"));
}

// ========= InitGame =========
void AEmersynGameMode::InitGame(const FString& MapName, const FString& Options, FString& ErrorMessage)
{
    Super::InitGame(MapName, Options, ErrorMessage);
    static ConstructorHelpers::FObjectFinder<UMaterial> MatFinder(TEXT("/Game/Materials/M_VertexColor"));
    if (MatFinder.Succeeded()) { M_VertexColor = MatFinder.Object; }
}

// ========= BeginPlay =========
void AEmersynGameMode::BeginPlay()
{
    Super::BeginPlay();
    APlayerController* PC = GetWorld()->GetFirstPlayerController();
    if (PC) {
        PC->SetIgnoreLookInput(true);
        PC->SetIgnoreMoveInput(true);
        APawn* P = PC->GetPawn();
        if (P) { P->SetActorHiddenInGame(true); P->SetActorEnableCollision(false); }
    }
    RoomIndex = 1; LoadRoom(RoomList[1]);
}

// ========= Tick =========
void AEmersynGameMode::Tick(float DeltaSeconds)
{
    Super::Tick(DeltaSeconds);
    if (bCameraMoving && IsoCam) {
        CamMoveAlpha = FMath::Clamp(CamMoveAlpha + DeltaSeconds * 2.0f, 0.f, 1.f);
        float T = FMath::InterpEaseInOut(0.f, 1.f, CamMoveAlpha, 2.0f);
        IsoCam->SetActorLocation(FMath::Lerp(CamStartPos, CamTargetPos, T));
        IsoCam->SetActorRotation(FMath::Lerp(CamStartRot, CamTargetRot, T));
        if (CamMoveAlpha >= 1.f) bCameraMoving = false;
    }
    // v21: Auto-cycle through all rooms every 5 seconds
    RoomTimer += DeltaSeconds;
    if (RoomTimer >= RoomDuration) {
        RoomTimer = 0.f;
        RoomIndex = (RoomIndex + 1) % RoomList.Num();
        LoadRoom(RoomList[RoomIndex]);
    }
}

// ========= LoadRoom =========
void AEmersynGameMode::LoadRoom(const FString& RoomName)
{
    ClearRoom();
    CurrentRoom = RoomName;
    if (RoomName == TEXT("Splash")) BuildSplashScreen();
    if (RoomName == TEXT("MainMenu")) BuildMainMenu();
    if (RoomName == TEXT("Bedroom")) BuildBedroom();
    if (RoomName == TEXT("Kitchen")) BuildKitchen();
    if (RoomName == TEXT("Bathroom")) BuildBathroom();
    if (RoomName == TEXT("LivingRoom")) BuildLivingRoom();
    if (RoomName == TEXT("Garden")) BuildGarden();
    if (RoomName == TEXT("School")) BuildSchool();
    if (RoomName == TEXT("Shop")) BuildShop();
    if (RoomName == TEXT("Playground")) BuildPlayground();
    if (RoomName == TEXT("Park")) BuildPark();
    if (RoomName == TEXT("Mall")) BuildMall();
    if (RoomName == TEXT("Arcade")) BuildArcade();
    if (RoomName == TEXT("AmusementPark")) BuildAmusementPark();
}

// ========= Noise Functions =========
float AEmersynGameMode::SimpleNoise(float X, float Y) const
{
    int32 IX = FMath::FloorToInt(X) & 255;
    int32 IY = FMath::FloorToInt(Y) & 255;
    float FX = X - FMath::FloorToFloat(X);
    float FY = Y - FMath::FloorToFloat(Y);
    float U = FX * FX * (3.f - 2.f * FX);
    float V = FY * FY * (3.f - 2.f * FY);
    int32 A = (IX * 127 + IY * 311 + 12345) & 255;
    int32 B = ((IX+1) * 127 + IY * 311 + 12345) & 255;
    int32 C = (IX * 127 + (IY+1) * 311 + 12345) & 255;
    int32 D = ((IX+1) * 127 + (IY+1) * 311 + 12345) & 255;
    float FA = (float)(A & 127) / 127.f;
    float FB = (float)(B & 127) / 127.f;
    float FC = (float)(C & 127) / 127.f;
    float FD = (float)(D & 127) / 127.f;
    float L1 = FMath::Lerp(FA, FB, U);
    float L2 = FMath::Lerp(FC, FD, U);
    return FMath::Lerp(L1, L2, V);
}

float AEmersynGameMode::FBMNoise(float X, float Y, int32 Octaves) const
{
    float Val = 0.f, Amp = 1.f, Freq = 1.f, MaxVal = 0.f;
    for (int32 I = 0; I < Octaves; I++) {
        Val += SimpleNoise(X * Freq, Y * Freq) * Amp;
        MaxVal += Amp; Amp *= 0.5f; Freq *= 2.f;
    }
    return Val / MaxVal;
}

// ========= v21 Normal-Based Directional Shading =========
FLinearColor AEmersynGameMode::ApplyDirectionalShading(FLinearColor BaseColor, FVector Normal, float AO) const
{
    FVector KeyLightDir = FVector(0.5f, -0.3f, -0.7f).GetSafeNormal();
    FVector FillLightDir = FVector(-0.6f, 0.4f, -0.3f).GetSafeNormal();
    FVector RimLightDir = FVector(0.0f, 0.8f, -0.2f).GetSafeNormal();
    float KeyDot = FMath::Max(0.f, FVector::DotProduct(Normal, -KeyLightDir));
    float FillDot = FMath::Max(0.f, FVector::DotProduct(Normal, -FillLightDir));
    float RimDot = FMath::Max(0.f, FVector::DotProduct(Normal, -RimLightDir));
    FLinearColor KeyColor(1.0f, 0.95f, 0.85f);
    FLinearColor FillColor(0.6f, 0.7f, 0.85f);
    FLinearColor RimColor(1.0f, 0.98f, 0.95f);
    FLinearColor Ambient(0.35f, 0.35f, 0.40f);
    FLinearColor Lit = Ambient;
    Lit.R += BaseColor.R * KeyDot * 0.80f * KeyColor.R;
    Lit.G += BaseColor.G * KeyDot * 0.80f * KeyColor.G;
    Lit.B += BaseColor.B * KeyDot * 0.80f * KeyColor.B;
    Lit.R += BaseColor.R * FillDot * 0.35f * FillColor.R;
    Lit.G += BaseColor.G * FillDot * 0.35f * FillColor.G;
    Lit.B += BaseColor.B * FillDot * 0.35f * FillColor.B;
    float RimPow = FMath::Pow(RimDot, 2.0f);
    Lit.R += RimPow * 0.15f * RimColor.R;
    Lit.G += RimPow * 0.15f * RimColor.G;
    Lit.B += RimPow * 0.15f * RimColor.B;
    Lit.R *= AO; Lit.G *= AO; Lit.B *= AO; Lit.A = 1.0f;
    Lit.R = FMath::Clamp(Lit.R, 0.f, 1.f);
    Lit.G = FMath::Clamp(Lit.G, 0.f, 1.f);
    Lit.B = FMath::Clamp(Lit.B, 0.f, 1.f);
    return Lit;
}

// ========= Texture Fill Functions =========
void AEmersynGameMode::FillWoodGrain(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Accent)
{
    for (int32 Y = 0; Y < H; Y++) {
        for (int32 X = 0; X < W; X++) {
            float N = FBMNoise(X * 0.03f, Y * 0.15f, 4);
            float Ring = FMath::Sin(N * 25.f + Y * 0.08f) * 0.5f + 0.5f;
            FLinearColor C = FMath::Lerp(Base, Accent, Ring * 0.6f);
            float Detail = SimpleNoise(X * 0.5f, Y * 0.5f) * 0.08f;
            C.R = FMath::Clamp(C.R + Detail, 0.f, 1.f);
            C.G = FMath::Clamp(C.G + Detail, 0.f, 1.f);
            C.B = FMath::Clamp(C.B + Detail, 0.f, 1.f);
            P[Y * W + X] = C.ToFColor(true);
        }
    }
}

void AEmersynGameMode::FillTileGrid(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Grout)
{
    int32 TileSize = 32;
    int32 GroutW = 2;
    for (int32 Y = 0; Y < H; Y++) {
        for (int32 X = 0; X < W; X++) {
            int32 TX = X % TileSize;
            int32 TY = Y % TileSize;
            bool bGrout = (TX < GroutW || TY < GroutW);
            FLinearColor C = bGrout ? Grout : Base;
            if (!bGrout) {
                float N = SimpleNoise(X * 0.2f, Y * 0.2f) * 0.06f;
                C.R = FMath::Clamp(C.R + N, 0.f, 1.f);
                C.G = FMath::Clamp(C.G + N, 0.f, 1.f);
                C.B = FMath::Clamp(C.B + N, 0.f, 1.f);
            }
            P[Y * W + X] = C.ToFColor(true);
        }
    }
}

void AEmersynGameMode::FillWallpaper(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Pattern)
{
    int32 StripeW = 16;
    for (int32 Y = 0; Y < H; Y++) {
        for (int32 X = 0; X < W; X++) {
            int32 SX = (X / StripeW) % 2;
            FLinearColor C = (SX == 0) ? Base : FMath::Lerp(Base, Pattern, 0.35f);
            float N = SimpleNoise(X * 0.1f, Y * 0.1f) * 0.03f;
            C.R = FMath::Clamp(C.R + N, 0.f, 1.f);
            C.G = FMath::Clamp(C.G + N, 0.f, 1.f);
            C.B = FMath::Clamp(C.B + N, 0.f, 1.f);
            P[Y * W + X] = C.ToFColor(true);
        }
    }
}

void AEmersynGameMode::FillCarpet(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Fiber)
{
    for (int32 Y = 0; Y < H; Y++) {
        for (int32 X = 0; X < W; X++) {
            float N1 = FBMNoise(X * 0.08f, Y * 0.08f, 3);
            float N2 = SimpleNoise(X * 2.f, Y * 2.f) * 0.15f;
            FLinearColor C = FMath::Lerp(Base, Fiber, N1 * 0.4f + N2);
            P[Y * W + X] = C.ToFColor(true);
        }
    }
}

void AEmersynGameMode::FillGrass(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Tip)
{
    for (int32 Y = 0; Y < H; Y++) {
        for (int32 X = 0; X < W; X++) {
            float N = FBMNoise(X * 0.06f, Y * 0.06f, 4);
            float Blade = FMath::Abs(FMath::Sin(X * 0.8f + N * 5.f));
            FLinearColor C = FMath::Lerp(Base, Tip, Blade * 0.5f + N * 0.3f);
            float D = SimpleNoise(X * 1.5f, Y * 1.5f) * 0.1f;
            C.R = FMath::Clamp(C.R + D - 0.05f, 0.f, 1.f);
            C.G = FMath::Clamp(C.G + D, 0.f, 1.f);
            C.B = FMath::Clamp(C.B + D - 0.05f, 0.f, 1.f);
            P[Y * W + X] = C.ToFColor(true);
        }
    }
}

void AEmersynGameMode::FillConcrete(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Speckle)
{
    for (int32 Y = 0; Y < H; Y++) {
        for (int32 X = 0; X < W; X++) {
            float N = FBMNoise(X * 0.04f, Y * 0.04f, 3);
            float S = SimpleNoise(X * 3.f, Y * 3.f);
            FLinearColor C = FMath::Lerp(Base, Speckle, N * 0.3f);
            if (S > 0.85f) C = FMath::Lerp(C, Speckle, 0.4f);
            P[Y * W + X] = C.ToFColor(true);
        }
    }
}

void AEmersynGameMode::FillBrick(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Mortar)
{
    int32 BW = 32, BH = 16, MW = 2;
    for (int32 Y = 0; Y < H; Y++) {
        for (int32 X = 0; X < W; X++) {
            int32 Row = Y / BH;
            int32 Offset = (Row % 2 == 0) ? 0 : BW / 2;
            int32 BX = (X + Offset) % BW;
            int32 BY = Y % BH;
            bool bMortar = (BX < MW || BY < MW);
            FLinearColor C = bMortar ? Mortar : Base;
            if (!bMortar) {
                float N = SimpleNoise(X * 0.15f, Y * 0.15f) * 0.1f;
                C.R = FMath::Clamp(C.R + N, 0.f, 1.f);
                C.G = FMath::Clamp(C.G + N * 0.5f, 0.f, 1.f);
                C.B = FMath::Clamp(C.B + N * 0.3f, 0.f, 1.f);
            }
            P[Y * W + X] = C.ToFColor(true);
        }
    }
}

void AEmersynGameMode::FillMarble(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Vein)
{
    for (int32 Y = 0; Y < H; Y++) {
        for (int32 X = 0; X < W; X++) {
            float N = FBMNoise(X * 0.02f, Y * 0.02f, 5);
            float VeinF = FMath::Abs(FMath::Sin((X + Y) * 0.03f + N * 8.f));
            VeinF = FMath::Pow(VeinF, 3.0f);
            FLinearColor C = FMath::Lerp(Base, Vein, VeinF * 0.6f);
            float Sheen = SimpleNoise(X * 0.3f, Y * 0.3f) * 0.04f;
            C.R = FMath::Clamp(C.R + Sheen, 0.f, 1.f);
            C.G = FMath::Clamp(C.G + Sheen, 0.f, 1.f);
            C.B = FMath::Clamp(C.B + Sheen, 0.f, 1.f);
            P[Y * W + X] = C.ToFColor(true);
        }
    }
}

void AEmersynGameMode::FillMetal(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Highlight)
{
    for (int32 Y = 0; Y < H; Y++) {
        for (int32 X = 0; X < W; X++) {
            float N = SimpleNoise(X * 0.5f, Y * 0.01f) * 0.15f;
            float Brush = FMath::Abs(FMath::Sin(Y * 0.3f + N * 10.f));
            FLinearColor C = FMath::Lerp(Base, Highlight, Brush * 0.3f + N);
            P[Y * W + X] = C.ToFColor(true);
        }
    }
}

void AEmersynGameMode::FillFabric(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Thread)
{
    for (int32 Y = 0; Y < H; Y++) {
        for (int32 X = 0; X < W; X++) {
            bool bThread = ((X + Y) % 3 == 0) || ((X - Y + 256) % 5 == 0);
            float N = SimpleNoise(X * 0.3f, Y * 0.3f) * 0.08f;
            FLinearColor C = bThread ? FMath::Lerp(Base, Thread, 0.3f) : Base;
            C.R = FMath::Clamp(C.R + N, 0.f, 1.f);
            C.G = FMath::Clamp(C.G + N, 0.f, 1.f);
            C.B = FMath::Clamp(C.B + N, 0.f, 1.f);
            P[Y * W + X] = C.ToFColor(true);
        }
    }
}

void AEmersynGameMode::FillSand(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Grain)
{
    for (int32 Y = 0; Y < H; Y++) {
        for (int32 X = 0; X < W; X++) {
            float N = FBMNoise(X * 0.05f, Y * 0.05f, 3);
            float G = SimpleNoise(X * 4.f, Y * 4.f);
            FLinearColor C = FMath::Lerp(Base, Grain, N * 0.25f);
            if (G > 0.7f) C = FMath::Lerp(C, Grain, 0.2f);
            P[Y * W + X] = C.ToFColor(true);
        }
    }
}

void AEmersynGameMode::FillWater(TArray<FColor>& P, int32 W, int32 H, FLinearColor Base, FLinearColor Highlight)
{
    for (int32 Y = 0; Y < H; Y++) {
        for (int32 X = 0; X < W; X++) {
            float N = FBMNoise(X * 0.03f, Y * 0.03f, 4);
            float Wave = FMath::Sin(X * 0.1f + N * 6.f) * 0.5f + 0.5f;
            FLinearColor C = FMath::Lerp(Base, Highlight, Wave * 0.3f);
            float Sparkle = SimpleNoise(X * 2.f, Y * 2.f);
            if (Sparkle > 0.92f) C = FMath::Lerp(C, FLinearColor::White, 0.5f);
            P[Y * W + X] = C.ToFColor(true);
        }
    }
}

// ========= Generate Procedural Texture =========
UTexture2D* AEmersynGameMode::GenerateProceduralTexture(ETexturePattern Pattern, FLinearColor BaseColor, FLinearColor AccentColor, int32 Size)
{
    FString Key = FString::Printf(TEXT("%d_%f_%f_%f_%f_%f_%f_%d"), (int)Pattern, BaseColor.R, BaseColor.G, BaseColor.B, AccentColor.R, AccentColor.G, AccentColor.B, Size);
    if (UTexture2D** Found = TextureCache.Find(Key)) return *Found;

    TArray<FColor> Pixels;
    Pixels.SetNum(Size * Size);

    switch (Pattern) {
    case ETexturePattern::WoodGrain: FillWoodGrain(Pixels, Size, Size, BaseColor, AccentColor); break;
    case ETexturePattern::TileGrid: FillTileGrid(Pixels, Size, Size, BaseColor, AccentColor); break;
    case ETexturePattern::Wallpaper: FillWallpaper(Pixels, Size, Size, BaseColor, AccentColor); break;
    case ETexturePattern::Carpet: FillCarpet(Pixels, Size, Size, BaseColor, AccentColor); break;
    case ETexturePattern::Grass: FillGrass(Pixels, Size, Size, BaseColor, AccentColor); break;
    case ETexturePattern::Concrete: FillConcrete(Pixels, Size, Size, BaseColor, AccentColor); break;
    case ETexturePattern::Brick: FillBrick(Pixels, Size, Size, BaseColor, AccentColor); break;
    case ETexturePattern::Marble: FillMarble(Pixels, Size, Size, BaseColor, AccentColor); break;
    case ETexturePattern::Metal: FillMetal(Pixels, Size, Size, BaseColor, AccentColor); break;
    case ETexturePattern::Fabric: FillFabric(Pixels, Size, Size, BaseColor, AccentColor); break;
    case ETexturePattern::Sand: FillSand(Pixels, Size, Size, BaseColor, AccentColor); break;
    case ETexturePattern::Water: FillWater(Pixels, Size, Size, BaseColor, AccentColor); break;
    }

    UTexture2D* Tex = UTexture2D::CreateTransient(Size, Size, PF_B8G8R8A8);
    if (!Tex) return nullptr;
    void* Data = Tex->GetPlatformData()->Mips[0].BulkData.Lock(LOCK_READ_WRITE);
    FMemory::Memcpy(Data, Pixels.GetData(), Pixels.Num() * sizeof(FColor));
    Tex->GetPlatformData()->Mips[0].BulkData.Unlock();
    Tex->Filter = TF_Bilinear;
    Tex->SRGB = true;
    Tex->UpdateResource();

    TextureCache.Add(Key, Tex);
    return Tex;
}

// ========= Create Textured Material =========
UMaterialInstanceDynamic* AEmersynGameMode::CreateTexturedMaterial(UTexture2D* Texture, float Roughness, float Metallic)
{
    if (!M_VertexColor) return nullptr;
    UMaterialInstanceDynamic* MID = UMaterialInstanceDynamic::Create(M_VertexColor, this);
    return MID;
}

// ========= Textured Floor =========
AActor* AEmersynGameMode::SpawnTexturedFloor(FVector Center, FVector Size, ETexturePattern Pattern, FLinearColor Base, FLinearColor Accent, float UVScale)
{
    AActor* A = GetWorld()->SpawnActor<AActor>(AActor::StaticClass(), FTransform(Center));
    if (!A) return nullptr;
    A->SetRootComponent(NewObject<USceneComponent>(A));
    A->GetRootComponent()->RegisterComponent();
    UProceduralMeshComponent* PMC = NewObject<UProceduralMeshComponent>(A);
    PMC->SetupAttachment(A->GetRootComponent());
    PMC->RegisterComponent();

    UTexture2D* Tex = GenerateProceduralTexture(Pattern, Base, Accent, 256);

    float HX = Size.X, HY = Size.Y;
    int32 GridRes = 32;
    TArray<FVector> Verts; TArray<int32> Tris; TArray<FColor> Colors;
    TArray<FVector> Normals; TArray<FVector2D> UVs; TArray<FProcMeshTangent> Tangents;

    for (int32 GY = 0; GY <= GridRes; GY++) {
        for (int32 GX = 0; GX <= GridRes; GX++) {
            float FracX = (float)GX / GridRes;
            float FracY = (float)GY / GridRes;
            Verts.Add(FVector(FracX * HX * 2 - HX, FracY * HY * 2 - HY, 0));
            Normals.Add(FVector(0, 0, 1));
            UVs.Add(FVector2D(FracX * UVScale, FracY * UVScale));
            Tangents.Add(FProcMeshTangent(1, 0, 0));

            float N = FBMNoise(FracX * 6.f * UVScale, FracY * 6.f * UVScale, 4);
            FLinearColor FloorC = FMath::Lerp(Base, Accent, N * 0.65f + 0.18f);
            FLinearColor ShadedFloor = ApplyDirectionalShading(FloorC, FVector(0, 0, 1), 1.0f);
            Colors.Add(ShadedFloor.ToFColor(true));
        }
    }

    for (int32 GY = 0; GY < GridRes; GY++) {
        for (int32 GX = 0; GX < GridRes; GX++) {
            int32 I = GY * (GridRes + 1) + GX;
            Tris.Add(I); Tris.Add(I + GridRes + 1); Tris.Add(I + 1);
            Tris.Add(I + 1); Tris.Add(I + GridRes + 1); Tris.Add(I + GridRes + 2);
        }
    }

    PMC->CreateMeshSection(0, Verts, Tris, Normals, UVs, Colors, Tangents, false);
    if (M_VertexColor) {
        UMaterialInstanceDynamic* MID = UMaterialInstanceDynamic::Create(M_VertexColor, this);
        PMC->SetMaterial(0, MID);
    }
    PMC->SetCastShadow(true);
    RoomActors.Add(A);
    return A;
}

// ========= Textured Wall =========
AActor* AEmersynGameMode::SpawnTexturedWall(FVector Start, FVector End, float Height, ETexturePattern Pattern, FLinearColor Base, FLinearColor Accent)
{
    AActor* A = GetWorld()->SpawnActor<AActor>(AActor::StaticClass(), FTransform(Start));
    if (!A) return nullptr;
    A->SetRootComponent(NewObject<USceneComponent>(A));
    A->GetRootComponent()->RegisterComponent();
    UProceduralMeshComponent* PMC = NewObject<UProceduralMeshComponent>(A);
    PMC->SetupAttachment(A->GetRootComponent());
    PMC->RegisterComponent();

    FVector Dir = End - Start;
    float WallLen = Dir.Size();
    Dir.Normalize();
    FVector Normal = FVector::CrossProduct(Dir, FVector::UpVector).GetSafeNormal();

    int32 SegX = 24, SegY = 16;
    TArray<FVector> Verts; TArray<int32> Tris; TArray<FColor> Colors;
    TArray<FVector> Normals; TArray<FVector2D> UVs; TArray<FProcMeshTangent> Tangents;

    for (int32 SY = 0; SY <= SegY; SY++) {
        for (int32 SX = 0; SX <= SegX; SX++) {
            float FX = (float)SX / SegX;
            float FY = (float)SY / SegY;
            FVector Pos = Start + Dir * WallLen * FX + FVector(0, 0, Height * FY);
            Verts.Add(Pos - Start);
            Normals.Add(Normal);
            UVs.Add(FVector2D(FX * 3.f, FY * 2.f));
            Tangents.Add(FProcMeshTangent(Dir.X, Dir.Y, Dir.Z));

            float N = SimpleNoise(FX * 8.f, FY * 4.f);
            FLinearColor C;
            if (Pattern == ETexturePattern::Wallpaper) {
                int32 Stripe = ((int32)(FX * 24.f)) % 2;
                C = (Stripe == 0) ? Base : FMath::Lerp(Base, Accent, 0.35f);
                C.R = FMath::Clamp(C.R + N * 0.04f, 0.f, 1.f);
                C.G = FMath::Clamp(C.G + N * 0.04f, 0.f, 1.f);
                C.B = FMath::Clamp(C.B + N * 0.04f, 0.f, 1.f);
            } else if (Pattern == ETexturePattern::Brick) {
                int32 Row = (int32)(FY * 16.f);
                float BX = FX * 24.f + ((Row % 2 == 0) ? 0.f : 0.5f);
                bool bMortar = (FMath::Frac(BX / 2.f) < 0.08f) || (FMath::Frac(FY * 16.f) < 0.12f);
                C = bMortar ? Accent : Base;
                C.R = FMath::Clamp(C.R + N * 0.08f, 0.f, 1.f);
                C.G = FMath::Clamp(C.G + N * 0.05f, 0.f, 1.f);
            } else if (Pattern == ETexturePattern::TileGrid) {
                int32 TileX = ((int32)(FX * 32.f)) % 4;
                int32 TileY = ((int32)(FY * 16.f)) % 4;
                bool bGrout = (TileX == 0 || TileY == 0);
                C = bGrout ? Accent : Base;
                C.R = FMath::Clamp(C.R + N * 0.04f, 0.f, 1.f);
                C.G = FMath::Clamp(C.G + N * 0.04f, 0.f, 1.f);
                C.B = FMath::Clamp(C.B + N * 0.04f, 0.f, 1.f);
            } else {
                C = FMath::Lerp(Base, Accent, N * 0.3f);
            }
            FLinearColor ShadedWall = ApplyDirectionalShading(C, Normal, 1.0f - FY * 0.1f);
            Colors.Add(ShadedWall.ToFColor(true));
        }
    }

    for (int32 SY = 0; SY < SegY; SY++) {
        for (int32 SX = 0; SX < SegX; SX++) {
            int32 I = SY * (SegX + 1) + SX;
            Tris.Add(I); Tris.Add(I + SegX + 1); Tris.Add(I + 1);
            Tris.Add(I + 1); Tris.Add(I + SegX + 1); Tris.Add(I + SegX + 2);
        }
    }

    PMC->CreateMeshSection(0, Verts, Tris, Normals, UVs, Colors, Tangents, false);
    if (M_VertexColor) {
        UMaterialInstanceDynamic* MID = UMaterialInstanceDynamic::Create(M_VertexColor, this);
        PMC->SetMaterial(0, MID);
    }
    PMC->SetCastShadow(true);
    RoomActors.Add(A);
    return A;
}

// ========= Textured Ceiling =========
AActor* AEmersynGameMode::SpawnTexturedCeiling(FVector Center, FVector Size, FLinearColor Color)
{
    return SpawnTexturedFloor(Center, Size, ETexturePattern::Concrete, Color, Color * 0.95f, 1.0f);
}

// ========= Textured Box =========
AActor* AEmersynGameMode::SpawnTexturedBox(FVector Loc, FVector Scale, ETexturePattern Pattern, FLinearColor Base, FLinearColor Accent)
{
    AActor* A = GetWorld()->SpawnActor<AActor>(AActor::StaticClass(), FTransform(Loc));
    if (!A) return nullptr;
    A->SetRootComponent(NewObject<USceneComponent>(A));
    A->GetRootComponent()->RegisterComponent();
    UProceduralMeshComponent* PMC = NewObject<UProceduralMeshComponent>(A);
    PMC->SetupAttachment(A->GetRootComponent());
    PMC->RegisterComponent();

    FVector HE = Scale;
    TArray<FVector> V; TArray<int32> T; TArray<FColor> C;
    TArray<FVector> N; TArray<FVector2D> UV; TArray<FProcMeshTangent> Tan;

    auto AddFace = [&](FVector P0, FVector P1, FVector P2, FVector P3, FVector Norm, FLinearColor C0, FLinearColor C1, FLinearColor C2, FLinearColor C3) {
        int32 BaseIdx = V.Num();
        V.Add(P0); V.Add(P1); V.Add(P2); V.Add(P3);
        N.Add(Norm); N.Add(Norm); N.Add(Norm); N.Add(Norm);
        UV.Add(FVector2D(0,0)); UV.Add(FVector2D(1,0)); UV.Add(FVector2D(1,1)); UV.Add(FVector2D(0,1));
        C.Add(C0.ToFColor(true)); C.Add(C1.ToFColor(true)); C.Add(C2.ToFColor(true)); C.Add(C3.ToFColor(true));
        Tan.Add(FProcMeshTangent(1,0,0)); Tan.Add(FProcMeshTangent(1,0,0)); Tan.Add(FProcMeshTangent(1,0,0)); Tan.Add(FProcMeshTangent(1,0,0));
        T.Add(BaseIdx); T.Add(BaseIdx+1); T.Add(BaseIdx+2);
        T.Add(BaseIdx); T.Add(BaseIdx+2); T.Add(BaseIdx+3);
    };

    float NV = SimpleNoise(Loc.X * 0.02f, Loc.Y * 0.02f) * 0.2f;
    FLinearColor BL = ApplyDirectionalShading(Base, FVector(0,-1,0), 0.85f);
    FLinearColor BR = ApplyDirectionalShading(FMath::Lerp(Base, Accent, 0.35f + NV), FVector(1,0,0), 0.9f);
    FLinearColor TL = ApplyDirectionalShading(FMath::Lerp(Base, Accent, 0.2f + NV), FVector(-1,0,0), 0.95f);
    FLinearColor TR = ApplyDirectionalShading(Accent, FVector(0,0,1), 1.0f);

    AddFace(FVector(-HE.X,-HE.Y,-HE.Z), FVector(HE.X,-HE.Y,-HE.Z), FVector(HE.X,-HE.Y,HE.Z), FVector(-HE.X,-HE.Y,HE.Z), FVector(0,-1,0), BL, BR, TR, TL);
    AddFace(FVector(HE.X,HE.Y,-HE.Z), FVector(-HE.X,HE.Y,-HE.Z), FVector(-HE.X,HE.Y,HE.Z), FVector(HE.X,HE.Y,HE.Z), FVector(0,1,0), BL, BR, TR, TL);
    AddFace(FVector(-HE.X,HE.Y,-HE.Z), FVector(-HE.X,-HE.Y,-HE.Z), FVector(-HE.X,-HE.Y,HE.Z), FVector(-HE.X,HE.Y,HE.Z), FVector(-1,0,0), BL, BR, TR, TL);
    AddFace(FVector(HE.X,-HE.Y,-HE.Z), FVector(HE.X,HE.Y,-HE.Z), FVector(HE.X,HE.Y,HE.Z), FVector(HE.X,-HE.Y,HE.Z), FVector(1,0,0), BL, BR, TR, TL);
    AddFace(FVector(-HE.X,-HE.Y,HE.Z), FVector(HE.X,-HE.Y,HE.Z), FVector(HE.X,HE.Y,HE.Z), FVector(-HE.X,HE.Y,HE.Z), FVector(0,0,1), TL, TR, TR, TL);
    AddFace(FVector(-HE.X,HE.Y,-HE.Z), FVector(HE.X,HE.Y,-HE.Z), FVector(HE.X,-HE.Y,-HE.Z), FVector(-HE.X,-HE.Y,-HE.Z), FVector(0,0,-1), BL, BR, BR, BL);

    PMC->CreateMeshSection(0, V, T, N, UV, C, Tan, false);
    if (M_VertexColor) {
        UMaterialInstanceDynamic* MID = UMaterialInstanceDynamic::Create(M_VertexColor, this);
        PMC->SetMaterial(0, MID);
    }
    PMC->SetCastShadow(true);
    RoomActors.Add(A);
    return A;
}

// ========= Spawn Sky =========
void AEmersynGameMode::SpawnSky()
{
    AActor* A = GetWorld()->SpawnActor<AActor>(AActor::StaticClass(), FTransform(FVector(0, 0, -500)));
    if (!A) return;
    A->SetRootComponent(NewObject<USceneComponent>(A));
    A->GetRootComponent()->RegisterComponent();
    UProceduralMeshComponent* PMC = NewObject<UProceduralMeshComponent>(A);
    PMC->SetupAttachment(A->GetRootComponent());
    PMC->RegisterComponent();

    int32 Seg = 32; float Radius = 10000.f;
    TArray<FVector> V; TArray<int32> T; TArray<FColor> C;
    TArray<FVector> N; TArray<FVector2D> UV; TArray<FProcMeshTangent> Tan;
    V.Add(FVector(0, 0, Radius)); N.Add(FVector(0, 0, -1));
    UV.Add(FVector2D(0.5f, 0.5f)); C.Add(SC::SkyTop.ToFColor(true));
    Tan.Add(FProcMeshTangent(1, 0, 0));

    for (int32 Ring = 1; Ring <= Seg / 2; Ring++) {
        float Phi = PI * Ring / (Seg / 2);
        float HeightFrac = 1.f - (float)Ring / (Seg / 2);
        FLinearColor RingColor = FMath::Lerp(SC::SkyBot, SC::SkyTop, HeightFrac);
        for (int32 S = 0; S < Seg; S++) {
            float Theta = 2.f * PI * S / Seg;
            V.Add(FVector(Radius * FMath::Sin(Phi) * FMath::Cos(Theta), Radius * FMath::Sin(Phi) * FMath::Sin(Theta), Radius * FMath::Cos(Phi)));
            N.Add(-V.Last().GetSafeNormal());
            UV.Add(FVector2D(FMath::Cos(Theta) * 0.5f + 0.5f, FMath::Sin(Theta) * 0.5f + 0.5f));
            C.Add(RingColor.ToFColor(true));
            Tan.Add(FProcMeshTangent(1, 0, 0));
        }
    }

    for (int32 S = 0; S < Seg; S++) {
        T.Add(0); T.Add(1 + (S + 1) % Seg); T.Add(1 + S);
    }
    for (int32 Ring = 0; Ring < Seg / 2 - 1; Ring++) {
        for (int32 S = 0; S < Seg; S++) {
            int32 Cur = 1 + Ring * Seg + S;
            int32 Next = 1 + Ring * Seg + (S + 1) % Seg;
            int32 CurB = 1 + (Ring + 1) * Seg + S;
            int32 NextB = 1 + (Ring + 1) * Seg + (S + 1) % Seg;
            T.Add(Cur); T.Add(Next); T.Add(CurB);
            T.Add(Next); T.Add(NextB); T.Add(CurB);
        }
    }

    PMC->CreateMeshSection(0, V, T, N, UV, C, Tan, false);
    if (M_VertexColor) {
        UMaterialInstanceDynamic* MID = UMaterialInstanceDynamic::Create(M_VertexColor, this);
        MID->TwoSided = true;
        PMC->SetMaterial(0, MID);
    }
    RoomActors.Add(A);
}

// ========= Spawn Mesh (textured version) =========
AActor* AEmersynGameMode::SpawnMesh(const float* Verts, const float* Norms, const float* UVData,
    const int32* Tris, int32 NumVerts, int32 NumTris,
    FVector Location, FRotator Rotation, FVector Scale,
    ETexturePattern Pattern, FLinearColor Base, FLinearColor Accent, float Brightness)
{
    AActor* A = GetWorld()->SpawnActor<AActor>(AActor::StaticClass(), FTransform(Rotation, Location, Scale));
    if (!A) return nullptr;
    A->SetRootComponent(NewObject<USceneComponent>(A));
    A->GetRootComponent()->RegisterComponent();
    UProceduralMeshComponent* PMC = NewObject<UProceduralMeshComponent>(A);
    PMC->SetupAttachment(A->GetRootComponent());
    PMC->RegisterComponent();

    TArray<FVector> V; TArray<int32> T; TArray<FColor> C;
    TArray<FVector> N; TArray<FVector2D> UV; TArray<FProcMeshTangent> Tan;
    V.Reserve(NumVerts); N.Reserve(NumVerts); UV.Reserve(NumVerts); C.Reserve(NumVerts);

    for (int32 I = 0; I < NumVerts; I++) {
        V.Add(FVector(Verts[I*3], Verts[I*3+1], Verts[I*3+2]));
        FVector Norm(0, 0, 1);
        if (Norms) Norm = FVector(Norms[I*3], Norms[I*3+1], Norms[I*3+2]);
        N.Add(Norm);
        FVector2D UVCoord(0, 0);
        if (UVData) UVCoord = FVector2D(UVData[I*2], UVData[I*2+1]);
        UV.Add(UVCoord);
        Tan.Add(FProcMeshTangent(1, 0, 0));

        float NV = SimpleNoise(Verts[I*3] * 0.08f, Verts[I*3+1] * 0.08f);
        float HeightFrac = FMath::Clamp((Verts[I*3+2] + 50.f) / 100.f, 0.f, 1.f);
        FLinearColor BaseVC = FMath::Lerp(Base, Accent, NV * 0.6f + HeightFrac * 0.25f);
        BaseVC *= Brightness;
        float AOVal = FMath::Clamp(0.7f + HeightFrac * 0.3f, 0.5f, 1.0f);
        FLinearColor VC = ApplyDirectionalShading(BaseVC, Norm, AOVal);
        VC.A = 1.f;
        C.Add(VC.ToFColor(true));
    }

    for (int32 I = 0; I < NumTris * 3; I++) {
        T.Add(FMath::Clamp(Tris[I], 0, NumVerts - 1));
    }

    PMC->CreateMeshSection(0, V, T, N, UV, C, Tan, false);
    if (M_VertexColor) {
        UMaterialInstanceDynamic* MID = UMaterialInstanceDynamic::Create(M_VertexColor, this);
        PMC->SetMaterial(0, MID);
    }
    PMC->SetCastShadow(true);
    RoomActors.Add(A);
    return A;
}

// ========= Spawn Mesh VC (fallback) =========
AActor* AEmersynGameMode::SpawnMeshVC(const float* Verts, const float* Norms, const float* UVData,
    const int32* Tris, int32 NumVerts, int32 NumTris,
    FVector Location, FRotator Rotation, FVector Scale,
    const FLinearColor& Tint, float Brightness)
{
    return SpawnMesh(Verts, Norms, UVData, Tris, NumVerts, NumTris, Location, Rotation, Scale, ETexturePattern::Fabric, Tint, Tint * 0.8f, Brightness);
}

// ========= Spawn Character Mesh =========
AActor* AEmersynGameMode::SpawnCharacterMesh(const FString& Name, FVector Location, FRotator Rotation,
    float InScale, const FLinearColor& SkinTint, const FLinearColor& OutfitTint)
{
    const float* V = nullptr; const float* N = nullptr; const float* UV = nullptr;
    const int32* T = nullptr; int32 NV = 0, NT = 0;
    if (Name == TEXT("Emersyn")) { V = MeshData_EMERSYN::Vertices; N = MeshData_EMERSYN::Normals; UV = MeshData_EMERSYN::UVs; T = MeshData_EMERSYN::Triangles; NV = MeshData_EMERSYN::NumVertices; NT = MeshData_EMERSYN::NumTriangles; }
    if (Name == TEXT("Ava")) { V = MeshData_AVA::Vertices; N = MeshData_AVA::Normals; UV = MeshData_AVA::UVs; T = MeshData_AVA::Triangles; NV = MeshData_AVA::NumVertices; NT = MeshData_AVA::NumTriangles; }
    if (Name == TEXT("Leo")) { V = MeshData_LEO::Vertices; N = MeshData_LEO::Normals; UV = MeshData_LEO::UVs; T = MeshData_LEO::Triangles; NV = MeshData_LEO::NumVertices; NT = MeshData_LEO::NumTriangles; }
    if (Name == TEXT("Mia")) { V = MeshData_MIA::Vertices; N = MeshData_MIA::Normals; UV = MeshData_MIA::UVs; T = MeshData_MIA::Triangles; NV = MeshData_MIA::NumVertices; NT = MeshData_MIA::NumTriangles; }
    if (Name == TEXT("Cat")) { V = MeshData_CAT::Vertices; N = MeshData_CAT::Normals; UV = MeshData_CAT::UVs; T = MeshData_CAT::Triangles; NV = MeshData_CAT::NumVertices; NT = MeshData_CAT::NumTriangles; }
    if (Name == TEXT("Dog")) { V = MeshData_DOG::Vertices; N = MeshData_DOG::Normals; UV = MeshData_DOG::UVs; T = MeshData_DOG::Triangles; NV = MeshData_DOG::NumVertices; NT = MeshData_DOG::NumTriangles; }
    if (!V) return nullptr;
    FLinearColor SkinAccent = SkinTint * 0.85f + OutfitTint * 0.15f;
    return SpawnMesh(V, N, UV, T, NV, NT, Location, Rotation, FVector(InScale), ETexturePattern::Fabric, SkinTint, SkinAccent, 1.2f);
}

// ========= Lighting =========
void AEmersynGameMode::SpawnLight(FVector Loc, float Intensity, FLinearColor Color, float Radius)
{
    AActor* A = GetWorld()->SpawnActor<AActor>(AActor::StaticClass(), FTransform(Loc));
    if (!A) return;
    A->SetRootComponent(NewObject<USceneComponent>(A));
    A->GetRootComponent()->RegisterComponent();
    UPointLightComponent* PLC = NewObject<UPointLightComponent>(A);
    PLC->SetupAttachment(A->GetRootComponent());
    PLC->RegisterComponent();
    PLC->SetIntensity(Intensity);
    PLC->SetLightColor(Color);
    PLC->SetAttenuationRadius(Radius);
    PLC->SetCastShadows(true);
    RoomActors.Add(A);
}

void AEmersynGameMode::SpawnDirectionalLight(FRotator Rot, float Intensity, FLinearColor Color)
{
    AActor* A = GetWorld()->SpawnActor<AActor>(AActor::StaticClass(), FTransform(Rot, FVector(0, 0, 2000)));
    if (!A) return;
    A->SetRootComponent(NewObject<USceneComponent>(A));
    A->GetRootComponent()->RegisterComponent();
    UDirectionalLightComponent* DLC = NewObject<UDirectionalLightComponent>(A);
    DLC->SetupAttachment(A->GetRootComponent());
    DLC->RegisterComponent();
    DLC->SetIntensity(Intensity);
    DLC->SetLightColor(Color);
    DLC->SetCastShadows(true);
    RoomActors.Add(A);
}

void AEmersynGameMode::SpawnSkyLight(float Intensity)
{
    FActorSpawnParameters Params;
    ASkyLight* SL = GetWorld()->SpawnActor<ASkyLight>(ASkyLight::StaticClass(), FTransform(FVector(0, 0, 1000)), Params);
    if (!SL) return;
    SL->GetLightComponent()->SetIntensity(Intensity);
    SL->GetLightComponent()->SetLightColor(FLinearColor(0.75f, 0.85f, 1.0f));
    SL->GetLightComponent()->bLowerHemisphereIsBlack = false;
    RoomActors.Add(SL);
}

void AEmersynGameMode::SpawnRoomLighting(FVector RoomCenter, FVector RoomSize)
{
    SpawnLight(RoomCenter + FVector(0, 0, RoomSize.Z * 0.95f), 20.f, FLinearColor(1.0f, 0.95f, 0.85f), RoomSize.X * 2.0f);
    SpawnLight(RoomCenter + FVector(RoomSize.X * 0.4f, -RoomSize.Y * 0.4f, RoomSize.Z * 0.7f), 10.f, FLinearColor(1.0f, 0.88f, 0.68f), RoomSize.X * 1.2f);
    SpawnLight(RoomCenter + FVector(-RoomSize.X * 0.4f, RoomSize.Y * 0.4f, RoomSize.Z * 0.6f), 8.f, FLinearColor(0.65f, 0.78f, 1.0f), RoomSize.X * 1.2f);
    SpawnLight(RoomCenter + FVector(0, 0, 20.f), 5.f, FLinearColor(0.92f, 0.90f, 0.85f), RoomSize.X * 1.5f);
    SpawnLight(RoomCenter + FVector(0, -RoomSize.Y * 0.5f, RoomSize.Z * 0.3f), 6.f, FLinearColor(0.95f, 0.92f, 0.88f), RoomSize.X);
}

// ========= Post-Processing =========
void AEmersynGameMode::SetupPostProcessing()
{
    FActorSpawnParameters Params;
    APostProcessVolume* PPV = GetWorld()->SpawnActor<APostProcessVolume>(APostProcessVolume::StaticClass(), FTransform(FVector::ZeroVector), Params);
    if (!PPV) return;
    PPV->bUnbound = true;
    PPV->Settings.bOverride_BloomIntensity = true; PPV->Settings.BloomIntensity = 0.65f;
    PPV->Settings.bOverride_BloomThreshold = true; PPV->Settings.BloomThreshold = 0.8f;
    PPV->Settings.bOverride_AmbientOcclusionIntensity = true; PPV->Settings.AmbientOcclusionIntensity = 1.2f;
    PPV->Settings.bOverride_AmbientOcclusionRadius = true; PPV->Settings.AmbientOcclusionRadius = 200.f;
    PPV->Settings.bOverride_AmbientOcclusionQuality = true; PPV->Settings.AmbientOcclusionQuality = 100.f;
    PPV->Settings.bOverride_ColorSaturation = true; PPV->Settings.ColorSaturation = FVector4(1.55f, 1.55f, 1.55f, 1.0f);
    PPV->Settings.bOverride_ColorContrast = true; PPV->Settings.ColorContrast = FVector4(1.25f, 1.25f, 1.25f, 1.0f);
    PPV->Settings.bOverride_ColorGamma = true; PPV->Settings.ColorGamma = FVector4(0.88f, 0.88f, 0.88f, 1.0f);
    PPV->Settings.bOverride_VignetteIntensity = true; PPV->Settings.VignetteIntensity = 0.08f;
    PPV->Settings.bOverride_AutoExposureBias = true; PPV->Settings.AutoExposureBias = 2.0f;
    PPV->Settings.bOverride_AutoExposureMinBrightness = true; PPV->Settings.AutoExposureMinBrightness = 0.5f;
    PPV->Settings.bOverride_AutoExposureMaxBrightness = true; PPV->Settings.AutoExposureMaxBrightness = 2.0f;
    RoomActors.Add(PPV);
}

// ========= World Text =========
void AEmersynGameMode::SpawnWorldText(const FString& Text, FVector Location, float Size, FLinearColor Color)
{
    AActor* A = GetWorld()->SpawnActor<AActor>(AActor::StaticClass(), FTransform(Location));
    if (!A) return;
    A->SetRootComponent(NewObject<USceneComponent>(A));
    A->GetRootComponent()->RegisterComponent();
    UTextRenderComponent* TRC = NewObject<UTextRenderComponent>(A);
    TRC->SetupAttachment(A->GetRootComponent());
    TRC->RegisterComponent();
    TRC->SetText(FText::FromString(Text));
    TRC->SetWorldSize(Size);
    TRC->SetTextRenderColor(Color.ToFColor(true));
    TRC->SetHorizontalAlignment(EHTA_Center);
    TRC->SetVerticalAlignment(EVRTA_TextCenter);
    RoomActors.Add(A);
}

void AEmersynGameMode::SpawnRoomLabel(const FString& Label)
{
    SpawnWorldText(Label, FVector(0, 0, 500), 55.f, FLinearColor(1.f, 1.f, 1.f));
}

// ========= Camera =========
void AEmersynGameMode::SetupIsometricCamera(FVector RoomCenter, float Distance)
{
    FRotator CamRot(-40.f, 30.f, 0.f);
    FVector CamOffset = CamRot.Vector() * -Distance;
    FVector CamPos = RoomCenter + CamOffset;

    if (!IsoCam) {
        IsoCam = GetWorld()->SpawnActor<ACameraActor>(ACameraActor::StaticClass(), FTransform(CamRot, CamPos));
        if (IsoCam) {
            IsoCam->GetCameraComponent()->FieldOfView = 45.f;
            APlayerController* PC = GetWorld()->GetFirstPlayerController();
            if (PC) PC->SetViewTarget(IsoCam);
        }
    } else {
        CamStartPos = IsoCam->GetActorLocation();
        CamStartRot = IsoCam->GetActorRotation();
        CamTargetPos = CamPos;
        CamTargetRot = CamRot;
        CamMoveAlpha = 0.f;
        bCameraMoving = true;
    }
}

// ========= Clear Room =========
void AEmersynGameMode::ClearRoom()
{
    for (AActor* A : RoomActors) { if (A && A != IsoCam) A->Destroy(); }
    RoomActors.Empty();
}

// ========= Room Builders =========

void AEmersynGameMode::BuildSplashScreen()
{
    SpawnSky();
    SetupPostProcessing();
    SpawnSkyLight(9.f);
    SpawnDirectionalLight(FRotator(-40.f, 100.f, 0.f), 20.f, FLinearColor(1.f, 0.96f, 0.88f));
    SpawnTexturedFloor(FVector::ZeroVector, FVector(600, 400, 0), ETexturePattern::Grass, SC::FloorGrass, FLinearColor(0.35f, 0.85f, 0.35f), 3.f);
    SpawnWorldText(TEXT("EMERSYN'S BIG DAY"), FVector(0, 0, 300), 80.f, FLinearColor(1.f, 0.85f, 0.3f));
    SpawnWorldText(TEXT("Loading..."), FVector(0, 0, 150), 25.f, FLinearColor(1.f, 1.f, 1.f));
    SpawnCharacterMesh(TEXT("Emersyn"), FVector(0, 0, 0), FRotator(0, 0, 0), 3.f, FLinearColor(0.92f, 0.75f, 0.60f), SC::FabricPink);
    SetupIsometricCamera(FVector(0, 0, 150), 500.f);
}

void AEmersynGameMode::BuildMainMenu()
{
    BuildSplashScreen();
}

void AEmersynGameMode::BuildBedroom()
{
    FVector RS(520.f, 455.f, 350.f);
    SpawnSky();
    SetupPostProcessing();
    SpawnSkyLight(9.f);
    SpawnDirectionalLight(FRotator(-40.f, 135.f, 0.f), 28.f, FLinearColor(1.f, 0.97f, 0.91f));

    SpawnTexturedFloor(FVector::ZeroVector, FVector(RS.X, RS.Y, 0), ETexturePattern::WoodGrain, SC::FloorWood, SC::WoodDark, 2.f);

    SpawnTexturedWall(FVector(-RS.X, -RS.Y, 0), FVector(RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallPink, SC::FabricPink);
    SpawnTexturedWall(FVector(RS.X, -RS.Y, 0), FVector(RS.X, RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallPink, SC::FabricPink);
    SpawnTexturedWall(FVector(RS.X, RS.Y, 0), FVector(-RS.X, RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallPink, SC::FabricPink);
    SpawnTexturedWall(FVector(-RS.X, RS.Y, 0), FVector(-RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallPink, SC::FabricPink);

    SpawnTexturedCeiling(FVector(0, 0, RS.Z), FVector(RS.X, RS.Y, 0), SC::CeilingWhite);
    SpawnRoomLighting(FVector(0, 0, RS.Z * 0.5f), RS);
    SpawnRoomLabel(TEXT("Emersyn's Bedroom"));

    SpawnMesh(MeshData_BEDROOM_BED::Vertices, MeshData_BEDROOM_BED::Normals, MeshData_BEDROOM_BED::UVs, MeshData_BEDROOM_BED::Triangles, MeshData_BEDROOM_BED::NumVertices, MeshData_BEDROOM_BED::NumTriangles, FVector(-150, -100, 0), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::Fabric, SC::FabricPink, SC::FabricCream);
    SpawnMesh(MeshData_BEDROOM_DRESSER::Vertices, MeshData_BEDROOM_DRESSER::Normals, MeshData_BEDROOM_DRESSER::UVs, MeshData_BEDROOM_DRESSER::Triangles, MeshData_BEDROOM_DRESSER::NumVertices, MeshData_BEDROOM_DRESSER::NumTriangles, FVector(200, -150, 0), FRotator::ZeroRotator, FVector(3.5f), ETexturePattern::WoodGrain, SC::WoodMedium, SC::WoodLight);
    SpawnMesh(MeshData_BEDROOM_BOOKSHELF::Vertices, MeshData_BEDROOM_BOOKSHELF::Normals, MeshData_BEDROOM_BOOKSHELF::UVs, MeshData_BEDROOM_BOOKSHELF::Triangles, MeshData_BEDROOM_BOOKSHELF::NumVertices, MeshData_BEDROOM_BOOKSHELF::NumTriangles, FVector(200, 150, 0), FRotator::ZeroRotator, FVector(3.5f), ETexturePattern::WoodGrain, SC::WoodDark, SC::WoodCherry);
    SpawnMesh(MeshData_BEDROOM_LAMP::Vertices, MeshData_BEDROOM_LAMP::Normals, MeshData_BEDROOM_LAMP::UVs, MeshData_BEDROOM_LAMP::Triangles, MeshData_BEDROOM_LAMP::NumVertices, MeshData_BEDROOM_LAMP::NumTriangles, FVector(150, -200, 80), FRotator::ZeroRotator, FVector(2.f), ETexturePattern::Metal, SC::MetalGold, SC::FabricCream);
    SpawnMesh(MeshData_BEDROOM_RUG::Vertices, MeshData_BEDROOM_RUG::Normals, MeshData_BEDROOM_RUG::UVs, MeshData_BEDROOM_RUG::Triangles, MeshData_BEDROOM_RUG::NumVertices, MeshData_BEDROOM_RUG::NumTriangles, FVector(0, 50, 1), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::Carpet, SC::CarpetPurple, SC::FabricPurple);

    SpawnCharacterMesh(TEXT("Emersyn"), FVector(-50, 80, 0), FRotator(0, 45, 0), 3.0f, FLinearColor(0.92f, 0.75f, 0.60f), SC::FabricPink);
    SetupIsometricCamera(FVector(0, 0, RS.Z * 0.3f), RS.X * 1.35f);
}

void AEmersynGameMode::BuildKitchen()
{
    FVector RS(520.f, 455.f, 350.f);
    SpawnSky();
    SetupPostProcessing();
    SpawnSkyLight(9.f);
    SpawnDirectionalLight(FRotator(-40.f, 135.f, 0.f), 28.f, FLinearColor(1.f, 0.97f, 0.91f));

    SpawnTexturedFloor(FVector::ZeroVector, FVector(RS.X, RS.Y, 0), ETexturePattern::TileGrid, SC::TileWhite, SC::FloorConcrete, 2.f);

    SpawnTexturedWall(FVector(-RS.X, -RS.Y, 0), FVector(RS.X, -RS.Y, 0), RS.Z, ETexturePattern::TileGrid, SC::TileBlue, SC::WallWhite);
    SpawnTexturedWall(FVector(RS.X, -RS.Y, 0), FVector(RS.X, RS.Y, 0), RS.Z, ETexturePattern::TileGrid, SC::TileBlue, SC::WallWhite);
    SpawnTexturedWall(FVector(RS.X, RS.Y, 0), FVector(-RS.X, RS.Y, 0), RS.Z, ETexturePattern::TileGrid, SC::TileBlue, SC::WallWhite);
    SpawnTexturedWall(FVector(-RS.X, RS.Y, 0), FVector(-RS.X, -RS.Y, 0), RS.Z, ETexturePattern::TileGrid, SC::TileBlue, SC::WallWhite);

    SpawnTexturedCeiling(FVector(0, 0, RS.Z), FVector(RS.X, RS.Y, 0), SC::CeilingWhite);
    SpawnRoomLighting(FVector(0, 0, RS.Z * 0.5f), RS);
    SpawnRoomLabel(TEXT("Kitchen"));

    SpawnMesh(MeshData_KITCHEN_TABLE::Vertices, MeshData_KITCHEN_TABLE::Normals, MeshData_KITCHEN_TABLE::UVs, MeshData_KITCHEN_TABLE::Triangles, MeshData_KITCHEN_TABLE::NumVertices, MeshData_KITCHEN_TABLE::NumTriangles, FVector(0, 0, 0), FRotator::ZeroRotator, FVector(3.5f), ETexturePattern::WoodGrain, SC::WoodLight, SC::WoodMedium);
    SpawnMesh(MeshData_KITCHEN_CHAIR::Vertices, MeshData_KITCHEN_CHAIR::Normals, MeshData_KITCHEN_CHAIR::UVs, MeshData_KITCHEN_CHAIR::Triangles, MeshData_KITCHEN_CHAIR::NumVertices, MeshData_KITCHEN_CHAIR::NumTriangles, FVector(80, 0, 0), FRotator(0, -90, 0), FVector(3.f), ETexturePattern::WoodGrain, SC::WoodMedium, SC::WoodDark);
    SpawnMesh(MeshData_KITCHEN_FRIDGE::Vertices, MeshData_KITCHEN_FRIDGE::Normals, MeshData_KITCHEN_FRIDGE::UVs, MeshData_KITCHEN_FRIDGE::Triangles, MeshData_KITCHEN_FRIDGE::NumVertices, MeshData_KITCHEN_FRIDGE::NumTriangles, FVector(-250, -200, 0), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::Metal, SC::MetalSilver, FLinearColor(0.85f, 0.85f, 0.88f));
    SpawnMesh(MeshData_KITCHEN_STOVE::Vertices, MeshData_KITCHEN_STOVE::Normals, MeshData_KITCHEN_STOVE::UVs, MeshData_KITCHEN_STOVE::Triangles, MeshData_KITCHEN_STOVE::NumVertices, MeshData_KITCHEN_STOVE::NumTriangles, FVector(-250, 0, 0), FRotator::ZeroRotator, FVector(3.5f), ETexturePattern::Metal, SC::MetalBlack, SC::MetalSilver);
    SpawnMesh(MeshData_KITCHEN_COUNTER::Vertices, MeshData_KITCHEN_COUNTER::Normals, MeshData_KITCHEN_COUNTER::UVs, MeshData_KITCHEN_COUNTER::Triangles, MeshData_KITCHEN_COUNTER::NumVertices, MeshData_KITCHEN_COUNTER::NumTriangles, FVector(-250, 200, 0), FRotator::ZeroRotator, FVector(3.5f), ETexturePattern::Marble, SC::MarbleWhite, SC::MarbleVein);

    SpawnCharacterMesh(TEXT("Mia"), FVector(80, 80, 0), FRotator(0, -45, 0), 3.0f, FLinearColor(0.88f, 0.70f, 0.55f), SC::FabricYellow);
    SetupIsometricCamera(FVector(0, 0, RS.Z * 0.3f), RS.X * 1.35f);
}

void AEmersynGameMode::BuildBathroom()
{
    FVector RS(300.f, 280.f, 280.f);
    SpawnSky();
    SetupPostProcessing();
    SpawnSkyLight(9.f);
    SpawnDirectionalLight(FRotator(-40.f, 135.f, 0.f), 28.f, FLinearColor(1.f, 0.97f, 0.91f));

    SpawnTexturedFloor(FVector::ZeroVector, FVector(RS.X, RS.Y, 0), ETexturePattern::TileGrid, SC::TileWhite, SC::TileBlue, 2.f);

    SpawnTexturedWall(FVector(-RS.X, -RS.Y, 0), FVector(RS.X, -RS.Y, 0), RS.Z, ETexturePattern::TileGrid, SC::WallWhite, SC::TileBlue);
    SpawnTexturedWall(FVector(RS.X, -RS.Y, 0), FVector(RS.X, RS.Y, 0), RS.Z, ETexturePattern::TileGrid, SC::WallWhite, SC::TileBlue);
    SpawnTexturedWall(FVector(RS.X, RS.Y, 0), FVector(-RS.X, RS.Y, 0), RS.Z, ETexturePattern::TileGrid, SC::WallWhite, SC::TileBlue);
    SpawnTexturedWall(FVector(-RS.X, RS.Y, 0), FVector(-RS.X, -RS.Y, 0), RS.Z, ETexturePattern::TileGrid, SC::WallWhite, SC::TileBlue);

    SpawnTexturedCeiling(FVector(0, 0, RS.Z), FVector(RS.X, RS.Y, 0), SC::CeilingWhite);
    SpawnRoomLighting(FVector(0, 0, RS.Z * 0.5f), RS);
    SpawnRoomLabel(TEXT("Bathroom"));

    SpawnMesh(MeshData_BATHROOM_TUB::Vertices, MeshData_BATHROOM_TUB::Normals, MeshData_BATHROOM_TUB::UVs, MeshData_BATHROOM_TUB::Triangles, MeshData_BATHROOM_TUB::NumVertices, MeshData_BATHROOM_TUB::NumTriangles, FVector(-100, -100, 0), FRotator::ZeroRotator, FVector(3.5f), ETexturePattern::Marble, SC::MarbleWhite, SC::TileBlue);
    SpawnMesh(MeshData_BATHROOM_SINK::Vertices, MeshData_BATHROOM_SINK::Normals, MeshData_BATHROOM_SINK::UVs, MeshData_BATHROOM_SINK::Triangles, MeshData_BATHROOM_SINK::NumVertices, MeshData_BATHROOM_SINK::NumTriangles, FVector(100, -150, 60), FRotator::ZeroRotator, FVector(3.f), ETexturePattern::Marble, SC::MarbleWhite, SC::MetalSilver);
    SpawnMesh(MeshData_BATHROOM_MIRROR::Vertices, MeshData_BATHROOM_MIRROR::Normals, MeshData_BATHROOM_MIRROR::UVs, MeshData_BATHROOM_MIRROR::Triangles, MeshData_BATHROOM_MIRROR::NumVertices, MeshData_BATHROOM_MIRROR::NumTriangles, FVector(100, -180, 140), FRotator::ZeroRotator, FVector(2.5f), ETexturePattern::Metal, SC::MetalSilver, FLinearColor(0.9f, 0.92f, 0.95f));
    SpawnMesh(MeshData_BATHROOM_TOWELRACK::Vertices, MeshData_BATHROOM_TOWELRACK::Normals, MeshData_BATHROOM_TOWELRACK::UVs, MeshData_BATHROOM_TOWELRACK::Triangles, MeshData_BATHROOM_TOWELRACK::NumVertices, MeshData_BATHROOM_TOWELRACK::NumTriangles, FVector(-150, 100, 100), FRotator::ZeroRotator, FVector(2.5f), ETexturePattern::Metal, SC::MetalSilver, SC::FabricBlue);

    SpawnCharacterMesh(TEXT("Emersyn"), FVector(0, 50, 0), FRotator(0, 180, 0), 3.0f, FLinearColor(0.92f, 0.75f, 0.60f), SC::FabricBlue);
    SetupIsometricCamera(FVector(0, 0, RS.Z * 0.3f), RS.X * 1.35f);
}

void AEmersynGameMode::BuildLivingRoom()
{
    FVector RS(450.f, 400.f, 300.f);
    SpawnSky();
    SetupPostProcessing();
    SpawnSkyLight(9.f);
    SpawnDirectionalLight(FRotator(-40.f, 135.f, 0.f), 28.f, FLinearColor(1.f, 0.97f, 0.91f));

    SpawnTexturedFloor(FVector::ZeroVector, FVector(RS.X, RS.Y, 0), ETexturePattern::WoodGrain, SC::FloorWood, SC::WoodLight, 2.f);

    SpawnTexturedWall(FVector(-RS.X, -RS.Y, 0), FVector(RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallYellow, SC::FabricOrange);
    SpawnTexturedWall(FVector(RS.X, -RS.Y, 0), FVector(RS.X, RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallYellow, SC::FabricOrange);
    SpawnTexturedWall(FVector(RS.X, RS.Y, 0), FVector(-RS.X, RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallYellow, SC::FabricOrange);
    SpawnTexturedWall(FVector(-RS.X, RS.Y, 0), FVector(-RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallYellow, SC::FabricOrange);

    SpawnTexturedCeiling(FVector(0, 0, RS.Z), FVector(RS.X, RS.Y, 0), SC::CeilingWhite);
    SpawnRoomLighting(FVector(0, 0, RS.Z * 0.5f), RS);
    SpawnRoomLabel(TEXT("Living Room"));

    SpawnMesh(MeshData_LIVINGROOM_SOFA::Vertices, MeshData_LIVINGROOM_SOFA::Normals, MeshData_LIVINGROOM_SOFA::UVs, MeshData_LIVINGROOM_SOFA::Triangles, MeshData_LIVINGROOM_SOFA::NumVertices, MeshData_LIVINGROOM_SOFA::NumTriangles, FVector(0, -150, 0), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::Fabric, SC::FabricBlue, SC::FabricCream);
    SpawnMesh(MeshData_LIVINGROOM_COFFEETABLE::Vertices, MeshData_LIVINGROOM_COFFEETABLE::Normals, MeshData_LIVINGROOM_COFFEETABLE::UVs, MeshData_LIVINGROOM_COFFEETABLE::Triangles, MeshData_LIVINGROOM_COFFEETABLE::NumVertices, MeshData_LIVINGROOM_COFFEETABLE::NumTriangles, FVector(0, 0, 0), FRotator::ZeroRotator, FVector(3.f), ETexturePattern::WoodGrain, SC::WoodCherry, SC::WoodDark);
    SpawnMesh(MeshData_LIVINGROOM_TV::Vertices, MeshData_LIVINGROOM_TV::Normals, MeshData_LIVINGROOM_TV::UVs, MeshData_LIVINGROOM_TV::Triangles, MeshData_LIVINGROOM_TV::NumVertices, MeshData_LIVINGROOM_TV::NumTriangles, FVector(0, 200, 60), FRotator::ZeroRotator, FVector(3.5f), ETexturePattern::Metal, SC::MetalBlack, SC::MetalSilver);
    SpawnMesh(MeshData_LIVINGROOM_PLANT::Vertices, MeshData_LIVINGROOM_PLANT::Normals, MeshData_LIVINGROOM_PLANT::UVs, MeshData_LIVINGROOM_PLANT::Triangles, MeshData_LIVINGROOM_PLANT::NumVertices, MeshData_LIVINGROOM_PLANT::NumTriangles, FVector(250, -200, 0), FRotator::ZeroRotator, FVector(3.f), ETexturePattern::Grass, SC::FloorGrass, FLinearColor(0.15f, 0.55f, 0.15f));

    SpawnCharacterMesh(TEXT("Leo"), FVector(-100, -50, 0), FRotator(0, 30, 0), 3.0f, FLinearColor(0.85f, 0.65f, 0.45f), SC::FabricGreen);
    SetupIsometricCamera(FVector(0, 0, RS.Z * 0.3f), RS.X * 1.35f);
}

void AEmersynGameMode::BuildGarden()
{
    FVector RS(600.f, 500.f, 100.f);
    SpawnSky();
    SetupPostProcessing();
    SpawnSkyLight(9.f);
    SpawnDirectionalLight(FRotator(-40.f, 135.f, 0.f), 28.f, FLinearColor(1.f, 0.97f, 0.91f));

    SpawnTexturedFloor(FVector::ZeroVector, FVector(RS.X, RS.Y, 0), ETexturePattern::Grass, SC::FloorGrass, SC::WallGreen, 2.f);

    SpawnTexturedWall(FVector(-RS.X, -RS.Y, 0), FVector(RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);
    SpawnTexturedWall(FVector(RS.X, -RS.Y, 0), FVector(RS.X, RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);
    SpawnTexturedWall(FVector(RS.X, RS.Y, 0), FVector(-RS.X, RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);
    SpawnTexturedWall(FVector(-RS.X, RS.Y, 0), FVector(-RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);

    SpawnTexturedCeiling(FVector(0, 0, RS.Z), FVector(RS.X, RS.Y, 0), SC::SkyTop);
    SpawnRoomLighting(FVector(0, 0, RS.Z * 0.5f), RS);
    SpawnRoomLabel(TEXT("Garden"));

    SpawnMesh(MeshData_GARDEN_TREE::Vertices, MeshData_GARDEN_TREE::Normals, MeshData_GARDEN_TREE::UVs, MeshData_GARDEN_TREE::Triangles, MeshData_GARDEN_TREE::NumVertices, MeshData_GARDEN_TREE::NumTriangles, FVector(-200, -150, 0), FRotator::ZeroRotator, FVector(5.f), ETexturePattern::WoodGrain, SC::WoodDark, SC::FloorGrass);
    SpawnMesh(MeshData_GARDEN_FENCE::Vertices, MeshData_GARDEN_FENCE::Normals, MeshData_GARDEN_FENCE::UVs, MeshData_GARDEN_FENCE::Triangles, MeshData_GARDEN_FENCE::NumVertices, MeshData_GARDEN_FENCE::NumTriangles, FVector(0, -300, 0), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::WoodGrain, SC::WoodLight, SC::WoodMedium);
    SpawnMesh(MeshData_GARDEN_FLOWERBED::Vertices, MeshData_GARDEN_FLOWERBED::Normals, MeshData_GARDEN_FLOWERBED::UVs, MeshData_GARDEN_FLOWERBED::Triangles, MeshData_GARDEN_FLOWERBED::NumVertices, MeshData_GARDEN_FLOWERBED::NumTriangles, FVector(150, 100, 0), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::Grass, SC::FloorGrass, SC::FabricPink);

    SpawnCharacterMesh(TEXT("Emersyn"), FVector(0, 0, 0), FRotator(0, 0, 0), 3.0f, FLinearColor(0.92f, 0.75f, 0.60f), SC::FabricGreen);
    SpawnCharacterMesh(TEXT("Dog"), FVector(100, 50, 0), FRotator(0, -60, 0), 2.f, FLinearColor(0.75f, 0.55f, 0.35f), FLinearColor(0.75f, 0.55f, 0.35f));
    SetupIsometricCamera(FVector(0, 0, RS.Z * 0.3f), RS.X * 1.35f);
}

void AEmersynGameMode::BuildSchool()
{
    FVector RS(450.f, 400.f, 320.f);
    SpawnSky();
    SetupPostProcessing();
    SpawnSkyLight(9.f);
    SpawnDirectionalLight(FRotator(-40.f, 135.f, 0.f), 28.f, FLinearColor(1.f, 0.97f, 0.91f));

    SpawnTexturedFloor(FVector::ZeroVector, FVector(RS.X, RS.Y, 0), ETexturePattern::WoodGrain, SC::FloorWood, SC::WoodMedium, 2.f);

    SpawnTexturedWall(FVector(-RS.X, -RS.Y, 0), FVector(RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallGreen, SC::WallWhite);
    SpawnTexturedWall(FVector(RS.X, -RS.Y, 0), FVector(RS.X, RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallGreen, SC::WallWhite);
    SpawnTexturedWall(FVector(RS.X, RS.Y, 0), FVector(-RS.X, RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallGreen, SC::WallWhite);
    SpawnTexturedWall(FVector(-RS.X, RS.Y, 0), FVector(-RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallGreen, SC::WallWhite);

    SpawnTexturedCeiling(FVector(0, 0, RS.Z), FVector(RS.X, RS.Y, 0), SC::CeilingWhite);
    SpawnRoomLighting(FVector(0, 0, RS.Z * 0.5f), RS);
    SpawnRoomLabel(TEXT("School"));

    SpawnMesh(MeshData_SCHOOL_DESK::Vertices, MeshData_SCHOOL_DESK::Normals, MeshData_SCHOOL_DESK::UVs, MeshData_SCHOOL_DESK::Triangles, MeshData_SCHOOL_DESK::NumVertices, MeshData_SCHOOL_DESK::NumTriangles, FVector(-80, -50, 0), FRotator::ZeroRotator, FVector(3.f), ETexturePattern::WoodGrain, SC::WoodLight, SC::WoodMedium);
    SpawnMesh(MeshData_SCHOOL_DESK::Vertices, MeshData_SCHOOL_DESK::Normals, MeshData_SCHOOL_DESK::UVs, MeshData_SCHOOL_DESK::Triangles, MeshData_SCHOOL_DESK::NumVertices, MeshData_SCHOOL_DESK::NumTriangles, FVector(80, -50, 0), FRotator::ZeroRotator, FVector(3.f), ETexturePattern::WoodGrain, SC::WoodLight, SC::WoodMedium);
    SpawnMesh(MeshData_SCHOOL_CHALKBOARD::Vertices, MeshData_SCHOOL_CHALKBOARD::Normals, MeshData_SCHOOL_CHALKBOARD::UVs, MeshData_SCHOOL_CHALKBOARD::Triangles, MeshData_SCHOOL_CHALKBOARD::NumVertices, MeshData_SCHOOL_CHALKBOARD::NumTriangles, FVector(0, 250, 120), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::Concrete, FLinearColor(0.15f, 0.30f, 0.15f), FLinearColor(0.10f, 0.20f, 0.10f));
    SpawnMesh(MeshData_SCHOOL_BACKPACK::Vertices, MeshData_SCHOOL_BACKPACK::Normals, MeshData_SCHOOL_BACKPACK::UVs, MeshData_SCHOOL_BACKPACK::Triangles, MeshData_SCHOOL_BACKPACK::NumVertices, MeshData_SCHOOL_BACKPACK::NumTriangles, FVector(-120, -80, 0), FRotator::ZeroRotator, FVector(2.f), ETexturePattern::Fabric, SC::FabricRed, SC::FabricOrange);

    SpawnCharacterMesh(TEXT("Ava"), FVector(0, 0, 0), FRotator(0, 0, 0), 2.5f, FLinearColor(0.90f, 0.72f, 0.55f), SC::FabricBlue);
    SetupIsometricCamera(FVector(0, 0, RS.Z * 0.3f), RS.X * 1.35f);
}

void AEmersynGameMode::BuildShop()
{
    FVector RS(520.f, 455.f, 350.f);
    SpawnSky();
    SetupPostProcessing();
    SpawnSkyLight(9.f);
    SpawnDirectionalLight(FRotator(-40.f, 135.f, 0.f), 28.f, FLinearColor(1.f, 0.97f, 0.91f));

    SpawnTexturedFloor(FVector::ZeroVector, FVector(RS.X, RS.Y, 0), ETexturePattern::TileGrid, SC::FloorTile, SC::FloorConcrete, 2.f);

    SpawnTexturedWall(FVector(-RS.X, -RS.Y, 0), FVector(RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallWhite, SC::FabricCream);
    SpawnTexturedWall(FVector(RS.X, -RS.Y, 0), FVector(RS.X, RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallWhite, SC::FabricCream);
    SpawnTexturedWall(FVector(RS.X, RS.Y, 0), FVector(-RS.X, RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallWhite, SC::FabricCream);
    SpawnTexturedWall(FVector(-RS.X, RS.Y, 0), FVector(-RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallWhite, SC::FabricCream);

    SpawnTexturedCeiling(FVector(0, 0, RS.Z), FVector(RS.X, RS.Y, 0), SC::CeilingWhite);
    SpawnRoomLighting(FVector(0, 0, RS.Z * 0.5f), RS);
    SpawnRoomLabel(TEXT("Shop"));

    SpawnMesh(MeshData_SHOP_COUNTER::Vertices, MeshData_SHOP_COUNTER::Normals, MeshData_SHOP_COUNTER::UVs, MeshData_SHOP_COUNTER::Triangles, MeshData_SHOP_COUNTER::NumVertices, MeshData_SHOP_COUNTER::NumTriangles, FVector(0, 150, 0), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::WoodGrain, SC::WoodMedium, SC::WoodDark);
    SpawnMesh(MeshData_SHOP_SHELF::Vertices, MeshData_SHOP_SHELF::Normals, MeshData_SHOP_SHELF::UVs, MeshData_SHOP_SHELF::Triangles, MeshData_SHOP_SHELF::NumVertices, MeshData_SHOP_SHELF::NumTriangles, FVector(-200, 0, 0), FRotator::ZeroRotator, FVector(3.5f), ETexturePattern::Metal, SC::MetalSilver, SC::MetalBlack);
    SpawnMesh(MeshData_SHOP_REGISTER::Vertices, MeshData_SHOP_REGISTER::Normals, MeshData_SHOP_REGISTER::UVs, MeshData_SHOP_REGISTER::Triangles, MeshData_SHOP_REGISTER::NumVertices, MeshData_SHOP_REGISTER::NumTriangles, FVector(0, 180, 60), FRotator::ZeroRotator, FVector(2.5f), ETexturePattern::Metal, SC::MetalBlack, SC::MetalSilver);

    SpawnCharacterMesh(TEXT("Emersyn"), FVector(0, 0, 0), FRotator(0, 180, 0), 3.0f, FLinearColor(0.92f, 0.75f, 0.60f), SC::FabricPurple);
    SetupIsometricCamera(FVector(0, 0, RS.Z * 0.3f), RS.X * 1.35f);
}

void AEmersynGameMode::BuildPlayground()
{
    FVector RS(500.f, 450.f, 100.f);
    SpawnSky();
    SetupPostProcessing();
    SpawnSkyLight(9.f);
    SpawnDirectionalLight(FRotator(-40.f, 135.f, 0.f), 28.f, FLinearColor(1.f, 0.97f, 0.91f));

    SpawnTexturedFloor(FVector::ZeroVector, FVector(RS.X, RS.Y, 0), ETexturePattern::Sand, SC::FloorSand, SC::FabricYellow, 2.f);

    SpawnTexturedWall(FVector(-RS.X, -RS.Y, 0), FVector(RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);
    SpawnTexturedWall(FVector(RS.X, -RS.Y, 0), FVector(RS.X, RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);
    SpawnTexturedWall(FVector(RS.X, RS.Y, 0), FVector(-RS.X, RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);
    SpawnTexturedWall(FVector(-RS.X, RS.Y, 0), FVector(-RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);

    SpawnTexturedCeiling(FVector(0, 0, RS.Z), FVector(RS.X, RS.Y, 0), SC::SkyTop);
    SpawnRoomLighting(FVector(0, 0, RS.Z * 0.5f), RS);
    SpawnRoomLabel(TEXT("Playground"));

    SpawnMesh(MeshData_PLAYGROUND_SLIDE::Vertices, MeshData_PLAYGROUND_SLIDE::Normals, MeshData_PLAYGROUND_SLIDE::UVs, MeshData_PLAYGROUND_SLIDE::Triangles, MeshData_PLAYGROUND_SLIDE::NumVertices, MeshData_PLAYGROUND_SLIDE::NumTriangles, FVector(-150, -100, 0), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::Metal, SC::FabricRed, SC::FabricYellow);
    SpawnMesh(MeshData_PLAYGROUND_SWING::Vertices, MeshData_PLAYGROUND_SWING::Normals, MeshData_PLAYGROUND_SWING::UVs, MeshData_PLAYGROUND_SWING::Triangles, MeshData_PLAYGROUND_SWING::NumVertices, MeshData_PLAYGROUND_SWING::NumTriangles, FVector(150, -100, 0), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::Metal, SC::MetalSilver, SC::FabricBlue);
    SpawnMesh(MeshData_PLAYGROUND_SANDBOX::Vertices, MeshData_PLAYGROUND_SANDBOX::Normals, MeshData_PLAYGROUND_SANDBOX::UVs, MeshData_PLAYGROUND_SANDBOX::Triangles, MeshData_PLAYGROUND_SANDBOX::NumVertices, MeshData_PLAYGROUND_SANDBOX::NumTriangles, FVector(0, 150, 0), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::Sand, SC::FloorSand, SC::FabricYellow);

    SpawnCharacterMesh(TEXT("Emersyn"), FVector(0, 0, 0), FRotator(0, 0, 0), 3.0f, FLinearColor(0.92f, 0.75f, 0.60f), SC::FabricOrange);
    SetupIsometricCamera(FVector(0, 0, RS.Z * 0.3f), RS.X * 1.35f);
}

void AEmersynGameMode::BuildPark()
{
    FVector RS(600.f, 500.f, 100.f);
    SpawnSky();
    SetupPostProcessing();
    SpawnSkyLight(9.f);
    SpawnDirectionalLight(FRotator(-40.f, 135.f, 0.f), 28.f, FLinearColor(1.f, 0.97f, 0.91f));

    SpawnTexturedFloor(FVector::ZeroVector, FVector(RS.X, RS.Y, 0), ETexturePattern::Grass, SC::FloorGrass, SC::WallGreen, 2.f);

    SpawnTexturedWall(FVector(-RS.X, -RS.Y, 0), FVector(RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);
    SpawnTexturedWall(FVector(RS.X, -RS.Y, 0), FVector(RS.X, RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);
    SpawnTexturedWall(FVector(RS.X, RS.Y, 0), FVector(-RS.X, RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);
    SpawnTexturedWall(FVector(-RS.X, RS.Y, 0), FVector(-RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);

    SpawnTexturedCeiling(FVector(0, 0, RS.Z), FVector(RS.X, RS.Y, 0), SC::SkyTop);
    SpawnRoomLighting(FVector(0, 0, RS.Z * 0.5f), RS);
    SpawnRoomLabel(TEXT("Park"));

    SpawnMesh(MeshData_PARK_BENCH::Vertices, MeshData_PARK_BENCH::Normals, MeshData_PARK_BENCH::UVs, MeshData_PARK_BENCH::Triangles, MeshData_PARK_BENCH::NumVertices, MeshData_PARK_BENCH::NumTriangles, FVector(100, -150, 0), FRotator::ZeroRotator, FVector(3.5f), ETexturePattern::WoodGrain, SC::WoodLight, SC::WoodDark);
    SpawnMesh(MeshData_PARK_FOUNTAIN::Vertices, MeshData_PARK_FOUNTAIN::Normals, MeshData_PARK_FOUNTAIN::UVs, MeshData_PARK_FOUNTAIN::Triangles, MeshData_PARK_FOUNTAIN::NumVertices, MeshData_PARK_FOUNTAIN::NumTriangles, FVector(0, 0, 0), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::Marble, SC::MarbleWhite, SC::TileBlue);
    SpawnMesh(MeshData_PARK_LAMPPOST::Vertices, MeshData_PARK_LAMPPOST::Normals, MeshData_PARK_LAMPPOST::UVs, MeshData_PARK_LAMPPOST::Triangles, MeshData_PARK_LAMPPOST::NumVertices, MeshData_PARK_LAMPPOST::NumTriangles, FVector(-200, 100, 0), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::Metal, SC::MetalBlack, SC::MetalGold);

    SpawnCharacterMesh(TEXT("Emersyn"), FVector(-50, 50, 0), FRotator(0, 30, 0), 3.0f, FLinearColor(0.92f, 0.75f, 0.60f), SC::FabricGreen);
    SpawnCharacterMesh(TEXT("Cat"), FVector(50, -50, 0), FRotator(0, 120, 0), 1.5f, FLinearColor(0.85f, 0.65f, 0.45f), FLinearColor(0.85f, 0.65f, 0.45f));
    SetupIsometricCamera(FVector(0, 0, RS.Z * 0.3f), RS.X * 1.35f);
}

void AEmersynGameMode::BuildMall()
{
    FVector RS(500.f, 450.f, 350.f);
    SpawnSky();
    SetupPostProcessing();
    SpawnSkyLight(9.f);
    SpawnDirectionalLight(FRotator(-40.f, 135.f, 0.f), 28.f, FLinearColor(1.f, 0.97f, 0.91f));

    SpawnTexturedFloor(FVector::ZeroVector, FVector(RS.X, RS.Y, 0), ETexturePattern::Marble, SC::MarbleWhite, SC::MarbleVein, 2.f);

    SpawnTexturedWall(FVector(-RS.X, -RS.Y, 0), FVector(RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallWhite, SC::FabricCream);
    SpawnTexturedWall(FVector(RS.X, -RS.Y, 0), FVector(RS.X, RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallWhite, SC::FabricCream);
    SpawnTexturedWall(FVector(RS.X, RS.Y, 0), FVector(-RS.X, RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallWhite, SC::FabricCream);
    SpawnTexturedWall(FVector(-RS.X, RS.Y, 0), FVector(-RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Wallpaper, SC::WallWhite, SC::FabricCream);

    SpawnTexturedCeiling(FVector(0, 0, RS.Z), FVector(RS.X, RS.Y, 0), SC::CeilingWhite);
    SpawnRoomLighting(FVector(0, 0, RS.Z * 0.5f), RS);
    SpawnRoomLabel(TEXT("Mall"));

    SpawnMesh(MeshData_MALL_ESCALATOR::Vertices, MeshData_MALL_ESCALATOR::Normals, MeshData_MALL_ESCALATOR::UVs, MeshData_MALL_ESCALATOR::Triangles, MeshData_MALL_ESCALATOR::NumVertices, MeshData_MALL_ESCALATOR::NumTriangles, FVector(0, 0, 0), FRotator::ZeroRotator, FVector(5.f), ETexturePattern::Metal, SC::MetalSilver, SC::MetalBlack);
    SpawnMesh(MeshData_MALL_PLANTER::Vertices, MeshData_MALL_PLANTER::Normals, MeshData_MALL_PLANTER::UVs, MeshData_MALL_PLANTER::Triangles, MeshData_MALL_PLANTER::NumVertices, MeshData_MALL_PLANTER::NumTriangles, FVector(-200, -150, 0), FRotator::ZeroRotator, FVector(3.f), ETexturePattern::Concrete, SC::FloorConcrete, SC::FloorGrass);
    SpawnMesh(MeshData_MALL_PLANTER::Vertices, MeshData_MALL_PLANTER::Normals, MeshData_MALL_PLANTER::UVs, MeshData_MALL_PLANTER::Triangles, MeshData_MALL_PLANTER::NumVertices, MeshData_MALL_PLANTER::NumTriangles, FVector(200, -150, 0), FRotator::ZeroRotator, FVector(3.f), ETexturePattern::Concrete, SC::FloorConcrete, SC::FloorGrass);

    SpawnCharacterMesh(TEXT("Ava"), FVector(100, 100, 0), FRotator(0, -90, 0), 3.0f, FLinearColor(0.88f, 0.70f, 0.52f), SC::FabricPurple);
    SetupIsometricCamera(FVector(0, 0, RS.Z * 0.3f), RS.X * 1.35f);
}

void AEmersynGameMode::BuildArcade()
{
    FVector RS(400.f, 380.f, 300.f);
    SpawnSky();
    SetupPostProcessing();
    SpawnSkyLight(9.f);
    SpawnDirectionalLight(FRotator(-40.f, 135.f, 0.f), 28.f, FLinearColor(1.f, 0.97f, 0.91f));

    SpawnTexturedFloor(FVector::ZeroVector, FVector(RS.X, RS.Y, 0), ETexturePattern::Concrete, SC::FloorConcrete, SC::MetalBlack, 2.f);

    SpawnTexturedWall(FVector(-RS.X, -RS.Y, 0), FVector(RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::MetalBlack, SC::FabricPurple);
    SpawnTexturedWall(FVector(RS.X, -RS.Y, 0), FVector(RS.X, RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::MetalBlack, SC::FabricPurple);
    SpawnTexturedWall(FVector(RS.X, RS.Y, 0), FVector(-RS.X, RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::MetalBlack, SC::FabricPurple);
    SpawnTexturedWall(FVector(-RS.X, RS.Y, 0), FVector(-RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::MetalBlack, SC::FabricPurple);

    SpawnTexturedCeiling(FVector(0, 0, RS.Z), FVector(RS.X, RS.Y, 0), SC::MetalBlack);
    SpawnRoomLighting(FVector(0, 0, RS.Z * 0.5f), RS);
    SpawnRoomLabel(TEXT("Arcade"));

    SpawnMesh(MeshData_ARCADE_CABINET::Vertices, MeshData_ARCADE_CABINET::Normals, MeshData_ARCADE_CABINET::UVs, MeshData_ARCADE_CABINET::Triangles, MeshData_ARCADE_CABINET::NumVertices, MeshData_ARCADE_CABINET::NumTriangles, FVector(-150, -150, 0), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::Metal, SC::MetalBlack, SC::FabricBlue);
    SpawnMesh(MeshData_ARCADE_CABINET::Vertices, MeshData_ARCADE_CABINET::Normals, MeshData_ARCADE_CABINET::UVs, MeshData_ARCADE_CABINET::Triangles, MeshData_ARCADE_CABINET::NumVertices, MeshData_ARCADE_CABINET::NumTriangles, FVector(150, -150, 0), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::Metal, SC::MetalBlack, SC::FabricRed);
    SpawnMesh(MeshData_ARCADE_CLAW_MACHINE::Vertices, MeshData_ARCADE_CLAW_MACHINE::Normals, MeshData_ARCADE_CLAW_MACHINE::UVs, MeshData_ARCADE_CLAW_MACHINE::Triangles, MeshData_ARCADE_CLAW_MACHINE::NumVertices, MeshData_ARCADE_CLAW_MACHINE::NumTriangles, FVector(0, 150, 0), FRotator::ZeroRotator, FVector(4.f), ETexturePattern::Metal, SC::FabricYellow, SC::FabricGreen);

    SpawnCharacterMesh(TEXT("Leo"), FVector(0, 0, 0), FRotator(0, 0, 0), 3.0f, FLinearColor(0.85f, 0.65f, 0.45f), SC::FabricRed);
    SetupIsometricCamera(FVector(0, 0, RS.Z * 0.3f), RS.X * 1.35f);
}

void AEmersynGameMode::BuildAmusementPark()
{
    FVector RS(700.f, 600.f, 100.f);
    SpawnSky();
    SetupPostProcessing();
    SpawnSkyLight(9.f);
    SpawnDirectionalLight(FRotator(-40.f, 135.f, 0.f), 28.f, FLinearColor(1.f, 0.97f, 0.91f));

    SpawnTexturedFloor(FVector::ZeroVector, FVector(RS.X, RS.Y, 0), ETexturePattern::Concrete, SC::FloorConcrete, SC::FloorSand, 2.f);

    SpawnTexturedWall(FVector(-RS.X, -RS.Y, 0), FVector(RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);
    SpawnTexturedWall(FVector(RS.X, -RS.Y, 0), FVector(RS.X, RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);
    SpawnTexturedWall(FVector(RS.X, RS.Y, 0), FVector(-RS.X, RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);
    SpawnTexturedWall(FVector(-RS.X, RS.Y, 0), FVector(-RS.X, -RS.Y, 0), RS.Z, ETexturePattern::Brick, SC::BrickRed, SC::BrickMortar);

    SpawnTexturedCeiling(FVector(0, 0, RS.Z), FVector(RS.X, RS.Y, 0), SC::SkyTop);
    SpawnRoomLighting(FVector(0, 0, RS.Z * 0.5f), RS);
    SpawnRoomLabel(TEXT("Amusement Park"));

    SpawnMesh(MeshData_AMUSEMENT_CAROUSEL::Vertices, MeshData_AMUSEMENT_CAROUSEL::Normals, MeshData_AMUSEMENT_CAROUSEL::UVs, MeshData_AMUSEMENT_CAROUSEL::Triangles, MeshData_AMUSEMENT_CAROUSEL::NumVertices, MeshData_AMUSEMENT_CAROUSEL::NumTriangles, FVector(-200, 0, 0), FRotator::ZeroRotator, FVector(5.f), ETexturePattern::Metal, SC::FabricRed, SC::MetalGold);
    SpawnMesh(MeshData_AMUSEMENT_FERRISWHEEL::Vertices, MeshData_AMUSEMENT_FERRISWHEEL::Normals, MeshData_AMUSEMENT_FERRISWHEEL::UVs, MeshData_AMUSEMENT_FERRISWHEEL::Triangles, MeshData_AMUSEMENT_FERRISWHEEL::NumVertices, MeshData_AMUSEMENT_FERRISWHEEL::NumTriangles, FVector(200, 0, 0), FRotator::ZeroRotator, FVector(5.f), ETexturePattern::Metal, SC::FabricBlue, SC::MetalSilver);
    SpawnMesh(MeshData_AMUSEMENT_FOODCART::Vertices, MeshData_AMUSEMENT_FOODCART::Normals, MeshData_AMUSEMENT_FOODCART::UVs, MeshData_AMUSEMENT_FOODCART::Triangles, MeshData_AMUSEMENT_FOODCART::NumVertices, MeshData_AMUSEMENT_FOODCART::NumTriangles, FVector(0, -250, 0), FRotator::ZeroRotator, FVector(3.f), ETexturePattern::WoodGrain, SC::FabricOrange, SC::FabricYellow);

    SpawnCharacterMesh(TEXT("Emersyn"), FVector(0, 100, 0), FRotator(0, 0, 0), 3.0f, FLinearColor(0.92f, 0.75f, 0.60f), SC::FabricPink);
    SpawnCharacterMesh(TEXT("Mia"), FVector(100, 100, 0), FRotator(0, -45, 0), 3.0f, FLinearColor(0.88f, 0.70f, 0.55f), SC::FabricYellow);
    SetupIsometricCamera(FVector(0, 0, RS.Z * 0.3f), RS.X * 1.35f);
}
