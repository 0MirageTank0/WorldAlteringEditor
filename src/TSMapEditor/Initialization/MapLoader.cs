using CNCMaps.FileFormats.Encodings;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TSMapEditor.CCEngine;
using TSMapEditor.Extensions;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Models.MapFormat;
using TSMapEditor.Rendering;

namespace TSMapEditor.Initialization
{
    /// <summary>
    /// Contains functions for parsing and applying different sections of a map file.
    /// </summary>
    public static class MapLoader
    {
        private const int BUILDING_PROPERTY_FIELD_COUNT = 17;
        private const int UNIT_PROPERTY_FIELD_COUNT = 14;
        private const int INFANTRY_PROPERTY_FIELD_COUNT = 14;
        private const int AIRCRAFT_PROPERTY_FIELD_COUNT = 12;
        private const int AI_TRIGGER_PROPERTY_FIELD_COUNT = 18;

        public static List<string> MapLoadErrors = new List<string>();

        private static void AddMapLoadError(string error)
        {
            Logger.Log(error);
            MapLoadErrors.Add(error);
        }

        public static void PreCheckMapIni(IniFile mapIni)
        {
            Logger.Log("执行预加载地图检查.");

            var section = mapIni.GetSection("Map");
            if (section == null)
                throw new MapLoadException("[Map] 在加载的文件中不存在!");

            string size = section.GetStringValue("Size", null);
            if (size == null)
                throw new MapLoadException("Invalid [Map] Size=");
            string[] parts = size.Split(',');
            if (parts.Length != 4)
                throw new MapLoadException("Invalid [Map] Size=");

            int width = int.Parse(parts[2], CultureInfo.InvariantCulture);
            int height = int.Parse(parts[3], CultureInfo.InvariantCulture);

            if (width > Constants.MaxMapWidth)
            {
                throw new MapLoadException($"地图宽度不能大于" +
                    $"{Constants.MaxMapWidth} 单元格,地图宽是 {width} !");
            }

            if (height > Constants.MaxMapHeight)
            {
                throw new MapLoadException("地图高度不能大于" +
                    $"{Constants.MaxMapHeight} 单元格,地图高是 {height} !");
            }

            Logger.Log("预加载地图检查完成.");
        }

        public static void PostCheckMap(IMap map, TheaterGraphics theaterGraphics)
        {
            Logger.Log("执行加载后地图检查.");

            map.DoForAllValidTiles(t =>
            {
                if (t.TileIndex >= theaterGraphics.TileCount)
                {
                    AddMapLoadError($"{t.CoordsToPoint()} 单元格的图块索引 {t.TileIndex} 无效 - 将其设置为 0");
                    t.TileIndex = 0;
                    t.SubTileIndex = 0;
                    return;
                }

                var tile = theaterGraphics.GetTile(t.TileIndex);
                var tileSet = theaterGraphics.Theater.TileSets[tile.TileSetId];
                int maxSubTileIndex = tile.SubTileCount - 1;
                if (t.SubTileIndex > maxSubTileIndex)
                {
                    AddMapLoadError($"对于 {t.CoordsToPoint()} 处的单元格,子图块索引 {t.SubTileIndex} 无效(最大值:{maxSubTileIndex}) - 将其设置为 0." +
                        $"图块集: {tileSet.SetName} ({tileSet.FileName}),其集合内的图块索引:{tile.TileIndexInTileSet}");

                    t.SubTileIndex = 0;

                    if (maxSubTileIndex < 0)
                    {
                        AddMapLoadError($"在 {t.CoordsToPoint()} 处检测到的图块的最大子图块计数为 0,同时将单元格的图块索引设置为 0.");
                        t.TileIndex = 0;
                    }

                    return;
                }

                if (tile.GetSubTile(t.SubTileIndex).TmpImage == null)
                {
                    AddMapLoadError($"位于单元格({t.CoordsToPoint()}) 的子图集 {t.SubTileIndex} 为null - 已清除图块. " +
                        $"图块集: {tileSet.SetName} ({tileSet.FileName}), 其集合内图块的索引: {tile.TileIndexInTileSet}");

                    t.ChangeTileIndex(0, 0);
                }
            });

            Logger.Log("加载后地图检查完成.");
        }

        public static void ReadMapSection(IMap map, IniFile mapIni)
        {
            Logger.Log("读取 [Map] 节.");

            var section = mapIni.GetSection("Map");
            if (section == null)
                throw new MapLoadException("[Map]节 在加载的文件中不存在!");

            string size = section.GetStringValue("Size", null);
            string[] parts = size.Split(',');

            int width = int.Parse(parts[2], CultureInfo.InvariantCulture);
            int height = int.Parse(parts[3], CultureInfo.InvariantCulture);
            map.Size = new Point2D(width, height);

            string localSize = section.GetStringValue("LocalSize", null);
            if (localSize == null)
                throw new MapLoadException("Invalid [Map] LocalSize=");
            parts = localSize.Split(',');
            if (parts.Length != 4)
                throw new MapLoadException("Invalid [Map] LocalSize=");

            map.LocalSize = new Rectangle(
                Conversions.IntFromString(parts[0], 0),
                Conversions.IntFromString(parts[1], 0),
                Conversions.IntFromString(parts[2], width),
                Conversions.IntFromString(parts[3], height));

            map.TheaterName = section.GetStringValue("Theater", string.Empty);

            Logger.Log("[Map]节 读取成功.");
        }

