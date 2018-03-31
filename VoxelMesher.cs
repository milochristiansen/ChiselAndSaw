
using System.Collections;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
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
			// bool[] sideVisible = new bool[6];
			// for (int x = 0; x < 16; x++) {
			// 	for (int y = 0; y < 16; y++) {
			// 		for (int z = 0; z < 16; z++) {
			// 			if (!voxels[at(x, y, z)]) continue;

			// 			sideVisible[0] = z == 0 || !voxels[at(x, y, z - 1)];
			// 			sideVisible[1] = x == 15 || !voxels[at(x + 1, y, z)];
			// 			sideVisible[2] = z == 15 || !voxels[at(x, y, z + 1)];
			// 			sideVisible[3] = x == 0 || !voxels[at(x - 1, y, z)];
			// 			sideVisible[4] = y == 15 || !voxels[at(x, y + 1, z)];
			// 			sideVisible[5] = y == 0 || !voxels[at(x, y - 1, z)];

			// 			for (int f = 0; f < 6; f++) {
			// 				if (!sideVisible[f]) continue; // TEMPORARY: Disabled generation of north an south faces.
			// 				mesh.AddMeshData(genQuad(f, x, y, z, 1, 1, capi, block));
			// 			}
			// 		}
			// 	}
			// }

			// Fully cover a cube.
			// mesh.AddMeshData(genQuad(0, 0, 0, 0, 16, 16, capi, block));
			// mesh.AddMeshData(genQuad(1, 15, 0, 0, 16, 16, capi, block));
			// mesh.AddMeshData(genQuad(2, 0, 0, 15, 16, 16, capi, block));
			// mesh.AddMeshData(genQuad(3, 0, 0, 0, 16, 16, capi, block));
			// mesh.AddMeshData(genQuad(4, 0, 15, 0, 16, 16, capi, block));
			// mesh.AddMeshData(genQuad(5, 0, 0, 0, 16, 16, capi, block));

			// Fully cover a small cube.
			// mesh.AddMeshData(genQuad(0, 3, 3, 3, 8, 8, capi, block));
			// mesh.AddMeshData(genQuad(1, 10, 3, 3, 8, 8, capi, block));
			// mesh.AddMeshData(genQuad(2, 3, 3, 10, 8, 8, capi, block));
			// mesh.AddMeshData(genQuad(3, 3, 3, 3, 8, 8, capi, block));
			// mesh.AddMeshData(genQuad(4, 3, 10, 3, 8, 8, capi, block));
			// mesh.AddMeshData(genQuad(5, 3, 3, 3, 8, 8, capi, block));

			// Draws narrow quads in the corners.
			// mesh.AddMeshData(genQuad(0, 10, 0, 0, 6, 10, capi, block));
			// mesh.AddMeshData(genQuad(1, 15, 0, 0, 6, 10, capi, block));
			// mesh.AddMeshData(genQuad(2, 0, 0, 15, 6, 10, capi, block));
			// mesh.AddMeshData(genQuad(3, 0, 0, 10, 6, 10, capi, block));
			// mesh.AddMeshData(genQuad(0, 0, 0, 0, 6, 10, capi, block));
			// mesh.AddMeshData(genQuad(1, 15, 0, 10, 6, 10, capi, block));
			// mesh.AddMeshData(genQuad(2, 10, 0, 15, 6, 10, capi, block));
			// mesh.AddMeshData(genQuad(3, 0, 0, 0, 6, 10, capi, block));

			// mesh.AddMeshData(genQuad(4, 0, 0, 0, 6, 10, capi, block));
			// mesh.AddMeshData(genQuad(5, 0, 0, 0, 6, 10, capi, block));
			// mesh.AddMeshData(genQuad(4, 0, 15, 0, 6, 10, capi, block));
			// mesh.AddMeshData(genQuad(5, 0, 0, 0, 6, 10, capi, block));

			meshFacing(0, mesh, voxels, capi, block);
			meshFacing(1, mesh, voxels, capi, block);
			meshFacing(2, mesh, voxels, capi, block);
			return mesh;
		}

		private static bool voxelAt(int axis, BitArray voxels, bool[,] mask, int a, int b, int c) {
			switch (axis) {
				case 0:
					return !mask[b, c] && voxels[at(b, c, a)];
				case 1:
					return !mask[b, c] && voxels[at(a, b, c)];
				default:
					return !mask[b, c] && voxels[at(b, a, c)];
			}
		}
		private static bool exposedAt(int axis, int offset, BitArray voxels, bool[,] mask, int a, int b, int c) {
			// maskB[b, c] || !(a == 15 || !voxels[at(b, c, a + 1)])
			if (!voxelAt(axis, voxels, mask, a, b, c)) {
				return false;
			}
			int edge = 15;
			if (offset < 0) {
				edge = 0;
			}
			switch (axis) {
				case 0:
					return !mask[b, c] && (a == edge || !voxels[at(b, c, a + offset)]);
				case 1:
					return !mask[b, c] && (a == edge || !voxels[at(a + offset, b, c)]);
				default:
					return !mask[b, c] && (a == edge || !voxels[at(b, a + offset, c)]);
			}
		}

		private static Vec3i translate(int axis, int a, int b, int c) {
			switch (axis) {
				case 0:
					return new Vec3i(b, c, a);
				case 1:
					return new Vec3i(a, b, c);
				default:
					return new Vec3i(b, a, c);
			}
		}

		private static void meshFacing(int axis, MeshData mesh, BitArray voxels, ICoreClientAPI capi, Block block) {
			for (int a = 0; a < 16; a++) {
				var maskA = new bool[16, 16];
				var maskB = new bool[16, 16];
				int faceA;
				int faceB;
				switch (axis) {
					case 0:
						faceA = 0;
						faceB = 2;
						break;
					case 1:
						faceA = 3;
						faceB = 1;
						break;
					default:
						faceA = 5;
						faceB = 4;
						break;
				}

				// For each and every cell in this slice:
				int debug = 0;
				for (int b = 0; b < 16; b++) {
					for (int c = 0; c < 16; c++) {
						if (!voxelAt(axis, voxels, maskA, a, b, c)) continue;
						// If the cell is not masked and the north side is exposed:
						if (exposedAt(axis, -1, voxels, maskA, a, b, c)) {
							// Count as many unmasked and exposed cells as you can along the first axis
							int w = 1;
							for (int tb = b + 1; tb < 16; tb++) {
								if (!exposedAt(axis, -1, voxels, maskA, a, tb, c)) {
									break;
								}
								w++;
							}
							// Now try to extend that down along the second axis
							int h = 1;
							for (int tc = c + 1; tc < 16; tc++) {
								bool rowok = true;
								for (int tb = b; tb < b + w; tb++) {
									if (!exposedAt(axis, -1, voxels, maskA, a, tb, tc)) {
										rowok = false;
										break;
									}
								}
								if (!rowok) {
									break;
								}
								h++;
							}
							// Mask the covered area.
							for (int tb = b; tb < b + w; tb++) {
								for (int tc = c; tc < c + h; tc++) {
									maskA[tb, tc] = true;
								}
							}
							// Finally make a properly sized quad.
							var coords = translate(axis, a, b, c);
							if (axis == 1) {
								var t = w;
								w = h;
								h = t;
							}
							mesh.AddMeshData(genQuad(faceA, coords.X, coords.Y, coords.Z, w, h, capi, block));
							debug++;
						}
						if (exposedAt(axis, 1, voxels, maskB, a, b, c)) {
							int w = 1;
							for (int tb = b + 1; tb < 16; tb++) {
								if (!exposedAt(axis, 1, voxels, maskB, a, tb, c)) {
									break;
								}
								w++;
							}
							int h = 1;
							for (int tc = c + 1; tc < 16; tc++) {
								bool rowok = true;
								for (int tb = b; tb < b + w; tb++) {
									if (!exposedAt(axis, 1, voxels, maskB, a, tb, tc)) {
										rowok = false;
										break;
									}
								}
								if (!rowok) {
									break;
								}
								h++;
							}
							for (int tb = b; tb < b + w; tb++) {
								for (int tc = c; tc < c + h; tc++) {
									maskB[tb, tc] = true;
								}
							}
							var coords = translate(axis, a, b, c);
							if (axis == 1) {
								var t = w;
								w = h;
								h = t;
							}
							mesh.AddMeshData(genQuad(faceB, coords.X, coords.Y, coords.Z, w, h, capi, block));
						}
					}
				}
			}

		}

		private static MeshData genQuad(int face, int x, int y, int z, int w, int h, ICoreClientAPI capi, Block block) {
			var shading = (byte)(255 * CubeMeshUtil.DefaultBlockSideShadingsByFacing[face]);

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
					quad = QuadMeshUtil.GetCustomQuad(xf - wf + u, yf, zf + u, wf, hf, 255, 255, 255, 255);
					quad.Rotate(new Vec3f(xf + hu, yf + hu, zf + hu), 0, GameMath.PI, 0);
					break;
				case 1: // E
					quad = QuadMeshUtil.GetCustomQuad(xf - wf + u, yf, zf + u, wf, hf, 255, 255, 255, 255);
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
					quad = QuadMeshUtil.GetCustomQuadHorizontal(xf, yf, zf - hf + u, wf, hf, 255, 255, 255, 255);
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

			// Correct the UVs for rotation.
			switch (face) {
				case 0: // N
				case 1: // E
				case 2: // S
				case 3: // W
					swapUVs(ref quad, 0, 6);
					swapUVs(ref quad, 2, 4);
					break;
				case 4: // U
				default: // D
					swapUVs(ref quad, 0, 2);
					swapUVs(ref quad, 4, 6);
					break;
			}

			// Correct the UVs for translation.
			// Appears to work for north and east, but I can't seem to get the other directions...
			// var coords = new float[] { xf, yf, zf };
			// var uo = coords[coordIndexByFace[face][0]];
			// var vo = coords[coordIndexByFace[face][1]];
			// for (int j = 0; j < quad.Uv.Length; j += 2) {
			// 	quad.Uv[j] = quad.Uv[j] + (1f - wf) - uo;
			// 	quad.Uv[j + 1] = quad.Uv[j + 1] + (1f - hf) - vo;
			// }

			// Scale the UVs to fit the texture.
			float padding = capi.BlockTextureAtlas.SubPixelPadding;
			float size = capi.BlockTextureAtlas.Size;
			TextureAtlasPosition tpos = capi.BlockTextureAtlas.GetPosition(block, BlockFacing.ALLFACES[face].Code);
			for (int j = 0; j < quad.Uv.Length; j += 2) {
				quad.Uv[j] = tpos.x1 + quad.Uv[j] * 32f / size - padding;
				quad.Uv[j + 1] = tpos.y1 + quad.Uv[j + 1] * 32f / size - padding;
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