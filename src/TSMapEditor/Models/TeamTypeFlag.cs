namespace TSMapEditor.Models
{
    public class TeamTypeFlag
    {
        public TeamTypeFlag(string uiName,string name, bool defaultValue)
        {
            UIName = uiName;
            Name = name;
            DefaultValue = defaultValue;
        }
        public string UIName { get; }
        public string Name { get; }
        public bool DefaultValue { get; }
    }
}
