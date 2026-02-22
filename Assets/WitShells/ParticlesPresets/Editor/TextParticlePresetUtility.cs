using UnityEditor;
using UnityEngine;

namespace WitShells.ParticlesPresets
{
    internal static class TextParticlePresetUtility
    {
        public static void ApplyToParticleSystem(ParticleSystem ps, string text, float lifetime, Vector3 direction, float spawnInterval, GameObject textPrefab)
        {
            if (ps == null)
                return;

            if (lifetime <= 0f)
                lifetime = 2f;
            if (spawnInterval <= 0f)
                spawnInterval = 0.5f;
            if (direction == Vector3.zero)
                direction = Vector3.up;

            ConfigureParticleSystem(ps, lifetime, direction, spawnInterval);

            // If a prefab is provided, try to use its mesh/material as the particle visual
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
            {
                renderer = ps.gameObject.AddComponent<ParticleSystemRenderer>();
            }

            if (textPrefab != null)
            {
                var prefabMeshFilter = textPrefab.GetComponentInChildren<MeshFilter>();
                var prefabRenderer = textPrefab.GetComponentInChildren<Renderer>();

                if (prefabMeshFilter != null)
                {
                    renderer.renderMode = ParticleSystemRenderMode.Mesh;
                    renderer.mesh = prefabMeshFilter.sharedMesh;
                }

                if (prefabRenderer != null)
                {
                    renderer.sharedMaterial = prefabRenderer.sharedMaterial;
                }
            }
            else
            {
                // Fallback: attach a TextMesh as a child so at least one moving text object exists
                var textTransform = ps.transform.Find("Text");
                TextMesh textMesh;
                if (textTransform == null)
                {
                    var textGo = new GameObject("Text");
                    textGo.transform.SetParent(ps.transform, false);
                    textMesh = textGo.AddComponent<TextMesh>();
                }
                else
                {
                    textMesh = textTransform.GetComponent<TextMesh>();
                    if (textMesh == null)
                        textMesh = textTransform.gameObject.AddComponent<TextMesh>();
                }

                textMesh.text = text;
                textMesh.fontSize = 48;
                textMesh.color = Color.white;
                textMesh.anchor = TextAnchor.MiddleCenter;
            }
        }

        public static GameObject CreateTextParticlePrefab(string assetPath, string text, float lifetime, Vector3 direction, float spawnInterval, GameObject textPrefab)
        {
            if (lifetime <= 0f)
                lifetime = 2f;
            if (spawnInterval <= 0f)
                spawnInterval = 0.5f;
            if (direction == Vector3.zero)
                direction = Vector3.up;

            var root = new GameObject("TextParticle");

            var ps = root.AddComponent<ParticleSystem>();
            ConfigureParticleSystem(ps, lifetime, direction, spawnInterval);

            var renderer = ps.gameObject.AddComponent<ParticleSystemRenderer>();

            if (textPrefab != null)
            {
                // Use the prefab as the visual representation for each particle
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(textPrefab);
                instance.name = "TextPrefabVisual";
                instance.transform.SetParent(root.transform, false);

                var prefabMeshFilter = instance.GetComponentInChildren<MeshFilter>();
                var prefabRenderer = instance.GetComponentInChildren<Renderer>();

                if (prefabMeshFilter != null)
                {
                    renderer.renderMode = ParticleSystemRenderMode.Mesh;
                    renderer.mesh = prefabMeshFilter.sharedMesh;
                }

                if (prefabRenderer != null)
                {
                    renderer.sharedMaterial = prefabRenderer.sharedMaterial;
                }
            }
            else
            {
                // Fallback: simple TextMesh child so there's at least one visible moving text object
                var textGo = new GameObject("Text");
                textGo.transform.SetParent(root.transform, false);
                var textMesh = textGo.AddComponent<TextMesh>();
                textMesh.text = text;
                textMesh.fontSize = 48;
                textMesh.color = Color.white;
                textMesh.anchor = TextAnchor.MiddleCenter;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath, out var success);
            Object.DestroyImmediate(root);

            if (!success)
            {
                Debug.LogError("[TextParticlePreset] Failed to save prefab at " + assetPath);
                return null;
            }

            Debug.Log("[TextParticlePreset] Created text particle prefab at " + assetPath);
            return prefab;
        }

        private static void ConfigureParticleSystem(ParticleSystem ps, float lifetime, Vector3 direction, float spawnInterval)
        {
            direction.Normalize();

            var main = ps.main;
            main.loop = true;
            main.duration = lifetime + 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.8f, lifetime * 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            main.gravityModifier = 0f;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(1f / spawnInterval);

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 0.5f;
            shape.radius = 0.01f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(direction.x * 0.5f, direction.x * 1f);
            velocity.y = new ParticleSystem.MinMaxCurve(direction.y * 0.5f, direction.y * 1f);
            velocity.z = new ParticleSystem.MinMaxCurve(direction.z * 0.5f, direction.z * 1f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.1f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.6f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0.8f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var collision = ps.collision;
            collision.enabled = false;

            var lights = ps.lights;
            lights.enabled = false;

            var trails = ps.trails;
            trails.enabled = false;

            var noise = ps.noise;
            noise.enabled = false;

            var subEmitters = ps.subEmitters;
            subEmitters.enabled = false;
        }
    }
}
