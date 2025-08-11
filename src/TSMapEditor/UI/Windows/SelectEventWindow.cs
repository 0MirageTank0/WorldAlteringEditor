using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class SelectEventWindow : SelectObjectWindow<TriggerEventType>
    {
        public SelectEventWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;
        private EditorDescriptionPanel panelContent;
        public bool IsAddingNew { get; set; }

        public override void Initialize()
        {
            Name = nameof(SelectEventWindow);
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

            SelectedObject = (TriggerEventType)lbObjectList.SelectedItem.Tag;
            panelContent.Text = SelectedObject.Description;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (TriggerEventType triggerEventType in map.EditorConfig.TriggerEventTypes.Values)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{triggerEventType.ID} {triggerEventType.Name}", Tag = triggerEventType });
                if (triggerEventType == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
