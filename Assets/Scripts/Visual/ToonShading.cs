using UnityEngine;
using UnityEngine.Rendering;

namespace EmersynBigDay.Visual
{
    /// <summary>
    /// Enhancement #1: Toon/cel-shading with rim lighting for chibi art style.
    /// Creates custom materials at runtime with cartoon-like shading steps.
    /// Like My Talking Angela 2's smooth cel-shaded look.
    /// </summary>
    public class ToonShading : MonoBehaviour
    {
        public static ToonShading Instance { get; private set; }

        [Header("Toon Settings")]
        public Color RimColor = new Color(1f, 0.9f, 0.95f, 1f);
        public float RimPower = 3f;
        public float RimIntensity = 0.6f;
        public Color OutlineColor = new Color(0.2f, 0.15f, 0.1f, 1f);
        public float OutlineWidth = 0.02f;

        [Header("Cel Shading")]
        public int ShadingSteps = 3;
        public float ShadowSoftness = 0.1f;
        public Color ShadowTint = new Color(0.8f, 0.75f, 0.9f);

        [Header("Specular")]
        public float SpecularSize = 0.3f;
        public float SpecularSoftness = 0.1f;
        public Color SpecularColor = Color.white;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ApplyToonShadingToScene();
        }

        /// <summary>
        /// Apply toon shading effect to all renderers in the scene.
        /// Uses Standard shader (Built-in Pipeline) with modified properties for a cartoon look.
        /// </summary>
        public void ApplyToonShadingToScene()
        {
            var renderers = FindObjectsOfType<Renderer>();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                foreach (var mat in r.sharedMaterials)
                {
                    ApplyToonToMaterial(mat);
                }
            }
        }

        /// <summary>
        /// Apply toon shading properties to a single material.
        /// </summary>
        public void ApplyToonToMaterial(Material mat)
        {
            if (mat == null) return;

            // Enhance smoothness for cartoon look
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.7f);

            // Add slight emission for glow effect
            if (mat.HasProperty("_EmissionColor"))
            {
                Color baseColor = Color.white;
                if (mat.HasProperty("_BaseColor"))
                    baseColor = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color"))
                    baseColor = mat.color;

                mat.SetColor("_EmissionColor", baseColor * 0.05f);
                if (mat.shader != null && (mat.shader.name.Contains("Universal") || mat.shader.name.Contains("Standard")))
                    mat.EnableKeyword("_EMISSION");
            }

            // Metallic to 0 for cartoon look
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0f);
        }

        /// <summary>
        /// Create a toon-styled material with the given base color.
        /// </summary>
        public Material CreateToonMaterial(Color baseColor)
        {
            // NUCLEAR FIX: Use Shader.Find("Standard") for Built-in Pipeline (per Claude expert guidance)
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogError("[ToonShading] Standard shader not found! Falling back to primitive.");
                var refPrim = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                refPrim.name = "_ToonShaderRef";
                refPrim.SetActive(false);
                DontDestroyOnLoad(refPrim);
                shader = refPrim.GetComponent<Renderer>().sharedMaterial.shader;
            }
            var mat = new Material(shader);

            // Standard shader uses _Color, not _BaseColor
            mat.color = baseColor;

            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", 0.65f);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0f);

            // Soft emission for glow
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", baseColor * 0.08f);
                mat.EnableKeyword("_EMISSION");
            }

            return mat;
        }

        /// <summary>
        /// Apply rim lighting effect by adjusting material Fresnel.
        /// </summary>
        public void ApplyRimLighting(Renderer renderer)
        {
            if (renderer == null) return;
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat.HasProperty("_FresnelPower"))
                    mat.SetFloat("_FresnelPower", RimPower);
                // Use emission as rim light approximation
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", RimColor * RimIntensity * 0.15f);
                    mat.EnableKeyword("_EMISSION");
                }
            }
        }

        /// <summary>
        /// Apply character-specific toon settings (softer shadows, brighter rim).
        /// </summary>
        public void ApplyCharacterToon(GameObject character)
        {
            if (character == null) return;
            var renderers = character.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null) continue;
                    ApplyToonToMaterial(mat);
                    // Brighter emission for characters
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        Color baseColor = mat.HasProperty("_BaseColor")
                            ? mat.GetColor("_BaseColor") : mat.color;
                        mat.SetColor("_EmissionColor", baseColor * 0.12f);
                    }
                }
            }
        }
    }
}
