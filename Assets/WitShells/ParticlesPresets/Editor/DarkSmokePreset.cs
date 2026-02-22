using UnityEngine;

namespace WitShells.ParticlesPresets
{
    public static class DarkSmokePreset
    {
        public static void Configure(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop = true;
            main.duration = 3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            main.gravityModifier = 0f;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(15f);
            emission.rateOverDistance = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 0.5f;
            shape.radius = 0.05f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            // Use the same curve mode (TwoConstants) on all axes to avoid Unity warnings
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.15f, 0.15f, 0.15f), 0f),
                    new GradientColorKey(new Color(0.1f, 0.1f, 0.1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.8f, 0.2f),
                    new GradientAlphaKey(0.4f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.4f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 1.2f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

            var collision = ps.collision;
            collision.enabled = false;

            var trigger = ps.trigger;
            trigger.enabled = false;

            var lights = ps.lights;
            lights.enabled = false;

            var trails = ps.trails;
            trails.enabled = false;

            var noise = ps.noise;
            noise.enabled = false;

            var subEmitters = ps.subEmitters;
            subEmitters.enabled = false;
        }

        public static void ConfigureRenderer(ParticleSystemRenderer renderer)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingFudge = 0.5f;
            renderer.minParticleSize = 0.1f;
            renderer.maxParticleSize = 0.7f;
        }
    }
}
