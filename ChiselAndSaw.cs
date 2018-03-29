
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace VSExampleMods {
	public class chiselandsaw : ModBase {
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

		// private MeshRef meshRef;
		// public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
		// 	if (meshRef == null) {
		// 		// TODO: Generate mesh.
		// 		//meshRef = capi.Render.UploadMesh(  );
		// 	}
		// 	renderinfo.ModelRef = meshRef;
		// }

		public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
			return GetSelectionBoxes(blockAccessor, pos);
		}
	}

	public class BlockEntityChisel : BlockEntity, IBlockShapeSupplier {
		Block block;
		BitArray voxels = new BitArray(16 * 16 * 16);
		MeshData mesh;
		Cuboidf[] selectionBoxes = new Cuboidf[0];
		int selectionSize = 8;

		public override void Initialize(ICoreAPI api) {
			base.Initialize(api);

			if (block != null) {
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
			this.block = block;

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

			block = worldAccessForResolve.GetBlock((ushort)tree.GetInt("blockid"));
			deserializeVoxels(tree.GetBytes("voxels"));

			// Sometimes the api is null.
			if (api != null && api.Side == EnumAppSide.Client && mesh == null) {
				RegenMesh();
			}
			RegenSelectionBoxes();
		}

		public override void ToTreeAttributes(ITreeAttribute tree) {
			base.ToTreeAttributes(tree);

			tree.SetInt("blockid", block.BlockId);
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

			mesh = new MeshData(24, 36, false).WithTints().WithRenderpasses();

			// The New! Shiny! Inefficent! version.
			// Dynamically generating the needed quads is a step along the path to greedy meshing though.
			bool[] sideVisible = new bool[6];
			for (int x = 0; x < 16; x++) {
				for (int y = 0; y < 16; y++) {
					for (int z = 0; z < 16; z++) {
						if (!voxels[at(x, y, z)]) continue;

						sideVisible[0] = z == 0 || !voxels[at(x, y, z - 1)];
						sideVisible[1] = x == 15 || !voxels[at(x + 1, y, z)];
						sideVisible[2] = z == 15 || !voxels[at(x, y, z + 1)];
						sideVisible[3] = x == 0 || !voxels[at(x - 1, y, z)];
						sideVisible[4] = y == 15 || !voxels[at(x, y + 1, z)];
						sideVisible[5] = y == 0 || !voxels[at(x, y - 1, z)];

						for (int f = 0; f < 6; f++) {
							if (!sideVisible[f]) continue;
							mesh.AddMeshData(GenQuad(f, x, y, z, 1, 1));
						}
					}
				}
			}
		}

		private int[][] coordIndexByFace = new int[][] {
			// N
			new int[] { 0, 1 },
			// E
			new int[] { 2, 1 },
			// S
			new int[] { 0, 1 },
			// W
			new int[] { 2, 1 },
			// U
			new int[] { 0, 2 },
			// D
			new int[] { 0, 2 }
		};

		private int[] flippedCords = new int[] { 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };

		private MeshData GenQuad(int face, int x, int y, int z, int w, int h) {
			var shading = (byte)(255 * CubeMeshUtil.DefaultBlockSideShadingsByFacing[face]);

			// I'm pretty sure this isn't correct for quads larger than w=1,h=1.
			// Larger quads probably need to be translated one way or another.

			// North: Negative Z
			// East: Positive X
			// South: Positive Z
			// West: Negative X

			var u = 1 / 16f;
			var hu = 1 / 32f;
			var xf = x * u;
			var yf = y * u;
			var zf = z * u;
			var wf = w * u;
			var hf = h * u;
			MeshData quad;
			switch (face) {
				case 0: // N
					quad = QuadMeshUtil.GetCustomQuad(xf, yf, zf + u, wf, hf, 255, 255, 255, 255);
					quad.Rotate(new Vec3f(xf + hu, yf + hu, zf + hu), 0, GameMath.PI, 0);
					break;
				case 1: // E
					quad = QuadMeshUtil.GetCustomQuad(xf, yf, zf + u, wf, hf, 255, 255, 255, 255);
					quad.Rotate(new Vec3f(xf + hu, yf + hu, zf + hu), 0, GameMath.PIHALF, 0);
					break;
				case 2: // S
					quad = QuadMeshUtil.GetCustomQuad(xf, yf, zf + u, wf, hf, 255, 255, 255, 255);
					break;
				case 3: // W
					quad = QuadMeshUtil.GetCustomQuad(xf, yf, zf + u, wf, hf, 255, 255, 255, 255);
					quad.Rotate(new Vec3f(xf + hu, yf + hu, zf + hu), 0, -GameMath.PIHALF, 0);
					break;
				case 4: // U
					quad = QuadMeshUtil.GetCustomQuadHorizontal(xf, yf, zf, wf, hf, 255, 255, 255, 255);
					quad.Rotate(new Vec3f(xf + hu, yf + hu, zf + hu), GameMath.PI, 0, 0);
					break;
				default: // D
					quad = QuadMeshUtil.GetCustomQuadHorizontal(xf, yf, zf, wf, hf, 255, 255, 255, 255);
					break;
			}

			quad.Rgba = new byte[16];
			quad.Rgba.Fill(shading);
			quad.rgba2 = new byte[16];
			quad.rgba2.Fill(shading);
			quad.Flags = new int[4];
			quad.Flags.Fill(0);
			quad.RenderPasses = new int[1];
			quad.RenderPassCount = 1;
			quad.Tints = new int[1];
			quad.TintsCount = 1;
			quad.XyzFaces = new int[] { face };
			quad.XyzFacesCount = 1;

			ICoreClientAPI capi = api as ICoreClientAPI;
			float subPixelPadding = capi.BlockTextureAtlas.SubPixelPadding;
			TextureAtlasPosition tpos = capi.BlockTextureAtlas.GetPosition(block, BlockFacing.ALLFACES[face].Code);
			for (int j = 0; j < quad.Uv.Length; j++) {
				quad.Uv[j] = (j % 2 > 0 ? tpos.y1 : tpos.x1) + quad.Uv[j] * 32f / capi.BlockTextureAtlas.Size - subPixelPadding;
			}

			int[] coords = new int[]{
				x, y, z,
			};

			// Flip the UVs as needed. Screws up rotation even more than it already is, but we need to fix that anyway.
			int ox = coords[coordIndexByFace[face][0]];
			int oy = coords[coordIndexByFace[face][1]];
			switch (face) {
				case 0: // N
					ox = flippedCords[ox];
					oy = flippedCords[oy];
					break;
				case 1: // E
					ox = flippedCords[ox];
					oy = flippedCords[oy];
					break;
				case 2: // S
					oy = flippedCords[oy];
					break;
				case 3: // W
					oy = flippedCords[oy];
					break;
				case 4: // U
					ox = flippedCords[ox];
					oy = flippedCords[oy];
					break;
				default: // D
					ox = flippedCords[ox];
					break;
			}

			float offsetX = (ox * 2f) / capi.BlockTextureAtlas.Size;
			float offsetZ = (oy * 2f) / capi.BlockTextureAtlas.Size;
			for (int i = 0; i < quad.Uv.Length; i += 2) {
				quad.Uv[i] += offsetX;
				quad.Uv[i + 1] += offsetZ;
			}

			switch (face) {
				case 0: // N
					SwapUV(ref quad, 0, 6);
					SwapUV(ref quad, 2, 4);
					break;
				case 1: // E
					SwapUV(ref quad, 0, 6);
					SwapUV(ref quad, 2, 4);
					break;
				case 2: // S
					SwapUV(ref quad, 0, 6);
					SwapUV(ref quad, 2, 4);
					break;
				case 3: // W
					SwapUV(ref quad, 0, 6);
					SwapUV(ref quad, 2, 4);
					break;
				case 4: // U
					SwapUV(ref quad, 0, 2);
					SwapUV(ref quad, 4, 6);
					break;
				default: // D
					SwapUV(ref quad, 0, 2);
					SwapUV(ref quad, 4, 6);
					break;
			}

			return quad;
		}

		private void SwapUV(ref MeshData quad, int a, int b) {
			var x = quad.Uv[a];
			var y = quad.Uv[a + 1];
			quad.Uv[a] = quad.Uv[b];
			quad.Uv[a + 1] = quad.Uv[b + 1];
			quad.Uv[b] = x;
			quad.Uv[b + 1] = y;
		}

		private int at(int x, int y, int z) {
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
