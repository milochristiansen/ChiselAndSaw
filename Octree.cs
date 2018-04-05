
using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;

namespace ChiselAndSaw {
	// This thing *murders* performance. Clearly it need a lot more work.
	public struct ArrayOctree : IVoxelProvider {
		private ushort[] data;

		public ArrayOctree(bool state) {
			if (state) {
				data = new ushort[] { 0xffff };
			} else {
				data = new ushort[] { 0x00ff };
			}
		}

		public ArrayOctree(ITreeAttribute tree) {
			var raw = tree.GetBytes("voxelmodel-shape");
			if (raw == null && raw.Length != 16 * 16 * 16 / 8) {
				data = new ushort[] { 0xffff };
				return;
			}
			data = new ushort[] { 0xffff };
			var voxels = new BitArray(raw);
			for (int x = 0; x < 16; x++) {
				for (int y = 0; y < 16; y++) {
					for (int z = 0; z < 16; z++) {
						Set(x, y, z, voxels[x + 16 * (y + 16 * z)]);
					}
				}
			}
			Reduce();
		}

		public void Serialize(ITreeAttribute tree) {
			var voxels = new BitArray(16 * 16 * 16);
			for (int x = 0; x < 16; x++) {
				for (int y = 0; y < 16; y++) {
					for (int z = 0; z < 16; z++) {
						voxels[x + 16 * (y + 16 * z)] = Get(x, y, z);
					}
				}
			}
			var rtn = new byte[16 * 16 * 16 / 8];
			voxels.CopyTo(rtn, 0);
			tree.SetBytes("voxelmodel-shape", rtn);
		}

		public bool Get(int x, int y, int z) {
			return get(0, x, y, z, 0, 0, 0, 16);
		}

		private bool get(int me, int x, int y, int z, int tlx, int tly, int tlz, int seg) {
			int octant = getOctant(x, y, z, ref tlx, ref tly, ref tlz, seg);
			if (octant == -1) {
				return false;
			}
			if ((data[me] & homogeneous[octant]) != 0) {
				return (data[me] & hasVoxels[octant]) != 0;
			}
			int offset = getOffset(me, octant);
			return get(me + offset + 1, x, y, z, tlx, tly, tlz, seg / 2);
		}

		public void Set(int x, int y, int z, bool state) {
			set(0, x, y, z, 0, 0, 0, 16, state);
		}

		private void set(int me, int x, int y, int z, int tlx, int tly, int tlz, int seg, bool state) {
			int octant = getOctant(x, y, z, ref tlx, ref tly, ref tlz, seg);
			if (octant == -1) {
				return;
			}
			if (seg == 2) {
				// We are at the lowest level, children are not possible.
				if (state) {
					data[me] = (ushort)(data[me] | hasVoxels[octant]);
				} else {
					data[me] = (ushort)(data[me] & ~hasVoxels[octant]);
				}
				return;
			}
			var offset = getOffset(me, octant);
			if ((data[me] & homogeneous[octant]) != 0) {
				var tmp = new List<ushort>(data);
				if ((data[me] & hasVoxels[octant]) != 0) {
					tmp.Insert(me + offset + 1, 0xffff);
				} else {
					tmp.Insert(me + offset + 1, 0x00ff);
				}
				data = tmp.ToArray();
			}
			data[me] = (ushort)(data[me] & ~homogeneous[octant]); // Clear the homogeneous bit for this region.
			set(me + offset + 1, x, y, z, tlx, tly, tlz, seg / 2, state);
		}

		public void Reduce() {
			for (int i = 7; i >= 0; i--) {
				int offset = getOffset(0, i);
				if ((data[0] & homogeneous[i]) == 0) {
					offset--;
					reduce(offset + 1, 0, i);
				}
			}
		}

		private void reduce(int me, int parent, int index) {
			int full = 0;
			int empty = 0;
			for (var i = 7; i >= 0; i--) {
				int offset = getOffset(me, i);
				if ((data[me] & homogeneous[i]) == 0) {
					offset--;
					reduce(me + offset + 1, me, i);
				}
				if ((data[me] & homogeneous[i]) != 0) {
					if ((data[me] & hasVoxels[i]) != 0) {
						full++;
					} else {
						empty++;
					}
				}
			}
			if (full == 8 || empty == 8) {
				var poffset = getOffset(parent, index);
				for (var i = 0; i <= index; i++) {
					if ((data[me] & homogeneous[i]) == 0) {
						poffset++;
					}
				}
				data[parent] = (ushort)(data[parent] | homogeneous[index]);
				if (full == 8) {
					data[parent] = (ushort)(data[parent] | hasVoxels[index]);
				} else {
					data[parent] = (ushort)(data[parent] & ~hasVoxels[index]);
				}
				var tmp = new List<ushort>(data);
				tmp.RemoveAt(parent + poffset + 1);
				data = tmp.ToArray();
			}
		}

		private int getOffset(int me, int octant) {
			var offset = 0;
			for (var i = 0; i < octant; i++) {
				if ((data[me] & homogeneous[i]) == 0) {
					offset += getOffset(me + offset + 1, 8);
					offset++;
				}
			}
			return offset;
		}

		private int getOctant(int x, int y, int z, ref int tlx, ref int tly, ref int tlz, int seg) {
			if (x < tlx || x > tlx + seg || y < tly || y > tly + seg || z < tlz || z > tlz + seg) {
				return -1; // Out of bounds.
			}
			var segh = seg / 2;
			if (x < tlx + segh) {
				if (y < tly + segh) {
					if (z < tlz + segh) {
						return 0;
					} else {
						tlz += segh;
						return 4;
					}
				} else {
					tly += segh;
					if (z < tlz + segh) {
						return 2;
					} else {
						tlz += segh;
						return 6;
					}
				}
			} else {
				tlx += segh;
				if (y < tly + segh) {
					if (z < tlz + segh) {
						return 1;
					} else {
						tlz += segh;
						return 5;
					}
				} else {
					tly += segh;
					if (z < tlz + segh) {
						return 3;
					} else {
						tlz += segh;
						return 7;
					}
				}
			}
		}

		static ushort[] hasVoxels = new ushort[8]{
			0x8000, 0x4000, 0x2000, 0x1000, 0x0800, 0x0400, 0x0200, 0x0100,
		};

		static ushort[] homogeneous = new ushort[8]{
			0x0080, 0x0040, 0x0020, 0x0010, 0x0008, 0x0004, 0x0002, 0x0001,
		};
	}
}
