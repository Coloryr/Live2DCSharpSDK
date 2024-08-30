#!/bin/bash

# 设置输入和输出目录
inputDirectory="."  # 输入目录
outputDirectory="./spv"  # 输出目录

# 确保输出目录存在
if [ ! -d "$outputDirectory" ]; then
    mkdir -p "$outputDirectory"
fi

# 获取所有的 .frag 和 .vert 文件
shaderFiles=$(find "$inputDirectory" -type f \( -name "*.frag" -o -name "*.vert" \))

# 初始化编译后的着色器文件列表
compiledShadersFramework=()

# 遍历每个着色器文件
for shader in $shaderFiles; do
    fileName=$(basename "$shader")

    # 跳过common.glsl文件
    if [ "$fileName" == "common.glsl" ]; then
        continue
    fi

    outputFileName="${fileName%.*}.spv"  # 获取不带扩展名的文件名并添加.spv扩展名
    outputFile="$outputDirectory/$outputFileName"

    # 添加到编译后的着色器列表
    compiledShadersFramework+=("$outputFile")

    # 创建自定义命令以编译着色器
    command="glslc \"$shader\" -o \"$outputFile\""

    # 执行命令
    eval $command
done

# 输出结果
echo "Compiled Shaders:"
for compiledShader in "${compiledShadersFramework[@]}"; do
    echo "$compiledShader"
done