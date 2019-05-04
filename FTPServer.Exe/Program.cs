using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FTPServer.Exe
{
	class Program
	{
		static void Main(string[] args)
		{
			var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			var dir = ConfigurationManager.AppSettings["dir"];
			var portStr = ConfigurationManager.AppSettings["port"];
			var port = string.IsNullOrEmpty(portStr)
				? default(ushort?)
				: ushort.Parse(portStr)
				;

			if (!Path.IsPathRooted(dir))
			{
				dir = Path.Combine(baseDir, dir);
			}

			var cfg = new FtpHostConfig
			{
				Dir = dir,
			};

			var auth = ConfigurationManager.GetSection("auth") as NameValueCollection;
			foreach (string user in auth)
			{
				cfg.Credentials.Add(user, auth[user]);
			}

			var host = new FTPHost(cfg);
			host.Listen(port);
			Console.ReadLine();
		}
	}
}
