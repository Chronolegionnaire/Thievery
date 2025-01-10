using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Thievery.LockpickAndTensionWrench
{
    public class LockpickHudElement : IRenderer, IDisposable
    {
        private const float CircleAlphaIn = 0.8F;
        private const float CircleAlphaOut = 0.8F;
        private const int CircleMaxSteps = 32;
        private const float OuterRadius = 24f;
        private const float InnerRadius = 18f;

        private const float DrainSpeed = 2f; // Speed at which progress drains to 0
        private const float NoProgressTimeout = 1f; // Time in seconds before draining starts

        private MeshRef circleMesh = null;
        private ICoreClientAPI api;
        private float circleAlpha = 0.0F;
        private float circleProgress = 0.0F;
        private float targetCircleProgress = 0.0F;

        private float timeSinceLastProgressUpdate = 0.0F; // Tracks how long since progress was last updated
        private bool isDraining = false;

        public bool CircleVisible { get; set; }

        public float CircleProgress
        {
            get => targetCircleProgress;
            set
            {
                targetCircleProgress = GameMath.Clamp(value, 0.0F, 1.0F);

                if (targetCircleProgress > 0.0F)
                {
                    CircleVisible = true;
                    isDraining = false;
                    timeSinceLastProgressUpdate = 0.0F; // Reset timeout when progress updates
                }
                else
                {
                    CircleVisible = false;
                }
            }
        }

        public LockpickHudElement(ICoreClientAPI api)
        {
            this.api = api;
            api.Event.RegisterRenderer(this, EnumRenderStage.Ortho, "lockpickoverlay");
            UpdateCircleMesh(1);
        }

        private void UpdateCircleMesh(float progress)
        {
            const float ringSize = InnerRadius / OuterRadius;
            const float stepSize = 1.0F / CircleMaxSteps;

            int steps = Math.Max(1, 1 + (int)Math.Ceiling(Math.Min(CircleMaxSteps * progress, int.MaxValue / CircleMaxSteps)));

            int vertexCapacity = steps * 2;
            int indexCapacity = steps * 6;

            var data = new MeshData(vertexCapacity, indexCapacity, withUv: true, withRgba: false, withFlags: false);

            for (int i = 0; i < steps; i++)
            {
                var p = Math.Min(progress, i * stepSize) * Math.PI * 2;
                var x = (float)Math.Sin(p);
                var y = -(float)Math.Cos(p);

                data.AddVertex(x, y, 0, 0, 0);
                data.AddVertex(x * ringSize, y * ringSize, 0, 0, 0);

                if (i > 0)
                {
                    data.AddIndices(new[] { (i * 2) - 2, (i * 2) - 1, (i * 2) });
                    data.AddIndices(new[] { (i * 2), (i * 2) - 1, (i * 2) + 1 });
                }
            }

            if (progress == 1.0f)
            {
                data.AddIndices(new[] { (steps * 2) - 2, (steps * 2) - 1, 0 });
                data.AddIndices(new[] { 0, (steps * 2) - 1, 1 });
            }

            if (circleMesh != null)
            {
                api.Render.UpdateMesh(circleMesh, data);
            }
            else
            {
                circleMesh = api.Render.UploadMesh(data);
            }
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (CircleVisible)
            {
                circleAlpha = Math.Min(1.0F, circleAlpha + (deltaTime * CircleAlphaIn));

                float smoothingSpeed = 5f;

                // If we're not draining, smoothly update progress
                if (!isDraining)
                {
                    circleProgress = GameMath.Lerp(circleProgress, targetCircleProgress, deltaTime * smoothingSpeed);

                    if (Math.Abs(circleProgress - targetCircleProgress) < 0.01f)
                    {
                        circleProgress = targetCircleProgress;
                    }

                    // Start draining if no progress update for timeout duration
                    timeSinceLastProgressUpdate += deltaTime;
                    if (timeSinceLastProgressUpdate >= NoProgressTimeout)
                    {
                        isDraining = true;
                    }
                }
                else
                {
                    // Drain progress to 0
                    circleProgress = Math.Max(0.0F, circleProgress - (deltaTime * DrainSpeed));
                    if (circleProgress <= 0.0F)
                    {
                        CircleVisible = false; // Hide the element when progress is fully drained
                        targetCircleProgress = 0.0F;
                        isDraining = false;
                    }
                }
            }
            else if (circleAlpha > 0.0F)
            {
                circleAlpha = Math.Max(0.0F, circleAlpha - (deltaTime * CircleAlphaOut));
            }

            if (circleAlpha <= 0.0F && !CircleVisible)
            {
                circleProgress = 0.0F;
                targetCircleProgress = 0.0F;
            }

            if (circleAlpha > 0.0F)
            {
                UpdateCircleMesh(circleProgress);
            }

            if (circleMesh != null)
            {
                Vec4f color = GetColorFromProgress(circleProgress);

                IRenderAPI render = api.Render;
                IShaderProgram shader = render.CurrentActiveShader;

                shader.Uniform("rgbaIn", color);
                shader.Uniform("extraGlow", 0);
                shader.Uniform("applyColor", 0);
                shader.Uniform("tex2d", 0);
                shader.Uniform("noTexture", 1.0F);
                shader.UniformMatrix("projectionMatrix", render.CurrentProjectionMatrix);

                int x, y;
                if (api.Input.MouseGrabbed)
                {
                    x = api.Render.FrameWidth / 2;
                    y = api.Render.FrameHeight / 2;
                }
                else
                {
                    x = api.Input.MouseX;
                    y = api.Input.MouseY;
                }

                render.GlPushMatrix();
                render.GlTranslate(x, y, 0);
                render.GlScale(OuterRadius, OuterRadius, 0);
                shader.UniformMatrix("modelViewMatrix", render.CurrentModelviewMatrix);
                render.GlPopMatrix();

                render.RenderMesh(circleMesh);
            }
        }

        private Vec4f GetColorFromProgress(float progress)
        {
            float r = progress < 0.5f ? 1.0f : 1.0f - ((progress - 0.5f) * 2.0f);
            float g = progress < 0.5f ? progress * 2.0f : 1.0f;
            float b = 0.0f;

            return new Vec4f(r, g, b, circleAlpha);
        }

        public void Dispose()
        {
            if (circleMesh != null)
            {
                api.Render.DeleteMesh(circleMesh);
                circleMesh = null;
            }
        }

        public double RenderOrder => 0.0;
        public int RenderRange => 1000;
    }
}