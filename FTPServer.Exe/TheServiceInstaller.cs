using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace FTPServer.Exe
{
	[RunInstaller(true)]
	public partial class TheServiceInstaller : System.Configuration.Install.Installer
	{
		public TheServiceInstaller()
		{
			InitializeComponent();
		}



		protected override void OnBeforeInstall(IDictionary savedState)
		{
			try
			{
				using (var sc = new ServiceController(serviceInstaller1.ServiceName))
				{
					sc.Stop();
				}
			}
			catch
			{

			}
			const string assemblyPathContextKey = "AssemblyPath";
			var path = Context.Parameters[assemblyPathContextKey];
			Context.Parameters[assemblyPathContextKey] = "\"" + path + "\" -service";
			base.OnBeforeInstall(savedState);
		}

		protected override void OnAfterInstall(IDictionary savedState)
		{
			base.OnAfterInstall(savedState);
			/*
			Console.WriteLine("AfterInstall...");
			var system = Registry.LocalMachine.OpenSubKey("System");
			var currentControlSet = system.OpenSubKey("CurrentControlSet");
			var servicesKey = currentControlSet.OpenSubKey("Services");
			var serviceKey = servicesKey.OpenSubKey(serviceInstaller1.ServiceName, true);
			serviceKey.SetValue("ImagePath", (string)serviceKey.GetValue("ImagePath") + " -service");
			*/
			try
			{
				using (var sc = new ServiceController(serviceInstaller1.ServiceName))
				{
					sc.Start();
				}
			}
			catch
			{
				Console.WriteLine("Unable to start service.");
			}
		}

		protected override void OnBeforeUninstall(IDictionary savedState)
		{
			try
			{
				using (var sc = new ServiceController(serviceInstaller1.ServiceName))
				{
					sc.Stop();
				}
			}
			catch
			{

			}
			base.OnBeforeUninstall(savedState);
		}
	}

}
