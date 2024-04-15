using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityPlugin.Protobuf
{
    public class ProtocolDataView : TreeView
    {
        const int MESSAGE_ITEM_DEPTH = 0; // Э����Ϣ������
        //const int FIELD_ITEM_DEPTH = 1; // Э������ֶ�������
        const int COLUMN6_BUTTON_SIDE_LENGTH = 20; // �����а�ť�ı߳�

        private static int itemID; // �½����id

        private static TreeViewItem root = new TreeViewItem(itemID++, -1); // ������ĸ��������
        private static List<TreeViewItem> itemList = new List<TreeViewItem>(); // ���б�ÿһ�����һ�У����ݴ��б���Ʋ㼶�б�
        private static Dictionary<int, MessageData> messageDataDict = new Dictionary<int, MessageData>(); // ��Ϣ�����ֵ䣬root�����ÿ�������Ӧһ����Ϣ
        private static Dictionary<int, FieldData> fieldDataDict = new Dictionary<int, FieldData>(); // �ֶ������ֵ䣬ÿ����Ϣ���Ӧһ���ֶ�

        private GUIContent toggleContent = new GUIContent("", "�Ƿ��Զ����ֶ����ͣ�map����Ҳ��Ҫ�Զ���");
        private GUIContent removeMessageButtonContent = new GUIContent("-", "�Ƴ������Ϣ"); // ���Ƴ���Ϣ���ť��GUI����
        private GUIContent addFieldButtonContent = new GUIContent("+", "����һ���ֶ�"); // �������ֶ����ť��GUI����
        private GUIContent removeFieldButtonContent = new GUIContent("-", "�Ƴ�����ֶ�"); // ���Ƴ��ֶ����ť��GUI����

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

            // ���ø��ӹ�ϵ�����
            SetupDepthsFromParentsAndChildren(root);

            if (root.hasChildren)
            {
                BuildItemList(root);
            }

            // ���ع����õ����б�
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
                    // ��ֻ��ʾitem�ĸ���Ϊparent��������parent��˵item��������������item��Ϊparent��������Ҫʹ��AddChild
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
                case 0: // ��һ�У��۵�/չ����Ϣ��
                    break;
                case 1: // �ڶ��У�ѡ���ֶιؼ��֣�ֻ�и������ֶ����Ҹ�����message���ͲŻ���
                    if(fieldDataDict.TryGetValue(item.id, out FieldData fieldData1) && IsMessageType(item.parent.id))
                    {
                        fieldData1.keywordIndex = EditorGUI.Popup(new Rect(rect.x, rect.y + EditorStyles.popup.lineHeight / 2, rect.width, rect.height), fieldData1.keywordIndex, FieldData.fieldKeywords);
                    }                    
                    break;
                case 2: // �����У��Ƿ��Զ����ֶ����ͣ�ֻ�и������ֶ����Ҹ�����message���ͲŻ���
                    CenterRectUsingSingleLineHeight(ref rect);
                    if (fieldDataDict.TryGetValue(item.id, out FieldData fieldData2) && IsMessageType(item.parent.id))
                    {
                        fieldData2.fieldTypeCustom = GUI.Toggle(rect, fieldData2.fieldTypeCustom, toggleContent);
                    }
                    break;
                case 3: // �����У�����id������Ϣ���ͻ��ֶ����������˵����������ֶ����Ҹ�����enum���Ͳ�����
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
                case 4: // �����У���ȡ��Ϣ/�ֶ�����
                    CenterRectUsingSingleLineHeight(ref rect); // ʹ����������в����и���LineHeight��ͬ
                    if (messageDataDict.TryGetValue(item.id, out MessageData messageData4))
                    {
                        messageData4.messageName = GUI.TextField(rect, messageData4.messageName);
                    }
                    else if (fieldDataDict.TryGetValue(item.id, out FieldData fieldData4))
                    {
                        fieldData4.fieldName = GUI.TextField(rect, fieldData4.fieldName);
                    }
                    break;
                case 5: // �����У����Ϊ�ֶ����ȡ�ֶα��
                    if (fieldDataDict.TryGetValue(item.id, out FieldData fieldData5))
                    {
                        CenterRectUsingSingleLineHeight(ref rect);
                        fieldData5.fieldNumber = EditorGUI.IntField(rect, fieldData5.fieldNumber);
                    }
                    break;
                case 6: // �����У����Ϊ��Ϣ��������Ӻ��Ƴ���ť������ֻ�����Ƴ���ť��ʹ��depth�ж�
                    if(item.depth == MESSAGE_ITEM_DEPTH)
                    {
                        if(GUI.Button(new Rect(rect.x + (rect.width / 2 - COLUMN6_BUTTON_SIDE_LENGTH) / 2, rect.y + (rect.height - COLUMN6_BUTTON_SIDE_LENGTH) / 2, COLUMN6_BUTTON_SIDE_LENGTH, COLUMN6_BUTTON_SIDE_LENGTH), removeMessageButtonContent))
                        {
                            if(EditorUtility.DisplayDialog("Tip", "�Ƴ�����Ϣ��", "yes", "no"))
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
                            if (EditorUtility.DisplayDialog("Tip", "�Ƴ����ֶΣ�", "yes", "no"))
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
            // �����ֶκ�չ��
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
                    TreeViewItem messageItem = root.children[i]; // ��ȡmessage��
                    MessageData messageData = messageDataDict[messageItem.id]; // ��ȡ��Ӧ��messageData
                    messageDataList.Add(messageData); // �����б�

                    if (messageItem.hasChildren)
                    {
                        // �����ֶ�ʱʵ����һ���б���ΪmessageDataDict�Ǿ�̬��
                        // �����ֵ�һֱ����messageData��ʵ���������MessageData����ʵ�����б���ÿ�μ������ݵ�ʱ�򶼻�����б�������е��ֶ�
                        messageData.fieldDataList = new List<FieldData>();
                        for (int j = 0; j < messageItem.children.Count; j++)
                        {
                            int id = messageItem.children[j].id; // ��ȡfield���Ӧ��id
                            FieldData fieldData = fieldDataDict[id]; // ��ȡ��Ӧ��fieldData
                            messageData.fieldDataList.Add(fieldData); // ��fieldData���뵽��Ӧ��messageDta��
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
        // ��Ϣ���ͣ�messageΪ�࣬enumΪö��
        public static readonly string[] messageTypes =
        {
            "message", "enum"
        };

        public int messageTypeIndex; // ��Ϣ��������
        public string messageName; // ��Ϣ����

        public List<FieldData> fieldDataList; // ��Ϣ�����е��ֶ��б�
    }

    public class FieldData
    {
        // �ֶεĹؼ���
        public static readonly string[] fieldKeywords =
        {
            "none", "repeated", "optional"
        };

        // �ֶ�����
        public static readonly string[] fieldTypes =
        {
            "double", "float", "int32", "int64", "uint32",
            "uint64", "sint32", "fixed32", "fixed64", "sfixed32",
            "sfixed64", "bool", "string", "bytes"
        };

        public int keywordIndex; // �ֶιؼ�������
        public bool fieldTypeCustom; // �Ƿ��Զ����ֶ�����
        public string customTypeName; // �Զ����ֶ����͵�����
        public int fieldTypeIndex; // �ֶ���������
        public string fieldName; // �ֶ�����
        public int fieldNumber; // �ֶ����
    }
}