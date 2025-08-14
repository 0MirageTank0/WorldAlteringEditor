using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI.C;
using Rampastring.XNAUI.C.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TSMapEditor.UI.Controls
{
    /// <summary>
    /// A base class for windows that can create themselves through an INI configuration file.
    /// </summary>
    public class INItializableWindow : EditorWindow
    {
        public INItializableWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        protected IniFile ConfigIni { get; set; }

        private bool _initialized = false;

        protected bool HasCloseButton { get; set; }

        protected EditorButton btnClose { get; private set; }

        protected string SubDirectory { get; set; } = "Windows";

        public T FindChild<T>(string childName, bool optional = false) where T : XNAControl
        {
            T child = FindChild<T>(Children, childName);
            if (child == null && !optional)
                throw new KeyNotFoundException("找不到所需的子控件: " + childName);

            return child;
        }

        private T FindChild<T>(IEnumerable<XNAControl> list, string controlName) where T : XNAControl
        {
            foreach (XNAControl child in list)
            {
                if (child.Name == controlName)
                    return (T)child;

                XNAControl childOfChild = FindChild<T>(child.Children, controlName);
                if (childOfChild != null)
                    return (T)childOfChild;
            }

            return null;
        }

        public override void Initialize()
        {
            if (_initialized)
                throw new InvalidOperationException("INItializableWindow不能初始化两次.");

            var dsc = Path.DirectorySeparatorChar;

            if (ConfigIni == null)
            {
                string defaultConfigIniPath = Path.Combine(Environment.CurrentDirectory, "Config", "Default", "UI", SubDirectory, Name + ".ini");
                string configIniPath = Path.Combine(Environment.CurrentDirectory, "Config", "UI", SubDirectory, Name + ".ini");

                if (File.Exists(configIniPath))
                    ConfigIni = new IniFile(configIniPath);
                else if (File.Exists(defaultConfigIniPath))
                    ConfigIni = new IniFile(defaultConfigIniPath);
                else
                    throw new FileNotFoundException("未找到配置 INI: " + configIniPath);
            }

            Parser.Instance.SetPrimaryControl(this);
            ReadINIForControl(this);
            ReadLateAttributesForControl(this);

            base.Initialize();

            if (HasCloseButton)
            {
                btnClose = new EditorButton(WindowManager);
                btnClose.Name = "btnCloseX";
                btnClose.Width = Constants.UIButtonHeight;
                btnClose.Height = Constants.UIButtonHeight;
                btnClose.Text = "X";
                btnClose.X = Width - btnClose.Width;
                btnClose.Y = 0;
                AddChild(btnClose);
                btnClose.LeftClick += (s, e) => Hide();
            }

            _initialized = true;
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == nameof(HasCloseButton))
                HasCloseButton = iniFile.GetBooleanValue(Name, key, HasCloseButton);
            
            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public void RefreshLayout()
        {
            Parser.Instance.SetPrimaryControl(this);

            var controls = new List<XNAControl>() { this }.Concat(Children);

            foreach (var control in controls)
            {
                ReadINIForControl(control, true);
            }

            if (btnClose != null)
                btnClose.X = Width - btnClose.Width;
        }

        private bool ReadINIForControl(XNAControl control, bool isForLayout = false)
        {
            var section = ConfigIni.GetSection(control.Name);
            if (section == null)
                return false;

            foreach (var kvp in section.Keys)
            {
                if (kvp.Key.StartsWith("$CC"))
                {
                    if (!isForLayout)
                    {
                        var child = CreateChildControl(control, kvp.Value);
                        if (!ReadINIForControl(child))
                            throw new INIConfigException("不存在用于子控件的节" + kvp.Value);

                        child.Initialize();
                    }
                    else
                    {
                        string childName = GetChildControlName(control, kvp.Value);
                        var child = Children.First(cc => cc.Name == childName);
                        if (child == null)
                            throw new INIConfigException($"在 {nameof(INItializableWindow)}中处理 {control.Name} ：找不到子控件 {kvp.Value} 的布局");

                        ReadINIForControl(child, true);
                    }
                }
                else if (kvp.Key == "$X")
                {
                    control.X = Parser.Instance.GetExprValue(kvp.Value, control);
                }
                else if (kvp.Key == "$Y")
                {
                    control.Y = Parser.Instance.GetExprValue(kvp.Value, control);
                }
                else if (kvp.Key == "$Width")
                {
                    control.Width = Parser.Instance.GetExprValue(kvp.Value, control);
                }
                else if (kvp.Key == "$Height")
                {
                    control.Height = Parser.Instance.GetExprValue(kvp.Value, control);
                }
                else if (kvp.Key == "$TextAnchor" && control is XNALabel)
                {
                    // TODO refactor these to be more object-oriented
                    ((XNALabel)control).TextAnchor = (LabelTextAnchorInfo)Enum.Parse(typeof(LabelTextAnchorInfo), kvp.Value);
                }
                else if (kvp.Key == "$AnchorPoint" && control is XNALabel)
                {
                    string[] parts = kvp.Value.Split(',');
                    if (parts.Length != 2)
                        throw new FormatException("AnchorPoint 的格式无效: " + kvp.Value);
                    ((XNALabel)control).AnchorPoint = new Vector2(Parser.Instance.GetExprValue(parts[0], control), Parser.Instance.GetExprValue(parts[1], control));
                }
                else if (!isForLayout && kvp.Key == "$MaxValue" && control is XNATrackbar)
                {
                    ((XNATrackbar)control).MaxValue = Parser.Instance.GetExprValue(kvp.Value, control);
                }
                else if (!isForLayout && kvp.Key == "$Enabled")
                {
                    int value = Parser.Instance.GetExprValue(kvp.Value, control);
                    if (value < 1)
                        control.Disable();
                    else
                        control.Enable();
                }
                else if (!isForLayout && kvp.Key == "$LeftClickAction")
                {
                    if (kvp.Value == "Disable")
                    {
                        control.LeftClick += (s, e) => Hide();
                    }
                }
                else if (!isForLayout)
                {
                    control.ParseINIAttribute(ConfigIni, kvp.Key, kvp.Value);
                }
            }

            return true;
        }

        /// <summary>
        /// Reads a second set of attributes for a control's child controls.
        /// Enables linking controls to controls that are defined after them.
        /// </summary>
        private void ReadLateAttributesForControl(XNAControl control)
        {
            var section = ConfigIni.GetSection(control.Name);
            if (section == null)
                return;

            var children = control.Children.ToList();
            foreach (var child in children)
            {
                var childSection = ConfigIni.GetSection(child.Name);

                if (childSection != null)
                {
                    // This logic should also be enabled for other types in the future,
                    // but it requires changes in XNAUI
                    if (child is XNATextBox)
                    {
                        string nextControl = childSection.GetStringValue("NextControl", null);

                        if (!string.IsNullOrWhiteSpace(nextControl))
                        {
                            var otherChild = children.Find(c => c.Name == nextControl);
                            if (otherChild != null)
                            {
                                ((XNATextBox)child).NextControl = otherChild;

                                if (otherChild is XNATextBox otherAsTb)
                                {
                                    if (otherAsTb.PreviousControl == null)
                                        otherAsTb.PreviousControl = child;
                                }
                            }
                        }

                        string previousControl = childSection.GetStringValue("PreviousControl", null);
                        if (!string.IsNullOrWhiteSpace(previousControl))
                        {
                            var otherChild = children.Find(c => c.Name == previousControl);
                            if (otherChild != null)
                                ((XNATextBox)child).PreviousControl = otherChild;
                        }
                    }

                    string toolTipText = childSection.GetStringValue("ToolTip", null);
                    if (!string.IsNullOrWhiteSpace(toolTipText))
                    {
                        var toolTipControl = new ToolTip(WindowManager, child);
                        toolTipControl.Text = toolTipText;
                        toolTipControl.ToolTipDelay = 0;
                    }
                }
                    
                ReadLateAttributesForControl(child);
            }
        }

        private XNAControl CreateChildControl(XNAControl parent, string keyValue)
        {
            string[] parts = keyValue.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            string childName = GetChildControlName(parent, keyValue);

            if (FindChild<XNAControl>(childName, true) != null)
            {
                throw new INIConfigException("名为" + childName + "的控件已多次定义。");
            }

            var childControl = EditorGUICreator.Instance.CreateControl(WindowManager, parts[1]);
            childControl.Name = childName;
            parent.AddChildWithoutInitialize(childControl);
            return childControl;
        }

        private string GetChildControlName(XNAControl parent, string keyValue)
        {
            string[] parts = keyValue.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                throw new INIConfigException("子控件定义无效" + keyValue);

            if (string.IsNullOrWhiteSpace(parts[0]))
                throw new INIConfigException(parent.Name + "中存在定义了空名称的子控件");
            return parts[0];
        }
    }
}
