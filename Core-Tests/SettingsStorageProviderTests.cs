
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using NUnit.Framework;
using Rhino.Mocks;

namespace ScrewTurn.Wiki.Tests {

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
