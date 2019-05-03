using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace FTPServer
{
	public class FTPHost
	{
		TcpListener _listener;

		public async void Listen(ushort port = 21)
		{
			_listener = new TcpListener(IPAddress.Any, port);
			_listener.Start();
			while (true)
			{
				var client = await _listener.AcceptTcpClientAsync();
				new FtpClient(client);
			}
		}

		class FtpClient
		{
			private readonly TcpClient _client;
			private readonly NetworkStream _stream;
			private readonly StreamWriter _writer;
			private readonly StreamReader _reader;

			public FtpClient(TcpClient client)
			{
				_client = client;
				_stream = _client.GetStream();
				_writer = new StreamWriter(_stream);
				_reader = new StreamReader(_stream);
				_writer.AutoFlush = true;
				_writer.WriteLine("220 FTP Service");
				ReadLines();
			}

			string userName;
			string cd = "/";
			string type = "A";

			async void ReadLines()
			{
				var line = await _reader.ReadLineAsync();

				var cmdi = line.IndexOf(' ');
				var cmd = cmdi > 0 ? line.Substring(0, cmdi).Trim() : line;
				var data = cmdi > 0 ? line.Substring(cmdi).TrimStart() : string.Empty;

				switch (cmd.ToUpperInvariant())
				{
					case "HELP":
						{
							_writer.WriteLine("501 Parameter not understood");
							break;
						}
					case "OPTS":
						{
							_writer.WriteLine("200 Successful");
							break;
						}
					case "TYPE":
						{
							type = data.ToUpperInvariant();
							_writer.WriteLine("200 TYPE set to " + type);
							break;
						}
					case "FEAT":
						{
							_writer.WriteLine("211-Extensions Supported:");
							_writer.WriteLine(" UTF8");
							_writer.WriteLine("211 END");
							break;
						}
					case "PWD":
						{
							_writer.WriteLine($@"257 ""{cd}"" is current directory.");
							break;
						}
					case "SYST":
						{
							_writer.WriteLine("215 Custom");
							break;
						}
					case "USER":
						{
							userName = data;
							_writer.WriteLine("331 Password required");
							break;
						}
					case "PASS":
						{
							_writer.WriteLine("230 User logged in");
							break;
						}
					default:
						_writer.WriteLine("500 Not Supported: " + cmd);
						break;
				}

				ReadLines();
			}
		}
	}
}
