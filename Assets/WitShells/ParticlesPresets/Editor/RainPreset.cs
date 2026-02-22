using UnityEngine;

namespace WitShells.ParticlesPresets
{
    public static class RainPreset
    {
        public static void Configure(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 250;
            main.gravityModifier = 0f;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(140f);
            emission.rateOverDistance = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8f, 0.1f, 8f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            // Straight downward motion, much softer so it feels like light rain
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(-3f, -5f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.7f, 0.8f, 1f), 0f),
                    new GradientColorKey(new Color(0.6f, 0.7f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.9f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(1f, 1f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var collision = ps.collision;
            collision.enabled = false; // enable if you need splashes (more expensive)

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
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2f;
            renderer.velocityScale = 0.5f;
            renderer.minParticleSize = 0.02f;
            renderer.maxParticleSize = 0.08f;
        }
    }
}
