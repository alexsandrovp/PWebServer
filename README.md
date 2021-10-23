# Powershell web server

This is a simple powershell web server. The idea is to have a way to do quick prototyping of simple web UI.

## How to install
Download the installer script at https://github.com/alexsandrovp/PWebServer/blob/1.0.0/installer/Install-PWebServer.ps1
and run it.

```powershell
PS> ./Install-PWebServer.ps1
```

You will be asked to choose in which Module path to copy the module.
Typically, you will have two options:

```
1.	C:\users\username\Documents\PowerShell\Modules
	Use this to install for the current user only

2.	C:\Program Files\PowerShell\Modules
	Use this to install for all users
```

Note: the **username** part is different for each user.

You could modify the environment variable PSModulePath to have more options in this step.

After the installation is complete, new instances of powershell will load the module automatically.

## How to use it
The simplest way:

```powershell
PS> Start-PWebServer
```

Or specifying a path:

```powershell
PS> Start-PWebServer <path to my site>
```

If you don't specify a folder to serve, it will use the **current directory**. The default binding address is *localhost:8080* for non-elevated shells, and *+:80* if you are running elevated.

Using full options:

```powershell
PS> Start-PWebServer -Folder c:\mysite -Secure -ListenAt 192.168.0.1 -Port 8888 -ShowIps
```

Some address/port combinations require administrative privileges to bind to.

The -ShowIps switch causes powershell to print all your ip addresses before starting the server.

This module provides the following cmdlets. After installation, type **Get-Help &lt;cmdlet name&gt;** to get help for each one.
	
```powershell
Start-PWebServer
New-PWebConfig
Select-Certificate
New-HttpsCertificate
New-HttpsCertificateBinding
Get-HttpsCertificateBinding
Remove-HttpsCertificateBinding
```

# Server configuration
You can create a configuration file in the folder being served to configure advanced options.
To learn more about the server configuration, type

```powershell
PS> Get-Help about_PWeb_Config
```
<a href="PWebServer/about_PWeb_Config.help.txt">about_PWeb_Config</a>

# HTTPS

The PWebServer supports the https protocol, but before starting it you must have an https certificate with private key for encryption, and bind this certificate to the port the server will be listening to.

To learn more about certificates, type

```powershell
PS> Get-Help about_PWeb_Certificates
```

<a href="PWebServer/about_PWeb_Certificates.help.txt">about_PWeb_Certificates</a>

# Relaying requests

You can use the server.json file to configure request relaying to other servers, making PWebServer act as a proxy. To learn more about that, see <a href="PWebServer/about_PWeb_Config.help.txt">about_PWeb_Config</a>

If your relayed request has the *Upgrade: websocket* header, the server should be able to negotiate a **ws** of **wss** connection with your backend server.