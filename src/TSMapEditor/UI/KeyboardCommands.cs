using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using System.Collections.Generic;
using TSMapEditor.Settings;

namespace TSMapEditor.UI
{
    public class KeyboardCommands
    {
        public KeyboardCommands()
        {
            Commands = new List<KeyboardCommand>()
            {
                Undo,
                Redo,
                Save,
                ConfigureCopiedObjects,
                Copy,
                CopyCustomShape,
                Paste,
                NextTile,
                PreviousTile,
                NextTileSet,
                PreviousTileSet,
                NextSidebarNode,
                PreviousSidebarNode,
                FrameworkMode,
                NextBrushSize,
                PreviousBrushSize,
                DeleteObject,
                ToggleAutoLAT,
                ToggleMapWideOverlay,
                Toggle2DMode,
                ZoomIn,
                ZoomOut,
                ResetZoomLevel,
                RotateUnit,
                RotateUnitOneStep,
                PlaceTerrainBelow,
                FillTerrain,
                CloneObject,
                OverlapObjects,
                ViewMegamap,
                GenerateTerrain,
                ConfigureTerrainGenerator,
                PlaceTunnel,
                ToggleFullscreen,
                AdjustTileHeightUp,
                AdjustTileHeightDown,
                PlaceConnectedTile,
                RepeatConnectedTile,
                CalculateCredits,
                CheckDistance,
                CheckDistancePathfinding,

                BuildingMenu,
                InfantryMenu,
                VehicleMenu,
                AircraftMenu,
                NavalMenu,
                TerrainObjectMenu,
                OverlayMenu,
                SmudgeMenu
            };

            // Theoretically not optimal for performance, but
            // cleaner this way
            if (Constants.IsFlatWorld)
                Commands.Remove(Toggle2DMode);
        }

        public void ReadFromSettings()
        {
            IniFile iniFile = UserSettings.Instance.UserSettingsIni;

            foreach (var command in Commands)
            {
                string dataString = iniFile.GetStringValue("Keybinds", command.ININame, null);
                if (string.IsNullOrWhiteSpace(dataString))
                    continue;

                command.Key.ApplyDataString(dataString);
            }
        }

        public void WriteToSettings()
        {
            IniFile iniFile = UserSettings.Instance.UserSettingsIni;

            foreach (var command in Commands)
            {
                iniFile.SetStringValue("Keybinds", command.ININame, command.Key.GetDataString());
            }
        }

        public void ClearCommandSubscriptions()
        {
            foreach (var command in Commands)
                command.ClearSubscriptions();
        }


        public static KeyboardCommands Instance { get; set; }

        public List<KeyboardCommand> Commands { get; }

