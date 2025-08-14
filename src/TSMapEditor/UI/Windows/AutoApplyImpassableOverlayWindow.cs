using Rampastring.Tools;
using Rampastring.XNAUI.C;
using Rampastring.XNAUI.C.XNAControls;
using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class AutoApplyImpassableOverlayWindow : INItializableWindow
    {
        public AutoApplyImpassableOverlayWindow(WindowManager windowManager, Map map, IMutationTarget mutationTarget) : base(windowManager)
        {
            this.map = map;
            this.mutationTarget = mutationTarget;
        }

        private readonly Map map;
        private readonly IMutationTarget mutationTarget;

        private XNACheckBox chkRemoveExistingImpassableOverlay;
        private XNACheckBox chkOverrideExistingOverlay;

        private string overlayTypeName = "";
        private OverlayType impassableOverlayType;

        public bool IsAvailable => impassableOverlayType != null;

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == "ImpassableOverlayTypeName")
            {
                overlayTypeName = value;
                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public override void Initialize()
        {
            Name = nameof(AutoApplyImpassableOverlayWindow);
            base.Initialize();

            impassableOverlayType = map.Rules.OverlayTypes.Find(ovt => ovt.ININame == overlayTypeName);
            
            if (impassableOverlayType == null)
            {
                Logger.Log(nameof(AutoApplyImpassableOverlayWindow) + ": 不合法的不可通过覆盖物 " + overlayTypeName);
            }

            chkRemoveExistingImpassableOverlay = FindChild<XNACheckBox>(nameof(chkRemoveExistingImpassableOverlay));
            chkOverrideExistingOverlay = FindChild<XNACheckBox>(nameof(chkOverrideExistingOverlay));
            FindChild<EditorButton>("btnApply").LeftClick += BtnApply_LeftClick;
        }

        public void Open()
        {
            Show();
        }

        private void BtnApply_LeftClick(object sender, EventArgs e)
        {
            if (impassableOverlayType == null)
            {
                EditorMessageBox.Show(WindowManager, "无法应用不可通过的覆盖物",
                    "编辑器未正确配置以应用不可通过的覆盖物.\r\n\r\n" +
                    "未找到预期的覆盖物类型: " + overlayTypeName, MessageBoxButtons.OK);

                return;
            }

            if (chkRemoveExistingImpassableOverlay.Checked)
            {
                map.DoForAllValidTiles(tile =>
                {
                    if (tile.Overlay != null && tile.Overlay.OverlayType == impassableOverlayType)
                        tile.Overlay = null;
                });
            }

            map.TerrainObjects.ForEach(tt =>
            {
                if (tt.TerrainType.ImpassableCells == null)
                    return;

                foreach (Point2D impassableCellOffset in tt.TerrainType.ImpassableCells)
                {
                    Point2D cellCoords = impassableCellOffset + tt.Position;
                    var mapTile = map.GetTile(cellCoords);
                    if (mapTile == null)
                        continue;

                    if (mapTile.Overlay != null && !chkOverrideExistingOverlay.Checked)
                        continue;

                    mapTile.Overlay = new Overlay()
                    {
                        Position = cellCoords,
                        OverlayType = impassableOverlayType,
                        FrameIndex = 0
                    };
                }
            });

            mutationTarget.InvalidateMap();

            Hide();
        }
    }
}