        public static void ReadIsoMapPack(IMap map, IniFile mapIni)
        {
            Logger.Log("读取 IsoMapPack5.");

            var section = mapIni.GetSection("IsoMapPack5");
            if (section == null)
            {
                map.SetTileData(new List<MapTile>(0));
                return;
            }

            if (section.Keys.Count == 0)
            {
                Logger.Log("[IsoMapPack5] 没有数据!");
                map.SetTileData(new List<MapTile>(0));
                return;
            }

            StringBuilder sb = new StringBuilder();
            section.Keys.ForEach(kvp => sb.Append(kvp.Value));

            byte[] compressedData = Convert.FromBase64String(sb.ToString());
            if (compressedData.Length < 4)
                throw new InvalidOperationException("无效的 IsoMapPack5 格式");

            Logger.Log("IsoMapPack5 压缩数据长度: " + compressedData.Length);

            List<byte> uncompressedData = new List<byte>();

            int position = 0;

            while (position < compressedData.Length)
            {
                ushort inputSize = BitConverter.ToUInt16(compressedData, position);
                ushort outputSize = BitConverter.ToUInt16(compressedData, position + 2);

                Logger.Log("解码 IsoMapPack5 块: pos: " + position + ", inSize: " + inputSize + ", outSize: " + outputSize);

                if (position + inputSize + 4 > compressedData.Length)
                    throw new InvalidOperationException("解码 IsoMapPack5 时出错");

                byte[] inData = new byte[inputSize];
                Array.Copy(compressedData, position + 4, inData, 0, inputSize);
                byte[] outData = new byte[outputSize];
                MiniLZO.MiniLZO.Decompress(inData, outData);
                uncompressedData.AddRange(outData);

                position += inputSize + 4;
            }

            // if ((uncompressedData.Count % IsoMapPack5Tile.Size) != 4)
            //      throw new InvalidOperationException("Decompressed IsoMapPack5 size does not match expected struct size");

            var tiles = new List<MapTile>(uncompressedData.Count / IsoMapPack5Tile.Size);
            position = 0;
            while (position < uncompressedData.Count - IsoMapPack5Tile.Size)
            {
                var mapTile = new MapTile(uncompressedData.GetRange(position, IsoMapPack5Tile.Size).ToArray());
                if (mapTile.TileIndex == ushort.MaxValue)
                {
                    mapTile.TileIndex = 0;
                }
                tiles.Add(mapTile);
                position += IsoMapPack5Tile.Size;
            }

            map.SetTileData(tiles);

            Logger.Log("IsoMapPack5 读取成功.");
        }

        public static void ReadBasicSection(IMap map, IniFile mapIni)
        {
            Logger.Log("读取 [Basic]节.");

            var section = mapIni.GetSection("Basic");
            if (section == null)
                return;

            map.Basic.ReadPropertiesFromIniSection(section);

            Logger.Log("[Basic]节 读取成功.");
        }

        public static void ReadTerrainObjects(IMap map, IniFile mapIni)
        {
            Logger.Log("读取 TerrainObjects");

            IniSection section = mapIni.GetSection("Terrain");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                string coords = kvp.Key;
                int yLength = coords.Length - 3;
                int y = Conversions.IntFromString(coords.Substring(0, yLength), -1);
                int x = Conversions.IntFromString(coords.Substring(yLength), -1);
                if (y < 0 || x < 0)
                    continue;

                TerrainType terrainType = map.Rules.TerrainTypes.Find(tt => tt.ININame == kvp.Value);
                if (terrainType == null)
                {
                    AddMapLoadError($"跳过在 {x},{y} 处的地形类型 {kvp.Value}, 因为它在规则中不存在.");
                    continue;
                }

                var terrainObject = new TerrainObject(terrainType, new Point2D(x, y));
                var tile = map.GetTile(x, y);
                if (tile == null)
                {
                    AddMapLoadError($"地形对象 {terrainType.ININame} 已放置在有效地图区域之外的 {x},{y} 处,已忽略.");
                    continue;
                }

                map.TerrainObjects.Add(terrainObject);
                tile.TerrainObject = terrainObject;
            }

            Logger.Log("TerrainObjects 读取成功.");
        }

        private static void FindAttachedTag(IMap map, TechnoBase techno, string attachedTagString)
        {
            if (attachedTagString != Constants.NoneValue1 && attachedTagString != Constants.NoneValue2)
            {
                Tag tag = map.Tags.Find(t => t.ID == attachedTagString);
                if (tag == null)
                {
                    AddMapLoadError($"找不到附加于 {techno.WhatAmI()}(坐标{techno.Position}) 的 {attachedTagString}");
                    return;
                }

                techno.AttachedTag = tag;
            }
        }

