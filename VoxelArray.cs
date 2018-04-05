
using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;

namespace ChiselAndSaw {
	public struct VoxelArray : IVoxelProvider {
		private BitArray data;

		public VoxelArray(bool state) {
			data = new BitArray(16 * 16 * 16);
			if (state) {
				data.SetAll(true);
			}
		}

		public VoxelArray(ITreeAttribute tree) {
			var raw = tree.GetBytes("voxelmodel-shape");
			if (raw == null && raw.Length != 16 * 16 * 16 / 8) {
				data = new BitArray(16 * 16 * 16);
				data.SetAll(true);
				return;
			}
			data = new BitArray(raw);
		}

		public void Serialize(ITreeAttribute tree) {
			var rtn = new byte[16 * 16 * 16 / 8];
			data.CopyTo(rtn, 0);
			tree.SetBytes("voxelmodel-shape", rtn);
		}

		public bool Get(int x, int y, int z) {
			return data[x + 16 * (y + 16 * z)];
		}

		public void Set(int x, int y, int z, bool state) {
			data[x + 16 * (y + 16 * z)] = state;
		}

		public void Reduce() {

		}
	}
}
