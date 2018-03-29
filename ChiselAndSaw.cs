
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

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
			var mesh = ModelCache.Get(itemstack.Attributes.GetInt("meshid"), capi.Render, capi.World);
			if (mesh == null) {
				var block = capi.World.GetBlock((ushort)itemstack.Attributes.GetInt("blockid"));
				var mdata = VoxelMesher.GenInventoryMesh(itemstack.Attributes.GetBytes("voxels"), capi, block);
				mesh = capi.Render.UploadMesh(mdata);
				int mid = ModelCache.New(mesh, capi.Render, capi.World);
				itemstack.Attributes.SetInt("meshid", mid);
			}
			renderinfo.ModelRef = mesh;
		}

		public override int TextureSubIdForRandomBlockPixel(IWorldAccessor world, BlockPos pos, BlockFacing facing, ref int tintIndex) {
			BlockEntityChisel be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityChisel;
			if (be != null && be.SrcBlock != null) {
				return be.SrcBlock.TextureSubIdForRandomBlockPixel(world, pos, facing, ref tintIndex);
			}
			return base.TextureSubIdForRandomBlockPixel(world, pos, facing, ref tintIndex);
		}

		public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
			return GetSelectionBoxes(blockAccessor, pos);
		}
	}

	public class BlockEntityChisel : BlockEntity, IBlockShapeSupplier {
		public Block SrcBlock;
		BitArray voxels = new BitArray(16 * 16 * 16);
		MeshData mesh;
		Cuboidf[] selectionBoxes = new Cuboidf[0];
		int selectionSize = 8;

		public override void Initialize(ICoreAPI api) {
			base.Initialize(api);

			if (SrcBlock != null) {
				if (api.Side == EnumAppSide.Client) RegenMesh();
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
			this.SrcBlock = block;

			voxels.SetAll(true);

			if (api.Side == EnumAppSide.Client && mesh == null) {
				RegenMesh();
			}
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
			if (!isBreak) {
				// Ugly.
				if (addAtPos.X > voxelPos.X) addAtPos.X += (selectionSize - 1);
				if (addAtPos.X < voxelPos.X) addAtPos.X -= (selectionSize - 1);
				if (addAtPos.Y > voxelPos.Y) addAtPos.Y += (selectionSize - 1);
				if (addAtPos.Y < voxelPos.Y) addAtPos.Y -= (selectionSize - 1);
				if (addAtPos.Z > voxelPos.Z) addAtPos.Z += (selectionSize - 1);
				if (addAtPos.Z < voxelPos.Z) addAtPos.Z -= (selectionSize - 1);

				if (addAtPos.X >= 0 && addAtPos.X < 16 && addAtPos.Y >= 0 && addAtPos.Y < 16 && addAtPos.Z >= 0 && addAtPos.Z < 16) {
					SetVoxels(addAtPos, true);
				}
			} else if (!LastBlock(voxelPos)) {
				SetVoxels(voxelPos, false);
			} else {
				return;
			}

			if (api.Side == EnumAppSide.Client) {
				RegenMesh();
			}
			RegenSelectionBoxes();
			MarkDirty(true);

			// Send a custom network packet for server side, because
			// serverside blockselection index is inaccurate
			if (api.Side == EnumAppSide.Client) {
				SendUseOverPacket(byPlayer, voxelPos, facing, isBreak);
			}
		}

		internal void SetVoxels(Vec3i voxelPos, bool state) {
			for (int x = 0; x < selectionSize; x++) {
				for (int y = 0; y < selectionSize; y++) {
					for (int z = 0; z < selectionSize; z++) {
						voxels[at(voxelPos.X + x, voxelPos.Y + y, voxelPos.Z + z)] = state;
					}
				}
			}
		}

		internal bool BlockIn(Vec3i voxelPos) {
			for (int x = 0; x < selectionSize; x++) {
				for (int y = 0; y < selectionSize; y++) {
					for (int z = 0; z < selectionSize; z++) {
						if (voxels[at(voxelPos.X + x, voxelPos.Y + y, voxelPos.Z + z)]) {
							return true;
						}
					}
				}
			}
			return false;
		}

		internal bool LastBlock(Vec3i voxelPos) {
			for (int x = 0; x < 16; x++) {
				for (int y = 0; y < 16; y++) {
					for (int z = 0; z < 16; z++) {
						if (voxels[at(x, y, z)]) {
							if ((x >= voxelPos.X && x <= voxelPos.X + selectionSize) &&
								(y >= voxelPos.Y && y <= voxelPos.Y + selectionSize) &&
								(z >= voxelPos.Z && z <= voxelPos.Z + selectionSize)) {
								continue;
							}
							return false;
						}
					}
				}
			}
			return true;
		}

		public void Rotate(bool right) {
			var nvoxels = new BitArray(16 * 16 * 16);
			for (int y = 0; y < 16; y++) {
				for (int x = 0; x < 16; x++) {
					for (int z = 0; z < 16; z++) {
						if (right) {
							nvoxels[at(x, y, z)] = voxels[at(16 - z - 1, y, x)];
						} else {
							nvoxels[at(x, y, z)] = voxels[at(z, y, 16 - x - 1)];
						}
					}
				}
			}
			voxels = nvoxels;
			if (api.Side == EnumAppSide.Client) {
				RegenMesh();
			}
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

			SrcBlock = worldAccessForResolve.GetBlock((ushort)tree.GetInt("blockid"));
			deserializeVoxels(tree.GetBytes("voxels"));

			// Sometimes the api is null.
			if (api != null && api.Side == EnumAppSide.Client && mesh == null) {
				RegenMesh();
			}
			RegenSelectionBoxes();
		}

		public override void ToTreeAttributes(ITreeAttribute tree) {
			base.ToTreeAttributes(tree);

			tree.SetInt("blockid", SrcBlock.BlockId);
			tree.SetBytes("voxels", serializeVoxels());
		}

		public bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
			if (mesh == null) return false;

			mesher.AddMeshData(mesh);
			return true;
		}

		public void RegenSelectionBoxes() {
			List<Cuboidf> boxes = new List<Cuboidf>();

			for (int x = 0; x < 16; x += selectionSize) {
				for (int y = 0; y < 16; y += selectionSize) {
					for (int z = 0; z < 16; z += selectionSize) {
						if (BlockIn(new Vec3i(x, y, z))) {
							boxes.Add(new Cuboidf(x / 16f, y / 16f, z / 16f, x / 16f + selectionSize / 16f, y / 16f + selectionSize / 16f, z / 16f + selectionSize / 16f));
						}
					}
				}
			}

			selectionBoxes = boxes.ToArray();
		}

		public void RegenMesh() {
			ICoreClientAPI capi = api as ICoreClientAPI;
			mesh = VoxelMesher.GenBlockMesh(voxels, capi, SrcBlock);
		}

		private static int at(int x, int y, int z) {
			return x + 16 * (y + 16 * z);
		}

		byte[] serializeVoxels() {
			byte[] data = new byte[16 * 16 * 16 / 8];
			voxels.CopyTo(data, 0);
			return data;
		}

		void deserializeVoxels(byte[] data) {
			if (data == null || data.Length < 16 * 16 * 16 / 8) {
				voxels = new BitArray(16 * 16 * 16);
				voxels.SetAll(true);
				return;
			};
			voxels = new BitArray(data);
		}
	}
}
