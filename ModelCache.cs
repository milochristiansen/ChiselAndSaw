
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace ChiselAndSaw {
	public static class ModelCache {
		private static Dictionary<int, CacheModel> data = new Dictionary<int, CacheModel>();
		private static int lastID = 0;
		private static long lastCollect;

		private static void collect(IRenderAPI render, IClientWorldAccessor world) {
			if (lastCollect > world.ElapsedMilliseconds - 1000) {
				return;
			}

			var toKill = new List<int>();
			foreach (var item in data) {
				if (item.Value.Lived < world.ElapsedMilliseconds - 10000) {
					render.DeleteMesh(item.Value.Mesh);
					toKill.Add(item.Key);
				}
			}
			foreach (var key in toKill) {
				data.Remove(key);
			}
		}

		public static int New(MeshRef mesh, IRenderAPI render, IClientWorldAccessor world) {
			lastID++;
			data[lastID] = new CacheModel { Mesh = mesh, Lived = world.ElapsedMilliseconds };
			collect(render, world);
			return lastID;
		}

		public static MeshRef Get(int id, IRenderAPI render, IClientWorldAccessor world) {
			collect(render, world);

			CacheModel model;
			var ok = data.TryGetValue(id, out model);
			if (ok) {
				model.Lived = world.ElapsedMilliseconds;
				return model.Mesh;
			}
			return null;
		}
	}

	public struct CacheModel {
		public MeshRef Mesh;
		public long Lived;
	}
}
