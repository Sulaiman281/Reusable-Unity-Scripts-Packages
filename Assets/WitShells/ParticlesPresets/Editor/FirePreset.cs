using UnityEngine;

namespace WitShells.ParticlesPresets
{
    public static class FirePreset
    {
        public static void Configure(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop = true;
            main.duration = 2.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 150;
            main.gravityModifier = 0f;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(25f);
            emission.rateOverDistance = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.1f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f),   // bright yellow
                    new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.4f), // orange
                    new GradientColorKey(new Color(0.5f, 0.1f, 0.05f), 1f) // dark red
                },
                new[]
                {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0.9f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.4f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0.6f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var noise = ps.noise;
            noise.enabled = true;
            // Softer noise for smoother fire motion
            noise.strength = 0.15f;
            noise.frequency = 0.2f;
            noise.scrollSpeed = 0.15f;

            var collision = ps.collision;
            collision.enabled = false;

            var lights = ps.lights;
            lights.enabled = false;

            var trails = ps.trails;
            trails.enabled = false;

            var subEmitters = ps.subEmitters;
            subEmitters.enabled = false;
        }

        public static void ConfigureRenderer(ParticleSystemRenderer renderer)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingFudge = 0.5f;
            renderer.minParticleSize = 0.05f;
            renderer.maxParticleSize = 0.5f;
        }
    }
}
