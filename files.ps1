# 1. Define your root folder
$root = 'D:\temp\_myProj\BlazorShell\5_Modules'

# 2. Get all files recursively and project only the properties you care about
$files = Get-ChildItem -Path $root -File -Recurse |
    Select-Object `
      @{Name='Path';       Expression={$_.FullName}},
      @{Name='Name';       Expression={$_.Name}},
      @{Name='SizeBytes';  Expression={$_.Length}},
      @{Name='Modified';   Expression={$_.LastWriteTime}}

# 3. Convert to JSON and write to a file (or to stdout)
$files | ConvertTo-Json -Depth 3 | Out-File 5_Modules_file_list.json -Encoding UTF8
