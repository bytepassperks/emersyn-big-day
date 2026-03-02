using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.Visual
{
    /// <summary>
    /// Enhancement #3: Runtime procedural particle effects - sparkles, hearts, confetti,
    /// dust motes, magic poof, bubbles. No prefabs needed - created at runtime.
    /// Like Animal Crossing's ambient particles and Toca Life's interaction sparkles.
    /// </summary>
    public class ProceduralParticles : MonoBehaviour
    {
        public static ProceduralParticles Instance { get; private set; }

        private Dictionary<string, ParticleSystem> particleCache = new Dictionary<string, ParticleSystem>();
        private Transform poolRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            poolRoot = new GameObject("ProceduralParticlePool").transform;
            poolRoot.SetParent(transform);
            CreateAllParticleSystems();
        }

        private void CreateAllParticleSystems()
        {
            CreateSparkleSystem();
            CreateHeartSystem();
            CreateConfettiSystem();
            CreateDustMoteSystem();
            CreateMagicPoofSystem();
            CreateBubbleSystem();
            CreateStarBurstSystem();
            CreateMusicNoteSystem();
            CreateSleepZSystem();
            CreateRainSystem();
            CreateSnowSystem();
            CreateLeavesSystem();
            CreateFireflySystem();
        }

        // --- SPARKLES ---
        private void CreateSparkleSystem()
        {
            var ps = CreateBaseSystem("Sparkles");
            var main = ps.main;
            main.startLifetime = 0.8f;
            main.startSpeed = 1.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 0.8f, 1f), new Color(1f, 0.9f, 1f, 1f));
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.yellow, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            particleCache["sparkle"] = ps;
        }

        // --- HEARTS ---
        private void CreateHeartSystem()
        {
            var ps = CreateBaseSystem("Hearts");
            var main = ps.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 1f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.3f, 0.5f, 1f), new Color(1f, 0.5f, 0.7f, 1f));
            main.maxParticles = 20;
            main.gravityModifier = -0.3f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.3f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.2f));

            particleCache["hearts"] = ps;
        }

        // --- CONFETTI ---
        private void CreateConfettiSystem()
        {
            var ps = CreateBaseSystem("Confetti");
            var main = ps.main;
            main.startLifetime = 3f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.gravityModifier = 0.5f;
            main.maxParticles = 100;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.5f;

            // Rainbow colors
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(Color.red, 0f),
                    new GradientColorKey(Color.yellow, 0.25f),
                    new GradientColorKey(Color.green, 0.5f),
                    new GradientColorKey(Color.cyan, 0.75f),
                    new GradientColorKey(new Color(1f, 0.5f, 1f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);

            particleCache["confetti"] = ps;
        }

        // --- DUST MOTES ---
        private void CreateDustMoteSystem()
        {
            var ps = CreateBaseSystem("DustMotes");
            var main = ps.main;
            main.startLifetime = 8f;
            main.startSpeed = 0.1f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new Color(1f, 1f, 0.9f, 0.3f);
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 5;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(10f, 4f, 8f);

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.05f);

            particleCache["dustmotes"] = ps;
        }

        // --- MAGIC POOF ---
        private void CreateMagicPoofSystem()
        {
            var ps = CreateBaseSystem("MagicPoof");
            var main = ps.main;
            main.startLifetime = 0.6f;
            main.startSpeed = 2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.8f, 0.6f, 1f, 1f), new Color(1f, 0.8f, 1f, 1f));
            main.maxParticles = 25;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            particleCache["magicpoof"] = ps;
        }

        // --- BUBBLES ---
        private void CreateBubbleSystem()
        {
            var ps = CreateBaseSystem("Bubbles");
            var main = ps.main;
            main.startLifetime = 3f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startColor = new Color(0.7f, 0.9f, 1f, 0.6f);
            main.gravityModifier = -0.2f;
            main.maxParticles = 30;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            particleCache["bubbles"] = ps;
        }

        // --- STAR BURST ---
        private void CreateStarBurstSystem()
        {
            var ps = CreateBaseSystem("StarBurst");
            var main = ps.main;
            main.startLifetime = 1f;
            main.startSpeed = 3f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.9f, 0.2f, 1f), new Color(1f, 0.7f, 0f, 1f));
            main.maxParticles = 20;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            particleCache["starburst"] = ps;
        }

        // --- MUSIC NOTES ---
        private void CreateMusicNoteSystem()
        {
            var ps = CreateBaseSystem("MusicNotes");
            var main = ps.main;
            main.startLifetime = 2f;
            main.startSpeed = 0.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.6f, 0.3f, 1f, 1f), new Color(1f, 0.5f, 0.8f, 1f));
            main.gravityModifier = -0.3f;
            main.maxParticles = 15;

            var emission = ps.emission;
            emission.rateOverTime = 3;

            particleCache["musicnotes"] = ps;
        }

        // --- SLEEP Zs ---
        private void CreateSleepZSystem()
        {
            var ps = CreateBaseSystem("SleepZ");
            var main = ps.main;
            main.startLifetime = 2.5f;
            main.startSpeed = 0.3f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
            main.startColor = new Color(0.7f, 0.8f, 1f, 0.7f);
            main.gravityModifier = -0.15f;
            main.maxParticles = 10;

            var emission = ps.emission;
            emission.rateOverTime = 2;

            particleCache["sleepz"] = ps;
        }

        // --- RAIN ---
        private void CreateRainSystem()
        {
            var ps = CreateBaseSystem("Rain");
            var main = ps.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.03f);
            main.startColor = new Color(0.6f, 0.7f, 0.9f, 0.5f);
            main.maxParticles = 500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 200;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(15f, 0.1f, 12f);
            shape.position = new Vector3(0, 12f, 0);

            particleCache["rain"] = ps;
        }

        // --- SNOW ---
        private void CreateSnowSystem()
        {
            var ps = CreateBaseSystem("Snow");
            var main = ps.main;
            main.startLifetime = 5f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.startColor = new Color(1f, 1f, 1f, 0.8f);
            main.gravityModifier = 0.1f;
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 30;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(15f, 0.1f, 12f);
            shape.position = new Vector3(0, 10f, 0);

            particleCache["snow"] = ps;
        }

        // --- LEAVES ---
        private void CreateLeavesSystem()
        {
            var ps = CreateBaseSystem("Leaves");
            var main = ps.main;
            main.startLifetime = 4f;
            main.startSpeed = 0.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.8f, 0.6f, 0.2f, 0.8f), new Color(0.9f, 0.3f, 0.1f, 0.8f));
            main.gravityModifier = 0.15f;
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 8;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 0.1f, 10f);
            shape.position = new Vector3(0, 8f, 0);

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-90f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad);

            particleCache["leaves"] = ps;
        }

        // --- FIREFLIES ---
        private void CreateFireflySystem()
        {
            var ps = CreateBaseSystem("Fireflies");
            var main = ps.main;
            main.startLifetime = 6f;
            main.startSpeed = 0.2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startColor = new Color(1f, 1f, 0.5f, 0.8f);
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 4;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(10f, 3f, 8f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1f));

            particleCache["fireflies"] = ps;
        }

        // --- HELPER: Create base particle system ---
        private ParticleSystem CreateBaseSystem(string name)
        {
            var go = new GameObject("PS_" + name);
            go.transform.SetParent(poolRoot);
            go.SetActive(false);
            var ps = go.AddComponent<ParticleSystem>();

            // Disable default renderer shape - use built-in circles
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            // Use the renderer's existing default material (survives IL2CPP shader stripping)
            // ParticleSystem comes with a default particle material - just keep it
            if (renderer.sharedMaterial != null)
                renderer.material = new Material(renderer.sharedMaterial);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;

            return ps;
        }

        // --- PUBLIC API ---
        public void Play(string effectName, Vector3 position, float duration = 0f)
        {
            if (!particleCache.ContainsKey(effectName)) return;
            var template = particleCache[effectName];
            var instance = Instantiate(template.gameObject, position, Quaternion.identity);
            instance.SetActive(true);
            var ps = instance.GetComponent<ParticleSystem>();
            ps.Play();
            float destroyTime = duration > 0 ? duration :
                ps.main.duration + ps.main.startLifetime.constantMax + 0.5f;
            Destroy(instance, destroyTime);
        }

        public void PlayAttached(string effectName, Transform parent, Vector3 localOffset = default, float duration = 0f)
        {
            if (!particleCache.ContainsKey(effectName)) return;
            var template = particleCache[effectName];
            var instance = Instantiate(template.gameObject, parent);
            instance.transform.localPosition = localOffset;
            instance.SetActive(true);
            var ps = instance.GetComponent<ParticleSystem>();
            ps.Play();
            if (duration > 0) Destroy(instance, duration);
        }

        public void PlayLooping(string effectName, Vector3 position)
        {
            if (!particleCache.ContainsKey(effectName)) return;
            var template = particleCache[effectName];
            var instance = Instantiate(template.gameObject, position, Quaternion.identity);
            instance.SetActive(true);
            var ps = instance.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            ps.Play();
        }

        // Convenience methods
        public void SpawnSparkles(Vector3 pos) => Play("sparkle", pos);
        public void SpawnHearts(Vector3 pos) => Play("hearts", pos);
        public void SpawnConfetti(Vector3 pos) => Play("confetti", pos);
        public void SpawnMagicPoof(Vector3 pos) => Play("magicpoof", pos);
        public void SpawnBubbles(Vector3 pos) => Play("bubbles", pos);
        public void SpawnStarBurst(Vector3 pos) => Play("starburst", pos);
        public void StartDustMotes() => PlayLooping("dustmotes", Vector3.up * 2f);
        public void StartFireflies() => PlayLooping("fireflies", Vector3.up * 1.5f);
    }
}
