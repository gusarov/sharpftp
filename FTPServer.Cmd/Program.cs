using System;

namespace FTPServer.Cmd
{
	class Program
	{
		static void Main(string[] args)
		{
			var host = new FTPHost();
			host.Listen();
			Console.ReadLine();
		}
	}
}
