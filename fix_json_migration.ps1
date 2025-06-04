# PowerShell 腳本用於修復 JSON 遷移問題

# 取得所有 .cs 檔案
$files = Get-ChildItem -Path ".\obs-websocket-dotnet" -Recurse -Filter "*.cs"

foreach ($file in $files) {
    Write-Host "Processing file: $($file.FullName)"
    
    # 讀取檔案內容
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    
    # 替換 using 語句
    $content = $content -replace 'using Newtonsoft\.Json;', 'using System.Text.Json;'
    $content = $content -replace 'using Newtonsoft\.Json\.Linq;', 'using System.Text.Json;'
    $content = $content -replace 'using Newtonsoft\.Json\.Serialization;', 'using System.Text.Json.Serialization;'
    
    # 替換屬性
    $content = $content -replace '\[JsonProperty\(', '[JsonPropertyName('
    $content = $content -replace '\[JsonPropertyAttribute\(', '[JsonPropertyNameAttribute('
    $content = $content -replace 'JsonProperty\]', 'JsonPropertyName]'
    $content = $content -replace 'JsonPropertyAttribute\]', 'JsonPropertyNameAttribute]'
    
    # 替換 JsonConverter
    $content = $content -replace '\[JsonConverter\(typeof\(StringEnumConverter\)\)\]', '[JsonConverter(typeof(JsonStringEnumConverter))]'
    
    # 替換 JObject 為 JsonElement
    $content = $content -replace '\bJObject\b', 'JsonElement'
    $content = $content -replace '\bJToken\b', 'JsonElement'
    
    # 如果有變更，寫回檔案
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        Write-Host "Updated: $($file.Name)"
    }
}

Write-Host "JSON migration fix completed!"
