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
            string protoFileText = GenerateProtocolText(package, depFileSet, messageDataList); // ����Э������

            GenerateProtoFile(exportPath, protoFileName, protoFileText); // ����Э���ļ�

            string batchFilePath = GenerateBatchFile(exportPath, protoFileName, exePath, depFileSet); // �����������ļ�

            GenerateSciptFile(batchFilePath); // ���ɽű�
        }

        public static void GenerateProtocolScript(string package, HashSet<string> depFileSet, List<MessageData> messageDataList, string protoFileName, string exePath, string[] exportPaths)
        {
            string protoExportPath = exportPaths[0]; // Э���ļ�����·��
            string batExportPath = exportPaths[1]; // �������ļ�����·��
            string scriptExportPath = exportPaths[2]; // �ű��ļ�����·��

            string protoFileText = GenerateProtocolText(package, depFileSet, messageDataList); // ����Э������

            GenerateProtoFile(protoExportPath, protoFileName, protoFileText); // ����Э���ļ�

            string batchFilePath = GenerateBatchFile(batExportPath, protoFileName, exePath, protoExportPath, scriptExportPath, depFileSet); // �����������ļ�

            GenerateSciptFile(batchFilePath);
        }

        public static string GenerateProtocolText(string package, HashSet<string> depFileSet, List<MessageData> messageDataList)
        {
            StringBuilder protoFileText = new StringBuilder(); // ƴ���ַ���
            protoFileText.AppendLine("syntax =\"proto3\";"); // ʹ��proto3�汾�﷨

            if (string.IsNullOrEmpty(package) == false) // �Ƿ�涨�������ռ�
            {
                protoFileText.AppendLine($"package {package};");
            }

            if (depFileSet.Count > 0) // �Ƿ���������Э���ļ�
            {
                AppendDepFile(protoFileText, depFileSet);
            }

            protoFileText.AppendLine();

            if (messageDataList != null && messageDataList.Count > 0)
            {
                for (int i = 0; i < messageDataList.Count; i++) // ����message��Ϣ
                {
                    MessageData message = messageDataList[i];
                    string messageType = MessageData.messageTypes[message.messageTypeIndex]; // ȷ����message����enum����

                    protoFileText.AppendLine($"{messageType} {message.messageName}");
                    protoFileText.AppendLine("{");

                    for (int j = 0; j < message.fieldDataList.Count; j++) // ����message�����ڵ��ֶ�
                    {
                        FieldData field = message.fieldDataList[j];

                        if (messageType == "message") // �����message����
                        {
                            string keyword = FieldData.fieldKeywords[field.keywordIndex]; // ��ȡ�ؼ���
                            if (field.fieldTypeCustom) // �Ƿ�Ϊ�Զ����ֶ�
                            {
                                protoFileText.AppendLine($"\t{(keyword == "none" ? "" : keyword)} {field.customTypeName} {field.fieldName} = {field.fieldNumber};");
                            }
                            else
                            {
                                protoFileText.AppendLine($"\t{(keyword == "none" ? "" : keyword)} {FieldData.fieldTypes[field.fieldTypeIndex]} {field.fieldName} = {field.fieldNumber};");
                            }
                        }
                        else // �����enum����
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
                int tempIndex = file.LastIndexOf("/"); // ��ȡ��б�ܵ������������Դ˷ָ��·�����ļ���
                protoFileText.AppendLine($"import \"{file[(tempIndex + 1)..]}\";");
            }
        }

        private static HashSet<string> GetDepFileRootPath(HashSet<string> depFileSet)
        {
            HashSet<string> depFileRootPathSet = new HashSet<string>();
            foreach (var file in depFileSet)
            {
                int tempIndex = file.LastIndexOf("/"); // ��ȡ��б�ܵ������������Դ˷ָ��·�����ļ���
                depFileRootPathSet.Add(file[0..tempIndex]);
            }

            return depFileRootPathSet;
        }

        private static void GenerateProtoFile(string protoExportPath, string protoFileName, string protoFileText)
        {
            string protoFilePath = Path.Combine(protoExportPath, protoFileName + ".proto"); // Э���ļ�·��

            File.WriteAllText(protoFilePath, protoFileText); // ��Э������д���ļ�
        }

        private static string GenerateBatchFile(string exportPath, string protoFileName, string exePath, HashSet<string> depFileSet)
        {
            string batchFilePath = Path.Combine(exportPath, protoFileName + ".bat"); // �������ļ�·��

            StringBuilder batchFileText = new StringBuilder();
            // ��������������
            batchFileText.AppendLine($"{exePath} ^"); // protoc.exe��������·��
            batchFileText.AppendLine($"--proto_path={exportPath} ^"); // �����proto�ļ��ĸ�·��

            if (depFileSet.Count > 0) // ���������ļ��������
            {
                HashSet<string> depFileRootPathSet = GetDepFileRootPath(depFileSet); // ��ȡÿ�������ļ��ĸ�·��
                foreach (var path in depFileRootPathSet)
                {
                    batchFileText.AppendLine($"--proto_path={path} ^"); // ÿ�������ļ��ĸ�·��
                }
            }

            batchFileText.AppendLine($"--csharp_out={exportPath} ^"); // �ű����·��
            batchFileText.AppendLine($"{protoFileName + ".proto"}"); // proto�ļ���
            batchFileText.AppendLine(); // ����
            batchFileText.AppendLine("pause"); // ��ͣ
            File.WriteAllText(batchFilePath, batchFileText.ToString()); // д���ļ�

            return batchFilePath;
        }

        private static string GenerateBatchFile(string batExportPath, string protoFileName, string exePath, string protoExportPath, string scriptExportPath, HashSet<string> depFileSet)
        {
            string batchFilePath = Path.Combine(batExportPath, protoFileName + ".bat"); // �������ļ�·��

            StringBuilder batchFileText = new StringBuilder();
            // ��������������
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
            File.WriteAllText(batchFilePath, batchFileText.ToString()); // д���ļ�

            return batchFilePath;
        }

        private static void GenerateSciptFile(string batchFilePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = batchFilePath, // ָ��ִ�е��ļ�·��
                UseShellExecute = true // ����һ�������д���ִ��
            };
            Process.Start(startInfo); // �����ļ������ɽű�
        }
    }
}