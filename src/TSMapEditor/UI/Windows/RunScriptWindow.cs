using Rampastring.Tools;
using Rampastring.XNAUI.C;
using Rampastring.XNAUI.C.XNAControls;
using System;
using System.IO;
using TSMapEditor.Scripts;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class RunScriptWindow : INItializableWindow
    {
        public RunScriptWindow(WindowManager windowManager, ScriptDependencies scriptDependencies) : base(windowManager)
        {
            this.scriptDependencies = scriptDependencies;
        }

        public event EventHandler ScriptRun;

        private readonly ScriptDependencies scriptDependencies;

        private EditorListBox lbScriptFiles;

        private string scriptPath;

        public override void Initialize()
        {
            Name = nameof(RunScriptWindow);
            base.Initialize();

            lbScriptFiles = FindChild<EditorListBox>(nameof(lbScriptFiles));
            FindChild<EditorButton>("btnRunScript").LeftClick += BtnRunScript_LeftClick;
        }

        private void BtnRunScript_LeftClick(object sender, EventArgs e)
        {
            // Run script on next game loop frame so that in case the script displays
            // UI, the UI will be shown on top of our window despite that the user
            // clicked on our window this frame
            AddCallback(RunScript_Callback);
        }

        private void RunScript_Callback()
        {
            if (lbScriptFiles.SelectedItem == null)
                return;

            string filePath = (string)lbScriptFiles.SelectedItem.Tag;
            if (!File.Exists(filePath))
            {
                EditorMessageBox.Show(WindowManager, "找不到文件",
                    "所选文件不存在！也许它被删除了?", MessageBoxButtons.OK);

                return;
            }

            scriptPath = filePath;

            string error = ScriptRunner.CompileScript(scriptDependencies, filePath);

            if (error != null)
            {
                Logger.Log("尝试运行脚本时出现编译错误: " + error);
                EditorMessageBox.Show(WindowManager, "Error",
                    "编译脚本失败！检查其语法，或联系其作者寻求支持." + Environment.NewLine + Environment.NewLine +
                    "返回的错误是: " + error, MessageBoxButtons.OK);
                return;
            }

            if (ScriptRunner.ActiveScriptAPIVersion == 1)
            {
                string confirmation = ScriptRunner.GetDescriptionFromScriptV1();

                // confirmation = Renderer.FixText(confirmation, Constants.UIDefaultFont, Width).Text;

                var messageBox = EditorMessageBox.Show(WindowManager, "是否确定?",
                    confirmation, MessageBoxButtons.YesNo);
                messageBox.YesClickedAction = (_) => ApplyCode();

            }
            else if (ScriptRunner.ActiveScriptAPIVersion == 2)
            {
                error = ScriptRunner.RunScriptV2();

                if (error != null)
                    EditorMessageBox.Show(WindowManager, "运行脚本时出错", error, MessageBoxButtons.OK);
            }
            else
            {
                EditorMessageBox.Show(WindowManager, "不支持的脚本 API 版本",
                    "脚本使用不受支持的脚本 API 版本: " + ScriptRunner.ActiveScriptAPIVersion, MessageBoxButtons.OK);
            }
        }

        private void ApplyCode()
        {
            if (scriptPath == null)
                throw new InvalidOperationException("挂起的脚本路径为 null!");

            string result = ScriptRunner.RunScriptV1(scriptDependencies.Map, scriptPath);
            // result = Renderer.FixText(result, Constants.UIDefaultFont, Width).Text;

            EditorMessageBox.Show(WindowManager, "结果", result, MessageBoxButtons.OK);
            ScriptRun?.Invoke(this, EventArgs.Empty);
        }

        public void Open()
        {
            lbScriptFiles.Clear();

            string directoryPath = Path.Combine(Environment.CurrentDirectory, "Config", "Scripts");

            if (!Directory.Exists(directoryPath))
            {
                Logger.Log("找不到 WAE scipts 目录!");
                EditorMessageBox.Show(WindowManager, "错误", "找不到脚本目录！\r\n\r\n预期路径: " + directoryPath, MessageBoxButtons.OK);
                return;
            }

            var iniFiles = Directory.GetFiles(directoryPath, "*.cs");

            foreach (string filePath in iniFiles)
            {
                lbScriptFiles.AddItem(new XNAListBoxItem(Path.GetFileName(filePath)) { Tag = filePath });
            }

            Show();
        }
    }
}
