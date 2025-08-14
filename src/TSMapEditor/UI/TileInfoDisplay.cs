using Microsoft.Xna.Framework;
using Rampastring.XNAUI.C;
using Rampastring.XNAUI.C.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Rendering;

namespace TSMapEditor.UI
{
    /// <summary>
    /// A panel that displays information about a single tile.
    /// </summary>
    public class TileInfoDisplay : EditorPanel
    {
        public TileInfoDisplay(WindowManager windowManager, Map map, TheaterGraphics theaterGraphics, EditorState editorState) : base(windowManager)
        {
            this.map = map;
            this.theaterGraphics = theaterGraphics;
            this.editorState = editorState;
            InputEnabled = false;
        }

        private readonly Map map;
        private readonly TheaterGraphics theaterGraphics;
        private readonly EditorState editorState;

        private MapTile _mapTile;
        public MapTile MapTile
        {
            get => _mapTile;
            set { _mapTile = value; RefreshInfo(); }
        }

        private XNATextRenderer textRenderer;

        public override void Initialize()
        {
            Name = nameof(TileInfoDisplay);

            Width = 300;

            textRenderer = new XNATextRenderer(WindowManager);
            textRenderer.Name = nameof(textRenderer);
            textRenderer.X = Constants.UIEmptySideSpace;
            textRenderer.Y = Constants.UIEmptyTopSpace;
            textRenderer.Padding = 0;
            textRenderer.SpaceBetweenLines = 2;
            textRenderer.Width = Width - Constants.UIEmptySideSpace * 2;
            AddChild(textRenderer);

            WindowManager.RenderResolutionChanged += WindowManager_RenderResolutionChanged;

            base.Initialize();
        }

        private void WindowManager_RenderResolutionChanged(object sender, EventArgs e)
        {
            X = WindowManager.RenderResolutionX - Width;
        }

        private void RefreshInfo()
        {
            textRenderer.ClearTextParts();

            if (MapTile == null)
            {
                Visible = false;
                return;
            }

            Color subtleTextColor = Color.Gray;
            Color baseTextColor = Color.White;

            Visible = true;

            if (editorState.CursorAction != null)
            {
                textRenderer.AddTextPart(new XNATextPart("所选工具: ", Constants.UIDefaultFontSize, subtleTextColor));
                textRenderer.AddTextPart(new XNATextPart(editorState.CursorAction.GetName() + Environment.NewLine, Constants.UIDefaultFontSize, baseTextColor));
            }
            else
            {
                textRenderer.AddTextPart(new XNATextPart("未选择工具" + Environment.NewLine, Constants.UIDefaultFontSize, subtleTextColor));
            }

            textRenderer.AddTextPart(new XNATextPart(MapTile.X + ", " + MapTile.Y + Environment.NewLine, Constants.UIDefaultFontSize, baseTextColor));

            TileImage tileGraphics = theaterGraphics.GetTileGraphics(MapTile.TileIndex);
            TileSet tileSet = theaterGraphics.Theater.TileSets[tileGraphics.TileSetId];
            textRenderer.AddTextPart(new XNATextPart("图块集: ", Constants.UIDefaultFontSize, subtleTextColor));
            textRenderer.AddTextPart(new XNATextPart(tileSet.SetName + " (" + tileGraphics.TileSetId + ")", Constants.UIDefaultFontSize, baseTextColor));
            textRenderer.AddTextPart(new XNATextPart("图块 #: ", Constants.UIDefaultFontSize, subtleTextColor));
            textRenderer.AddTextPart(new XNATextPart((MapTile.TileIndex - tileSet.StartTileIndex).ToString(CultureInfo.InvariantCulture), Constants.UIDefaultFontSize, baseTextColor));

            MGTMPImage subCellImage = MapTile.SubTileIndex < tileGraphics.TMPImages.Length ? tileGraphics.TMPImages[MapTile.SubTileIndex] : null;
            string terrainType = subCellImage != null && subCellImage.TmpImage != null ? Helpers.LandTypeToString(subCellImage.TmpImage.TerrainType) : "Unknown";

            textRenderer.AddTextLine(new XNATextPart("地形类型: ", Constants.UIDefaultFontSize, subtleTextColor));
            textRenderer.AddTextPart(new XNATextPart(terrainType, Constants.UIDefaultFontSize, baseTextColor));

            if (!Constants.IsFlatWorld)
            {
                textRenderer.AddTextLine(new XNATextPart("高度: ", Constants.UIDefaultFontSize, subtleTextColor));
                textRenderer.AddTextPart(new XNATextPart(MapTile.Level.ToString(), Constants.UIDefaultFontSize, baseTextColor));
            }

            CellTag cellTag = MapTile.CellTag;
            if (cellTag != null)
            {
                textRenderer.AddTextLine(new XNATextPart("Cell标签: ",
                    Constants.UIDefaultFontSize, subtleTextColor));
                textRenderer.AddTextPart(new XNATextPart(cellTag.Tag.Name + " (" + cellTag.Tag.ID + ")",
                    Constants.UIDefaultFontSize, cellTag.Tag.Trigger.EditorColor == null ? baseTextColor : cellTag.Tag.Trigger.XNAColor));
            }

            Overlay overlay = MapTile.Overlay;
            if (overlay != null)
            {
                textRenderer.AddTextLine(new XNATextPart(
                    "覆盖物: ",
                    Constants.UIDefaultFontSize, subtleTextColor));

                textRenderer.AddTextPart(new XNATextPart(
                    overlay.OverlayType.Name + " (" + overlay.OverlayType.Index + " " + overlay.OverlayType.ININame + "), 帧: " + overlay.FrameIndex + ", 地形类型: " + overlay.OverlayType.Land,
                    Constants.UIDefaultFontSize, baseTextColor));
            }

            MapTile.DoForAllAircraft(aircraft => AddObjectInformation("飞行器: ", aircraft));
            MapTile.DoForAllVehicles(unit => AddObjectInformation("载具: ", unit));
            MapTile.DoForAllBuildings(structure => AddObjectInformation("建筑物: ", structure));
            MapTile.DoForAllInfantry(inf => AddObjectInformation("步兵: ", inf));
            MapTile.DoForAllWaypoints(waypoint => AddWaypointInfo(waypoint));
            AddBaseNodeInformation(map.GetBaseNodes(MapTile.CoordsToPoint()));
            AddTerrainObjectInformation(MapTile.TerrainObject);

            textRenderer.PrepareTextParts();

            Height = textRenderer.Bottom + Constants.UIEmptyBottomSpace;
        }

