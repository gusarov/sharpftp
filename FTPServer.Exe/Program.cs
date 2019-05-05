using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FTPServer.Exe
{
	class Program
	{
		static void Main(string[] args)
		{
			var svc = new TheWindowsService();
			if (args.Any(x => string.Equals("-service", x, StringComparison.OrdinalIgnoreCase)))
			{
				ServiceBase.Run(new ServiceBase[] { svc });
			}
			else if (args.Any(x => string.Equals("-install", x, StringComparison.OrdinalIgnoreCase)))
			{
				ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
			}
			else if (args.Any(x => string.Equals("-uninstall", x, StringComparison.OrdinalIgnoreCase)))
			{
				ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
			}
			else
			{
				Console.WriteLine("-service: use to run as windows service");
				Console.WriteLine("-install: install windows service");
				Console.WriteLine("-uninstall: uninstall windows service");
				Console.WriteLine("Running as a console applicaiton...");
				svc.StartImpl();
				Console.WriteLine("Press any key to stop . . .");
				Console.ReadKey();
				svc.StopImpl();
			}
			
		}



	}
}
