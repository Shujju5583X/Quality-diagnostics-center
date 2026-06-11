Remove-Item -Recurse -Force LabSystem.Core, LabSystem.Data, LabSystem.Services, LabSystem.UI, LabSystem.Tests, LabSystem.sln -ErrorAction SilentlyContinue

dotnet new sln -n LabSystem
dotnet new classlib -n LabSystem.Core
dotnet new classlib -n LabSystem.Data
dotnet new classlib -n LabSystem.Services
dotnet new wpf -n LabSystem.UI
dotnet new nunit -n LabSystem.Tests

Set-Content -Path "LabSystem.Core\LabSystem.Core.csproj" -Value @"
<Project Sdk=`"Microsoft.NET.Sdk`">
  <PropertyGroup>
    <TargetFramework>net451</TargetFramework>
    <LangVersion>7.3</LangVersion>
  </PropertyGroup>
</Project>
"@

Set-Content -Path "LabSystem.Data\LabSystem.Data.csproj" -Value @"
<Project Sdk=`"Microsoft.NET.Sdk`">
  <PropertyGroup>
    <TargetFramework>net451</TargetFramework>
    <LangVersion>7.3</LangVersion>
  </PropertyGroup>
</Project>
"@

Set-Content -Path "LabSystem.Services\LabSystem.Services.csproj" -Value @"
<Project Sdk=`"Microsoft.NET.Sdk`">
  <PropertyGroup>
    <TargetFramework>net451</TargetFramework>
    <LangVersion>7.3</LangVersion>
  </PropertyGroup>
</Project>
"@

Set-Content -Path "LabSystem.UI\LabSystem.UI.csproj" -Value @"
<Project Sdk=`"Microsoft.NET.Sdk`">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net451</TargetFramework>
    <UseWPF>true</UseWPF>
    <LangVersion>7.3</LangVersion>
  </PropertyGroup>
</Project>
"@

Set-Content -Path "LabSystem.Tests\LabSystem.Tests.csproj" -Value @"
<Project Sdk=`"Microsoft.NET.Sdk`">
  <PropertyGroup>
    <TargetFramework>net451</TargetFramework>
    <LangVersion>7.3</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=`"Microsoft.NET.Test.Sdk`" Version=`"16.11.0`" />
    <PackageReference Include=`"NUnit`" Version=`"3.14.0`" />
    <PackageReference Include=`"NUnit3TestAdapter`" Version=`"4.3.0`" />
    <PackageReference Include=`"Moq`" Version=`"4.16.1`" />
  </ItemGroup>
</Project>
"@

dotnet sln LabSystem.sln add LabSystem.Core/LabSystem.Core.csproj
dotnet sln LabSystem.sln add LabSystem.Data/LabSystem.Data.csproj
dotnet sln LabSystem.sln add LabSystem.Services/LabSystem.Services.csproj
dotnet sln LabSystem.sln add LabSystem.UI/LabSystem.UI.csproj
dotnet sln LabSystem.sln add LabSystem.Tests/LabSystem.Tests.csproj

dotnet add LabSystem.Data/LabSystem.Data.csproj reference LabSystem.Core/LabSystem.Core.csproj
dotnet add LabSystem.Services/LabSystem.Services.csproj reference LabSystem.Core/LabSystem.Core.csproj
dotnet add LabSystem.UI/LabSystem.UI.csproj reference LabSystem.Core/LabSystem.Core.csproj LabSystem.Data/LabSystem.Data.csproj LabSystem.Services/LabSystem.Services.csproj
dotnet add LabSystem.Tests/LabSystem.Tests.csproj reference LabSystem.Core/LabSystem.Core.csproj LabSystem.Services/LabSystem.Services.csproj LabSystem.UI/LabSystem.UI.csproj

dotnet add LabSystem.Data/LabSystem.Data.csproj package EntityFramework -v 6.4.4
dotnet add LabSystem.Data/LabSystem.Data.csproj package System.Data.SQLite.EF6 -v 1.0.118
dotnet add LabSystem.Data/LabSystem.Data.csproj package System.Data.SQLite -v 1.0.118
dotnet add LabSystem.Services/LabSystem.Services.csproj package MigraDoc -v 1.50.5147
dotnet add LabSystem.Services/LabSystem.Services.csproj package ClosedXML -v 0.95.4
dotnet add LabSystem.Services/LabSystem.Services.csproj package BCrypt.Net-Next -v 4.0.3
dotnet add LabSystem.UI/LabSystem.UI.csproj package SimpleInjector -v 5.4.1
dotnet add LabSystem.UI/LabSystem.UI.csproj package Serilog -v 2.10.0
dotnet add LabSystem.UI/LabSystem.UI.csproj package Serilog.Sinks.File -v 5.0.0
dotnet add LabSystem.UI/LabSystem.UI.csproj package MaterialDesignThemes -v 3.2.0

# Remove implicit using and global statements from UI project template if they exist
$mainWindowPath = "LabSystem.UI\MainWindow.xaml.cs"
if (Test-Path $mainWindowPath) {
    $content = Get-Content $mainWindowPath -Raw
    $content = $content -replace "namespace LabSystem.UI;", "namespace LabSystem.UI`n{`n"
    $content = $content + "`n}"
    Set-Content -Path $mainWindowPath -Value $content
}

$appPath = "LabSystem.UI\App.xaml.cs"
if (Test-Path $appPath) {
    $content = Get-Content $appPath -Raw
    $content = $content -replace "namespace LabSystem.UI;", "namespace LabSystem.UI`n{`n"
    $content = $content + "`n}"
    Set-Content -Path $appPath -Value $content
}
