
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace ChiselAndSaw {
	public static class ModelCache {
		private static int lastID = 0;
		private static long lastCollect;

		private static void collect(IRenderAPI render, IClientWorldAccessor world, Dictionary<int, CacheModel> data) {
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

		public static int New(MeshRef mesh, IRenderAPI render, ICoreClientAPI capi) {
			var data = getData(capi);
			lastID++;
			data[lastID] = new CacheModel { Mesh = mesh, Lived = capi.World.ElapsedMilliseconds };
			collect(render, capi.World, data);
			return lastID;
		}

		public static MeshRef Get(int id, IRenderAPI render, ICoreClientAPI capi) {
			var data = getData(capi);
			collect(render, capi.World, data);

			CacheModel model;
			var ok = data.TryGetValue(id, out model);
			if (ok) {
				model.Lived = capi.World.ElapsedMilliseconds;
				return model.Mesh;
			}
			return null;
		}

		private static Dictionary<int, CacheModel> getData(ICoreClientAPI capi) {
			object data;
			bool ok = capi.ObjectCache.TryGetValue("chiselandsaw-modelcache", out data);
			if (!ok) {
				var ndata = new Dictionary<int, CacheModel>();
				capi.ObjectCache["chiselandsaw-modelcache"] = ndata;
				return ndata;
			}
			return data as Dictionary<int, CacheModel>;
		}
	}

	public struct CacheModel {
		public MeshRef Mesh;
		public long Lived;
	}
}
