using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Thievery.LockpickAndTensionWrench
{
    public class ProgressHudElement : IRenderer, IDisposable
    {
        private const float CircleAlphaIn = 0.8F;
        private const float CircleAlphaOut = 0.8F;
        private const int CircleMaxSteps = 32;
        private const float OuterRadius = 24f;
        private const float InnerRadius = 18f;

        private const float DrainSpeed = 2f;
        private const float NoProgressTimeout = 1f;

        private MeshRef circleMesh = null;
        private ICoreClientAPI api;
        private float circleAlpha = 0.0F;
        private float circleProgress = 0.0F;
        private float targetCircleProgress = 0.0F;

        private float timeSinceLastProgressUpdate = 0.0F;
        private bool isDraining = false;

        public bool CircleVisible { get; set; }

        public float CircleProgress
        {
            get => targetCircleProgress;
            set
            {
                targetCircleProgress = GameMath.Clamp(value, 0f, 1f);

                if (targetCircleProgress > 0f && CircleVisible)
                {
                    isDraining = false;
                    timeSinceLastProgressUpdate = 0f;
                }

                if (targetCircleProgress <= 0f)
                {
                    CircleVisible = false;
                }
            }
        }


        public ProgressHudElement(ICoreClientAPI api)
        {
            this.api = api;
            api.Event.RegisterRenderer(this, EnumRenderStage.Ortho, "pickoverlay");
            UpdateCircleMesh(1);
        }

        private void UpdateCircleMesh(float progress)
        {
            const float ringSize = InnerRadius / OuterRadius;
            progress = GameMath.Clamp(progress, 0f, 1f);

            int steps = Math.Max(2, (int)Math.Ceiling(CircleMaxSteps * Math.Max(progress, 0.001f))) + 1;

            var data = new MeshData(steps * 2, steps * 6, withUv: true, withRgba: false, withFlags: false);

            for (int i = 0; i < steps; i++)
            {
                float t = (i / (float)(steps - 1)) * progress * GameMath.TWOPI;
                float x = GameMath.Sin(t);
                float y = -GameMath.Cos(t);

                data.AddVertex(x, y, 0, 0, 0);
                data.AddVertex(x * ringSize, y * ringSize, 0, 0, 0);

                if (i > 0)
                {
                    int o = i * 2;
                    int a = o - 2, b = o - 1, c = o, d = o + 1;

                    data.AddIndices(a, b, c,  c, b, d);
                }
            }

            if (progress >= 0.999f)
            {
                int o = steps * 2;
                int a = o - 2, b = o - 1, c = 0, d = 1;

                data.AddIndices(a, b, c,  c, b, d);
            }

            if (circleMesh != null) api.Render.UpdateMesh(circleMesh, data);
            else circleMesh = api.Render.UploadMesh(data);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (CircleVisible)
            {
                circleAlpha = Math.Min(1.0F, circleAlpha + (deltaTime * CircleAlphaIn));

                float smoothingSpeed = 5f;

                if (!isDraining)
                {
                    if (targetCircleProgress >= 0.999f)
                    {
                        circleProgress = 1f;
                    }
                    else
                    {
                        circleProgress = GameMath.Lerp(circleProgress, targetCircleProgress, deltaTime * smoothingSpeed);
                        if (Math.Abs(circleProgress - targetCircleProgress) < 0.01f) circleProgress = targetCircleProgress;
                    }

                    timeSinceLastProgressUpdate += deltaTime;
                    if (timeSinceLastProgressUpdate >= NoProgressTimeout) isDraining = true;
                }
                else
                {
                    circleProgress = Math.Max(0.0F, circleProgress - (deltaTime * DrainSpeed));
                    if (circleProgress <= 0.0F)
                    {
                        CircleVisible = false;
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
            api.Event.UnregisterRenderer(this, EnumRenderStage.Ortho);
    
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