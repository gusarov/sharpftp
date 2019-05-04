using System;
using System.Collections.Generic;
using System.Text;

namespace FTPServer
{
	public class FtpHostConfig
	{
		public ushort? Port { get; set; }
		public ushort? DataPortFrom { get; set; }
		public ushort? DataPortTo { get; set; }
		public string Dir { get; set; }
		public Dictionary<string, string> Credentials { get; }
			= new Dictionary<string, string>();
	}
}
