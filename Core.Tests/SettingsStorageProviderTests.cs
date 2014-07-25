namespace Core.Tests {
	using NUnit.Framework;
	using ScrewTurn.Wiki;
	using ScrewTurn.Wiki.PluginFramework;
	using ScrewTurn.Wiki.Tests;

	public class SettingsStorageProviderTests : SettingsStorageProviderTestScaffolding {

		public override ISettingsStorageProviderV30 GetProvider() {
			SettingsStorageProvider prov = new SettingsStorageProvider();
			prov.Init(MockHost(), "");
			return prov;
		}

		[Test]
		public void Init() {
			ISettingsStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), "");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

	}

}
