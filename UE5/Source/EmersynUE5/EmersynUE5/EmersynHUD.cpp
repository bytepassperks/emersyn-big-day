#include "EmersynHUD.h"
#include "Engine/Canvas.h"

AEmersynHUD::AEmersynHUD()
{
    bShowSplash = true;
    SplashAlpha = 1.0f;
}

void AEmersynHUD::DrawHUD()
{
    Super::DrawHUD();

    if (!Canvas) return;

    float SW = Canvas->SizeX;
    float SH = Canvas->SizeY;
    float TextScale = FMath::Max(SW / 600.0f, 1.0f);

    if (bShowSplash)
    {
        // Splash overlay
        DrawRect(FLinearColor(0.08f, 0.05f, 0.15f, SplashAlpha), 0, 0, SW, SH);

        // Title
        float TitleX = SW * 0.18f;
        float TitleY = SH * 0.25f;
        DrawText(TEXT("EMERSYN'S"), FLinearColor(1.0f, 0.6f, 0.8f, SplashAlpha), TitleX, TitleY, nullptr, TextScale * 2.0f);
        DrawText(TEXT("BIG DAY"), FLinearColor(0.8f, 0.6f, 1.0f, SplashAlpha), TitleX + SW * 0.05f, TitleY + SH * 0.12f, nullptr, TextScale * 2.5f);

        // Subtitle
        DrawText(TEXT("A Day Full of Adventures!"), FLinearColor(1.0f, 0.9f, 0.7f, SplashAlpha * 0.8f), SW * 0.22f, SH * 0.55f, nullptr, TextScale * 1.0f);

        // Loading indicator
        DrawText(TEXT("Loading..."), FLinearColor(1.0f, 1.0f, 1.0f, SplashAlpha * 0.5f), SW * 0.4f, SH * 0.8f, nullptr, TextScale * 0.8f);

        // Decorative stars
        DrawRect(FLinearColor(1.0f, 0.95f, 0.3f, SplashAlpha * 0.8f), SW * 0.1f, SH * 0.15f, 8, 8);
        DrawRect(FLinearColor(0.95f, 0.7f, 0.85f, SplashAlpha * 0.7f), SW * 0.85f, SH * 0.2f, 6, 6);
        DrawRect(FLinearColor(0.7f, 0.85f, 1.0f, SplashAlpha * 0.6f), SW * 0.75f, SH * 0.7f, 7, 7);
        DrawRect(FLinearColor(0.85f, 1.0f, 0.7f, SplashAlpha * 0.9f), SW * 0.15f, SH * 0.75f, 5, 5);
    }
    else if (!RoomDisplayName.IsEmpty())
    {
        // Room name banner at top
        float BannerH = SH * 0.07f;

        // Semi-transparent banner background
        DrawRect(FLinearColor(0.0f, 0.0f, 0.0f, 0.45f), 0, 0, SW, BannerH);
        // Accent line
        DrawRect(FLinearColor(1.0f, 0.6f, 0.8f, 0.8f), 0, BannerH - 2, SW, 2);

        // Room name
        DrawText(*RoomDisplayName, FLinearColor(1.0f, 1.0f, 1.0f, 0.95f), SW * 0.03f, BannerH * 0.15f, nullptr, TextScale * 1.2f);

        // Game title on right
        DrawText(TEXT("Emersyn's Big Day"), FLinearColor(1.0f, 0.8f, 0.9f, 0.6f), SW * 0.7f, BannerH * 0.2f, nullptr, TextScale * 0.7f);

        // Bottom navigation hint
        float BottomY = SH - BannerH;
        DrawRect(FLinearColor(0.0f, 0.0f, 0.0f, 0.3f), 0, BottomY, SW, BannerH);
        DrawText(TEXT("Tap anywhere to explore!"), FLinearColor(1.0f, 1.0f, 1.0f, 0.5f), SW * 0.3f, BottomY + BannerH * 0.15f, nullptr, TextScale * 0.8f);
    }
}
