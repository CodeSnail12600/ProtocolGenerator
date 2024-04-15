using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace UnityPlugin.Protobuf
{
    public static class ProtocolHelper
    {
        public static void GenerateProtocolScript(string package, HashSet<string> depFileSet, List<MessageData> messageDataList, string protoFileName, string exePath, string exportPath)
        {
            string protoFileText = GenerateProtocolText(package, depFileSet, messageDataList); // 生成协议内容

            GenerateProtoFile(exportPath, protoFileName, protoFileText); // 生成协议文件

            string batchFilePath = GenerateBatchFile(exportPath, protoFileName, exePath, depFileSet); // 生成批处理文件

            GenerateSciptFile(batchFilePath); // 生成脚本
        }

        public static void GenerateProtocolScript(string package, HashSet<string> depFileSet, List<MessageData> messageDataList, string protoFileName, string exePath, string[] exportPaths)
        {
            string protoExportPath = exportPaths[0]; // 协议文件导出路径
            string batExportPath = exportPaths[1]; // 批处理文件导出路径
            string scriptExportPath = exportPaths[2]; // 脚本文件导出路径

            string protoFileText = GenerateProtocolText(package, depFileSet, messageDataList); // 生成协议内容

            GenerateProtoFile(protoExportPath, protoFileName, protoFileText); // 生成协议文件

            string batchFilePath = GenerateBatchFile(batExportPath, protoFileName, exePath, protoExportPath, scriptExportPath, depFileSet); // 生成批处理文件

            GenerateSciptFile(batchFilePath);
        }

        public static string GenerateProtocolText(string package, HashSet<string> depFileSet, List<MessageData> messageDataList)
        {
            StringBuilder protoFileText = new StringBuilder(); // 拼接字符串
            protoFileText.AppendLine("syntax =\"proto3\";"); // 使用proto3版本语法

            if (string.IsNullOrEmpty(package) == false) // 是否规定了命名空间
            {
                protoFileText.AppendLine($"package {package};");
            }

            if (depFileSet.Count > 0) // 是否依赖其他协议文件
            {
                AppendDepFile(protoFileText, depFileSet);
            }

            protoFileText.AppendLine();

            if (messageDataList != null && messageDataList.Count > 0)
            {
                for (int i = 0; i < messageDataList.Count; i++) // 遍历message信息
                {
                    MessageData message = messageDataList[i];
                    string messageType = MessageData.messageTypes[message.messageTypeIndex]; // 确定是message还是enum类型

                    protoFileText.AppendLine($"{messageType} {message.messageName}");
                    protoFileText.AppendLine("{");

                    for (int j = 0; j < message.fieldDataList.Count; j++) // 遍历message区块内的字段
                    {
                        FieldData field = message.fieldDataList[j];

                        if (messageType == "message") // 如果是message类型
                        {
                            string keyword = FieldData.fieldKeywords[field.keywordIndex]; // 获取关键字
                            if (field.fieldTypeCustom) // 是否为自定义字段
                            {
                                protoFileText.AppendLine($"\t{(keyword == "none" ? "" : keyword)} {field.customTypeName} {field.fieldName} = {field.fieldNumber};");
                            }
                            else
                            {
                                protoFileText.AppendLine($"\t{(keyword == "none" ? "" : keyword)} {FieldData.fieldTypes[field.fieldTypeIndex]} {field.fieldName} = {field.fieldNumber};");
                            }
                        }
                        else // 如果是enum类型
                        {
                            protoFileText.AppendLine($"\t{field.fieldName} = {field.fieldNumber};");
                        }
                    }

                    protoFileText.AppendLine("}");
                }
            }

            return protoFileText.ToString();
        }

        private static void AppendDepFile(StringBuilder protoFileText, HashSet<string> depFileSet)
        {
            foreach (var file in depFileSet)
            {
                int tempIndex = file.LastIndexOf("/"); // 获取正斜杠的最后的索引，以此分割根路径和文件名
                protoFileText.AppendLine($"import \"{file[(tempIndex + 1)..]}\";");
            }
        }

        private static HashSet<string> GetDepFileRootPath(HashSet<string> depFileSet)
        {
            HashSet<string> depFileRootPathSet = new HashSet<string>();
            foreach (var file in depFileSet)
            {
                int tempIndex = file.LastIndexOf("/"); // 获取正斜杠的最后的索引，以此分割根路径和文件名
                depFileRootPathSet.Add(file[0..tempIndex]);
            }

            return depFileRootPathSet;
        }

        private static void GenerateProtoFile(string protoExportPath, string protoFileName, string protoFileText)
        {
            string protoFilePath = Path.Combine(protoExportPath, protoFileName + ".proto"); // 协议文件路径

            File.WriteAllText(protoFilePath, protoFileText); // 将协议内容写入文件
        }

        private static string GenerateBatchFile(string exportPath, string protoFileName, string exePath, HashSet<string> depFileSet)
        {
            string batchFilePath = Path.Combine(exportPath, protoFileName + ".bat"); // 批处理文件路径

            StringBuilder batchFileText = new StringBuilder();
            // 生成批处理命令
            batchFileText.AppendLine($"{exePath} ^"); // protoc.exe编译器的路径
            batchFileText.AppendLine($"--proto_path={exportPath} ^"); // 处理的proto文件的根路径

            if (depFileSet.Count > 0) // 在有依赖文件的情况下
            {
                HashSet<string> depFileRootPathSet = GetDepFileRootPath(depFileSet); // 获取每个依赖文件的根路径
                foreach (var path in depFileRootPathSet)
                {
                    batchFileText.AppendLine($"--proto_path={path} ^"); // 每个依赖文件的根路径
                }
            }

            batchFileText.AppendLine($"--csharp_out={exportPath} ^"); // 脚本输出路径
            batchFileText.AppendLine($"{protoFileName + ".proto"}"); // proto文件名
            batchFileText.AppendLine(); // 换行
            batchFileText.AppendLine("pause"); // 暂停
            File.WriteAllText(batchFilePath, batchFileText.ToString()); // 写入文件

            return batchFilePath;
        }

        private static string GenerateBatchFile(string batExportPath, string protoFileName, string exePath, string protoExportPath, string scriptExportPath, HashSet<string> depFileSet)
        {
            string batchFilePath = Path.Combine(batExportPath, protoFileName + ".bat"); // 批处理文件路径

            StringBuilder batchFileText = new StringBuilder();
            // 生成批处理命令
            batchFileText.AppendLine($"{exePath} ^");
            batchFileText.AppendLine($"--proto_path={protoExportPath} ^");

            if (depFileSet.Count > 0)
            {
                HashSet<string> depFileRootPathSet = GetDepFileRootPath(depFileSet);
                foreach (var path in depFileRootPathSet)
                {
                    batchFileText.AppendLine($"--proto_path={path} ^");
                }
            }

            batchFileText.AppendLine($"--csharp_out={scriptExportPath} ^");
            batchFileText.AppendLine($"{protoFileName + ".proto"}");
            batchFileText.AppendLine();
            batchFileText.AppendLine("pause");
            File.WriteAllText(batchFilePath, batchFileText.ToString()); // 写入文件

            return batchFilePath;
        }

        private static void GenerateSciptFile(string batchFilePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = batchFilePath, // 指定执行的文件路径
                UseShellExecute = true // 开启一个命令行窗口执行
            };
            Process.Start(startInfo); // 运行文件，生成脚本
        }
    }
}