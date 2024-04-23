using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityPlugin.Protobuf
{
    using Column = MultiColumnHeaderState.Column;

    public class ProtocolGeneratorWindow : EditorWindow
    {
        const int LABEL_WIDTH = 120;

        private static ProtocolGeneratorWindow window;

        [MenuItem("Protobuf/Protocol Generator")]
        private static void ShowWindow()
        {
            window = GetWindow<ProtocolGeneratorWindow>("ProtocolGenerator");
            window.minSize = new Vector2(370, 200);
            window.Show();
        }

        private TreeViewState state; // 包含与Editor中的TreeView字段交互时更改的状态信息
        private MultiColumnHeader header; // 使TreeView支持多列
        private ProtocolDataView protocolDataView; // 绘制字段列表

        private void OnEnable()
        {
            state = new TreeViewState();
            header = CreateHeader();
            protocolDataView = new ProtocolDataView(state, header);
        }

        private void OnGUI()
        {
            GUILayout.Space(5);
            SetFileLayout();
            GUILayout.Space(10);
            SetProtocolInfoLayout();
            GUILayout.Space(10);
            SetAddMessageButton();

            Rect lastRect = GUILayoutUtility.GetLastRect(); // 获取fieldTable之前的最后一个GUILayout区域
            float fieldTableY = lastRect.y + lastRect.height + 5; // 字段列表的竖直方向的起点

            ShowError(lastRect);
            protocolDataView.Reload();
            protocolDataView.OnGUI(new Rect(0, fieldTableY, position.width, position.height - fieldTableY - 30));

            SetButtonGroupLayout();
        }

        private static int currentToolbarIndex; // 是否分别设置proto、bat、cs文件的输出路径
        private GUIContent[] toolbarContents = new GUIContent[2]
        {
            new GUIContent("Set Exprot Path Together", "集中设置文件导出路径"),
            new GUIContent("Set Exprot Path Separately", "分别设置文件导出路径")
        };

        private static string exePath = "点击Browse指定可执行文件"; // 存储可执行文件目录
        private GUIContent exePathLabelContent = new GUIContent("protoc.exe File", "protoc.exe文件");

        private static string exportPath = "点击Browse设置导出路径"; // 所有文件的输出路径
        private GUIContent exportPathLabelContent = new GUIContent("Export Path", "所有文件导出路径");

        private static string[] exportPaths = new string[3]
        {
            "点击Browse设置协议文件导出路径", // 协议文件的输出路径
            "点击Browse设置批处理文件导出路径", // 批处理文件的输出路径
            "点击Browse设置脚本导出路径" // 协议生成的脚本的输出路径
        };
        private static GUIContent[] exportPathContents = new GUIContent[3]
        {
            new GUIContent("Proto Export Path", "协议文件导出路径"),
            new GUIContent("Bat Export Path", "批处理文件导出路径"),
            new GUIContent("Script Export Path", "协议生成的脚本文件导出路径")
        };        

        private void SetFileLayout()
        {
            currentToolbarIndex = GUILayout.Toolbar(currentToolbarIndex, toolbarContents);

            GUILayout.BeginHorizontal(); // 开始一个水平控件组
            {
                EditorGUILayout.LabelField(exePathLabelContent, new GUIContent(exePath));
                if (GUILayout.Button("Browse", GUILayout.Width(70)))
                {
                    // OpenFilePanel在打开文件夹并取消的情况下也会返回值，返回一个空字符串
                    string tempExePath = EditorUtility.OpenFilePanel("Browse", Application.dataPath, "exe");
                    if (string.IsNullOrEmpty(tempExePath) == false)
                    {
                        exePath = tempExePath;

                        if (File.Exists(exePath))
                        {
                            errorDict.Remove(ErrorType.ExePathNotFoundError);
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();

            if(currentToolbarIndex == 0)
            {
                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField(exportPathLabelContent, new GUIContent(exportPath));
                    if (GUILayout.Button("Browse", GUILayout.Width(70)))
                    {
                        string tempExportPath = EditorUtility.OpenFolderPanel("Browse", Application.dataPath, "");
                        if (string.IsNullOrEmpty(tempExportPath) == false)
                        {
                            exportPath = tempExportPath;

                            if (Directory.Exists(exportPath))
                            {
                                errorDict.Remove(ErrorType.ExportPathNotFoundError);
                            }
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                for (int i = 0; i < exportPaths.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(exportPathContents[i], new GUIContent(exportPaths[i]));
                        if (GUILayout.Button("Browse", GUILayout.Width(70)))
                        {
                            string tempExportPath = EditorUtility.OpenFolderPanel("Browse", Application.dataPath, "");
                            if (string.IsNullOrEmpty(tempExportPath) == false)
                            {
                                exportPaths[i] = tempExportPath;

                                if(Directory.Exists(exportPaths[i]))
                                {
                                    errorDict.Remove(ErrorType.ExportPathNotFoundError);
                                }
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        private static string fileName; // 协议名称
        private static string package; // 命名空间
        private static HashSet<string> depFileSet = new HashSet<string>();

        private GUIContent fileNameLabelContent = new GUIContent("File Name", "生成的协议文件名称，不用加文件后缀");
        private GUIContent packageLabelContent = new GUIContent("Namespace", "协议类的命名空间");
        private GUIContent addDepFileButtonContent = new GUIContent("Add Dependency File", "添加一个依赖文件，如果该依赖文件依赖另一个文件且两文件不在同一目录内，则另一个依赖文件也需要被添加");
        private GUIContent deleteDepFileButtonContent = new GUIContent("Remove", "移除该协议文件");

        private void SetProtocolInfoLayout()
        {
            GUILayout.BeginHorizontal();
            {
                fileName = EditorGUILayout.TextField(fileNameLabelContent, fileName);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            {
                package = EditorGUILayout.TextField(packageLabelContent, package);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(addDepFileButtonContent))
                {
                    string depFilePath = EditorUtility.OpenFilePanel("Browse", Application.dataPath, "proto");
                    // 确保返回正确路径
                    if (string.IsNullOrEmpty(depFilePath) == false)
                    {
                        depFileSet.Add(depFilePath);
                    }
                }
            }
            GUILayout.EndHorizontal();

            if(depFileSet.Count > 0)
            {
                int count = 1;
                string tempFile = "";
                foreach (var file in depFileSet)
                {
                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField($"Dep File{count++}", file);
                        if (GUILayout.Button(deleteDepFileButtonContent, GUILayout.Width(70)))
                        {
                            tempFile = file;
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                if(string.IsNullOrEmpty(tempFile) == false) // 移除某个依赖文件
                {
                    depFileSet.Remove(tempFile);
                }
            }
        }

        private GUIContent addMessageButtonContent = new GUIContent("Add Message", "增加一个消息");

        private void SetAddMessageButton()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if(GUILayout.Button(addMessageButtonContent))
                {
                    protocolDataView.AddMessage();
                }
            }
            GUILayout.EndHorizontal();            
        }

        private bool haveError; // 当前是否出现错误
        private Dictionary<ErrorType, string> errorDict = new Dictionary<ErrorType, string>(); // 当前的错误字典

        private bool CheckError()
        {
            if (File.Exists(exePath) == false)
            {
                haveError = true;
                errorDict.TryAdd(ErrorType.ExePathNotFoundError, "protoc.exe路径不正确");
            }

            if(currentToolbarIndex == 0)
            {
                if (Directory.Exists(exportPath) == false)
                {
                    haveError = true;
                    errorDict.TryAdd(ErrorType.ExportPathNotFoundError, "Export路径不正确");
                }
            }
            else
            {
                for (int i = 0; i < exportPaths.Length; i++)
                {
                    if (Directory.Exists(exportPaths[i]) == false)
                    {
                        haveError = true;
                        errorDict.TryAdd(ErrorType.ExportPathNotFoundError, "Export路径不正确");
                    }
                }
            }

            if (string.IsNullOrEmpty(fileName))
            {
                haveError = true;
                errorDict.TryAdd(ErrorType.FileNameNullError, "请填写File Name");
            }
            else
            {
                errorDict.Remove(ErrorType.FileNameNullError);
            }

            return haveError;
        }

        private void ShowError(Rect rect)
        {
            if (haveError) // 错误消息
            {
                int flag = 0;
                foreach (var error in errorDict)
                {
                    EditorGUI.HelpBox(new Rect(rect.x + flag * 150, rect.y - 10, 150, rect.height + 10), error.Value, MessageType.Error);
                    flag++;
                }

                if (EditorGUIUtility.editingTextField) // 如果正在编辑TextField
                {
                    CheckError();
                }

                if(errorDict.Count == 0) // 纠错成功
                {
                    haveError = false;
                }
            }
        }

        private GUIContent clearButtonContent = new GUIContent("Clear", "清除所有数据");
        private GUIContent previewButtonContent = new GUIContent("Preview", "预览proto文件");
        private GUIContent generateButtonContent = new GUIContent("Generate", "生成协议文件");

        private void SetButtonGroupLayout()
        {
            GUILayout.BeginArea(new Rect(0, position.height - EditorGUIUtility.singleLineHeight - 5, position.width - 3, EditorGUIUtility.singleLineHeight + 5));
            {
                GUILayout.BeginHorizontal();
                {
                    if (EditorGUILayout.LinkButton("GitHub"))
                    {
                        Application.OpenURL("https://github.com/CodeSnail12600/ProtocolGenerator");
                    }
                    GUILayout.FlexibleSpace();
                    if(GUILayout.Button(clearButtonContent))
                    {
                        if(EditorUtility.DisplayDialog("Tip", "清除全部数据？", "yes", "no"))
                        {
                            Clear();
                        }
                    }
                    if (GUILayout.Button(previewButtonContent))
                    {
                        // 展示返回的字符串数组的第一项
                        PreviewWindow.ShowWindow(ProtocolHelper.GenerateProtocolText(package, depFileSet, protocolDataView.GetProtocolData()));
                    }
                    if (GUILayout.Button(generateButtonContent))
                    {
                        if(CheckError() == false) // 检查错误
                        {
                            if(currentToolbarIndex == 0)
                            {
                                ProtocolHelper.GenerateProtocolScript(package, depFileSet, protocolDataView.GetProtocolData(), fileName, exePath, exportPath);
                            }
                            else
                            {
                                ProtocolHelper.GenerateProtocolScript(package, depFileSet, protocolDataView.GetProtocolData(), fileName, exePath, exportPaths);
                            }
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private void Clear()
        {
            exePath = "点击Browse以指定可执行文件";
            exportPath = "点击Browse以指定协议导出路径";
            fileName = "";
            package = "";
            depFileSet.Clear();
            protocolDataView.Clear();
            haveError = false;
            errorDict.Clear();
        }

        private MultiColumnHeader CreateHeader()
        {
            Column column1 = new Column
            {
                width = 15,
                minWidth = 15,
                maxWidth = 15,
            };

            Column column2 = new Column
            {
                headerContent = new GUIContent("Keyword", "字段关键字"),
                width = 100,
                minWidth = 70,
                maxWidth = 400,
                headerTextAlignment = TextAlignment.Center,
            };

            Column column3 = new Column
            {
                width = 20,
                minWidth = 20,
                maxWidth = 20,
            };

            Column column4 = new Column
            {
                headerContent = new GUIContent("Message/Field Type", "消息/字段类型"),
                width = 150,
                minWidth = 70,
                maxWidth = 400,
                headerTextAlignment = TextAlignment.Center,
            };

            Column column5 = new Column
            {
                headerContent = new GUIContent("Field Name", "字段名称"),
                width = 100,
                minWidth = 70,
                maxWidth = 400,
                headerTextAlignment = TextAlignment.Center,
            };

            Column column6 = new Column
            {
                headerContent = new GUIContent("Field Number", "字段编号"),
                width = 100,
                minWidth = 70,
                maxWidth = 400,
                headerTextAlignment = TextAlignment.Center,
            };

            Column column7 = new Column
            {
                width = 100,
                minWidth = 70,
                maxWidth = 200,
            };

            Column[] columns = { column1, column2, column3, column4, column5, column6, column7 };
            MultiColumnHeaderState headerState = new MultiColumnHeaderState(columns);

            MultiColumnHeader header = new MultiColumnHeader(headerState);

            return header;
        }
    }

    public enum ErrorType
    {
        FileNameNullError,
        ExePathNotFoundError, ExportPathNotFoundError
    }
}