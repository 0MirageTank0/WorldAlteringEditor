using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class SelectScriptActionWindow : SelectObjectWindow<ScriptAction>
    {
        public SelectScriptActionWindow(WindowManager windowManager, EditorConfig editorConfig) : base(windowManager)
        {
            this.editorConfig = editorConfig;
        }

        private EditorConfig editorConfig;

        private EditorDescriptionPanel panelContent;
        public override void Initialize()
        {
            Name = nameof(SelectScriptActionWindow);
            base.Initialize();
            panelContent = FindChild<EditorDescriptionPanel>(nameof(panelContent));
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (ScriptAction)lbObjectList.SelectedItem.Tag;
            panelContent.Text = SelectedObject.Description;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (ScriptAction scriptAction in editorConfig.ScriptActions.Values)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{scriptAction.ID} {scriptAction.Name}", Tag = scriptAction });
                if (scriptAction == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
