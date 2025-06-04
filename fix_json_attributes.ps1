# PowerShell script to fix JSON migration issues
param(
    [string]$ProjectPath = "c:\Users\Aron\Desktop\Project\obs-websocket-dotnet-aot\obs-websocket-dotnet"
)

Write-Host "Starting JSON migration fixes..."

# Get all .cs files in Types folder
$typeFiles = Get-ChildItem -Path "$ProjectPath\Types" -Filter "*.cs" -Recurse

foreach ($file in $typeFiles) {
    Write-Host "Processing: $($file.Name)"
    $content = Get-Content $file.FullName -Raw
    $modified = $false
    
    # Fix using statements - remove duplicates and add Serialization namespace
    if ($content -match 'using System\.Text\.Json;') {
        # Remove duplicate using statements
        $content = $content -replace 'using System\.Text\.Json;\s*\r?\nusing System\.Text\.Json\.Serialization;\s*\r?\nusing Newtonsoft\.Json\.Converters;\s*\r?\nusing System\.Text\.Json;\s*\r?\nusing System\.Text\.Json\.Serialization;', 'using System.Text.Json;' + "`r`nusing System.Text.Json.Serialization;"
        $content = $content -replace 'using System\.Text\.Json;\s*\r?\nusing System\.Text\.Json;', 'using System.Text.Json;'
        
        # Add Serialization namespace if JsonPropertyName is used but namespace is missing
        if ($content -match '\[JsonPropertyName' -and $content -notmatch 'using System\.Text\.Json\.Serialization;') {
            $content = $content -replace '(using System\.Text\.Json;)', '$1' + "`r`nusing System.Text.Json.Serialization;"
            $modified = $true
        }
        
        # Remove any Newtonsoft references
        $content = $content -replace 'using Newtonsoft\.Json\.Converters;\s*\r?\n', ''
        $content = $content -replace 'using Newtonsoft\.Json;\s*\r?\n', ''
        
        $modified = $true
    }
    
    # Fix JsonPropertyName syntax
    if ($content -match '\[JsonPropertyName\(PropertyName = "([^"]+)"\)\]') {
        $content = $content -replace '\[JsonPropertyName\(PropertyName = "([^"]+)"\)\]', '[JsonPropertyName("$1")]'
        $modified = $true
    }
    
    # Fix JsonExtensionData
    if ($content -match '\[JsonExtensionData\]') {
        # Make sure we have the using statement
        if ($content -notmatch 'using System\.Text\.Json\.Serialization;') {
            $content = $content -replace '(using System\.Text\.Json;)', '$1' + "`r`nusing System.Text.Json.Serialization;"
        }
        $modified = $true
    }
    
    # Fix JsonConverter for enum
    if ($content -match '\[JsonConverter\(typeof\(JsonStringEnumConverter\)\)\]') {
        # Make sure we have the using statement
        if ($content -notmatch 'using System\.Text\.Json\.Serialization;') {
            $content = $content -replace '(using System\.Text\.Json;)', '$1' + "`r`nusing System.Text.Json.Serialization;"
        }
        $modified = $true
    }
    
    # Fix JsonConvert usage
    if ($content -match 'JsonConvert\.') {
        $content = $content -replace 'JsonConvert\.DeserializeObject<([^>]+)>\(([^)]+)\)', 'JsonSerializer.Deserialize<$1>($2)'
        $content = $content -replace 'JsonConvert\.SerializeObject\(([^)]+)\)', 'JsonSerializer.Serialize($1)'
        $modified = $true
    }
    
    if ($modified) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "  Updated: $($file.Name)"
    }
}

Write-Host "JSON migration fixes completed!"
