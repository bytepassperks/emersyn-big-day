#pragma once
#include "CoreMinimal.h"
#include "ProceduralMeshComponent.h"

class FMeshLoader
{
public:
    static void LoadMesh(
        UProceduralMeshComponent* PMC, int32 Section,
        const float* Verts, const float* Norms, const float* UVData,
        const float* ColData, const int32* Tris,
        int32 NumVerts, int32 NumTris,
        const FLinearColor& Tint, float Brightness);
};
