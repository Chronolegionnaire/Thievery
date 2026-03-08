using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Thievery.LockAndKey;

public class BlockEntityKeyMold : BlockEntityToolMold
  {
    private string keyUID;
    private string keyName;
    private ITreeAttribute attributes = new TreeAttribute();
    private ICoreClientAPI localCapi;
    private IClientNetworkChannel clientChannel;
    public override void Initialize(ICoreAPI api)
    {
      base.Initialize(api);

      if (api == null || !(api is ICoreServerAPI coreServerApi))
      {
        return;
      }
      if (this.Block == null || this.Block.Code == null || this.Block.Attributes == null)
      {
        return;
      }
      if (attributes.HasAttribute("keyUID") && attributes.HasAttribute("keyName") 
                                            && !string.IsNullOrEmpty(attributes.GetString("keyUID")) 
                                            && !string.IsNullOrEmpty(attributes.GetString("keyName")))
      {
      }
      else
      {
        if (coreServerApi.World.AllPlayers.Length > 0)
        {
          IPlayer player = coreServerApi.World.AllPlayers[0];
          if (player != null && player.InventoryManager != null)
          {
            ItemSlot activeSlot = player.InventoryManager.ActiveHotbarSlot;
            if (activeSlot?.Itemstack != null)
            {
              keyUID = activeSlot.Itemstack.Attributes.GetString("keyUID");
              keyName = activeSlot.Itemstack.Attributes.GetString("keyName");
              if (!string.IsNullOrEmpty(keyUID) && !string.IsNullOrEmpty(keyName))
              {
                UpdateAttributes(keyUID, keyName);
                MarkDirty();
              }
            }
          }
        }
      }
      this.MetalContent?.ResolveBlockOrItem(this.Api.World);
      this.fillHeight = this.Block.Attributes["fillHeight"].AsFloat(1f);
      this.requiredUnits = this.Block.Attributes["requiredUnits"].AsInt(100);
      if (this.Block.Attributes["fillQuadsByLevel"].Exists)
      {
        this.fillQuadsByLevel = this.Block.Attributes["fillQuadsByLevel"].AsObject<Cuboidf[]>();
      }

      if (this.fillQuadsByLevel == null)
      {
        this.fillQuadsByLevel = new Cuboidf[1]
        {
          new Cuboidf(2f, 0.0f, 2f, 14f, 0.0f, 14f)
        };
      }
      this.localCapi = api as ICoreClientAPI;
      if (this.localCapi != null && !this.Shattered)
      {
        this.localCapi.Event.RegisterRenderer(
          new ToolMoldRenderer(this, this.localCapi, this.fillQuadsByLevel),
          EnumRenderStage.Opaque,
          "toolmoldrenderer"
        );
        this.UpdateRenderer();
      }
      if (!this.Shattered)
      {
        this.RegisterGameTickListener(this.OnGameTick, 50);
      }
    }
    public void UpdateAttributes(string newKeyUID, string newKeyName)
    {
      if (keyUID != newKeyUID || keyName != newKeyName)
      {
        keyUID = newKeyUID;
        keyName = newKeyName;
        attributes.SetString("keyUID", keyUID);
        attributes.SetString("keyName", keyName);
        if (Api?.Side == EnumAppSide.Client)
        {
          SyncAttributesToServer();
        }
        base.MarkDirty();
      }
    }

    private void SyncAttributesToServer()
    {
      if (clientChannel != null)
      {
        var packet = new SyncKeyAttributesPacket
        {
          BlockPos = Pos,
          KeyUID = keyUID,
          KeyName = keyName
        };

        clientChannel.SendPacket(packet);
      }
    }

    private void OnGameTick(float dt)
    {
      if (this.renderer != null)
        this.renderer.Level = (float) this.FillLevel * this.fillHeight / (float) this.requiredUnits;
      if (this.MetalContent == null || this.renderer == null)
        return;
      this.renderer.stack = this.MetalContent;
      this.renderer.Temperature = Math.Min(1300f, this.MetalContent.Collectible.GetTemperature(this.Api.World, this.MetalContent));
    }
    protected override bool TryTakeContents(IPlayer byPlayer)
    {
      if (this.Shattered)
        return false;
      if (this.Api is ICoreServerAPI)
        this.MarkDirty();
      if (this.MetalContent == null || this.FillLevel < this.requiredUnits || !this.IsHardened)
        return false;
      this.Api.World.PlaySoundAt(new AssetLocation("game:sounds/block/ingot"), this.Pos, -0.5, byPlayer, false);
      if (this.Api is ICoreServerAPI)
      {
        ItemStack[] awareMoldedStacks = this.GetStateAwareMoldedStacks();
        if (awareMoldedStacks != null)
        {
          foreach (ItemStack itemstack in awareMoldedStacks)
          {
            itemstack.Attributes.SetString("keyUID", this.keyUID);
            itemstack.Attributes.SetString("keyName", this.keyName);
            if (!byPlayer.InventoryManager.TryGiveItemstack(itemstack))
            {
              this.Api.World.SpawnItemEntity(itemstack, this.Pos.ToVec3d().Add(0.5, 0.2, 0.5));
            }
          }
        }
        this.MetalContent = null;
        this.FillLevel = 0;
      }
      this.UpdateRenderer();
      return true;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
    {
      base.FromTreeAttributes(tree, worldForResolve);
      this.MetalContent = tree.GetItemstack("contents");
      this.FillLevel = tree.GetInt("fillLevel");
      this.Shattered = tree.GetBool("shattered");
      this.keyUID = tree.GetString("keyUID");
      this.keyName = tree.GetString("keyName");
      if (this.Api?.World != null && this.MetalContent != null)
        this.MetalContent.ResolveBlockOrItem(this.Api.World);
      this.UpdateRenderer();
      ICoreAPI api = this.Api;
      if ((api != null ? (api.Side == EnumAppSide.Client ? 1 : 0) : 0) == 0)
        return;
      this.Api.World.BlockAccessor.MarkBlockDirty(this.Pos);
    }
    public override void OnBlockBroken(IPlayer byPlayer = null)
    {
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
      base.ToTreeAttributes(tree);
      tree.SetItemstack("contents", this.MetalContent);
      tree.SetInt("fillLevel", this.FillLevel);
      tree.SetBool("shattered", this.Shattered);
      tree.SetString("keyUID", keyUID);
      tree.SetString("keyName", keyName);
    }
  }