        public KeyboardCommand Undo { get; } =                      new("Undo", "撤销", new KeyboardCommandInput(Keys.Z, KeyboardModifiers.Ctrl));
        public KeyboardCommand Redo { get; } =                      new("Redo", "重做", new KeyboardCommandInput(Keys.Y, KeyboardModifiers.Ctrl));
        public KeyboardCommand Save { get; } =                      new("Save", "保存地图", new KeyboardCommandInput(Keys.S, KeyboardModifiers.Ctrl));
        public KeyboardCommand ConfigureCopiedObjects { get; } =    new("ConfigureCopiedObjects", "复制配置", new KeyboardCommandInput(Keys.None, KeyboardModifiers.None), false);
        public KeyboardCommand Copy { get; } =                      new("Copy", "复制", new KeyboardCommandInput(Keys.C, KeyboardModifiers.Ctrl));
        public KeyboardCommand CopyCustomShape { get; } =           new("CopyCustomShape", "复制（自定义形状）", new KeyboardCommandInput(Keys.C, KeyboardModifiers.Alt));
        public KeyboardCommand Paste { get; } =                     new("Paste", "粘贴", new KeyboardCommandInput(Keys.V, KeyboardModifiers.Ctrl));
        public KeyboardCommand NextTile { get; } =                  new("NextTile", "选择下一个图块", new KeyboardCommandInput(Keys.M, KeyboardModifiers.None));
        public KeyboardCommand PreviousTile { get; } =              new("PreviousTile", "选择上一个图块", new KeyboardCommandInput(Keys.N, KeyboardModifiers.None));
        public KeyboardCommand NextTileSet { get; } =               new("NextTileSet", "选择下一个图块集", new KeyboardCommandInput(Keys.J, KeyboardModifiers.None));
        public KeyboardCommand PreviousTileSet { get; } =           new("PreviousTileSet", "选择上一个图块集", new KeyboardCommandInput(Keys.H, KeyboardModifiers.None));
        public KeyboardCommand NextSidebarNode { get; } =           new("NextSidebarNode", "选择下一个侧边栏节点", new KeyboardCommandInput(Keys.P, KeyboardModifiers.None));
        public KeyboardCommand PreviousSidebarNode { get; } =       new("PreviousSidebarNode", "选择上一个侧边栏节点", new KeyboardCommandInput(Keys.O, KeyboardModifiers.None));
        public KeyboardCommand FrameworkMode { get; } =             new("MarbleMadness", "框架模式（Marble Madness）", new KeyboardCommandInput(Keys.F, KeyboardModifiers.Shift));
        public KeyboardCommand NextBrushSize { get; } =             new("NextBrushSize", "下一个笔刷大小", new KeyboardCommandInput(Keys.OemPlus, KeyboardModifiers.None));
        public KeyboardCommand PreviousBrushSize { get; } =         new("PreviousBrushSize", "上一个画笔大小", new KeyboardCommandInput(Keys.D0, KeyboardModifiers.None));
        public KeyboardCommand DeleteObject { get; } =              new("DeleteObject", "删除对象", new KeyboardCommandInput(Keys.Delete, KeyboardModifiers.None));
        public KeyboardCommand ToggleAutoLAT { get; } =             new("ToggleAutoLAT", "切换启用LAT", new KeyboardCommandInput(Keys.L, KeyboardModifiers.Ctrl));
        public KeyboardCommand ToggleMapWideOverlay { get; } =      new("ToggleMapWideOverlay", "切换地图范围的叠加", new KeyboardCommandInput(Keys.F2, KeyboardModifiers.None));
        public KeyboardCommand Toggle2DMode { get; } =              new("Toggle2DMode", "切换 2D 模式", new KeyboardCommandInput(Keys.D, KeyboardModifiers.Shift));
        public KeyboardCommand ZoomIn { get; } =                    new("ZoomIn", "放大", new KeyboardCommandInput(Keys.OemPlus, KeyboardModifiers.Ctrl));
        public KeyboardCommand ZoomOut { get; } =                   new("ZoomOut", "缩小", new KeyboardCommandInput(Keys.OemMinus, KeyboardModifiers.Ctrl));
        public KeyboardCommand ResetZoomLevel { get; } =            new("ResetZoomLevel", "重置缩放级别", new KeyboardCommandInput(Keys.D0, KeyboardModifiers.Ctrl));
        public KeyboardCommand RotateUnit { get; } =                new("RotateUnit", "旋转单位", new KeyboardCommandInput(Keys.A, KeyboardModifiers.None));
        public KeyboardCommand RotateUnitOneStep { get; } =         new("RotateUnitOneStep", "旋转步进旋转单位", new KeyboardCommandInput(Keys.A, KeyboardModifiers.Shift));
        public KeyboardCommand PlaceTerrainBelow { get; } =         new("PlaceTerrainBelow", "将地形放置在光标下方", new KeyboardCommandInput(Keys.None, KeyboardModifiers.Alt), true);
        public KeyboardCommand FillTerrain { get; } =               new("FillTerrain", "填充地形（仅限 1x1 图块）", new KeyboardCommandInput(Keys.None, KeyboardModifiers.Ctrl), true);
        public KeyboardCommand CloneObject { get; } =               new("CloneObject", "克隆对象 （编辑）", new KeyboardCommandInput(Keys.None, KeyboardModifiers.Shift), true);
        public KeyboardCommand OverlapObjects { get; } =            new("OverlapObjects", "重叠对象（修改器）", new KeyboardCommandInput(Keys.None, KeyboardModifiers.Alt), true);
        public KeyboardCommand ViewMegamap { get; } =               new("ViewMegamap", "查看超级地图", new KeyboardCommandInput(Keys.F12, KeyboardModifiers.None));
        public KeyboardCommand GenerateTerrain { get; } =           new("GenerateTerrain", "生成地形", new KeyboardCommandInput(Keys.G, KeyboardModifiers.Ctrl));
        public KeyboardCommand ConfigureTerrainGenerator { get; } = new("ConfigureTerrainGenerator", "配置地形生成器", new KeyboardCommandInput(Keys.G, KeyboardModifiers.Alt));
        public KeyboardCommand PlaceTunnel { get; } =               new("PlaceTunnel", "放置隧道", new KeyboardCommandInput(Keys.OemPeriod, KeyboardModifiers.None));
        public KeyboardCommand ToggleFullscreen { get; } =          new("ToggleFullscreen", "切换全屏幕", new KeyboardCommandInput(Keys.F11, KeyboardModifiers.None));
        public KeyboardCommand AdjustTileHeightUp { get; } =        new("AdjustTileHeightUp", "向上调整图块高度", new KeyboardCommandInput(Keys.PageUp, KeyboardModifiers.None), forActionsOnly:true);
        public KeyboardCommand AdjustTileHeightDown { get; } =      new("AdjustTileHeightDown", "向下调整图块高度", new KeyboardCommandInput(Keys.PageDown, KeyboardModifiers.None), forActionsOnly:true);
        public KeyboardCommand PlaceConnectedTile { get; } =        new("PlaceConnectedTile", "放置可连接图块", new KeyboardCommandInput(Keys.D, KeyboardModifiers.Alt));
        public KeyboardCommand RepeatConnectedTile { get; } =       new("RepeatConnectedTile", "重复放置上一次可连接图块", new KeyboardCommandInput(Keys.D, KeyboardModifiers.Ctrl));
        public KeyboardCommand CalculateCredits { get; } =          new("CalculateCredits", "计算矿石价值", new KeyboardCommandInput(Keys.C, KeyboardModifiers.Shift));
        public KeyboardCommand CheckDistance { get; } =             new("CheckDistance", "测距", new KeyboardCommandInput(Keys.B, KeyboardModifiers.None));
        public KeyboardCommand CheckDistancePathfinding { get; } =  new("CheckDistancePathfinding", "测距（寻路）", new KeyboardCommandInput(Keys.B, KeyboardModifiers.Shift));

