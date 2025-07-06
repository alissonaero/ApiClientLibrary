param (
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath
)

Write-Host "?? Instalando pacotes de teste no projeto: $ProjectPath" -ForegroundColor Cyan

# Pacotes recomendados
$packages = @(
    "xunit",
    "xunit.runner.visualstudio",
    "Microsoft.NET.Test.Sdk",
    "FluentAssertions",
    "Moq",
    "coverlet.collector"
)

foreach ($pkg in $packages) {
    Write-Host "`n? Instalando $pkg..." -ForegroundColor Yellow
    dotnet add "$ProjectPath" package $pkg
}

Write-Host "`n? Instalação concluída." -ForegroundColor Green