        public static void ReadBuildings(IMap map, IniFile mapIni)
        {
            Logger.Log("读取建筑物.");

            IniSection section = mapIni.GetSection("Structures");
            if (section == null)
                return;

            // [Structures]
            // INDEX=OWNER,ID,HEALTH,X,Y,FACING,TAG,AI_SELLABLE,AI_REBUILDABLE,POWERED_ON,UPGRADES,SPOTLIGHT,UPGRADE_1,UPGRADE_2,UPGRADE_3,AI_REPAIRABLE,NOMINAL

            foreach (var kvp in section.Keys)
            {
                string[] values = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < BUILDING_PROPERTY_FIELD_COUNT)
                    continue;

                string ownerName = values[0];
                string buildingTypeId = values[1];
                int health = Math.Min(Constants.ObjectHealthMax, Math.Max(0, Conversions.IntFromString(values[2], Constants.ObjectHealthMax)));
                int x = Conversions.IntFromString(values[3], 0);
                int y = Conversions.IntFromString(values[4], 0);
                int facing = Math.Min(Constants.FacingMax, Math.Max(0, Conversions.IntFromString(values[5], Constants.FacingMax)));
                string attachedTag = values[6];
                bool aiSellable = Conversions.BooleanFromString(values[7], true);
                // AI_REBUILDABLE is a leftover
                bool powered = Conversions.BooleanFromString(values[9], true);
                int upgradeCount = Conversions.IntFromString(values[10], 0);
                int spotlight = Conversions.IntFromString(values[11], 0);
                string[] upgradeIds = new string[] { values[12], values[13], values[14] };
                bool aiRepairable = Conversions.BooleanFromString(values[15], false);
                bool nominal = Conversions.BooleanFromString(values[16], false);

                var buildingType = map.Rules.BuildingTypes.Find(bt => bt.ININame == buildingTypeId);
                if (buildingType == null)
                {
                    AddMapLoadError($"找不到建筑物类型 {buildingTypeId} - 已跳过");
                    continue;
                }

                House owner = map.FindOrMakeHouse(ownerName);
                var building = new Structure(buildingType)
                {
                    HP = health,
                    Position = new Point2D(x, y),
                    Facing = (byte)facing,
                    AISellable = aiSellable,
                    Powered = powered,
                    Spotlight = (SpotlightType)spotlight,
                    AIRepairable = aiRepairable,
                    Nominal = nominal,
                    Owner = map.FindOrMakeHouse(ownerName)
                };

                if (upgradeCount > 0)
                {
                    int appliedUpgrades = 0;

                    for (int i = 0; i < Structure.MaxUpgradeCount; i++)
                    {
                        if (!Helpers.IsStringNoneValue(upgradeIds[i]))
                        {
                            var upgradeBuildingType = map.Rules.BuildingTypes.Find(b => b.ININame == upgradeIds[i]);
                            if (upgradeBuildingType == null)
                            {
                                AddMapLoadError($"为建筑物 {buildingTypeId} 指定的建筑物升级 {upgradeIds[i]} 无效");
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(upgradeBuildingType.PowersUpBuilding) || !upgradeBuildingType.PowersUpBuilding.Equals(buildingType.ININame, StringComparison.OrdinalIgnoreCase))
                            {
                                AddMapLoadError($"Building {buildingTypeId} has an upgrade {upgradeBuildingType.ININame}, but \r\n{upgradeBuildingType.ININame} " +
                                    $"does not specify {buildingTypeId} in its PowersUpBuilding= key. Skipping adding upgrade to map.");
                                continue;
                            }

                            if (appliedUpgrades >= buildingType.Upgrades)
                            {
                                AddMapLoadError($"建筑 {buildingTypeId} 在位置 {building.Position} 的升级数 ({appliedUpgrades + 1}) " +
                                       $"超过了规则中 Upgrades= 值 ({buildingType.Upgrades}) 的限制.正在跳过添加该建筑的一个或多个升级.");
                                break;
                            }

                            building.Upgrades[appliedUpgrades] = upgradeBuildingType;
                            appliedUpgrades++;
                        }
                    }
                }

                FindAttachedTag(map, building, attachedTag);

                bool isClear = true;

                void CheckFoundationCell(Point2D cellCoords)
                {
                    if (!isClear)
                        return;

                    var tile = map.GetTile(cellCoords);
                    if (tile == null)
                    {
                        isClear = false;
                        AddMapLoadError($"建筑 {buildingType.ININame} 被放置在地图外的位置 {cellCoords}.正在跳过将其添加到地图.");
                        return;
                    }

                    if (tile.Structures.Count > 0)
                    {
                        Logger.Log($"注意: 建筑 {buildingType.ININame} 存在于位置 {cellCoords} 的单元格中,该单元格已包含其他建筑: {string.Join(", ", tile.Structures.Select(s => s.ObjectType.ININame))}");
                    }
                }

                buildingType.ArtConfig.DoForFoundationCoordsOrOrigin(offset => CheckFoundationCell(building.Position + offset));

                if (!isClear)
                    continue;

                map.Structures.Add(building);
                buildingType.ArtConfig.DoForFoundationCoordsOrOrigin(offset =>
                {
                    var tile = map.GetTile(building.Position + offset);
                    tile.Structures.Add(building);
                });

                building.LightTiles(map.Tiles);
            }

            map.Structures.ForEach(s => s.UpdatePowerUpAnims());