        private void AddWaypointInfo(Waypoint waypoint)
        {
            // Find all usages for this waypoint
            List<string> usages = new List<string>(0);

            foreach (Trigger trigger in map.Triggers)
            {
                bool usageFound = false;

                foreach (var action in trigger.Actions)
                {
                    if (usageFound)
                        break;

                    var triggerActionType = map.EditorConfig.TriggerActionTypes.GetValueOrDefault(action.ActionIndex);

                    if (triggerActionType == null)
                        continue;

                    for (int i = 0; i < triggerActionType.Parameters.Length; i++)
                    {
                        if (triggerActionType.Parameters[i] == null)
                            continue;

                        var param = triggerActionType.Parameters[i];

                        if (param.TriggerParamType == TriggerParamType.Waypoint && action.Parameters[i] == waypoint.Identifier.ToString())
                        {
                            usageFound = true;
                        }
                        else if (param.TriggerParamType == TriggerParamType.WaypointZZ && action.Parameters[i] == Helpers.WaypointNumberToAlphabeticalString(waypoint.Identifier))
                        {
                            usageFound = true;
                        }
                    }
                }

                foreach (var condition in trigger.Conditions)
                {
                    if (usageFound)
                        break;

                    var triggerEventType = map.EditorConfig.TriggerEventTypes.GetValueOrDefault(condition.ConditionIndex);

                    if (triggerEventType == null)
                        continue;

                    for (int i = 0; i < triggerEventType.Parameters.Length; i++)
                    {
                        if (triggerEventType.Parameters[i] == null)
                            continue;

                        var param = triggerEventType.Parameters[i];

                        if (param.TriggerParamType == TriggerParamType.Waypoint && condition.Parameters[i] == waypoint.Identifier.ToString())
                        {
                            usageFound = true;
                        }
                        else if (param.TriggerParamType == TriggerParamType.WaypointZZ && condition.Parameters[i] == Helpers.WaypointNumberToAlphabeticalString(waypoint.Identifier))
                        {
                            usageFound = true;
                        }
                    }
                }

                if (usageFound)
                    usages.Add("触发器 '" + trigger.Name + "', ");
            }

            foreach (Script script in map.Scripts)
            {
                foreach (var actionEntry in script.Actions)
                {
                    var scriptAction = map.EditorConfig.ScriptActions.GetValueOrDefault(actionEntry.Action);

                    if (scriptAction == null)
                    {
                        continue;
                    }

                    if (scriptAction.ParamType == TriggerParamType.Waypoint && actionEntry.Argument == waypoint.Identifier)
                    {
                        usages.Add("脚本 '" + script.Name + "', ");
                    }
                }
            }

            foreach (TeamType team in map.TeamTypes)
            {
                if (team.Waypoint == Helpers.WaypointNumberToAlphabeticalString(waypoint.Identifier))
                {
                    usages.Add("作战小队 '" + team.Name + "', ");
                }
            }

            if (usages.Count > 0)
            {
                string lastUsage = usages[usages.Count - 1];
                usages[usages.Count - 1] = lastUsage.Substring(0, lastUsage.Length - 2);

                textRenderer.AddTextLine(new XNATextPart("路径点 " + waypoint.Identifier + " 用法:", Constants.UIDefaultFontSize, Color.Gray));

                foreach (var usage in usages)
                {
                    textRenderer.AddTextPart(new XNATextPart(usage, Constants.UIDefaultFontSize, Color.White));
                }
            }
        }

