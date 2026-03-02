// Claude Bedrock Round 2 fix #1: Isolated GLTFast dependency wrapper.
// This class is in a SEPARATE file so that if GLTFast assembly fails to resolve
// on IL2CPP Android, ONLY this class fails to load — SceneBuilder.cs remains intact
// and can still create geometry, UI, lighting, etc.
//
// The key insight: if 'using GLTFast;' is in SceneBuilder.cs and GLTFast has any
// IL2CPP assembly resolution issue, the ENTIRE SceneBuilder MonoBehaviour silently
// fails to load, causing the blue screen (no Awake() → no geometry → just camera background).

using UnityEngine;
using System.Threading.Tasks;
using GLTFast;

namespace EmersynBigDay.Core
{
    /// <summary>
    /// Thin wrapper around GLTFast to isolate assembly dependency.
    /// If GLTFast is not available, methods throw and callers handle gracefully.
    /// </summary>
    public static class GLTFHelper
    {
        public static object CreateImport()
        {
            return new GltfImport();
        }

        public static Task<bool> Load(object import, string uri)
        {
            var gltf = (GltfImport)import;
            return gltf.Load(uri);
        }

        public static Task<bool> InstantiateMainScene(object import, Transform parent)
        {
            var gltf = (GltfImport)import;
            return gltf.InstantiateMainSceneAsync(parent);
        }
    }
}
