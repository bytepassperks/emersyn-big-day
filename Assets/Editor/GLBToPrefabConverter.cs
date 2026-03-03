using UnityEditor;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using GLTFast;

/// <summary>
/// Claude 4.5 Bedrock recommendation: Convert GLB files to native Unity prefabs at EDITOR TIME.
/// This avoids all IL2CPP runtime GLB parsing issues that caused 30 rounds of failures.
/// The resulting prefabs are compiled into the build as native Unity assets.
/// </summary>
public class GLBToPrefabConverter : EditorWindow
{
    private static readonly string[] CharacterGLBs = {
        "emersyn", "ava", "mia", "leo", "shopkeeper", "teacher"
    };
    private static readonly string[] PetGLBs = {
        "cat", "dog", "bunny"
    };

    [MenuItem("Tools/Convert GLBs to Prefabs")]
    public static void ConvertAll()
    {
        Debug.Log("[GLBConverter] Starting GLB to Prefab conversion...");
        ConvertAllAsync();
    }

    /// <summary>
    /// Called from BuildScript before building APK to ensure prefabs exist.
    /// </summary>
    public static void ConvertAllSync()
    {
        Debug.Log("[GLBConverter] ConvertAllSync called from build pipeline");
        ConvertAllAsync();
    }

    private static async void ConvertAllAsync()
    {
        string glbSourceDir = Path.Combine(Application.streamingAssetsPath, "Characters");
        // Claude 4.5 Bedrock (full 30-round history): Save to Resources/ for guaranteed inclusion in IL2CPP build
        string prefabDir = "Assets/Resources/Characters";
        string meshDir = "Assets/Resources/Characters/Meshes";
        string matDir = "Assets/Resources/Characters/Materials";

        // Create output directories
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources", "Characters"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources", "Characters", "Meshes"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources", "Characters", "Materials"));

        int converted = 0;
        int failed = 0;

        // Convert all character GLBs
        string[] allGLBs = new string[CharacterGLBs.Length + PetGLBs.Length];
        CharacterGLBs.CopyTo(allGLBs, 0);
        PetGLBs.CopyTo(allGLBs, CharacterGLBs.Length);

        for (int i = 0; i < allGLBs.Length; i++)
        {
            string glbName = allGLBs[i];
            string glbPath = Path.Combine(glbSourceDir, glbName + ".glb");

            if (!File.Exists(glbPath))
            {
                Debug.LogWarning($"[GLBConverter] GLB not found: {glbPath}");
                failed++;
                continue;
            }

            EditorUtility.DisplayProgressBar("Converting GLBs",
                $"Processing {glbName}.glb ({i + 1}/{allGLBs.Length})",
                (float)i / allGLBs.Length);

            bool success = await ConvertSingleGLB(glbName, glbPath, prefabDir, meshDir, matDir);
            if (success)
                converted++;
            else
                failed++;
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

        Debug.Log($"[GLBConverter] COMPLETE: {converted} converted, {failed} failed out of {allGLBs.Length} total");
    }

    private static async Task<bool> ConvertSingleGLB(string glbName, string glbPath, string prefabDir, string meshDir, string matDir)
    {
        try
        {
            Debug.Log($"[GLBConverter] Loading GLB: {glbPath}");

            // Read GLB binary data
            byte[] glbData = File.ReadAllBytes(glbPath);
            Debug.Log($"[GLBConverter] GLB data: {glbData.Length} bytes for {glbName}");

            // Parse material colors from GLB binary (same logic as SceneBuilder)
            var materialColors = ParseMaterialColorsFromGLB(glbData, glbName);

            // Use GLTFast to load the model in Editor context (this works reliably in Editor, unlike IL2CPP runtime)
            var gltf = new GltfImport();
            bool loadSuccess = await gltf.Load(glbPath);

            if (!loadSuccess)
            {
                // Try loading from binary data instead
                Debug.LogWarning($"[GLBConverter] File load failed for {glbName}, trying binary load...");
                loadSuccess = await gltf.LoadGltfBinary(glbData, new System.Uri("file://" + glbPath));
            }

            if (!loadSuccess)
            {
                Debug.LogError($"[GLBConverter] Failed to load GLB: {glbName}");
                return false;
            }

            // Create a temporary GameObject to instantiate the model
            GameObject tempObj = new GameObject(glbName);
            bool instantiateSuccess = await gltf.InstantiateMainSceneAsync(tempObj.transform);

            if (!instantiateSuccess)
            {
                Debug.LogError($"[GLBConverter] Failed to instantiate GLB: {glbName}");
                Object.DestroyImmediate(tempObj);
                return false;
            }

            Debug.Log($"[GLBConverter] GLB instantiated: {glbName} with {tempObj.transform.childCount} children");

            // Save meshes as Unity assets
            int meshCount = 0;
            var meshFilters = tempObj.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh != null)
                {
                    string meshAssetPath = $"{meshDir}/{glbName}_mesh_{meshCount}.asset";
                    // Clone the mesh to make it a standalone asset
                    Mesh meshClone = Object.Instantiate(mf.sharedMesh);
                    meshClone.name = $"{glbName}_mesh_{meshCount}";
                    AssetDatabase.CreateAsset(meshClone, meshAssetPath);
                    mf.sharedMesh = meshClone;
                    meshCount++;
                }
            }

            var skinnedMeshRenderers = tempObj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in skinnedMeshRenderers)
            {
                if (smr.sharedMesh != null)
                {
                    string meshAssetPath = $"{meshDir}/{glbName}_skinned_{meshCount}.asset";
                    Mesh meshClone = Object.Instantiate(smr.sharedMesh);
                    meshClone.name = $"{glbName}_skinned_{meshCount}";
                    AssetDatabase.CreateAsset(meshClone, meshAssetPath);
                    smr.sharedMesh = meshClone;
                    meshCount++;
                }
            }

