using UnityEngine;

namespace WitShells.ParticlesPresets
{
    public static class AmberDustMotesPreset
    {
        public static void Configure(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop = true;
            main.duration = 6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.06f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 250;
            main.gravityModifier = -0.005f; // near-weightless, barely float upward
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(22f);
            emission.rateOverDistance = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(5f, 4f, 5f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.07f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f,   0.85f, 0.35f), 0f),   // bright amber
                    new GradientColorKey(new Color(1f,   0.6f,  0.1f),  0.35f), // deep amber-orange
                    new GradientColorKey(new Color(0.95f, 0.78f, 0.3f), 0.65f), // warm gold
                    new GradientColorKey(new Color(1f,   0.92f, 0.55f), 1f)    // pale golden white
                },
                new[]
                {
                    new GradientAlphaKey(0f,    0f),
                    new GradientAlphaKey(0.75f, 0.1f),
                    new GradientAlphaKey(0.6f,  0.7f),
                    new GradientAlphaKey(0f,    1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f,    0.0f),
                new Keyframe(0.1f,  1f),
                new Keyframe(0.85f, 0.8f),
                new Keyframe(1f,    0.0f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.12f;
            noise.scrollSpeed = 0.04f;
            noise.octaveCount = 3;
            noise.octaveMultiplier = 0.5f;
            noise.octaveScale = 2f;
            noise.damping = true;

            var lights = ps.lights;
            lights.enabled = false;

            var trails = ps.trails;
            trails.enabled = false;

            var collision = ps.collision;
            collision.enabled = false;

            var subEmitters = ps.subEmitters;
            subEmitters.enabled = false;
        }

        public static void ConfigureRenderer(ParticleSystemRenderer renderer)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingFudge = 0.3f;
            renderer.minParticleSize = 0.004f;
            renderer.maxParticleSize = 0.08f;
        }
    }
}
