$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
    $compiler='C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    $sources=@('PnyColor.cs','PnyColor.Features.cs','PnyColor.Bridge.cs','PnyColor.Packaging.cs','PnyColor.Eyedropper.cs','PnyColor.Ui.cs','PnyColor.Localization.cs','PnyColor.Persistence.Tests.cs','PnyColor.Assembly.cs')
    & $compiler /nologo /platform:x64 /target:winexe /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /out:PnyColor.exe /win32icon:pny-color.ico /resource:pny-color.ico,PNYColor.Icon.ico /resource:pny-color-icon.png,PNYColor.Brand.png /resource:SignalRGB/PNY_Color_Bridge.js,PNYColor.Bridge.js $sources
    if($LASTEXITCODE -ne 0){throw 'Falha ao compilar aplicativo.'}
} finally {Pop-Location}