            // Save materials as Unity assets and apply correct colors
            int matCount = 0;
            var renderers = tempObj.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {
                    if (materials[m] != null)
                    {
                        // Create a Standard shader material with the correct color
                        Material newMat = new Material(Shader.Find("Standard"));
                        string matName = materials[m].name ?? $"mat_{matCount}";
                        newMat.name = $"{glbName}_{matName}";

                        // Apply color from parsed GLB data
                        Color baseColor = Color.white;
                        if (materialColors.ContainsKey(matName))
                        {
                            baseColor = materialColors[matName];
                        }
                        else if (materials[m].HasProperty("_Color"))
                        {
                            baseColor = materials[m].GetColor("_Color");
                        }
                        else if (materials[m].HasProperty("_BaseColor"))
                        {
                            baseColor = materials[m].GetColor("_BaseColor");
                        }

                        newMat.SetColor("_Color", baseColor);
                        newMat.SetFloat("_Metallic", 0f);
                        newMat.SetFloat("_Glossiness", 0.5f);
                        newMat.SetFloat("_Mode", 0f); // Opaque

                        // Configure for mobile rendering
                        newMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        newMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        newMat.SetInt("_ZWrite", 1);
                        newMat.DisableKeyword("_ALPHATEST_ON");
                        newMat.DisableKeyword("_ALPHABLEND_ON");
                        newMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

                        // Enable shadows and lighting
                        newMat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                        newMat.EnableKeyword("_GLOSSYREFLECTIONS_OFF");

                        string matAssetPath = $"{matDir}/{glbName}_mat_{matCount}.mat";
                        AssetDatabase.CreateAsset(newMat, matAssetPath);
                        materials[m] = newMat;
                        matCount++;

                        Debug.Log($"[GLBConverter] Material '{matName}' -> color ({baseColor.r:F2},{baseColor.g:F2},{baseColor.b:F2})");
                    }
                }
                renderer.sharedMaterials = materials;

                // Configure renderer for quality
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            // Save as prefab
            string prefabPath = $"{prefabDir}/{glbName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempObj, prefabPath);
            Object.DestroyImmediate(tempObj);

            if (prefab != null)
            {
                Debug.Log($"[GLBConverter] SUCCESS: Created prefab {prefabPath} ({meshCount} meshes, {matCount} materials)");
                return true;
            }
            else
            {
                Debug.LogError($"[GLBConverter] Failed to save prefab for {glbName}");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GLBConverter] EXCEPTION converting {glbName}: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Parse GLB binary JSON to extract material name -> baseColorFactor mapping.
    /// Same logic as SceneBuilder.ParseMaterialColorsFromGLB but in Editor context.
    /// </summary>
    private static Dictionary<string, Color> ParseMaterialColorsFromGLB(byte[] glbData, string modelName)
    {
        var materials = new Dictionary<string, Color>();
        try
        {
            if (glbData == null || glbData.Length < 20) return materials;

            uint magic = System.BitConverter.ToUInt32(glbData, 0);
            if (magic != 0x46546C67) return materials; // "glTF"

            uint jsonChunkLength = System.BitConverter.ToUInt32(glbData, 12);
            uint jsonChunkType = System.BitConverter.ToUInt32(glbData, 16);
            if (jsonChunkType != 0x4E4F534A) return materials; // "JSON"

            string json = System.Text.Encoding.UTF8.GetString(glbData, 20, (int)jsonChunkLength);

            // Simple JSON parsing for materials array
            int matStart = json.IndexOf("\"materials\"");
            if (matStart < 0) return materials;

            // Find material objects
            int searchPos = matStart;
            while (true)
            {
                int nameStart = json.IndexOf("\"name\"", searchPos);
                if (nameStart < 0) break;

                // Extract material name
                int nameValStart = json.IndexOf("\"", nameStart + 7);
                if (nameValStart < 0) break;
                int nameValEnd = json.IndexOf("\"", nameValStart + 1);
                if (nameValEnd < 0) break;
                string matName = json.Substring(nameValStart + 1, nameValEnd - nameValStart - 1);

                // Find baseColorFactor near this material
                int factorStart = json.IndexOf("\"baseColorFactor\"", nameStart);
                if (factorStart < 0 || factorStart - nameStart > 500) // Don't look too far
                {
                    searchPos = nameValEnd + 1;
                    continue;
                }

                int arrayStart = json.IndexOf("[", factorStart);
                int arrayEnd = json.IndexOf("]", arrayStart);
                if (arrayStart < 0 || arrayEnd < 0)
                {
                    searchPos = nameValEnd + 1;
                    continue;
                }

                string arrayStr = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                string[] parts = arrayStr.Split(',');
                if (parts.Length >= 3)
                {
                    float r = float.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    float g = float.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    float b = float.Parse(parts[2].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    float a = parts.Length >= 4 ? float.Parse(parts[3].Trim(), System.Globalization.CultureInfo.InvariantCulture) : 1f;

                    materials[matName] = new Color(r, g, b, a);
                    Debug.Log($"[GLBConverter] Parsed material '{matName}' for {modelName}: ({r:F2},{g:F2},{b:F2},{a:F2})");
                }

                searchPos = arrayEnd + 1;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[GLBConverter] Error parsing GLB materials for {modelName}: {e.Message}");
        }

        return materials;
    }
}
