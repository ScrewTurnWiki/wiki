using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Tests {
	
	public class CacheProviderTests : CacheProviderTestScaffolding {

		public override ICacheProviderV30 GetProvider() {
			CacheProvider prov = new CacheProvider();
			prov.Init(MockHost(), "");
			return prov;
		}

	}

}