        private void AddObjectInformation<T>(string objectTypeLabel, Techno<T> techno) where T : TechnoType
        {
            textRenderer.AddTextLine(new XNATextPart(objectTypeLabel,
                Constants.UIDefaultFontSize, Color.Gray));
            textRenderer.AddTextPart(new XNATextPart(techno.ObjectType.Name + " (" + techno.ObjectType.ININame + "), 所属:",
                    Constants.UIDefaultFontSize, Color.White));
            textRenderer.AddTextPart(new XNATextPart(techno.Owner.ININame, Constants.UIBoldFontSize, techno.Owner.XNAColor));

            if (techno.IsFoot())
            {
                var technoAsFoot = techno as Foot<T>;
                textRenderer.AddTextPart(new XNATextPart("状态: " + technoAsFoot.Mission, Constants.UIDefaultFontSize, Color.White));
            }

            if (techno.WhatAmI() == RTTIType.Unit)
            {
                var unit = techno as Unit;
                int id = map.Units.IndexOf(unit);
                textRenderer.AddTextPart(new XNATextPart("面朝向: " + techno.Facing, Constants.UIDefaultFontSize, Color.White));

                if (unit.FollowerUnit != null)
                {
                    int followerId = map.Units.IndexOf(unit.FollowerUnit);
                    if (followerId > -1)
                    {
                        string followerName = unit.FollowerUnit.UnitType.GetEditorDisplayName();
                        textRenderer.AddTextPart(new XNATextPart("跟随单位: " + followerName + " 位于 " + unit.FollowerUnit.Position, Constants.UIDefaultFontSize, Color.White));
                    }
                }
            }
            
            if (techno.AttachedTag != null)
            {
                textRenderer.AddTextPart(new XNATextPart(",", Constants.UIDefaultFontSize, Color.White));
                textRenderer.AddTextPart(new XNATextPart("关联标记:", Constants.UIDefaultFontSize, Color.White));
                textRenderer.AddTextPart(new XNATextPart(techno.AttachedTag.Name + " (" + techno.AttachedTag.ID + ")", Constants.UIBoldFontSize, Color.White));
            }
        }

        private void AddBaseNodeInformation(List<BaseNode> baseNodes)
        {
            foreach (var baseNode in baseNodes)
            {
                var nodeBuildingType = map.Rules.BuildingTypes.Find(bt => bt.ININame == baseNode.StructureTypeName);
                var house = map.Houses.Find(house => house.BaseNodes.Contains(baseNode));

                if (nodeBuildingType == null || house == null)
                    return;

                textRenderer.AddTextLine(new XNATextPart("基地节点: ", Constants.UIDefaultFontSize, Color.Gray));
                textRenderer.AddTextPart(new XNATextPart($"{nodeBuildingType.Name} ({nodeBuildingType.ININame}), 所属:", Constants.UIDefaultFontSize, Color.White));
                textRenderer.AddTextPart(new XNATextPart(house.ININame, Constants.UIBoldFontSize, house.XNAColor));
            }
        }

        private void AddTerrainObjectInformation(TerrainObject terrainObject)
        {
            if (terrainObject == null)
                return;

            textRenderer.AddTextLine(new XNATextPart("地形对象: ", Constants.UIDefaultFontSize, Color.Gray));
            textRenderer.AddTextPart(new XNATextPart($"{terrainObject.TerrainType.Name} (${terrainObject.TerrainType.ININame})", Constants.UIDefaultFontSize, Color.White));
        }
    }
}
