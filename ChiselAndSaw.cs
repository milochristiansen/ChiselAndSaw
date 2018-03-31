
using System.IO;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace ChiselAndSaw {
	public class ChiselAndSaw : ModBase {
		public override void Start(ICoreAPI api) {
			api.RegisterItemClass("ItemChisel", typeof(ItemChisel));
			api.RegisterBlockClass("BlockChisel", typeof(BockChisel));
			api.RegisterBlockEntityClass("BlockEntityChisel", typeof(BlockEntityChisel));
		}
	}

	public class ItemChisel : Item {
		public override bool OnHeldAttackStart(IItemSlot slot, IEntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
			if (blockSel == null) {
				return base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel);
			}

			Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
			Block chiseledblock = byEntity.World.GetBlock(new AssetLocation("chiselandsaw:chiseledblock"));

			if (block == chiseledblock) {
				IPlayer byPlayer = null;
				if (byEntity is IEntityPlayer) {
					byPlayer = byEntity.World.PlayerByUid(((IEntityPlayer)byEntity).PlayerUID);
				}
				if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) {
					DamageItem(byEntity.World, byEntity, slot);
				}
				return OnBlockInteract(byEntity.World, byPlayer, blockSel, true);
			}

			return false;
		}

		public override bool OnHeldInteractStart(IItemSlot slot, IEntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
			if (blockSel == null) {
				return base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel);
			}

			Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
			Block chiseledblock = byEntity.World.GetBlock(new AssetLocation("chiselandsaw:chiseledblock"));

			if (block == chiseledblock) {
				IPlayer byPlayer = null;
				if (byEntity is IEntityPlayer) {
					byPlayer = byEntity.World.PlayerByUid(((IEntityPlayer)byEntity).PlayerUID);
				}

				if (byPlayer.Entity.Controls.Sneak) {
					var b = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChisel;
					if (b == null) {
						return false;
					}
					b.SetSelSize(0);
					return true;
				} else if (byPlayer.Entity.Controls.Sprint) {
					var b = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChisel;
					if (b == null) {
						return false;
					}
					b.Rotate(true);
					return true;
				}
				if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) {
					DamageItem(byEntity.World, byEntity, slot);
				}
				return OnBlockInteract(byEntity.World, byPlayer, blockSel, false);
			}

			if (block.DrawType != Vintagestory.API.Client.EnumDrawType.Cube) {
				return false;
			}

			byEntity.World.BlockAccessor.SetBlock(chiseledblock.BlockId, blockSel.Position);

			BlockEntityChisel be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChisel;
			if (be == null) {
				return false;
			}

			be.WasPlaced(block);
			return true;
		}

		public bool OnBlockInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, bool isBreak) {
			BlockEntityChisel bec = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChisel;
			if (bec != null) {
				if (!isBreak && bec.Model.IsFullBlock()) {
					world.BlockAccessor.SetBlock((ushort)bec.Model.SrcBlock.Id, blockSel.Position);
					return true;
				}

				bec.OnBlockInteract(byPlayer, blockSel, isBreak);
				return true;
			}
			return false;
		}
	}

	public class BockChisel : Block {
		public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) {
			return true;
		}

		public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
			BlockEntityChisel bec = blockAccessor.GetBlockEntity(pos) as BlockEntityChisel;
			if (bec != null) {
				Cuboidf[] selectionBoxes = bec.GetSelectionBoxes(blockAccessor, pos);

				return selectionBoxes;
			}
			return base.GetSelectionBoxes(blockAccessor, pos);
		}

		public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos) {
			BlockEntityChisel bec = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityChisel;
			if (bec == null) {
				return null;
			}
			var tree = new TreeAttribute();
			bec.ToTreeAttributes(tree);
			return new ItemStack(this.Id, EnumItemClass.Block, 1, tree, world);
		}

		public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
			if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)) {
				ItemStack[] drops = new ItemStack[] { OnPickBlock(world, pos) };

				if (drops != null) {
					for (int i = 0; i < drops.Length; i++) {
						world.SpawnItemEntity(drops[i], new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
					}
				}

				if (Sounds != null && Sounds.Break != null) {
					world.PlaySoundAt(Sounds.Break, pos.X, pos.Y, pos.Z, byPlayer);
				}
			}

			world.BlockAccessor.SetBlock(0, pos);
		}

		public override void DoPlaceBlock(IWorldAccessor world, BlockPos pos, BlockFacing onBlockFace, ItemStack byItemStack) {
			base.DoPlaceBlock(world, pos, onBlockFace, byItemStack);

			BlockEntityChisel be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityChisel;
			if (be != null) {
				byItemStack.Attributes.SetInt("posx", pos.X);
				byItemStack.Attributes.SetInt("posy", pos.Y);
				byItemStack.Attributes.SetInt("posz", pos.Z);

				be.FromTreeAtributes(byItemStack.Attributes, world);
			}
		}

		public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
			var mesh = ModelCache.Get(itemstack.Attributes.GetInt("meshid"), capi.Render, capi);
			if (mesh == null) {
				var tdata = VoxelModel.GetTreeData(itemstack.Attributes);
				var block = capi.World.GetBlock(tdata.Item2);
				var mdata = VoxelMesher.GenInventoryMesh(tdata.Item1, capi, block);
				mesh = capi.Render.UploadMesh(mdata);
				int mid = ModelCache.New(mesh, capi.Render, capi);
				itemstack.Attributes.SetInt("meshid", mid);
			}
			renderinfo.ModelRef = mesh;
		}

		public override int TextureSubIdForRandomBlockPixel(IWorldAccessor world, BlockPos pos, BlockFacing facing, ref int tintIndex) {
			BlockEntityChisel be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityChisel;
			if (be != null && be.Model != null && be.Model.SrcBlock != null) {
				return be.Model.SrcBlock.TextureSubIdForRandomBlockPixel(world, pos, facing, ref tintIndex);
			}
			return base.TextureSubIdForRandomBlockPixel(world, pos, facing, ref tintIndex);
		}

		public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
			return GetSelectionBoxes(blockAccessor, pos);
		}
	}

	public class BlockEntityChisel : BlockEntity, IBlockShapeSupplier {
		public VoxelModel Model = null;
		Cuboidf[] selectionBoxes = new Cuboidf[0];
		int selectionSize = 8;

		public override void Initialize(ICoreAPI api) {
			base.Initialize(api);

			if (Model != null) {
				RegenSelectionBoxes();
				MarkDirty(true);
			}
		}

		public void SetSelSize(int mode) {
			switch (mode) {
				case 0:
					if (selectionSize == 1) {
						selectionSize = 8;
					} else {
						selectionSize = selectionSize / 2;
					}
					break;
				case 2:
					selectionSize = 2;
					break;
				case 4:
					selectionSize = 4;
					break;
				case 8:
					selectionSize = 8;
					break;
				default:
					selectionSize = 1;
					break;
			}
			RegenSelectionBoxes();
			MarkDirty(true);
		}

		internal void WasPlaced(Block block) {
			if (Model == null) {
				Model = new VoxelModel(block);
			}
			Model.SrcBlock = block;
			RegenSelectionBoxes();
			MarkDirty(true);
		}

		internal void OnBlockInteract(IPlayer byPlayer, BlockSelection blockSel, bool isBreak) {
			if (api.World.Side == EnumAppSide.Client) {
				Cuboidf box = selectionBoxes[blockSel.SelectionBoxIndex];
				Vec3i voxelPos = new Vec3i((int)(16 * box.X1), (int)(16 * box.Y1), (int)(16 * box.Z1));

				UpdateVoxels(byPlayer, voxelPos, blockSel.Face, isBreak);
			}
		}

		internal void UpdateVoxels(IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool isBreak) {
			Vec3i addAtPos = voxelPos.Clone().Add(facing);

			var run = selectionSize - 1;
			if (!isBreak) {
				// Ugly.
				if (addAtPos.X > voxelPos.X) addAtPos.X += run;
				if (addAtPos.X < voxelPos.X) addAtPos.X -= run;
				if (addAtPos.Y > voxelPos.Y) addAtPos.Y += run;
				if (addAtPos.Y < voxelPos.Y) addAtPos.Y -= run;
				if (addAtPos.Z > voxelPos.Z) addAtPos.Z += run;
				if (addAtPos.Z < voxelPos.Z) addAtPos.Z -= run;
				Model.SetVoxels(addAtPos, addAtPos.Clone().Add(run, run, run), true);
			} else if (!Model.LastVoxel(voxelPos, voxelPos.Clone().Add(run, run, run))) {
				Model.SetVoxels(voxelPos, voxelPos.Clone().Add(run, run, run), false);
			} else {
				return;
			}

			RegenSelectionBoxes();
			MarkDirty(true);

			// Send a custom network packet for server side, because
			// serverside blockselection index is inaccurate
			if (api.Side == EnumAppSide.Client) {
				SendUseOverPacket(byPlayer, voxelPos, facing, isBreak);
			}
		}

		public void Rotate(bool right) {
			Model.Rotate(right);
			RegenSelectionBoxes();
			MarkDirty(true);
		}

		public void SendUseOverPacket(IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool isBreak) {
			byte[] data;

			using (MemoryStream ms = new MemoryStream()) {
				BinaryWriter writer = new BinaryWriter(ms);
				writer.Write(voxelPos.X);
				writer.Write(voxelPos.Y);
				writer.Write(voxelPos.Z);
				writer.Write(isBreak);
				writer.Write((ushort)facing.Index);
				data = ms.ToArray();
			}

			((ICoreClientAPI)api).Network.SendBlockEntityPacket(
				pos.X, pos.Y, pos.Z,
				(int)1000,
				data
			);
		}

		public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data) {
			if (packetid == 1000) {
				Vec3i voxelPos;
				bool isBreak;
				BlockFacing facing;
				using (MemoryStream ms = new MemoryStream(data)) {
					BinaryReader reader = new BinaryReader(ms);
					voxelPos = new Vec3i(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
					isBreak = reader.ReadBoolean();
					facing = BlockFacing.ALLFACES[reader.ReadInt16()];
				}

				UpdateVoxels(player, voxelPos, facing, isBreak);
			}
		}

		internal Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos) {
			return selectionBoxes;
		}

		public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
			base.FromTreeAtributes(tree, worldAccessForResolve);
			Model = new VoxelModel(tree, worldAccessForResolve);
			RegenSelectionBoxes();
		}

		public override void ToTreeAttributes(ITreeAttribute tree) {
			base.ToTreeAttributes(tree);
			Model.Serialize(tree);
		}

		public bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
			if (Model == null) return false;

			ICoreClientAPI capi = api as ICoreClientAPI;
			mesher.AddMeshData(Model.GetBlockMesh(capi));
			return true;
		}

		public void RegenSelectionBoxes() {
			List<Cuboidf> boxes = new List<Cuboidf>();

			for (int x = 0; x < 16; x += selectionSize) {
				for (int y = 0; y < 16; y += selectionSize) {
					for (int z = 0; z < 16; z += selectionSize) {
						if (Model.VoxelIn(x, y, z, x + selectionSize - 1, y + selectionSize - 1, z + selectionSize - 1)) {
							boxes.Add(new Cuboidf(x / 16f, y / 16f, z / 16f,
								x / 16f + selectionSize / 16f, y / 16f + selectionSize / 16f, z / 16f + selectionSize / 16f));
						}
					}
				}
			}

			selectionBoxes = boxes.ToArray();
		}
	}
}
