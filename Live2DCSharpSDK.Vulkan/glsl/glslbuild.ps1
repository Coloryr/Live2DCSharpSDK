# 设置输入和输出目录
$inputDirectory = ".\"  # 输入目录
$outputDirectory = ".\spv\"    # 输出目录

# 确保输出目录存在
if (-Not (Test-Path -Path $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory
}

# 获取所有的 .frag 和 .vert 文件
$shaderFiles = Get-ChildItem -Path $inputDirectory -Recurse -Include *.frag, *.vert

if (-not (Test-Path -Path $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

# 初始化编译后的着色器文件列表
$compiledShadersFramework = @()

foreach ($shader in $shaderFiles) {
    $fileName = $shader.Name
    
    # 跳过common.glsl文件
    if ($fileName -eq "common.glsl") {
        continue
    }
    
    $fullPath = $shader.FullName
    $outputFileName = [System.IO.Path]::GetFileNameWithoutExtension($fileName) + ".spv"
    $outputFile = Join-Path -Path $outputDirectory -ChildPath $outputFileName

    # 添加到编译后的着色器列表
    $compiledShadersFramework += $outputFile

    # 创建自定义命令以编译着色器
    $command = "$env:VK_SDK_PATH\Bin\glslc.exe `"$fullPath`" -o `"$outputFile`""
    
    # 执行命令
    Invoke-Expression $command
}

# 输出结果
Write-Host "Compiled Shaders:"
$compiledShadersFramework