using Rampastring.Tools;
using Rampastring.XNAUI.C;
using Rampastring.XNAUI.C.XNAControls;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace TSMapEditor.UI.Controls
{
    public class EditorLinkLabel : XNALinkLabel
    {
        public EditorLinkLabel(WindowManager windowManager) : base(windowManager)
        {
        }

        public string URL { get; set; }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == "URL")
            {
                URL = value;
                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public override void OnLeftClick(InputEventArgs inputEventArgs)
        {
            inputEventArgs.Handled = true;

            if (!string.IsNullOrWhiteSpace(URL))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(URL) { UseShellExecute = true });
                }
                catch (Win32Exception ex)
                {
                    Logger.Log($"调用 Process.Start 从链接标签 （URL： {URL}） 时出现 Win32Exception，异常消息: {ex.Message}");
                }
                catch (FileNotFoundException ex)
                {
                    Logger.Log($"从链接标签调用 Process.Start 时的 FileNotFoundException （URL： {URL}） ，异常消息: {ex.Message}");
                }
            }

            base.OnLeftClick(inputEventArgs);
        }
    }
}
