
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Tests {

	public class FilesStorageProviderTests : FilesStorageProviderTestScaffolding {

		public override IFilesStorageProviderV30 GetProvider() {
			FilesStorageProvider prov = new FilesStorageProvider();
			prov.Init(MockHost(), "");
			return prov;
		}

		[Test]
		public void Init() {
			IFilesStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), "");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

	}

}
