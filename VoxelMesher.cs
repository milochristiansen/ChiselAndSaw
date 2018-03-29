
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
	public static class VoxelMesher {
		public static MeshData GenInventoryMesh(byte[] data, ICoreClientAPI capi, Block block) {
			if (data == null || data.Length < 16 * 16 * 16 / 8) {
				return null;
			};
			var mesh = genMesh(new BitArray(data), capi, block);
			mesh.rgba2 = null;
			return mesh;
		}

		public static MeshData GenInventoryMesh(BitArray data, ICoreClientAPI capi, Block block) {
			if (data == null || data.Length < 16 * 16 * 16) {
				return null;
			};
			var mesh = genMesh(data, capi, block);
			mesh.rgba2 = null;
			return mesh;
		}

		public static MeshData GenBlockMesh(byte[] data, ICoreClientAPI capi, Block block) {
			if (data == null || data.Length < 16 * 16 * 16 / 8) {
				return null;
			};
			return genMesh(new BitArray(data), capi, block);
		}

		public static MeshData GenBlockMesh(BitArray data, ICoreClientAPI capi, Block block) {
			if (data == null || data.Length < 16 * 16 * 16) {
				return null;
			};
			return genMesh(data, capi, block);
		}

		private static MeshData genMesh(BitArray voxels, ICoreClientAPI capi, Block block) {
			var mesh = new MeshData(24, 36, false).WithTints().WithRenderpasses();

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
							mesh.AddMeshData(GenQuad(f, x, y, z, 1, 1, capi, block));
						}
					}
				}
			}
			return mesh;
		}

		private static MeshData GenQuad(int face, int x, int y, int z, int w, int h, ICoreClientAPI capi, Block block) {
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
					ox = 16 - ox;
					oy = 16 - oy;
					break;
				case 1: // E
					ox = 16 - ox;
					oy = 16 - oy;
					break;
				case 2: // S
					oy = 16 - oy;
					break;
				case 3: // W
					oy = 16 - oy;
					break;
				case 4: // U
					ox = 16 - ox;
					oy = 16 - oy;
					break;
				default: // D
					ox = 16 - ox;
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
					swapUVs(ref quad, 0, 6);
					swapUVs(ref quad, 2, 4);
					break;
				case 1: // E
					swapUVs(ref quad, 0, 6);
					swapUVs(ref quad, 2, 4);
					break;
				case 2: // S
					swapUVs(ref quad, 0, 6);
					swapUVs(ref quad, 2, 4);
					break;
				case 3: // W
					swapUVs(ref quad, 0, 6);
					swapUVs(ref quad, 2, 4);
					break;
				case 4: // U
					swapUVs(ref quad, 0, 2);
					swapUVs(ref quad, 4, 6);
					break;
				default: // D
					swapUVs(ref quad, 0, 2);
					swapUVs(ref quad, 4, 6);
					break;
			}

			return quad;
		}

		private static void swapUVs(ref MeshData quad, int a, int b) {
			var x = quad.Uv[a];
			var y = quad.Uv[a + 1];
			quad.Uv[a] = quad.Uv[b];
			quad.Uv[a + 1] = quad.Uv[b + 1];
			quad.Uv[b] = x;
			quad.Uv[b + 1] = y;
		}

		private static int[][] coordIndexByFace = new int[][] {
			new int[] { 0, 1 },// N
			new int[] { 2, 1 },// E
			new int[] { 0, 1 },// S
			new int[] { 2, 1 },// W
			new int[] { 0, 2 },// U
			new int[] { 0, 2 }// D
		};

		private static int at(int x, int y, int z) {
			return x + 16 * (y + 16 * z);
		}
	}
}