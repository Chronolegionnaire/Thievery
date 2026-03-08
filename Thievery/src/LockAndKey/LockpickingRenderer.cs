using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Thievery.LockpickAndTensionWrench
{
    public class SingleMeshGuiRenderer : IRenderer, IDisposable
    {
        private readonly ICoreClientAPI capi;
        private readonly MultiTextureMeshRef? mesh;
        private readonly bool ownsMesh;
        private readonly ItemSlot? fallback;

        public const string SamplerName = "tex";
        public float OffX { get; set; }
        public float OffY { get; set; }
        public float ZLift { get; set; }
        public float Scale { get; set; }
        public float YawDeg { get; set; }
        public float PitchDeg { get; set; }
        public float RollDeg { get; set; }

        public Vec3f? AmbientOverride { get; set; }
        public Vec3f Pivot { get; }
        public Vec3f Center { get; } = new Vec3f(0.5f, 0.5f, 0.5f);
        
        public bool SwayEnabled { get; set; } = false;
        public float SwayAmpDeg { get; set; } = 3f;
        public float SwaySpeed { get; set; } = 0.8f;
        public Vec3f SwayAxisWeights { get; set; } = new Vec3f(0.4f, 1f, 0f);
        public float SwayPhaseRad { get; set; } = 0f;
        private float swayTime;
        
        private readonly double renderOrder;
        public double RenderOrder => renderOrder;
        public int RenderRange => int.MaxValue;

        public SingleMeshGuiRenderer(
            ICoreClientAPI capi,
            MultiTextureMeshRef? mesh, bool ownsMesh,
            ItemSlot? fallback,
            float offsetX, float offsetY, float zLift, float scale,
            float yawDeg = 0f, float pitchDeg = 0f, float rollDeg = 0f,
            Vec3f? pivot = null,
            double renderOrder = 0.9)
        {
            this.capi = capi;
            this.mesh = mesh;
            this.ownsMesh = ownsMesh;
            this.fallback = fallback;

            OffX = offsetX;
            OffY = offsetY;
            ZLift = zLift;
            Scale = scale;
            YawDeg = yawDeg;
            PitchDeg = pitchDeg;
            RollDeg = rollDeg;
            Pivot = pivot ?? new Vec3f(0f, 0f, 0f);
            this.renderOrder = renderOrder;
        }

        public (float x, float y, float z, float scale, float yaw, float pitch, float roll) Snapshot()
            => (OffX, OffY, ZLift, Scale, YawDeg, PitchDeg, RollDeg);

        public void Nudge(float dx = 0, float dy = 0, float dz = 0, float dscale = 0, float dyaw = 0, float dpitch = 0,
            float droll = 0)
        {
            OffX += dx;
            OffY += dy;
            ZLift += dz;
            Scale += dscale;
            YawDeg += dyaw;
            PitchDeg += dpitch;
            RollDeg += droll;
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (stage != EnumRenderStage.Ortho) return;
            var r = capi.Render;
            var e = capi.World.Player.Entity;
            var pos = e.SidedPos;
            double lx = pos.X;
            double ly = pos.Y + e.LocalEyePos.Y;
            double lz = pos.Z;
            double cx = r.FrameWidth * 0.5, cy = r.FrameHeight * 0.5;

            if (mesh != null)
            {
                var prev = r.CurrentActiveShader;
                try
                {
                    prev?.Stop();
                    var lpos = new BlockPos((int)Math.Floor(lx), (int)Math.Floor(ly), (int)Math.Floor(lz));

                    IStandardShaderProgram prog = r.PreparedStandardShader(lpos.X, lpos.Y, lpos.Z);
                    
                    float yawEff = YawDeg, pitchEff = PitchDeg, rollEff = RollDeg;
                    if (SwayEnabled && (SwayAxisWeights.X != 0 || SwayAxisWeights.Y != 0 || SwayAxisWeights.Z != 0))
                    {
                        swayTime += deltaTime;
                        float sway = SwayAmpDeg * GameMath.Sin(SwaySpeed * swayTime + SwayPhaseRad);
                        pitchEff += SwayAxisWeights.X * sway;
                        yawEff   += SwayAxisWeights.Y * sway;
                        rollEff  += SwayAxisWeights.Z * sway;
                    }

                    var m = new Matrixf().Identity()
                        .Translate(-Center.X, -Center.Y, -Center.Z)
                        .Translate(Pivot.X, Pivot.Y, Pivot.Z)
                        .RotateZ(rollEff  * GameMath.DEG2RAD)
                        .RotateX(pitchEff * GameMath.DEG2RAD)
                        .RotateY(yawEff   * GameMath.DEG2RAD)
                        .Translate(-Pivot.X, -Pivot.Y, -Pivot.Z);
                    prog.ModelMatrix = m.Values;

                    prog.ViewMatrix = new Matrixf()
                        .Identity()
                        .Translate((float)(cx + OffX), (float)(cy + OffY), ZLift)
                        .Scale(Scale, Scale, Scale)
                        .Values;

                    prog.ProjectionMatrix = r.CurrentProjectionMatrix;

                    r.RenderMultiTextureMesh(mesh, SamplerName, 0);
                    prog.Stop();
                }
                finally
                {
                    prev?.Use();
                }
            }
            else if (fallback?.Itemstack != null)
            {
                r.RenderItemstackToGui(fallback, cx + OffX, cy + OffY, 0, 64f, unchecked((int)0xFFFFFFFF), true, true,
                    false);
            }
        }

        public void Dispose()
        {
            if (ownsMesh) mesh?.Dispose();
        }
    }
}