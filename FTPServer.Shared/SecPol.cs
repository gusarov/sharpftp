using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Net.Sockets;

namespace FTPServer
{
	public class SecPol
	{
		class Stat
		{
			public ConcurrentDictionary<string, DateTime> UserNames
				= new ConcurrentDictionary<string, DateTime>();
			public ConcurrentBag<DateTime> FailedLogins
				= new ConcurrentBag<DateTime>();
		}

		ConcurrentDictionary<IPAddress, Stat> _stat = new ConcurrentDictionary<IPAddress, Stat>();

		byte _cleanSeed;

		Stat GetStatForIp(IPAddress ip)
		{
			var stat = _stat.GetOrAdd(ip, x => new Stat());
			if (unchecked(++_cleanSeed) == 0)
			{
				Cleanup(stat);
			}
			return stat;
		}

		public void LoginAttempt(Socket socket, string user, bool? passwordIsGood = null)
		{
			var ip = ((IPEndPoint)socket.RemoteEndPoint).Address;
			// up to 5 users from IP

			var stat = GetStatForIp(ip);
			stat.UserNames[user] = DateTime.UtcNow;
			if (stat.UserNames.Count > 5)
			{
				throw new SecurityException("Too many user names from single IP");
			}

			if (passwordIsGood == false)
			{
				stat.FailedLogins.Add(DateTime.UtcNow);
				if (stat.FailedLogins.Count > 10)
				{
					throw new SecurityException("Too many failed logins from single IP");
				}
			}
		}

		void Cleanup(Stat stat)
		{
			var now = DateTime.UtcNow;

			var keepF = stat.FailedLogins.Where(x => (now - x).TotalDays < 1);
			stat.FailedLogins = new ConcurrentBag<DateTime>();
			foreach (var item in keepF)
			{
				stat.FailedLogins.Add(item);
			}

			var keepU = stat.UserNames.Where(x => (now - x.Value).TotalDays < 1);
			stat.UserNames = new ConcurrentDictionary<string, DateTime>();
			foreach (var item in keepU)
			{
				stat.UserNames[item.Key] = item.Value;
			}
		}
	}


	[Serializable]
	public class SecurityException : Exception
	{
		public SecurityException() { }
		public SecurityException(string message) : base(message) { }
		public SecurityException(string message, Exception inner) : base(message, inner) { }
		protected SecurityException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
