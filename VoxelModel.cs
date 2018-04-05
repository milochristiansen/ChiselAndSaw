
using System;
using System.Collections;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace ChiselAndSaw {
	public interface IVoxelProvider {
		bool Get(int x, int y, int z);
		void Set(int x, int y, int z, bool state);
		void Reduce();
		void Serialize(ITreeAttribute tree);
	}

	public class VoxelModel {
		public Block SrcBlock; // The block used for textures and such.
		IVoxelProvider voxels;
		MeshData inventorymesh = null;
		MeshData blockmesh = null;

		public VoxelModel(Block block) {
			SrcBlock = block;
			voxels = new VoxelArray(true);
		}

		public VoxelModel(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
			SrcBlock = worldAccessForResolve.GetBlock((ushort)tree.GetInt("voxelmodel-id"));
			voxels = new VoxelArray(tree);
		}

		private VoxelModel() { }

		public static Tuple<IVoxelProvider, ushort> GetTreeData(ITreeAttribute tree) {
			return Tuple.Create((IVoxelProvider)new VoxelArray(tree), (ushort)tree.GetInt("voxelmodel-id"));
		}

		// At returns true if there is an active voxel at the given location.
		public bool At(Vec3i pos) {
			return voxels.Get(pos.X, pos.Y, pos.Z);
		}
		public bool At(int x, int y, int z) {
			return voxels.Get(x, y, z);
		}

		// SetVoxels sets the state of the voxels in the given region.
		public void SetVoxels(Vec3i tl, Vec3i br, bool state) {
			SetVoxels(tl.X, tl.Y, tl.Z, br.X, br.Y, br.Z, state);
		}
		public void SetVoxels(int x1, int y1, int z1, int x2, int y2, int z2, bool state) {
			if (x1 > 15 || x1 < 0 || y1 > 15 || y1 < 0 || z1 > 15 || z1 < 0 ||
				x2 > 15 || x2 < 0 || y2 > 15 || y2 < 0 || z2 > 15 || z2 < 0) {
				return;
			}
			inventorymesh = null;
			blockmesh = null;
			for (int x = x1; x <= x2; x++) {
				for (int y = y1; y <= y2; y++) {
					for (int z = z1; z <= z2; z++) {
						voxels.Set(x, y, z, state);
					}
				}
			}
			voxels.Reduce();
		}

		// VoxelIn returns true if there is an active voxel in the given region.
		public bool VoxelIn(Vec3i tl, Vec3i br) {
			return VoxelIn(tl.X, tl.Y, tl.Z, br.X, br.Y, br.Z);
		}
		public bool VoxelIn(int x1, int y1, int z1, int x2, int y2, int z2) {
			if (x1 > 15 || x1 < 0 || y1 > 15 || y1 < 0 || z1 > 15 || z1 < 0 ||
				x2 > 15 || x2 < 0 || y2 > 15 || y2 < 0 || z2 > 15 || z2 < 0) {
				return false;
			}
			for (int x = x1; x <= x2; x++) {
				for (int y = y1; y <= y2; y++) {
					for (int z = z1; z <= z2; z++) {
						if (voxels.Get(x, y, z)) {
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
			if (x1 > 15 || x1 < 0 || y1 > 15 || y1 < 0 || z1 > 15 || z1 < 0 ||
				x2 > 15 || x2 < 0 || y2 > 15 || y2 < 0 || z2 > 15 || z2 < 0) {
				return true;
			}
			for (int x = 0; x < 16; x++) {
				for (int y = 0; y < 16; y++) {
					for (int z = 0; z < 16; z++) {
						if (voxels.Get(x, y, z)) {
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
						if (!voxels.Get(x, y, z)) {
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

			var nvoxels = new VoxelArray(false);
			for (int y = 0; y < 16; y++) {
				for (int x = 0; x < 16; x++) {
					for (int z = 0; z < 16; z++) {
						if (right) {
							nvoxels.Set(x, y, z, voxels.Get(16 - z - 1, y, x));
						} else {
							nvoxels.Set(x, y, z, voxels.Get(z, y, 16 - x - 1));
						}
					}
				}
			}
			voxels = nvoxels;
		}

		// Serialize stores all the information required to recreate this model into a tree attribute.
		public void Serialize(ITreeAttribute tree) {
			tree.SetInt("voxelmodel-id", SrcBlock.BlockId);
			voxels.Serialize(tree);
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
