using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FTPServer
{
	public class Root
	{
		public static SecPol SecPol = new SecPol();
	}

	public class FTPHost
	{
		private readonly FtpHostConfig _config;

		public FTPHost(FtpHostConfig config)
		{
			_config = config;
		}

		TcpListener _listener;

		public async void Listen(ushort? port = null)
		{
			if (port == null)
			{
				port = 21;
			}
			_listener = new TcpListener(IPAddress.Any, port.Value);
			_listener.Start();
			Console.WriteLine($"FTP Server started on {port} port");
			while (true)
			{
				var client = await _listener.AcceptTcpClientAsync();
				new FtpClient(client, _config);
			}
		}

		class FtpClient
		{
			private readonly FtpHostConfig _config;

			private readonly TcpClient _client;
			private readonly NetworkStream _stream;
			private readonly StreamWriter _writer;
			private readonly StreamReader _reader;

			TcpListener _dataListener;
			private TcpClient _dataClient;
			private NetworkStream _dataStream;
			private StreamWriter _dataWriter;
			private StreamReader _dataReader;

			public FtpClient(TcpClient client, FtpHostConfig config)
			{
				_client = client;
				_config = config;
				_client.NoDelay = true;
				_stream = _client.GetStream();
				_writer = new LoggedWriter(_stream);
				_reader = new LoggedReader(_stream);
				_writer.AutoFlush = true;
				_writer.WriteLine("220 Custom FTP Service");
				Console.WriteLine("Connected from " + _client.Client.RemoteEndPoint);
				ReadLines();
			}

			string rnfr;
			string rnto;

			string userName;
			bool authorised;
			string cd = "/";
			string type = "A";
			string mode;
			string stru;
			int port = 0;

			async Task<bool> EnsureData()
			{
				if (port == 0)
				{
					_writer.WriteLine("425 passive mode is not requested yet");
					return false;
				}
				for (int i = 0; i < 100; i++)
				{
					var dc = _dataClient;
					var ds = _dataStream;
					if (ds == null || _dataClient == null || !_dataClient.Connected)
					{
						await Task.Delay(100);
					}
					else
					{
						return true;
					}
				}
				_writer.WriteLine("425 Data channel is not connected yet");
				return false;
			}

			void DataClose()
			{
				try { _dataClient?.Client?.Shutdown(SocketShutdown.Both); } catch { }
				try { _dataClient?.Close(); } catch { }
				_dataClient = null;
				_dataStream = null;
				_dataReader = null;
				_dataWriter = null;
			}

			async void ReadLines()
			{
				try
				{
					var line = await _reader.ReadLineAsync();
					if (line == null)
					{
						return;
					}
					var cmdi = line.IndexOf(' ');
					var cmd = cmdi > 0 ? line.Substring(0, cmdi).Trim() : line;
					var data = cmdi > 0 ? line.Substring(cmdi).TrimStart() : string.Empty;

					switch (cmd.ToUpperInvariant())
					{
						case "LIST": // not RFC required but defacto standard
							{
								EnsureLoggedIn();
								_writer.WriteLine("125 list");
								/*
								if (_dataClient == null)
								{
									_writer.WriteLine("150 list");
								}
								else
								{
									_writer.WriteLine("125 list");
								}
								*/
								if (await EnsureData())
								{
									try
									{
										_dataWriter.AutoFlush = false;
										foreach (var file in new DirectoryInfo(_config.Dir).GetFiles())
										{
											// ugly and hard to parse output
											_dataWriter.WriteLine($"{file.LastWriteTime:MM-dd-yy}  {file.LastWriteTime:HH:mm}  {file.Length}  {file.Name}");

										}
										_dataWriter.Flush();
									}
									finally
									{
										_dataWriter.AutoFlush = true;
									}
									_dataStream.Close();
									_writer.WriteLine("226 OK");
								}
								break;
							}
						case "RETR": // required minimum by RFC
							{
								EnsureLoggedIn();
								var file = Path.Combine(_config.Dir, data);
								CheckPath(file);
								if (_dataClient == null)
								{
									_writer.WriteLine("150 transferring");
								}
								else
								{
									_writer.WriteLine("125 transferring");
								}
								if (await EnsureData())
								{
									if (File.Exists(file))
									{

										// todo streams for large files
										var buf = File.ReadAllBytes(file);
										_dataStream.Write(buf, 0, buf.Length);

										_dataStream.Close();
										_writer.WriteLine("226 OK retr");
									}
									else
									{
										_writer.WriteLine("550 No file");
										DataClose();
									}
								}
								break;
							}
						case "STOR": // required minimum by RFC
							{
								EnsureLoggedIn();
								var file = Path.Combine(_config.Dir, data);
								CheckPath(file);
								if (_dataClient == null)
								{
									_writer.WriteLine("150 transferring");
								}
								else
								{
									_writer.WriteLine("125 transferring");
								}
								if (await EnsureData())
								{

									var buf = new byte[1024 * 1024];
									using (var fileHandle = File.OpenWrite(file))
									{
										try
										{
											var totalLen = 0L;
											while (true)
											{
												var len = _dataStream.Read(buf, 0, buf.Length);
												if (len == 0)
												{
													break;
												}
												totalLen += len;
												Console.WriteLine($"{len} bytes received");
												fileHandle.Write(buf, 0, len);
											}
											fileHandle.SetLength(totalLen); // truncate
										}
										catch (Exception ex)
										{
											Console.WriteLine(ex);
										}
									}
									// _dataStream.Write(buf, 0, buf.Length);
									// _dataStream.Close();
									_writer.WriteLine("226 OK stored");
								}
								break;
							}
						case "SIZE":
							{
								EnsureLoggedIn();
								var file = Path.Combine(_config.Dir, data);
								CheckPath(file);
								Catch(() =>
								{
									if (File.Exists(file))
									{
										var len = new FileInfo(file).Length;
										_writer.WriteLine($"213 {len}");
									}
									else
									{
										_writer.WriteLine($"550 file does not exists");
									}
								});
								break;
							}
						case "DELE":
							{
								EnsureLoggedIn();
								var file = Path.Combine(_config.Dir, data);
								CheckPath(file);
								Catch(() =>
								{
									if (File.Exists(file))
									{
										File.Delete(file);
									}
									_writer.WriteLine($"250 DELE comand successful.");
								});
								break;
							}
						case "RNFR":
							{
								EnsureLoggedIn();
								var file = Path.Combine(_config.Dir, data);
								CheckPath(file);
								rnfr = file;
								_writer.WriteLine("350 OK RNFR");
								break;
							}
						case "RNTO":
							{
								EnsureLoggedIn();
								var file = Path.Combine(_config.Dir, data);
								CheckPath(file);
								Catch(() =>
								{
									File.Move(rnfr, file);
									_writer.WriteLine("200 OK RNTO");
								});
								break;
							}
						case "NOOP": // required minimum by RFC
							{
								_writer.WriteLine("200 OK Noop");
								break;
							}
						case "QUIT": // rfc minimum
							{
								_writer.WriteLine("221 OK Quit");

								DataClose();

								try { _client.Client.Shutdown(SocketShutdown.Both); } catch { }
								try { _client.Close(); } catch { }

								if (_dataListener != null)
								{
									try
									{
										_dataListener.Stop();
									}
									catch { }
								}

								return;
								break;
							}
						case "PASV":
							{
								DataClose();

								var ip = ((IPEndPoint)_client.Client.LocalEndPoint).Address;

								if (_dataListener != null)
								{
									try
									{
										_dataListener.Stop();
									}
									catch { }
								}
								_dataListener = new TcpListener(IPAddress.Any, 0);
								_dataListener.Start();
								DataConnection();

								port = ((IPEndPoint)_dataListener.LocalEndpoint).Port;
								var h = (byte)(port >> 8);
								var l = (byte)(port % 256);
								//_writer.WriteLine("200 Successful");

								// todo IPv6 response 228 long passive mode - lack of documentation
								var rline = $"227 Entering Passive Mode ({ip.ToString().Replace('.', ',')},{h},{l}).";
								_writer.WriteLine(rline);
								break;
							}
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
						case "TYPE": // rfc minimum
							{
								type = data.ToUpperInvariant();
								_writer.WriteLine("200 TYPE set to " + type);
								break;
							}
						case "MODE": // rfc minimum
							{
								mode = data.ToUpperInvariant();
								_writer.WriteLine("200 MODE set to " + mode);
								break;
							}
						case "STRU": // rfc minimum
							{
								stru = data.ToUpperInvariant();
								_writer.WriteLine("200 MODE set to " + stru);
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
						case "CWD":
							{
								if (Path.DirectorySeparatorChar != '/') // for Windows
								{
									data = data.Replace('/', Path.DirectorySeparatorChar);
								}
								data = data.TrimStart(Path.DirectorySeparatorChar);
								var newDir = data;
								newDir = Path.GetFullPath(Path.Combine(_config.Dir, newDir));
								CheckPath(newDir);
								cd = newDir.Substring(_config.Dir.TrimEnd('\\', '/').Length);
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
								/*
								if (userName == "anonymous")
								{
									_writer.WriteLine("530 User can not log in");
								}
								else
								{
									_writer.WriteLine("331 Password required");
								}
								*/
								break;
							}
						case "PASS":
							{
								var exist = _config.Credentials.TryGetValue(userName, out var pwd);
								var good = exist && pwd == data;
								authorised = good;
								// Root.SecPol.LoginAttempt(_client.Client, data, good);
								if (good)
								{
									_writer.WriteLine("230 User logged in");
								}
								else
								{
									_writer.WriteLine("530 User cannot log in.");
								}
								break;
							}
						default:
							{
								_writer.WriteLine("500 Not Supported: " + cmd);
								break;
							}
					}

					ReadLines();
				}
				catch (ProtocolException ex)
				{
					_writer.WriteLine($"{ex.Code ?? 550} " + ex.Message);
					ReadLines();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					try
					{
						_dataListener?.Stop();
					}
					catch { }
					_dataListener = null;
					try
					{
						_dataClient?.Close();
					}
					catch { }
					_dataClient = null;
					_dataStream = null;
					_dataReader = null;
					_dataWriter = null;
					try
					{
						_client?.Close();
					}
					catch { }
				}
			}

			void CheckPath(string path)
			{
				if (!path.ToLowerInvariant().TrimEnd('\\', '/').StartsWith(_config.Dir.ToLowerInvariant()))
				{
					throw new ProtocolException("Illegal dirrectory navigation")
					{
						Code = 550,
					};
				}
			}

			void EnsureLoggedIn()
			{
				if (!authorised)
				{
					throw new ProtocolException("Please login with USER and PASS.")
					{
						Code = 530,
					};
				}
			}

			void Catch(Action act)
			{
				try
				{
					act();
				}
				catch (Exception ex)
				{
					_writer.WriteLine($"550 Error: {ex.GetType().Name} {ex.Message}");
				}
			}

			async void DataConnection()
			{
				try
				{
					var cli = await _dataListener.AcceptTcpClientAsync();
					var dc = _dataClient;
					if (dc != null)
					{
						_dataClient = null;
						try
						{
							dc.Close();
						}
						catch { }
					}
					_dataClient = cli;
					Console.WriteLine("Data Connected from " + _dataClient.Client.RemoteEndPoint);
					// Some clients don't like this line:
					// _writer.WriteLine("150 Connection accepted");
					_dataStream = _dataClient.GetStream();
					_dataWriter = new LoggedWriter(_dataStream);
					_dataReader = new LoggedReader(_dataStream);
					_dataWriter.AutoFlush = true;
					// ReadDataLines(_dataReader);
					DataConnection();
				}
				catch (ObjectDisposedException) { } // listener stopped outside of this
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			}

			async void ReadDataLines(StreamReader reader)
			{
				var line = await reader.ReadLineAsync();
				// ...
				ReadDataLines(reader);
			}
		}
	}


	[Serializable]
	public class ProtocolException : Exception
	{
		public int? Code { get; set; }
		public ProtocolException() { }
		public ProtocolException(string message) : base(message) { }
		public ProtocolException(string message, Exception inner) : base(message, inner) { }
		protected ProtocolException(
		System.Runtime.Serialization.SerializationInfo info,
		System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}

	class LoggedReader : StreamReader
	{
		public LoggedReader(Stream stream) : base(stream)
		{
		}

		public override async Task<string> ReadLineAsync()
		{
			var line = await base.ReadLineAsync();
			Console.WriteLine(line);
			return line;
		}
	}

	class LoggedWriter : StreamWriter
	{
		public LoggedWriter(Stream stream) : base(stream)
		{
		}

		public override void WriteLine(string value)
		{
			Console.WriteLine(value);
			base.WriteLine(value);
		}
	}

}
