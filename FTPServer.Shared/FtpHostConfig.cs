using System;
using System.Collections.Generic;
using System.Text;

namespace FTPServer
{
	public class FtpHostConfig
	{
		public string Dir { get; set; }
		public Dictionary<string, string> Credentials { get; }
			= new Dictionary<string, string>();
	}
}
