using Rampastring.Tools;
using System.Collections.Generic;
using TSMapEditor.Models;

namespace TSMapEditor.UI
{
    /// <summary>
    /// Combines many terrain objects into a single entry.
    /// </summary>
    public class TerrainObjectCollection : ObjectTypeCollection
    {
        public struct TerrainObjectCollectionEntry
        {
            public TerrainType TerrainType;

            public TerrainObjectCollectionEntry(TerrainType terrainType)
            {
                TerrainType = terrainType;
            }
        }

        public TerrainObjectCollectionEntry[] Entries;

        public static TerrainObjectCollection InitFromIniSection(IniSection iniSection, List<TerrainType> terrainTypes)
        {
            var terrainObjectCollection = new TerrainObjectCollection();
            terrainObjectCollection.Name = iniSection.GetStringValue("Name", "未命名集合");
            terrainObjectCollection.AllowedTheaters = iniSection.GetListValue("AllowedTheaters", ',', s => s);

            var entryList = new List<TerrainObjectCollectionEntry>();

            int i = 0;
            while (true)
            {
                string terrainTypeName = iniSection.GetStringValue("TerrainObjectType" + i, null);
                if (string.IsNullOrWhiteSpace(terrainTypeName))
                    break;

                var terrainType = terrainTypes.Find(o => o.ININame == terrainTypeName);
                if (terrainType == null)
                {
                    throw new INIConfigException($"初始化地形对象集合 “{terrainObjectCollection.Name}” 时找不到地形对象类型 “{terrainTypeName}”!");
                }

                entryList.Add(new TerrainObjectCollectionEntry(terrainType));

                i++;
            }

            terrainObjectCollection.Entries = entryList.ToArray();
            return terrainObjectCollection;
        }
    }
}
