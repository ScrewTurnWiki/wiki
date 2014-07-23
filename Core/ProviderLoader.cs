
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using ScrewTurn.Wiki.PluginFramework;
using System.Globalization;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Loads providers from assemblies.
	/// </summary>
	public static class ProviderLoader {

		// These must be const because they are used in switch constructs
		internal const string UsersProviderInterfaceName = "ScrewTurn.Wiki.PluginFramework.IUsersStorageProviderV30";
		internal const string PagesProviderInterfaceName = "ScrewTurn.Wiki.PluginFramework.IPagesStorageProviderV30";
		internal const string FilesProviderInterfaceName = "ScrewTurn.Wiki.PluginFramework.IFilesStorageProviderV30";
		internal const string FormatterProviderInterfaceName = "ScrewTurn.Wiki.PluginFramework.IFormatterProviderV30";
		internal const string CacheProviderInterfaceName = "ScrewTurn.Wiki.PluginFramework.ICacheProviderV30";

		internal static string SettingsStorageProviderAssemblyName = "";

		/// <summary>
		/// Verifies the read-only/read-write constraints of providers.
		/// </summary>
		/// <typeparam name="T">The type of the provider.</typeparam>
		/// <param name="provider">The provider.</param>
		/// <exception cref="T:ProviderConstraintException">Thrown when a constraint is not fulfilled.</exception>
		private static void VerifyConstraints<T>(T provider) {
			if(typeof(T) == typeof(IUsersStorageProviderV30)) {
				// If the provider allows to write user accounts data, then group membership must be writeable too

				IUsersStorageProviderV30 actualInstance = (IUsersStorageProviderV30)provider;
				if(!actualInstance.UserAccountsReadOnly && actualInstance.GroupMembershipReadOnly) {
					throw new ProviderConstraintException("If UserAccountsReadOnly is false, then also GroupMembershipReadOnly must be false");
				}
			}
		}

		/// <summary>
		/// Tries to inizialize a provider.
		/// </summary>
		/// <typeparam name="T">The type of the provider, which must implement <b>IProvider</b>.</typeparam>
		/// <param name="instance">The provider instance to initialize.</param>
		/// <param name="collectorEnabled">The collector for enabled providers.</param>
		/// <param name="collectorDisabled">The collector for disabled providers.</param>
		private static void Initialize<T>(T instance, ProviderCollector<T> collectorEnabled,
			ProviderCollector<T> collectorDisabled) where T : class, IProviderV30 {

			if(collectorEnabled.GetProvider(instance.GetType().FullName) != null ||
				collectorDisabled.GetProvider(instance.GetType().FullName) != null) {

				Log.LogEntry("Provider " + instance.Information.Name + " already in memory", EntryType.Warning, Log.SystemUsername);
				return;
			}

			bool enabled = !IsDisabled(instance.GetType().FullName);
			try {
				if(enabled) {
					instance.Init(Host.Instance, LoadConfiguration(instance.GetType().FullName));
				}
			}
			catch(InvalidConfigurationException) {
				// Disable Provider
				enabled = false;
				Log.LogEntry("Unable to load provider " + instance.Information.Name + " (configuration rejected), disabling it", EntryType.Error, Log.SystemUsername);
				SaveStatus(instance.GetType().FullName, false);
			}
			catch {
				// Disable Provider
				enabled = false;
				Log.LogEntry("Unable to load provider " + instance.Information.Name + " (unknown error), disabling it", EntryType.Error, Log.SystemUsername);
				SaveStatus(instance.GetType().FullName, false);
				throw; // Exception is rethrown because it's not a normal condition
			}
			if(enabled) collectorEnabled.AddProvider(instance);
			else collectorDisabled.AddProvider(instance);

			// Verify constraints
			VerifyConstraints<T>(instance);

			Log.LogEntry("Provider " + instance.Information.Name + " loaded (" + (enabled ? "Enabled" : "Disabled") + ")", EntryType.General, Log.SystemUsername);
		}

		/// <summary>
		/// Loads all the Providers and initialises them.
		/// </summary>
		/// <param name="loadUsers">A value indicating whether to load users storage providers.</param>
		/// <param name="loadPages">A value indicating whether to load pages storage providers.</param>
		/// <param name="loadFiles">A value indicating whether to load files storage providers.</param>
		/// <param name="loadFormatters">A value indicating whether to load formatter providers.</param>
		/// <param name="loadCache">A value indicating whether to load cache providers.</param>
		public static void FullLoad(bool loadUsers, bool loadPages, bool loadFiles, bool loadFormatters, bool loadCache) {
			string[] pluginAssemblies = Settings.Provider.ListPluginAssemblies();

			List<IUsersStorageProviderV30> users = new List<IUsersStorageProviderV30>(2);
			List<IUsersStorageProviderV30> dUsers = new List<IUsersStorageProviderV30>(2);
			List<IPagesStorageProviderV30> pages = new List<IPagesStorageProviderV30>(2);
			List<IPagesStorageProviderV30> dPages = new List<IPagesStorageProviderV30>(2);
			List<IFilesStorageProviderV30> files = new List<IFilesStorageProviderV30>(2);
			List<IFilesStorageProviderV30> dFiles = new List<IFilesStorageProviderV30>(2);
			List<IFormatterProviderV30> forms = new List<IFormatterProviderV30>(2);
			List<IFormatterProviderV30> dForms = new List<IFormatterProviderV30>(2);
			List<ICacheProviderV30> cache = new List<ICacheProviderV30>(2);
			List<ICacheProviderV30> dCache = new List<ICacheProviderV30>(2);

			for(int i = 0; i < pluginAssemblies.Length; i++) {
				IFilesStorageProviderV30[] d;
				IUsersStorageProviderV30[] u;
				IPagesStorageProviderV30[] p;
				IFormatterProviderV30[] f;
				ICacheProviderV30[] c;
				LoadFrom(pluginAssemblies[i], out u, out p, out d, out f, out c);
				if(loadFiles) files.AddRange(d);
				if(loadUsers) users.AddRange(u);
				if(loadPages) pages.AddRange(p);
				if(loadFormatters) forms.AddRange(f);
				if(loadCache) cache.AddRange(c);
			}

			// Init and add to the Collectors, starting from files providers
			for(int i = 0; i < files.Count; i++) {
				Initialize<IFilesStorageProviderV30>(files[i], Collectors.FilesProviderCollector, Collectors.DisabledFilesProviderCollector);
			}

			for(int i = 0; i < users.Count; i++) {
				Initialize<IUsersStorageProviderV30>(users[i], Collectors.UsersProviderCollector, Collectors.DisabledUsersProviderCollector);
			}

			for(int i = 0; i < pages.Count; i++) {
				Initialize<IPagesStorageProviderV30>(pages[i], Collectors.PagesProviderCollector, Collectors.DisabledPagesProviderCollector);
			}

			for(int i = 0; i < forms.Count; i++) {
				Initialize<IFormatterProviderV30>(forms[i], Collectors.FormatterProviderCollector, Collectors.DisabledFormatterProviderCollector);
			}

			for(int i = 0; i < cache.Count; i++) {
				Initialize<ICacheProviderV30>(cache[i], Collectors.CacheProviderCollector, Collectors.DisabledCacheProviderCollector);
			}
		}

		/// <summary>
		/// Loads the Configuration data of a Provider.
		/// </summary>
		/// <param name="typeName">The Type Name of the Provider.</param>
		/// <returns>The Configuration, if available, otherwise an empty string.</returns>
		public static string LoadConfiguration(string typeName) {
			return Settings.Provider.GetPluginConfiguration(typeName);
		}

		/// <summary>
		/// Saves the Configuration data of a Provider.
		/// </summary>
		/// <param name="typeName">The Type Name of the Provider.</param>
		/// <param name="config">The Configuration data to save.</param>
		public static void SaveConfiguration(string typeName, string config) {
			Settings.Provider.SetPluginConfiguration(typeName, config);
		}

		/// <summary>
		/// Saves the Status of a Provider.
		/// </summary>
		/// <param name="typeName">The Type Name of the Provider.</param>
		/// <param name="enabled">A value specifying whether or not the Provider is enabled.</param>
		public static void SaveStatus(string typeName, bool enabled) {
			Settings.Provider.SetPluginStatus(typeName, enabled);
		}

		/// <summary>
		/// Returns a value specifying whether or not a Provider is disabled.
		/// </summary>
		/// <param name="typeName">The Type Name of the Provider.</param>
		/// <returns>True if the Provider is disabled.</returns>
		public static bool IsDisabled(string typeName) {
			return !Settings.Provider.GetPluginStatus(typeName);
		}

		/// <summary>
		/// Loads Providers from an assembly.
		/// </summary>
		/// <param name="assembly">The path of the Assembly to load the Providers from.</param>
		public static int LoadFromAuto(string assembly) {
			IUsersStorageProviderV30[] users;
			IPagesStorageProviderV30[] pages;
			IFilesStorageProviderV30[] files;
			IFormatterProviderV30[] forms;
			ICacheProviderV30[] cache;
			LoadFrom(assembly, out users, out pages, out files, out forms, out cache);

			int count = 0;

			// Init and add to the Collectors, starting from files providers
			for(int i = 0; i < files.Length; i++) {
				Initialize<IFilesStorageProviderV30>(files[i], Collectors.FilesProviderCollector, Collectors.DisabledFilesProviderCollector);
				count++;
			}

			for(int i = 0; i < users.Length; i++) {
				Initialize<IUsersStorageProviderV30>(users[i], Collectors.UsersProviderCollector, Collectors.DisabledUsersProviderCollector);
				count++;
			}

			for(int i = 0; i < pages.Length; i++) {
				Initialize<IPagesStorageProviderV30>(pages[i], Collectors.PagesProviderCollector, Collectors.DisabledPagesProviderCollector);
				count++;
			}

			for(int i = 0; i < forms.Length; i++) {
				Initialize<IFormatterProviderV30>(forms[i], Collectors.FormatterProviderCollector, Collectors.DisabledFormatterProviderCollector);
				count++;
			}

			for(int i = 0; i < cache.Length; i++) {
				Initialize<ICacheProviderV30>(cache[i], Collectors.CacheProviderCollector, Collectors.DisabledCacheProviderCollector);
				count++;
			}

			return count;
		}

		/// <summary>
		/// Loads Providers from an assembly.
		/// </summary>
		/// <param name="assembly">The path of the Assembly to load the Providers from.</param>
		/// <param name="users">The Users Providers.</param>
		/// <param name="files">The Files Providers.</param>
		/// <param name="pages">The Pages Providers.</param>
		/// <param name="formatters">The Formatter Providers.</param>
		/// <param name="cache">The Cache Providers.</param>
		/// <remarks>The Components returned are <b>not</b> initialized.</remarks>
		public static void LoadFrom(string assembly, out IUsersStorageProviderV30[] users, out IPagesStorageProviderV30[] pages,
			out IFilesStorageProviderV30[] files, out IFormatterProviderV30[] formatters, out ICacheProviderV30[] cache) {

			Assembly asm = null;
			try {
				//asm = Assembly.LoadFile(assembly);
				// This way the DLL is not locked and can be deleted at runtime
				asm = Assembly.Load(LoadAssemblyFromProvider(Path.GetFileName(assembly)));
			}
			catch {
				files = new IFilesStorageProviderV30[0];
				users = new IUsersStorageProviderV30[0];
				pages = new IPagesStorageProviderV30[0];
				formatters = new IFormatterProviderV30[0];
				cache = new ICacheProviderV30[0];

				Log.LogEntry("Unable to load assembly " + Path.GetFileNameWithoutExtension(assembly), EntryType.Error, Log.SystemUsername);
				return;
			}

			Type[] types = null;

			try {
				types = asm.GetTypes();
			}
			catch(ReflectionTypeLoadException) {
				files = new IFilesStorageProviderV30[0];
				users = new IUsersStorageProviderV30[0];
				pages = new IPagesStorageProviderV30[0];
				formatters = new IFormatterProviderV30[0];
				cache = new ICacheProviderV30[0];

				Log.LogEntry("Unable to load providers from (probably v2) assembly " + Path.GetFileNameWithoutExtension(assembly), EntryType.Error, Log.SystemUsername);
				return;
			}

			List<IUsersStorageProviderV30> urs = new List<IUsersStorageProviderV30>();
			List<IPagesStorageProviderV30> pgs = new List<IPagesStorageProviderV30>();
			List<IFilesStorageProviderV30> fls = new List<IFilesStorageProviderV30>();
			List<IFormatterProviderV30> frs = new List<IFormatterProviderV30>();
			List<ICacheProviderV30> che = new List<ICacheProviderV30>();

			Type[] interfaces;
			for(int i = 0; i < types.Length; i++) {
				// Avoid to load abstract classes as they cannot be instantiated
				if(types[i].IsAbstract) continue;

				interfaces = types[i].GetInterfaces();
				foreach(Type iface in interfaces) {
					if(iface == typeof(IUsersStorageProviderV30)) {
						IUsersStorageProviderV30 tmpu = CreateInstance<IUsersStorageProviderV30>(asm, types[i]);
						if(tmpu != null) {
							urs.Add(tmpu);
							Collectors.FileNames[tmpu.GetType().FullName] = assembly;
						}
					}
					if(iface == typeof(IPagesStorageProviderV30)) {
						IPagesStorageProviderV30 tmpp = CreateInstance<IPagesStorageProviderV30>(asm, types[i]);
						if(tmpp != null) {
							pgs.Add(tmpp);
							Collectors.FileNames[tmpp.GetType().FullName] = assembly;
						}
					}
					if(iface == typeof(IFilesStorageProviderV30)) {
						IFilesStorageProviderV30 tmpd = CreateInstance<IFilesStorageProviderV30>(asm, types[i]);
						if(tmpd != null) {
							fls.Add(tmpd);
							Collectors.FileNames[tmpd.GetType().FullName] = assembly;
						}
					}
					if(iface == typeof(IFormatterProviderV30)) {
						IFormatterProviderV30 tmpf = CreateInstance<IFormatterProviderV30>(asm, types[i]);
						if(tmpf != null) {
							frs.Add(tmpf);
							Collectors.FileNames[tmpf.GetType().FullName] = assembly;
						}
					}
					if(iface == typeof(ICacheProviderV30)) {
						ICacheProviderV30 tmpc = CreateInstance<ICacheProviderV30>(asm, types[i]);
						if(tmpc != null) {
							che.Add(tmpc);
							Collectors.FileNames[tmpc.GetType().FullName] = assembly;
						}
					}
				}
			}

			users = urs.ToArray();
			pages = pgs.ToArray();
			files = fls.ToArray();
			formatters = frs.ToArray();
			cache = che.ToArray();
		}

		/// <summary>
		/// Creates an instance of a type implementing a provider interface.
		/// </summary>
		/// <typeparam name="T">The provider interface type.</typeparam>
		/// <param name="asm">The assembly that contains the type.</param>
		/// <param name="type">The type to create an instance of.</param>
		/// <returns>The instance, or <c>null</c>.</returns>
		private static T CreateInstance<T>(Assembly asm, Type type) where T : class, IProviderV30 {
			T instance;
			try {
				instance = asm.CreateInstance(type.ToString()) as T;
				return instance;
			}
			catch {
				Log.LogEntry("Unable to create instance of " + type.ToString(), EntryType.Error, Log.SystemUsername);
				throw;
			}
		}

		/// <summary>
		/// Loads the content of an assembly from disk.
		/// </summary>
		/// <param name="assembly">The assembly file full path.</param>
		/// <returns>The content of the assembly, in a byte array form.</returns>
		private static byte[] LoadAssemblyFromDisk(string assembly) {
			return File.ReadAllBytes(assembly);
		}

		/// <summary>
		/// Loads the content of an assembly from the settings provider.
		/// </summary>
		/// <param name="assemblyName">The name of the assembly, such as "Assembly.dll".</param>
		/// <returns>The content fo the assembly.</returns>
		private static byte[] LoadAssemblyFromProvider(string assemblyName) {
			return Settings.Provider.RetrievePluginAssembly(assemblyName);
		}

		/// <summary>
		/// Loads the proper Setting Storage Provider, given its name.
		/// </summary>
		/// <param name="name">The fully qualified name (such as "Namespace.ProviderClass, MyAssembly"), or <c>null</c>/<b>String.Empty</b>/"<b>default</b>" for the default provider.</param>
		/// <returns>The settings storage provider.</returns>
		public static ISettingsStorageProviderV30 LoadSettingsStorageProvider(string name) {
			if(name == null || name.Length == 0 || string.Compare(name, "default", true, CultureInfo.InvariantCulture) == 0) {
				return new SettingsStorageProvider();
			}

			ISettingsStorageProviderV30 result = null;

			Exception inner = null;

			if(name.Contains(",")) {
				string[] fields = name.Split(',');
				if(fields.Length == 2) {
					fields[0] = fields[0].Trim(' ', '"');
					fields[1] = fields[1].Trim(' ', '"');
					try {
						// assemblyName should be an absolute path or a relative path in bin or public\Plugins

						Assembly asm;
						Type t;
						string assemblyName = fields[1];
						if(!assemblyName.ToLowerInvariant().EndsWith(".dll")) assemblyName += ".dll";

						if(File.Exists(assemblyName)) {
							asm = Assembly.Load(LoadAssemblyFromDisk(assemblyName));
							t = asm.GetType(fields[0]);
							SettingsStorageProviderAssemblyName = Path.GetFileName(assemblyName);
						}
						else {
							string tentativePluginsPath = null;
							try {
								// Settings.PublicDirectory is only available when running the web app
								tentativePluginsPath = Path.Combine(Settings.PublicDirectory, "Plugins");
								tentativePluginsPath = Path.Combine(tentativePluginsPath, assemblyName);
							}
							catch { }

							if(!string.IsNullOrEmpty(tentativePluginsPath) && File.Exists(tentativePluginsPath)) {
								asm = Assembly.Load(LoadAssemblyFromDisk(tentativePluginsPath));
								t = asm.GetType(fields[0]);
								SettingsStorageProviderAssemblyName = Path.GetFileName(tentativePluginsPath);
							}
							else {
								// Trim .dll
								t = Type.GetType(fields[0] + "," + assemblyName.Substring(0, assemblyName.Length - 4), true, true);
								SettingsStorageProviderAssemblyName = assemblyName;
							}
						}

						result = t.GetConstructor(new Type[0]).Invoke(new object[0]) as ISettingsStorageProviderV30;
					}
					catch(Exception ex) {
						inner = ex;
						result = null;
					}
				}
			}

			if(result == null) throw new ArgumentException("Could not load the specified Settings Storage Provider", inner);
			else return result;
		}

		/// <summary>
		/// Loads all settings storage providers available in all DLLs stored in a provider.
		/// </summary>
		/// <param name="repository">The input provider.</param>
		/// <returns>The providers found (not initialized).</returns>
		public static ISettingsStorageProviderV30[] LoadAllSettingsStorageProviders(ISettingsStorageProviderV30 repository) {
			// This method is actually a memory leak because it can be executed multimple times
			// Every time it loads a set of assemblies which cannot be unloaded (unless a separate AppDomain is used)

			List<ISettingsStorageProviderV30> result = new List<ISettingsStorageProviderV30>();

			foreach(string dll in repository.ListPluginAssemblies()) {
				byte[] asmBin = repository.RetrievePluginAssembly(dll);
				Assembly asm = Assembly.Load(asmBin);

				Type[] types = null;
				try {
					types = asm.GetTypes();
				}
				catch(ReflectionTypeLoadException) {
					// Skip assembly
					Log.LogEntry("Unable to load providers from (probably v2) assembly " + Path.GetFileNameWithoutExtension(dll), EntryType.Error, Log.SystemUsername);
					continue;
				}

				foreach(Type type in types) {
					// Avoid to load abstract classes as they cannot be instantiated
					if(type.IsAbstract) continue;

					Type[] interfaces = type.GetInterfaces();

					foreach(Type iface in interfaces) {
						if(iface == typeof(ISettingsStorageProviderV30)) {
							try {
								ISettingsStorageProviderV30 temp = asm.CreateInstance(type.ToString()) as ISettingsStorageProviderV30;
								if(temp != null) result.Add(temp);
							}
							catch { }
						}
					}
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Tries to change a provider's configuration.
		/// </summary>
		/// <param name="typeName">The provider.</param>
		/// <param name="configuration">The new configuration.</param>
		/// <param name="error">The error message, if any.</param>
		/// <returns><c>true</c> if the configuration is saved, <c>false</c> if the provider rejected it.</returns>
		public static bool TryChangeConfiguration(string typeName, string configuration, out string error) {
			error = null;

			bool enabled, canDisable;
			IProviderV30 provider = Collectors.FindProvider(typeName, out enabled, out canDisable);

			try {
				provider.Init(Host.Instance, configuration);
			}
			catch(InvalidConfigurationException icex) {
				error = icex.Message;
				return false;
			}

			SaveConfiguration(typeName, configuration);
			return true;
		}

		/// <summary>
		/// Disables a provider.
		/// </summary>
		/// <param name="typeName">The provider to disable.</param>
		public static void DisableProvider(string typeName) {
			bool enabled, canDisable;
			IProviderV30 provider = Collectors.FindProvider(typeName, out enabled, out canDisable);
			if(enabled && canDisable) {
				provider.Shutdown();
				Collectors.TryDisable(typeName);
				SaveStatus(typeName, false);
			}
		}

		/// <summary>
		/// Enables a provider.
		/// </summary>
		/// <param name="typeName">The provider to enable.</param>
		public static void EnableProvider(string typeName) {
			bool enabled, canDisable;
			IProviderV30 provider = Collectors.FindProvider(typeName, out enabled, out canDisable);
			if(!enabled) {
				provider.Init(Host.Instance, LoadConfiguration(typeName));
				Collectors.TryEnable(typeName);
				SaveStatus(typeName, true);
			}
		}

		/// <summary>
		/// Unloads a provider from memory.
		/// </summary>
		/// <param name="typeName">The provider to unload.</param>
		public static void UnloadProvider(string typeName) {
			DisableProvider(typeName);
			Collectors.TryUnload(typeName);
		}

	}

	/// <summary>
	/// Defines an exception thrown when a constraint is not fulfilled by a provider.
	/// </summary>
	public class ProviderConstraintException : Exception {

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ProviderConstraintException" /> class.
		/// </summary>
		/// <param name="message">The message.</param>
		public ProviderConstraintException(string message)
			: base(message) { }

	}

}
