# PowerShell script to convert Newtonsoft.Json to System.Text.Json

$files = @(
    "obs-websocket-dotnet\Types\InputVolume.cs",
    "obs-websocket-dotnet\Types\InputSettings.cs", 
    "obs-websocket-dotnet\Types\InputFFMpegSettings.cs",
    "obs-websocket-dotnet\Types\InputBrowserSourceSettings.cs",
    "obs-websocket-dotnet\Types\InputBasicInfo.cs",
    "obs-websocket-dotnet\Types\GetTransitionListInfo.cs",
    "obs-websocket-dotnet\Types\GetSceneListInfo.cs",
    "obs-websocket-dotnet\Types\GetProfileListInfo.cs",
    "obs-websocket-dotnet\Events.cs"
)

foreach ($file in $files) {
    $fullPath = Join-Path $PSScriptRoot $file
    if (Test-Path $fullPath) {
        Write-Host "Processing $file"
        
        $content = Get-Content $fullPath -Raw
        
        # Replace using statements
        $content = $content -replace 'using Newtonsoft\.Json;', 'using System.Text.Json;'
        $content = $content -replace 'using Newtonsoft\.Json\.Linq;', 'using System.Text.Json.Serialization;'
        
        # Replace JsonProperty attributes
        $content = $content -replace '\[JsonProperty\(PropertyName = "([^"]+)"\)\]', '[JsonPropertyName("$1")]'
        
        # Replace JObject with JsonElement
        $content = $content -replace 'JObject', 'JsonElement'
        
        # Replace JsonConvert.PopulateObject calls
        $content = $content -replace 'JsonConvert\.PopulateObject\([^,]+\.ToString\(\), this\);', '// Converted to use JsonHelper'
        
        Set-Content $fullPath $content
    }
}

Write-Host "Conversion completed!"
