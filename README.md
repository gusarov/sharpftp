# sharpftp

The simplest possible C# FTP Server from scratch.

For Net Core & Full Net Desktop.

## Full Desktop .NET Executable

app.config:
```xml
<configuration>
	<configSections>..</configSections>
	<startup>..</startup>
	<appSettings>
		<add key="dir" value="_data"/>
		<add key="port" value="821"/>
	</appSettings>
	<auth>
		<add key="bob" value="123" />
		<add key="alice" value="321" />
		<add key="dataPortFrom" value="20000"/>
		<add key="dataPortTo" value="20999"/>
	</auth>
</configuration>
```FTPServer.Exe.exe -install```

Install as Windows Service:
```netsh advfirewall set global StatefulFtp enable```

Automatic stateful firewall rule for Windows:
```netsh advfirewall set global StatefulFtp enable```

## .NET Core Console App

appsettings.json:
```json
{
	"dir": "_data",
	"port": "821",
	"auth": {
		"bob": "123",
		"alice": "321",
	},
}
```
