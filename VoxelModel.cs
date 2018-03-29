
using System;
using System.Collections;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace ChiselAndSaw {
	public class VoxelModel {
		public Block SrcBlock; // The block used for textures and such.
		BitArray voxels;
		MeshData inventorymesh = null;
		MeshData blockmesh = null;

		public VoxelModel(byte[] data, Block block) {
			SrcBlock = block;
			if (data == null || data.Length < 16 * 16 * 16 / 8) {
				voxels = new BitArray(16 * 16 * 16);
				voxels.SetAll(true);
				return;
			};
			voxels = new BitArray(data);
		}

		public VoxelModel(BitArray data, Block block) {
			SrcBlock = block;
			if (data == null || data.Length < 16 * 16 * 16) {
				voxels = new BitArray(16 * 16 * 16);
				voxels.SetAll(true);
				return;
			};
			voxels = data;
		}

		public VoxelModel(Block block) {
			SrcBlock = block;
			voxels = new BitArray(16 * 16 * 16);
			voxels.SetAll(true);
		}

		public VoxelModel(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
			SrcBlock = worldAccessForResolve.GetBlock((ushort)tree.GetInt("voxelmodel-id"));
			var data = tree.GetBytes("voxelmodel-shape");
			if (data == null || data.Length < 16 * 16 * 16 / 8) {
				voxels = new BitArray(16 * 16 * 16);
				voxels.SetAll(true);
				return;
			};
			voxels = new BitArray(data);
		}

		private VoxelModel() { }

		public static Tuple<byte[], ushort> GetTreeData(ITreeAttribute tree) {
			return Tuple.Create(tree.GetBytes("voxelmodel-shape"), (ushort)tree.GetInt("voxelmodel-id"));
		}

		// At returns true if there is an active voxel at the given location.
		public bool At(Vec3i pos) {
			return At(pos.X, pos.Y, pos.Z);
		}
		public bool At(int x, int y, int z) {
			if (x > 16 || x < 0 || y > 16 || y < 0 || z > 16 || z < 0) {
				return false;
			}
			return voxels[at(x, y, z)];
		}

		// SetVoxels sets the state of the voxels in the given region.
		public void SetVoxels(Vec3i tl, Vec3i br, bool state) {
			SetVoxels(tl.X, tl.Y, tl.Z, br.X, br.Y, br.Z, state);
		}
		public void SetVoxels(int x1, int y1, int z1, int x2, int y2, int z2, bool state) {
			if (x1 > 16 || x1 < 0 || y1 > 16 || y1 < 0 || z1 > 16 || z1 < 0 ||
				x2 > 16 || x2 < 0 || y2 > 16 || y2 < 0 || z2 > 16 || z2 < 0) {
				return;
			}
			inventorymesh = null;
			blockmesh = null;
			for (int x = x1; x <= x2; x++) {
				for (int y = y1; y <= y2; y++) {
					for (int z = z1; z <= z2; z++) {
						voxels[at(x, y, z)] = state;
					}
				}
			}
		}

		// VoxelIn returns true if there is an active voxel in the given region.
		public bool VoxelIn(Vec3i tl, Vec3i br) {
			return VoxelIn(tl.X, tl.Y, tl.Z, br.X, br.Y, br.Z);
		}
		public bool VoxelIn(int x1, int y1, int z1, int x2, int y2, int z2) {
			if (x1 > 16 || x1 < 0 || y1 > 16 || y1 < 0 || z1 > 16 || z1 < 0 ||
				x2 > 16 || x2 < 0 || y2 > 16 || y2 < 0 || z2 > 16 || z2 < 0) {
				return false;
			}
			for (int x = x1; x <= x2; x++) {
				for (int y = y1; y <= y2; y++) {
					for (int z = z1; z <= z2; z++) {
						if (voxels[at(x, y, z)]) {
							return true;
						}
					}
				}
			}
			return false;
		}

		// LastVoxel returns true if the last active voxel(s) in the model are in the given region.
		public bool LastVoxel(Vec3i tl, Vec3i br) {
			return LastVoxel(tl.X, tl.Y, tl.Z, br.X, br.Y, br.Z);
		}
		public bool LastVoxel(int x1, int y1, int z1, int x2, int y2, int z2) {
			if (x1 > 16 || x1 < 0 || y1 > 16 || y1 < 0 || z1 > 16 || z1 < 0 ||
				x2 > 16 || x2 < 0 || y2 > 16 || y2 < 0 || z2 > 16 || z2 < 0) {
				return true;
			}
			for (int x = 0; x < 16; x++) {
				for (int y = 0; y < 16; y++) {
					for (int z = 0; z < 16; z++) {
						if (voxels[at(x, y, z)]) {
							if ((x >= x1 && x <= x2) &&
								(y >= y1 && y <= y2) &&
								(z >= z1 && z <= z2)) {
								continue;
							}
							return false;
						}
					}
				}
			}
			return true;
		}

		public bool IsFullBlock() {
			for (int x = 0; x < 16; x++) {
				for (int y = 0; y < 16; y++) {
					for (int z = 0; z < 16; z++) {
						if (!voxels[at(x, y, z)]) {
							return false;
						}
					}
				}
			}
			return true;
		}

		// Rotate rotates the voxel data 90 degrees left or right.
		public void Rotate(bool right) {
			inventorymesh = null;
			blockmesh = null;

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
		}

		// Serialize stores all the information required to recreate this model into a tree attribute.
		public void Serialize(ITreeAttribute tree) {
			tree.SetInt("voxelmodel-id", SrcBlock.BlockId);
			byte[] data = new byte[16 * 16 * 16 / 8];
			voxels.CopyTo(data, 0);
			tree.SetBytes("voxelmodel-shape", data);
		}

		public MeshData GetInventoryMesh(ICoreClientAPI capi) {
			if (inventorymesh == null) {
				inventorymesh = VoxelMesher.GenInventoryMesh(voxels, capi, SrcBlock);
			}
			return inventorymesh;
		}

		public MeshData GetBlockMesh(ICoreClientAPI capi) {
			if (blockmesh == null) {
				blockmesh = VoxelMesher.GenBlockMesh(voxels, capi, SrcBlock);
			}
			return blockmesh;
		}

		private static int at(int x, int y, int z) {
			return x + 16 * (y + 16 * z);
		}
	}
}
