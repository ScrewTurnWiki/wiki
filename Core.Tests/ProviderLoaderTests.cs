namespace Core.Tests {
	using System;
	using NUnit.Framework;
	using ScrewTurn.Wiki;
	using ScrewTurn.Wiki.PluginFramework;

	[TestFixture]
	public class ProviderLoaderTests {

		[TestCase(null, typeof(SettingsStorageProvider))]
		[TestCase("", typeof(SettingsStorageProvider))]
		[TestCase("default", typeof(SettingsStorageProvider))]
		[TestCase("DEfaulT", typeof(SettingsStorageProvider))]
		[TestCase("ScrewTurn.Wiki.SettingsStorageProvider, ScrewTurn.Wiki.Core", typeof(SettingsStorageProvider))]
		[TestCase("ScrewTurn.Wiki.SettingsStorageProvider, ScrewTurn.Wiki.Core.dll", typeof(SettingsStorageProvider))]
		[TestCase("Core.Tests.TestSettingsStorageProvider, Core.Tests.dll", typeof(TestSettingsStorageProvider))]
		[TestCase("glglglglglglg, gfgfgfgfggf.dll", typeof(string), ExpectedException = typeof(ArgumentException))]
		public void Static_LoadSettingsStorageProvider(string p, Type type) {
			ISettingsStorageProviderV30 prov = ProviderLoader.LoadSettingsStorageProvider(p);
			Assert.IsNotNull(prov, "Provider should not be null");
			// type == prov.GetType() seems to fail due to reflection
			Assert.AreEqual(type.ToString(), prov.GetType().FullName, "Wrong return type");
		}

	}

}
