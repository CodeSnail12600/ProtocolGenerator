using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityPlugin.Protobuf
{
    public class ProtocolDataView : TreeView
    {
        const int MESSAGE_ITEM_DEPTH = 0; // 协议消息项的深度
        //const int FIELD_ITEM_DEPTH = 1; // 协议根中字段项的深度
        const int COLUMN6_BUTTON_SIDE_LENGTH = 20; // 第六列按钮的边长

        private static int itemID; // 新建项的id

        private static TreeViewItem root = new TreeViewItem(itemID++, -1); // 所有项的根项，不绘制
        private static List<TreeViewItem> itemList = new List<TreeViewItem>(); // 行列表，每一项都代表一行，依据此列表绘制层级列表
        private static Dictionary<int, MessageData> messageDataDict = new Dictionary<int, MessageData>(); // 消息数据字典，root根项的每个子项对应一个消息
        private static Dictionary<int, FieldData> fieldDataDict = new Dictionary<int, FieldData>(); // 字段数据字典，每个消息项对应一个字段

        private GUIContent toggleContent = new GUIContent("", "是否自定义字段类型，map类型也需要自定义");
        private GUIContent removeMessageButtonContent = new GUIContent("-", "移除这个消息"); // “移除消息项”按钮的GUI内容
        private GUIContent addFieldButtonContent = new GUIContent("+", "增加一个字段"); // “增加字段项”按钮的GUI内容
        private GUIContent removeFieldButtonContent = new GUIContent("-", "移除这个字段"); // “移除字段项”按钮的GUI内容

        public ProtocolDataView(TreeViewState state, MultiColumnHeader header) : base(state, header)
        {
            rowHeight = 30;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            columnIndexForTreeFoldouts = 0;
        }

        protected override TreeViewItem BuildRoot()
        {
            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            itemList.Clear();

            // 设置父子关系和深度
            SetupDepthsFromParentsAndChildren(root);

            if (root.hasChildren)
            {
                BuildItemList(root);
            }

            // 返回构建好的行列表
            return itemList;
        }

        private void BuildItemList(TreeViewItem parent, TreeViewItem parentCopy = null)
        {
            foreach (TreeViewItem child in parent.children)
            {                
                TreeViewItem item = new TreeViewItem(child.id, child.depth);
                if(parentCopy != null)
                {
                    parentCopy.AddChild(item);
                }
                else
                {
                    // 这只表示item的父项为parent，但对于parent来说item不是子项，如果想让item成为parent的子项需要使用AddChild
                    item.parent = parent;
                }
                itemList.Add(item);

                if (child.hasChildren)
                {
                    if (IsExpanded(child.id))
                    {
                        BuildItemList(child, item);
                    }
                    else
                    {
                        item.children = CreateChildListForCollapsedParent();
                    }
                }
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);

            int visibleColumns = args.GetNumVisibleColumns();

            for (int i = 0; i < visibleColumns; i++)
            {
                CreateGUI(args.GetCellRect(i), i, args.item);
            }
        }

        private void CreateGUI(Rect rect, int columnIndex, TreeViewItem item)
        {
            switch(columnIndex)
            {
                case 0: // 第一列，折叠/展开消息项
                    break;
                case 1: // 第二列，选择字段关键字，只有该项是字段项且父项是message类型才绘制
                    if(fieldDataDict.TryGetValue(item.id, out FieldData fieldData1) && IsMessageType(item.parent.id))
                    {
                        fieldData1.keywordIndex = EditorGUI.Popup(new Rect(rect.x, rect.y + EditorStyles.popup.lineHeight / 2, rect.width, rect.height), fieldData1.keywordIndex, FieldData.fieldKeywords);
                    }                    
                    break;
                case 2: // 第三列，是否自定义字段类型，只有该项是字段项且父项是message类型才绘制
                    CenterRectUsingSingleLineHeight(ref rect);
                    if (fieldDataDict.TryGetValue(item.id, out FieldData fieldData2) && IsMessageType(item.parent.id))
                    {
                        fieldData2.fieldTypeCustom = GUI.Toggle(rect, fieldData2.fieldTypeCustom, toggleContent);
                    }
                    break;
                case 3: // 第四列，根据id绘制消息类型或字段类型下拉菜单，该项是字段项且父项是enum类型不绘制
                    CenterRectUsingSingleLineHeight(ref rect);
                    if (messageDataDict.TryGetValue(item.id, out MessageData messageData3))
                    {
                        messageData3.messageTypeIndex = EditorGUI.Popup(rect, messageData3.messageTypeIndex, MessageData.messageTypes);
                    }
                    else if(fieldDataDict.TryGetValue(item.id, out FieldData fieldData3) && IsMessageType(item.parent.id))
                    {
                        if(fieldData3.fieldTypeCustom)
                        {
                            fieldData3.customTypeName = EditorGUI.TextField(rect, fieldData3.customTypeName);
                        }
                        else
                        {
                            fieldData3.fieldTypeIndex = EditorGUI.Popup(rect, fieldData3.fieldTypeIndex, FieldData.fieldTypes);
                        }
                        
                    }
                    break;
                case 4: // 第五列，获取消息/字段名称
                    CenterRectUsingSingleLineHeight(ref rect); // 使绘制区域居中并且行高与LineHeight相同
                    if (messageDataDict.TryGetValue(item.id, out MessageData messageData4))
                    {
                        messageData4.messageName = GUI.TextField(rect, messageData4.messageName);
                    }
                    else if (fieldDataDict.TryGetValue(item.id, out FieldData fieldData4))
                    {
                        fieldData4.fieldName = GUI.TextField(rect, fieldData4.fieldName);
                    }
                    break;
                case 5: // 第六列，如果为字段项，获取字段编号
                    if (fieldDataDict.TryGetValue(item.id, out FieldData fieldData5))
                    {
                        CenterRectUsingSingleLineHeight(ref rect);
                        fieldData5.fieldNumber = EditorGUI.IntField(rect, fieldData5.fieldNumber);
                    }
                    break;
                case 6: // 第七列，如果为消息项，绘制增加和移除按钮，否则只绘制移除按钮；使用depth判断
                    if(item.depth == MESSAGE_ITEM_DEPTH)
                    {
                        if(GUI.Button(new Rect(rect.x + (rect.width / 2 - COLUMN6_BUTTON_SIDE_LENGTH) / 2, rect.y + (rect.height - COLUMN6_BUTTON_SIDE_LENGTH) / 2, COLUMN6_BUTTON_SIDE_LENGTH, COLUMN6_BUTTON_SIDE_LENGTH), removeMessageButtonContent))
                        {
                            if(EditorUtility.DisplayDialog("Tip", "移除该消息？", "yes", "no"))
                            {
                                RemoveMessage(item.id);
                            }
                        }
                        if (GUI.Button(new Rect(rect.x + (rect.width * 1.5f - COLUMN6_BUTTON_SIDE_LENGTH) / 2, rect.y + (rect.height - COLUMN6_BUTTON_SIDE_LENGTH) / 2, COLUMN6_BUTTON_SIDE_LENGTH, COLUMN6_BUTTON_SIDE_LENGTH), addFieldButtonContent))
                        {
                            AddField(item.id);
                        }
                    }
                    else
                    {
                        if (GUI.Button(new Rect(rect.x + (rect.width - COLUMN6_BUTTON_SIDE_LENGTH) / 2, rect.y + (rect.height - COLUMN6_BUTTON_SIDE_LENGTH) / 2, COLUMN6_BUTTON_SIDE_LENGTH, COLUMN6_BUTTON_SIDE_LENGTH), removeFieldButtonContent))
                        {
                            if (EditorUtility.DisplayDialog("Tip", "移除该字段？", "yes", "no"))
                            {
                                RemoveField(item.id, item.parent.id);
                            }
                        }
                    }
                    break;
            }
        }

        private bool IsMessageType(int id)
        {
            MessageData messageData = messageDataDict[id];
            if (MessageData.messageTypes[messageData.messageTypeIndex] == "message")
            {
                return true;
            }
            return false;
        }

        public void AddMessage()
        {
            TreeViewItem item = new TreeViewItem(itemID++);
            root.AddChild(item);
            messageDataDict.TryAdd(item.id, new MessageData());
        }

        private void RemoveMessage(int id)
        {
            root.children.Remove(FindItem(id, root));
        }

        private void AddField(int parentID)
        {
            TreeViewItem item = new TreeViewItem(itemID++);
            FindItem(parentID, root).AddChild(item);
            fieldDataDict.TryAdd(item.id, new FieldData());
            // 增加字段后展开
            if(IsExpanded(parentID) == false)
            {
                SetExpanded(parentID, true);
            }
        }

        private void RemoveField(int id, int parentID)
        {
            FindItem(parentID, root).children.Remove(FindItem(id, root));
        }

        public void Clear()
        {
            if(root.hasChildren)
            {
                root.children.Clear();
            }
        }

        public List<MessageData> GetProtocolData()
        {
            if (root.hasChildren)
            {
                List<MessageData> messageDataList = new List<MessageData>();
                for (int i = 0; i < root.children.Count; i++)
                {
                    TreeViewItem messageItem = root.children[i]; // 获取message项
                    MessageData messageData = messageDataDict[messageItem.id]; // 获取对应的messageData
                    messageDataList.Add(messageData); // 加入列表

                    if (messageItem.hasChildren)
                    {
                        // 当有字段时实例化一个列表，因为messageDataDict是静态的
                        // 所以字典一直持有messageData的实例，如果在MessageData类中实例化列表，将每次集成数据的时候都会向该列表添加已有的字段
                        messageData.fieldDataList = new List<FieldData>();
                        for (int j = 0; j < messageItem.children.Count; j++)
                        {
                            int id = messageItem.children[j].id; // 获取field项对应的id
                            FieldData fieldData = fieldDataDict[id]; // 获取对应的fieldData
                            messageData.fieldDataList.Add(fieldData); // 将fieldData加入到对应的messageDta中
                        }
                    }
                }
                return messageDataList;
            }

            return null;
        }
    }

    public class MessageData
    {
        // 消息类型，message为类，enum为枚举
        public static readonly string[] messageTypes =
        {
            "message", "enum"
        };

        public int messageTypeIndex; // 消息类型索引
        public string messageName; // 消息名称

        public List<FieldData> fieldDataList; // 消息区块中的字段列表
    }

    public class FieldData
    {
        // 字段的关键字
        public static readonly string[] fieldKeywords =
        {
            "none", "repeated", "optional"
        };

        // 字段类型
        public static readonly string[] fieldTypes =
        {
            "double", "float", "int32", "int64", "uint32",
            "uint64", "sint32", "fixed32", "fixed64", "sfixed32",
            "sfixed64", "bool", "string", "bytes"
        };

        public int keywordIndex; // 字段关键字索引
        public bool fieldTypeCustom; // 是否自定义字段类型
        public string customTypeName; // 自定义字段类型的名称
        public int fieldTypeIndex; // 字段类型索引
        public string fieldName; // 字段名称
        public int fieldNumber; // 字段序号
    }
}