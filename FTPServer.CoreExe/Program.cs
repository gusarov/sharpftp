using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace FTPServer.Cmd
{
	class Program
	{
		static void Main(string[] args)
		{
			var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var builder = new ConfigurationBuilder()
				.SetBasePath(baseDir)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				;
			var configuration = builder.Build();

			var dir = configuration["dir"];
			var port = ParseUshort(configuration["port"]);
			var dataPortFrom = ParseUshort(configuration["dataPortFrom"]);
			var dataPortTo = ParseUshort(configuration["dataPortTo"]);

			if (!Path.IsPathRooted(dir))
			{
				dir = Path.Combine(baseDir, dir);
			}

			var cfg = new FtpHostConfig
			{
				Dir = dir,
				Port = port,
				DataPortFrom = dataPortFrom,
				DataPortTo = dataPortTo,
			};

			var auth  = configuration.GetSection("auth");
			foreach (var item in auth.GetChildren())
			{
				cfg.Credentials.Add(item.Key, item.Value);
			}

			var host = new FTPHost(cfg);
			host.Listen();
			Console.ReadLine();
		}

		static ushort? ParseUshort(string data)
		{
			return string.IsNullOrEmpty(data)
				? default(ushort?)
				: ushort.Parse(data)
				;
		}
	}
}
