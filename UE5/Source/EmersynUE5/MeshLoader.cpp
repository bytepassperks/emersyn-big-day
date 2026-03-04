#include "MeshLoader.h"

void FMeshLoader::LoadMesh(
    UProceduralMeshComponent* PMC, int32 Section,
    const float* Verts, const float* Norms, const float* UVData,
    const float* ColData, const int32* Tris,
    int32 NumVerts, int32 NumTris,
    const FLinearColor& Tint, float Brightness)
{
    if (!PMC || NumVerts == 0) return;

    TArray<FVector> Vertices;
    TArray<FVector> Normals;
    TArray<FVector2D> UVs;
    TArray<FLinearColor> Colors;
    TArray<int32> Triangles;

    Vertices.Reserve(NumVerts);
    Normals.Reserve(NumVerts);
    UVs.Reserve(NumVerts);
    Colors.Reserve(NumVerts);
    Triangles.Reserve(NumTris);

    // Key light for shading
    const FVector KeyLight = FVector(0.5f, -0.3f, 0.7071f).GetSafeNormal();

    for (int32 i = 0; i < NumVerts; i++)
    {
        // UE5 uses X-forward, Y-right, Z-up. OBJ data may need swizzle.
        float vx = Verts[i*3 + 0];
        float vy = Verts[i*3 + 1];
        float vz = Verts[i*3 + 2];
        Vertices.Add(FVector(vx, vy, vz));

        float nx = Norms[i*3 + 0];
        float ny = Norms[i*3 + 1];
        float nz = Norms[i*3 + 2];
        FVector N(nx, ny, nz);
        Normals.Add(N);

        float u = UVData[i*2 + 0];
        float v = UVData[i*2 + 1];
        UVs.Add(FVector2D(u, v));

        // Compute lit vertex color
        float KeyDot = FMath::Max(0.0f, FVector::DotProduct(N, KeyLight));
        float TopDot = FMath::Max(0.0f, N.Z);
        float Light = FMath::Clamp(0.3f + KeyDot * 0.4f + TopDot * 0.3f, 0.0f, 1.3f) * Brightness;

        FLinearColor C;
        C.R = FMath::Clamp(Tint.R * Light, 0.0f, 1.0f);
        C.G = FMath::Clamp(Tint.G * Light, 0.0f, 1.0f);
        C.B = FMath::Clamp(Tint.B * Light, 0.0f, 1.0f);
        C.A = 1.0f;
        Colors.Add(C);
    }

    for (int32 i = 0; i < NumTris; i++)
    {
        Triangles.Add(Tris[i]);
    }

    PMC->CreateMeshSection_LinearColor(Section, Vertices, Triangles, Normals, UVs, Colors, TArray<FProcMeshTangent>(), false);
}
