@{
	ModuleVersion     = '2.0.0'
	PowerShellVersion = '7.0'
	GUID              = 'a41cf228-d4e8-4322-b5df-e6b8c4695572'
	Author            = 'Alex Vargas'
	CompanyName       = 'The Void Company'
	Copyright         = '(c) 2021 Alex Vargas. All rights reserved.'
	Description       = 'A simple powershell webserver'
	RootModule        = 'PWebServer.dll'
	NestedModules     = @('certificates.ps1', 'utils.ps1')
	FunctionsToExport = @(
		'Select-Certificate',
		'New-HttpsCertificate',
		'New-HttpsCertificateBinding',
		'Get-HttpsCertificateBinding',
		'Remove-HttpsCertificateBinding')

	# Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
	CmdletsToExport = @('Start-PWebServer', 'New-PWebConfig')
	PrivateData       = @{
		PSData = @{
			LicenseUri               = 'https://github.com/alexsandrovp/pwshws/blob/master/LICENSE'
			ProjectUri               = 'https://github.com/alexsandrovp/pwshws'
			RequireLicenseAcceptance = $true
		}
	}
}