            Logger.Log("建筑物读取成功.");
        }

        public static void ReadAircraft(IMap map, IniFile mapIni)
        {
            Logger.Log("读取飞行物.");

            IniSection section = mapIni.GetSection("Aircraft");
            if (section == null)
                return;

            // [Aircraft]
            // INDEX=OWNER,ID,HEALTH,X,Y,FACING,MISSION,TAG,VETERANCY,GROUP,AUTOCREATE_NO_RECRUITABLE,AUTOCREATE_YES_RECRUITABLE

            foreach (var kvp in section.Keys)
            {
                string[] values = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < AIRCRAFT_PROPERTY_FIELD_COUNT)
                    continue;

                string ownerName = values[0];
                string aircraftTypeId = values[1];
                int health = Math.Min(Constants.ObjectHealthMax, Math.Max(0, Conversions.IntFromString(values[2], Constants.ObjectHealthMax)));
                int x = Conversions.IntFromString(values[3], 0);
                int y = Conversions.IntFromString(values[4], 0);
                int facing = Math.Min(Constants.FacingMax, Math.Max(0, Conversions.IntFromString(values[5], Constants.FacingMax)));
                string mission = values[6];
                string attachedTag = values[7];
                int veterancy = Conversions.IntFromString(values[8], 0);
                int group = Conversions.IntFromString(values[9], 0);
                bool autocreateNoRecruitable = Conversions.BooleanFromString(values[10], false);
                bool autocreateYesRecruitable = Conversions.BooleanFromString(values[11], false);

                var aircraftType = map.Rules.AircraftTypes.Find(ut => ut.ININame == aircraftTypeId);
                if (aircraftType == null)
                {
                    AddMapLoadError($"无法找到飞行器类型 {aircraftTypeId} - 正在跳过将其添加到地图.");
                    continue;
                }

                var aircraft = new Aircraft(aircraftType)
                {
                    HP = health,
                    Position = new Point2D(x, y),
                    Facing = (byte)facing,
                    Mission = mission,
                    Veterancy = veterancy,
                    Group = group,
                    AutocreateNoRecruitable = autocreateNoRecruitable,
                    AutocreateYesRecruitable = autocreateYesRecruitable,
                    Owner = map.FindOrMakeHouse(ownerName)
                };

                FindAttachedTag(map, aircraft, attachedTag);

                map.Aircraft.Add(aircraft);
                var tile = map.GetTile(x, y);
                if (tile != null)
                    tile.Aircraft.Add(aircraft);
            }

            Logger.Log("飞行器读取成功.");
        }

        public static void ReadUnits(IMap map, IniFile mapIni)
        {
            Logger.Log("读取单位.");

            IniSection section = mapIni.GetSection("Units");
            if (section == null)
                return;

            // [Units]
            // INDEX=OWNER,ID,HEALTH,X,Y,FACING,MISSION,TAG,VETERANCY,GROUP,HIGH,FOLLOWS_INDEX,AUTOCREATE_NO_RECRUITABLE,AUTOCREATE_YES_RECRUITABLE

            foreach (var kvp in section.Keys)
            {
                string[] values = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < UNIT_PROPERTY_FIELD_COUNT)
                    continue;

                string ownerName = values[0];
                string unitTypeId = values[1];
                int health = Math.Min(Constants.ObjectHealthMax, Math.Max(0, Conversions.IntFromString(values[2], Constants.ObjectHealthMax)));
                int x = Conversions.IntFromString(values[3], 0);
                int y = Conversions.IntFromString(values[4], 0);
                int facing = Math.Min(Constants.FacingMax, Math.Max(0, Conversions.IntFromString(values[5], Constants.FacingMax)));
                string mission = values[6];
                string attachedTag = values[7];
                int veterancy = Conversions.IntFromString(values[8], 0);
                int group = Conversions.IntFromString(values[9], 0);
                bool high = Conversions.BooleanFromString(values[10], false);
                int followsIndex = Conversions.IntFromString(values[11], 0);
                bool autocreateNoRecruitable = Conversions.BooleanFromString(values[12], false);
                bool autocreateYesRecruitable = Conversions.BooleanFromString(values[13], false);

                var unitType = map.Rules.UnitTypes.Find(ut => ut.ININame == unitTypeId);
                if (unitType == null)
                {
                    AddMapLoadError($"无法找到单位类型 {unitTypeId} - 正在跳过将其添加到地图.");
                    continue;
                }

                var unit = new Unit(unitType)
                {
                    HP = health,
                    Position = new Point2D(x, y),
                    Facing = (byte)facing,
                    Mission = mission,
                    Veterancy = veterancy,
                    Group = group,
                    High = high,
                    FollowerID = followsIndex,
                    AutocreateNoRecruitable = autocreateNoRecruitable,
                    AutocreateYesRecruitable = autocreateYesRecruitable,
                    Owner = map.FindOrMakeHouse(ownerName)
                };

                FindAttachedTag(map, unit, attachedTag);

                map.Units.Add(unit);
                var tile = map.GetTile(x, y);
                if (tile != null)
                    tile.Vehicles.Add(unit);
            }

            // Process follow IDs
            foreach (var unit in map.Units)
            {
                if (unit.FollowerID < 0 || unit.FollowerID >= map.Units.Count)
                    continue;

                unit.FollowerUnit = map.Units[unit.FollowerID];
            }

            Logger.Log("单位数据读取成功.");
        }

        public static void ReadInfantry(IMap map, IniFile mapIni)
        {
            Logger.Log("读取步兵.");

            IniSection section = mapIni.GetSection("Infantry");
            if (section == null)
                return;

            // [Infantry]
            // INDEX=OWNER,ID,HEALTH,X,Y,SUB_CELL,MISSION,FACING,TAG,VETERANCY,GROUP,HIGH,AUTOCREATE_NO_RECRUITABLE,AUTOCREATE_YES_RECRUITABLE

            foreach (var kvp in section.Keys)
            {
                string[] values = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < INFANTRY_PROPERTY_FIELD_COUNT)
                    continue;

                string ownerName = values[0];
                string infantryTypeId = values[1];
                int health = Math.Min(Constants.ObjectHealthMax, Math.Max(0, Conversions.IntFromString(values[2], Constants.ObjectHealthMax)));
                int x = Conversions.IntFromString(values[3], 0);
                int y = Conversions.IntFromString(values[4], 0);
                SubCell subCell = (SubCell)Conversions.IntFromString(values[5], 0);
                string mission = values[6];
                int facing = Math.Min(Constants.FacingMax, Math.Max(0, Conversions.IntFromString(values[7], Constants.FacingMax)));
                string attachedTag = values[8];
                int veterancy = Conversions.IntFromString(values[9], 0);
                int group = Conversions.IntFromString(values[10], 0);
                bool high = Conversions.BooleanFromString(values[11], false);
                bool autocreateNoRecruitable = Conversions.BooleanFromString(values[12], false);
                bool autocreateYesRecruitable = Conversions.BooleanFromString(values[13], false);

                var infantryType = map.Rules.InfantryTypes.Find(it => it.ININame == infantryTypeId);
                if (infantryType == null)
                {
                    AddMapLoadError($"无法找到步兵类型 {infantryTypeId} - 正在跳过将其添加到地图.");
                    continue;
                }

                var infantry = new Infantry(infantryType)
                {
                    HP = health,
                    Position = new Point2D(x, y), // TODO handle sub-cell in position?
                    Facing = (byte)facing,
                    Veterancy = veterancy,
                    Group = group,
                    High = high,
                    AutocreateNoRecruitable = autocreateNoRecruitable,
                    AutocreateYesRecruitable = autocreateYesRecruitable,
                    SubCell = subCell,
                    Mission = mission,
                    Owner = map.FindOrMakeHouse(ownerName)
                };

                FindAttachedTag(map, infantry, attachedTag);

                map.Infantry.Add(infantry);
                var tile = map.GetTile(x, y);
                if (tile != null)
                    tile.Infantry[(int)subCell] = infantry;
            }

            Logger.Log("步兵读取成功.");
        }

        public static void ReadSmudges(IMap map, IniFile mapIni)
        {
            Logger.Log("读取污迹.");

            var smudgesSection = mapIni.GetSection("Smudge");
            if (smudgesSection == null)
                return;

            foreach (var kvp in smudgesSection.Keys)
            {
                string[] values = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (values.Length < 3)
                {
                    AddMapLoadError($"地图中定义的污渍语法无效: {kvp.Value}");
                    continue;
                }

                string smudgeTypeId = values[0];
                int x = Conversions.IntFromString(values[1], -1);
                int y = Conversions.IntFromString(values[2], -1);
                if (values.Length > 3 && values[3] != "0")
                {
                    AddMapLoadError($"位置 {x},{y} 的污迹语法无效: {kvp.Value}");
                    continue;
                }

                var smudgeType = map.Rules.SmudgeTypes.Find(st => st.ININame == smudgeTypeId);
                if (smudgeType == null)
                {
                    AddMapLoadError($"位置 {x},{y} 的单元格包含一个在 Rules.ini 中不存在的污迹 '{smudgeTypeId}'. 已忽略");
                    continue;
                }

                var cell = map.GetTile(x, y);
                if (cell == null)
                {
                    AddMapLoadError($"位置 {x},{y} 的污渍被放置在地图外. 已忽略");
                    continue;
                }

                cell.Smudge = new Smudge() { SmudgeType = smudgeType, Position = new Point2D(x, y) };
            }

            Logger.Log("污迹读取成功.");
        }

        public static void ReadOverlays(IMap map, IniFile mapIni)
        {
            Logger.Log("读取覆盖物 (OverlayPack与OverlayDataPack).");

            var overlayPackSection = mapIni.GetSection("OverlayPack");
            var overlayDataPackSection = mapIni.GetSection("OverlayDataPack");
            if (overlayPackSection == null || overlayDataPackSection == null)
                return;

            bool needsExtendedOverlayPack = map.Basic.NewINIFormat >= 5;

            var stringBuilder = new StringBuilder();
            overlayPackSection.Keys.ForEach(kvp => stringBuilder.Append(kvp.Value));
            byte[] compressedData = Convert.FromBase64String(stringBuilder.ToString());
            byte[] uncompressedOverlayPack = new byte[Constants.MAX_MAP_LENGTH_IN_DIMENSION * Constants.MAX_MAP_LENGTH_IN_DIMENSION * (needsExtendedOverlayPack ? 2 : 1)];
            Format5.DecodeInto(compressedData, uncompressedOverlayPack, Constants.OverlayPackFormat);

            stringBuilder.Clear();
            overlayDataPackSection.Keys.ForEach(kvp => stringBuilder.Append(kvp.Value));
            compressedData = Convert.FromBase64String(stringBuilder.ToString());
            byte[] uncompressedOverlayDataPack = new byte[Constants.MAX_MAP_LENGTH_IN_DIMENSION * Constants.MAX_MAP_LENGTH_IN_DIMENSION];
            Format5.DecodeInto(compressedData, uncompressedOverlayDataPack, Constants.OverlayPackFormat);

            for (int y = 0; y < map.Tiles.Length; y++)
            {
                for (int x = 0; x < map.Tiles[y].Length; x++)
                {
                    var tile = map.Tiles[y][x];
                    if (tile == null)
                        continue;

                    int overlayDataIndex = (tile.Y * Constants.MAX_MAP_LENGTH_IN_DIMENSION) + tile.X;

                    int overlayTypeIndex;
                    if (needsExtendedOverlayPack)
                    {
                        ushort index = BinaryPrimitives.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(uncompressedOverlayPack, overlayDataIndex * 2, 2));
                        overlayTypeIndex = index != ushort.MaxValue ? index : Constants.NO_OVERLAY;
                    }
                    else
                    {
                        byte index = uncompressedOverlayPack[overlayDataIndex];
                        overlayTypeIndex = index != byte.MaxValue ? index : Constants.NO_OVERLAY;
                    }

                    if (overlayTypeIndex == Constants.NO_OVERLAY)
                        continue;

                    if (overlayTypeIndex >= map.Rules.OverlayTypes.Count)
                    {
                        AddMapLoadError("正在忽略位置 " + x + ", " + y + " 的覆盖层,因为它超出了 Rules.ini 覆盖层列表的范围");
                        continue;
                    }

                    var overlayType = map.Rules.OverlayTypes[overlayTypeIndex];
                    var overlay = new Overlay()
                    {
                        OverlayType = overlayType,
                        FrameIndex = uncompressedOverlayDataPack[overlayDataIndex],
                        Position = new Point2D(tile.X, tile.Y)
                    };
                    tile.Overlay = overlay;
                }
            }

            Logger.Log("覆盖物读取成功.");
        }

        public static void ReadWaypoints(IMap map, IniFile mapIni)
        {
            Logger.Log("读取路径点.");

            var waypointsSection = mapIni.GetSection("Waypoints");
            if (waypointsSection == null)
                return;

            foreach (var kvp in waypointsSection.Keys)
            {
                var waypoint = Waypoint.ParseWaypoint(kvp.Key, kvp.Value);
                if (waypoint == null)
                {
                    AddMapLoadError($"路点语法无效: {kvp.Key}={kvp.Value}");
                    continue;
                }

                var tile = map.GetTile(waypoint.Position.X, waypoint.Position.Y);
                if (tile == null)
                {
                    Point2D oldPosition = waypoint.Position;

                    // Find new cell to move waypoint to
                    // Lazy and inefficient implementation, but waypoints outside the map aren't common
                    int lowestDistance = int.MaxValue;
                    Point2D nearestCell = Point2D.NegativeOne;
                    map.DoForAllValidTiles(cell =>
                    {
                        int distance = cell.CoordsToPoint().DistanceTo(waypoint.Position);
                        if (distance < lowestDistance)
                        {
                            lowestDistance = distance;
                            nearestCell = cell.CoordsToPoint();
                        }
                    });

                    waypoint.Position = nearestCell;
                    tile = map.GetTile(waypoint.Position);

                    AddMapLoadError($"路点 {waypoint.Identifier} 在位置 {oldPosition} 不在有效地图区域内.它已被移动到 {waypoint.Position}.");
                }

                if (tile.Waypoints.Count > 0)
                {
                    Logger.Log($"注意: 路点 {waypoint.Identifier} 存在于位置 {waypoint.Position} 的单元格中,该单元格已包含其他路点: {string.Join(", ", tile.Waypoints.Select(s => s.Identifier))}");
                }

                waypoint.ParseEditorInfo(mapIni);

                map.AddWaypoint(waypoint);
            }

            Logger.Log("路径点读取成功.");
        }

        public static void ReadTaskForces(IMap map, IniFile mapIni)
        {
            Logger.Log("读取特遣部队.");

            map.TaskForces.ReadTaskForces(mapIni, map.Rules, AddMapLoadError);

            Logger.Log("特遣部队读取成功.");
        }

        public static void ReadTriggers(IMap map, IniFile mapIni)
        {
            Logger.Log("读取触发器 (Triggers, Events, Actions).");

            var section = mapIni.GetSection("Triggers");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                var trigger = Trigger.ParseTrigger(kvp.Key, kvp.Value);
                if (trigger != null)
                    map.AddTrigger(trigger);

                string actionData = mapIni.GetStringValue("Actions", trigger.ID, null);
                trigger.ParseActions(actionData);

                string conditionData = mapIni.GetStringValue("Events", trigger.ID, null);
                trigger.ParseConditions(conditionData, map.EditorConfig);

                trigger.ParseEditorInfo(mapIni);
            }

            // Parse and apply linked triggers
            foreach (var trigger in map.Triggers)
            {
                if (Helpers.IsStringNoneValue(trigger.LinkedTriggerId))
                    continue;

                trigger.LinkedTrigger = map.Triggers.Find(otherTrigger => otherTrigger.ID == trigger.LinkedTriggerId);
            }

            Logger.Log("触发器读取成功.");

            TriggerFix(map);
        }

        /// <summary>
        /// Checks for triggers having invalid values for uncustomizable parameters.
        /// Some earlier versions of WAE could set these parameters wrong, so we are
        /// cleaning up any potential mess we caused.
        /// </summary>
        private static void TriggerFix(IMap map)
        {
            Logger.Log("检查有错误的触发器.");

            // Check for mismatched uncustomizable trigger action parameters
            foreach (var trigger in map.Triggers)
            {
                foreach (var action in trigger.Actions)
                {
                    if (!map.EditorConfig.TriggerActionTypes.TryGetValue(action.ActionIndex, out var triggerActionType))
                        continue;

                    for (int i = 0; i < triggerActionType.Parameters.Length - 1; i++)
                    {
                        var paramType = triggerActionType.Parameters[i].TriggerParamType;

                        string valueToSet = null;

                        if ((int)paramType < 0 && Conversions.IntFromString(action.Parameters[i], 0) != Math.Abs((int)paramType))
                        {
                            valueToSet = Math.Abs((int)paramType).ToString(CultureInfo.InvariantCulture);
                        }

                        if (paramType == TriggerParamType.Unused && action.Parameters[i] != "0")
                        {
                            valueToSet = "0";
                        }

                        if (valueToSet != null)
                        {
                            AddMapLoadError($"触发器 \"{trigger.Name}\" 的动作 \"{triggerActionType.Name}\" 中不可自定义参数 #{i} 的值无效: \"{action.Parameters[i]}\".已自动更正为 \"{valueToSet}\".");
                            action.Parameters[i] = valueToSet;
                        }
                    }

                    const int lastParamIndex = TriggerActionType.MAX_PARAM_COUNT - 1;
                    // Handle P7 separately due to WaypointZZ hardcoding
                    if (triggerActionType.Parameters[lastParamIndex].TriggerParamType == TriggerParamType.Unused && action.Parameters[lastParamIndex] != "A")
                    {
                        string valueToSet = "A";
                        AddMapLoadError($"触发器 '{trigger.Name}' 的动作 \"{triggerActionType.Name}\" 中不可自定义参数 #{lastParamIndex} 的值无效: \"{action.Parameters[lastParamIndex]}\".已自动更正为 \"{valueToSet}\".");
                        action.Parameters[lastParamIndex] = valueToSet;
                    }
                }
            }

            Logger.Log("错误触发器的检查已完成.");
        }

        public static void ReadTags(IMap map, IniFile mapIni)
        {
            Logger.Log("读取标签.");

            var section = mapIni.GetSection("Tags");
            if (section == null)
                return;

            // [Tags]
            // ID=REPEATING,NAME,TRIGGER_ID

            foreach (var kvp in section.Keys)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                string[] parts = kvp.Value.Split(',');
                if (parts.Length != 3)
                    continue;

                int repeating = Conversions.IntFromString(parts[0], -1);
                if (repeating < 0 || repeating > Tag.REPEAT_TYPE_MAX)
                    continue;

                string triggerId = parts[2];
                Trigger trigger = map.Triggers.Find(t => t.ID == triggerId);
                if (trigger == null)
                {
                    AddMapLoadError("正在忽略标签 " + kvp.Key + ",因为其关联的触发器 " + triggerId + " 不存在！");
                    continue;
                }

                var tag = new Tag() { ID = kvp.Key, Repeating = repeating, Name = parts[1], Trigger = trigger };
                map.AddTag(tag);
            }

            Logger.Log("标签读取成功.");
        }

        public static void ReadScripts(IMap map, IniFile mapIni)
        {
            Logger.Log("读取脚本.");

            map.Scripts.ReadScripts(mapIni, AddMapLoadError);

            Logger.Log("脚本读取成功.");
        }

        public static void ReadTeamTypes(IMap map, IniFile mapIni, List<TeamTypeFlag> teamTypeFlags)
        {
            Logger.Log("读取作战小队.");

            map.TeamTypes.ReadTeamTypes(mapIni,
                name => map.FindHouseType(name),
                name => map.Scripts.Concat(map.Rules.Scripts).FirstOrDefault(s => s.ININame == name),
                name => map.TaskForces.Concat(map.Rules.TaskForces).FirstOrDefault(tf => tf.ININame == name),
                name => map.Tags.Find(t => t.ID == name),
                teamTypeFlags,
                AddMapLoadError,
                false);

            Logger.Log("作战小队读取成功.");
        }

        public static void ReadAITriggerTypes(IMap map, IniFile mapIni)
        {
            Logger.Log("读取AIAI触发器类型.");

            var section = mapIni.GetSection("AITriggerTypes");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                string[] parts = kvp.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != AI_TRIGGER_PROPERTY_FIELD_COUNT)
                {
                    AddMapLoadError($"AI触发器类型 {kvp.Key} 无效,正在跳过读取它.");
                    continue;
                }

                var aiTriggerType = new AITriggerType(kvp.Key);

                aiTriggerType.Name = parts[0];
                aiTriggerType.PrimaryTeam = map.TeamTypes.Concat(map.Rules.TeamTypes).FirstOrDefault(tt => tt.ININame == parts[1]);

                if (aiTriggerType.PrimaryTeam == null)
                {
                    AddMapLoadError($"AI触发器类型 \"{aiTriggerType.Name}\" ({kvp.Key}) 指定了一个不存在的队伍 ({parts[1]}) 作为其主要队伍！");
                }

                aiTriggerType.OwnerName = parts[2];

                if (!int.TryParse(parts[3], CultureInfo.InvariantCulture, out int techLevel))
                {
                    AddMapLoadError($"AI触发器类型 {kvp.Key} 的科技等级无效,正在跳过解析该AI触发器.");
                    continue;
                }
                aiTriggerType.TechLevel = techLevel;

                if (!int.TryParse(parts[4], CultureInfo.InvariantCulture, out int conditionType))
                {
                    AddMapLoadError($"AI触发器类型 {kvp.Key} 的条件类型无效,正在跳过解析该AI触发器.");
                    continue;
                }

                aiTriggerType.ConditionType = (AITriggerConditionType)conditionType;

                if (!Helpers.IsStringNoneValue(parts[5]))
                {
                    TechnoType conditionObject = map.Rules.FindTechnoType(parts[5]);

                    if (conditionObject == null)
                    {
                        AddMapLoadError($"AI触发器类型 {kvp.Key} 包含一个不存在的条件对象 \"{parts[5]}\"");
                    }

                    aiTriggerType.ConditionObject = conditionObject;
                }

                aiTriggerType.LoadedComparatorString = parts[6];
                AITriggerComparator? comparator = AITriggerComparator.Parse(aiTriggerType.LoadedComparatorString);
                if (comparator == null)
                {
                    AddMapLoadError($"无法解析AI触发器类型 {kvp.Key} ({aiTriggerType.Name}) 的比较器！正在跳过加载该AI触发器.");
                    continue;
                }
                aiTriggerType.Comparator = comparator.Value;

                aiTriggerType.InitialWeight = Conversions.DoubleFromString(parts[7], 0.0);
                aiTriggerType.MinimumWeight = Conversions.DoubleFromString(parts[8], 0.0);
                aiTriggerType.MaximumWeight = Conversions.DoubleFromString(parts[9], 0.0);
                aiTriggerType.EnabledInMultiplayer = parts[10] != "0";
                aiTriggerType.Unused = parts[11] != "0";
                aiTriggerType.Side = Conversions.IntFromString(parts[12], 0);
                aiTriggerType.IsBaseDefense = parts[13] != "0";

                if (!Helpers.IsStringNoneValue(parts[14]) )
                {
                    aiTriggerType.SecondaryTeam = map.TeamTypes.Concat(map.Rules.TeamTypes).FirstOrDefault(tt => tt.ININame == parts[14]);

                    if (aiTriggerType.SecondaryTeam == null)
                    {
                        AddMapLoadError($"AI触发器类型 {kvp.Key} 包含一个不存在的次要队伍类型 \"{parts[14]}\"");
                    }
                }

                aiTriggerType.Easy = parts[15] != "0";
                aiTriggerType.Medium = parts[16] != "0";
                aiTriggerType.Hard = parts[17] != "0";

                map.AITriggerTypes.Add(aiTriggerType);
            }

            Logger.Log("AI触发器类型读取成功.");
        }

        public static void ReadHouseTypes(IMap map, IniFile mapIni)
        {
            Logger.Log("读取所属方. 是否使用国家: " + Constants.IsRA2YR);

            var section = mapIni.GetSection(Constants.IsRA2YR ? "Countries" : "Houses");
            if (section == null)
                return;

            int id = 0;
            foreach (var kvp in section.Keys)
            {
                IniSection houseTypeSection = mapIni.GetSection(kvp.Value);

                // HouseTypes can't be redefined, so check if the HouseType already exists.
                // If it does, in Tiberian Sun we can skip it.
                // In RA2/YR we need to search for the HouseType from Rules as well as the map itself.
                // In case it exists in either, we still need to read the HouseType's properties,
                // but skip adding the HouseType to the list of map-specific HouseTypes.
                if (Constants.IsRA2YR)
                {
                    var existingHouseType = map.FindHouseType(kvp.Value);
                    if (existingHouseType != null)
                    {
                        if (houseTypeSection != null)
                        {
                            existingHouseType.ReadFromIniSection(houseTypeSection);
                            existingHouseType.ModifiedInMap = true;
                        }

                        continue;
                    }
                }
                else
                {
                    if (map.HouseTypes.Exists(ht => ht.ININame == kvp.Value))
                        continue;
                }

                var houseType = new HouseType(kvp.Value);
                houseType.Index = id + (Constants.IsRA2YR ? map.Rules.RulesHouseTypes.Count : 0);
                id++;

                if (houseTypeSection != null)
                    houseType.ReadFromIniSection(houseTypeSection);

                map.HouseTypes.Add(houseType);
            }

            // Assign colors
            map.GetHouseTypes().ForEach(houseType =>
            {
                var color = map.Rules.Colors.Find(c => c.Name == houseType.Color);
                if (color == null)
                {
                    houseType.XNAColor = Color.White;
                }
                else
                {
                    houseType.XNAColor = color.XNAColor;
                }
            });

            Logger.Log("所属方读取成功.");
        }

        public static void ReadHouses(IMap map, IniFile mapIni)
        {
            Logger.Log("读取House国家.");

            var section = mapIni.GetSection("Houses");
            if (section == null)
                return;

            int id = 0;
            foreach (var kvp in section.Keys)
            {
                string houseName = kvp.Value;
                HouseType houseType = null;

                var house = new House(houseName);
                house.ID = id;
                id++;

                map.Houses.Add(house);

                var houseSection = mapIni.GetSection(houseName);
                if (houseSection != null)
                {
                    house.ReadFromIniSection(houseSection);
                    var color = map.Rules.Colors.Find(c => c.Name == house.Color);
                    if (color == null)
                    {
                        house.XNAColor = Color.Black;
                    }
                    else
                    {
                        house.XNAColor = color.XNAColor;
                    }
                }

                var invalidBaseNodes = house.BaseNodes.FindAll(bn => !map.Rules.BuildingTypes.Exists(bt => bt.ININame == bn.StructureTypeName));
                invalidBaseNodes.ForEach(bn =>
                {
                    AddMapLoadError($"正在跳过加载国家 {houseName} 的无效基地节点,该节点对应建筑类型 \"{bn.StructureTypeName}\".该建筑类型在规则中不存在！");
                    house.BaseNodes.Remove(bn);
                });

                if (Constants.IsRA2YR)
                {
                    if (house.Country != null)
                        houseType = map.FindHouseType(house.Country);

                    if (houseType == null)
                    {
                        houseType = map.GetHouseTypes()[0];
                        AddMapLoadError($"House {houseName} 的 Country 不存在或未指定.将使用 ({houseType.ININame}).");
                    }
                }
                else
                {
                    houseType = map.HouseTypes[house.ID];
                }

                house.HouseType = houseType;
                if (houseType != null)
                {
                    houseType.Color = house.Color;
                    houseType.XNAColor = house.XNAColor;
                }
            }

            Logger.Log("读取House国家成功.");
        }

        public static void ReadCellTags(IMap map, IniFile mapIni)
        {
            Logger.Log("读取单元标记.");

            var section = mapIni.GetSection("CellTags");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                Point2D? coords = Helpers.CoordStringToPoint(kvp.Key);
                if (coords == null)
                    continue;

                var tile = map.GetTile(coords.Value.X, coords.Value.Y);
                if (tile == null)
                    continue;

                Tag tag = map.Tags.Find(t => t.ID == kvp.Value);
                if (tag == null)
                    continue;

                map.AddCellTag(new CellTag(coords.Value, tag));
            }

            Logger.Log("单元标记读取成功.");
        }

        public static void ReadLocalVariables(IMap map, IniFile mapIni)
        {
            Logger.Log("读取本地变量 (VariableNames).");

            var section = mapIni.GetSection("VariableNames");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                if (!int.TryParse(kvp.Key, out int variableIndex))
                {
                    AddMapLoadError($"条目 {kvp.Key}: {kvp.Value} 中的局部变量索引无效,正在跳过读取该局部变量.");
                    continue;
                }

                if (map.LocalVariables.Exists(c => c.Index == variableIndex))
                {
                    AddMapLoadError($"条目 {kvp.Key}: {kvp.Value} 中的局部变量索引重复,正在跳过读取该局部变量.");
                    continue;
                }

                string[] parts = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                {
                    AddMapLoadError($"条目 {kvp.Key}: {kvp.Value} 中的局部变量语法无效,正在跳过读取该局部变量.");
                    continue;
                }

                var localVariable = new LocalVariable(variableIndex);
                localVariable.Name = parts[0];
                localVariable.InitialState = int.Parse(parts[1], CultureInfo.InvariantCulture);

                map.LocalVariables.Add(localVariable);
            }

            Logger.Log("局部变量加载成功.");
        }

        public static void ReadTubes(IMap map, IniFile mapIni)
        {
            Logger.Log("读取隧道.");

            var section = mapIni.GetSection("Tubes");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                // Index=ENTER_X,ENTER_Y,FACING,EXIT_X,EXIT_Y,DIRECTIONS

                string[] parts = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 6)
                    return;

                int enterX = Conversions.IntFromString(parts[0], -1);
                int enterY = Conversions.IntFromString(parts[1], -1);
                TubeDirection initialFacing = (TubeDirection)Conversions.IntFromString(parts[2], -1);
                int exitX = Conversions.IntFromString(parts[3], -1);
                int exitY = Conversions.IntFromString(parts[4], -1);
                var directions = new List<TubeDirection>();
                for (int i = 5; i < parts.Length; i++)
                {
                    directions.Add((TubeDirection)Conversions.IntFromString(parts[i], -1));
                }

                if (enterX < 1 || enterY < 1 || exitX < 1 || exitY < 1 || (int)initialFacing < -1 || initialFacing > TubeDirection.Max)
                {
                    AddMapLoadError("无效的隧道条目: " + kvp.Value);
                    continue;
                }

                var tube = new Tube(new Point2D(enterX, enterY), new Point2D(exitX, exitY), initialFacing, directions);
                map.Tubes.Add(tube);
            }

            Logger.Log("隧道读取成功.");
        }
    }
}