        public KeyboardCommand BuildingMenu { get; } =              new("BuildingMenu", "建筑物菜单", new KeyboardCommandInput(Keys.D1, KeyboardModifiers.None));
        public KeyboardCommand InfantryMenu { get; } =              new("InfantryMenu", "步兵菜单", new KeyboardCommandInput(Keys.D2, KeyboardModifiers.None));
        public KeyboardCommand VehicleMenu { get; } =               new("VehicleMenu", "载具菜单", new KeyboardCommandInput(Keys.D3, KeyboardModifiers.None));
        public KeyboardCommand AircraftMenu { get; } =              new("AircraftMenu", "飞行器菜单", new KeyboardCommandInput(Keys.D4, KeyboardModifiers.None));
        public KeyboardCommand NavalMenu { get; } =                 new("NavalMenu", "海军菜单", new KeyboardCommandInput(Keys.D5, KeyboardModifiers.None));
        public KeyboardCommand TerrainObjectMenu { get; } =         new("TerrainObjectMenu", "地形对象菜单", new KeyboardCommandInput(Keys.D6, KeyboardModifiers.None));
        public KeyboardCommand OverlayMenu { get; } =               new("OverlayMenu", "覆盖物菜单", new KeyboardCommandInput(Keys.D7, KeyboardModifiers.None));
        public KeyboardCommand SmudgeMenu { get; } =                new("SmudgeMenu", "脏迹菜单", new KeyboardCommandInput(Keys.D8, KeyboardModifiers.None));
    }
}
