using Microsoft.Xna.Framework;

namespace TSMapEditor.Misc
{
    public static class NamedColors
    {        
        public static NamedColor[] GenericSupportedNamedColors = new NamedColor[]
        {
            new NamedColor("蓝绿色", new Color(0, 196, 196)),
            new NamedColor("绿色", new Color(0, 255, 0)),
            new NamedColor("深绿色", Color.Green),
            new NamedColor("柠檬绿", Color.LimeGreen),
            new NamedColor("黄色", Color.Yellow),
            new NamedColor("橙色", Color.Orange),
            new NamedColor("红色", Color.Red),
            new NamedColor("血红色", Color.DarkRed),
            new NamedColor("粉色", Color.HotPink),
            new NamedColor("樱桃红", Color.Pink),
            new NamedColor("紫色", Color.MediumPurple),
            new NamedColor("天蓝色", Color.SkyBlue),
            new NamedColor("蓝色", new Color(40, 40, 255)),
            new NamedColor("棕色", Color.Brown),
            new NamedColor("金属色", new Color(160, 160, 200)),

        };
    }

    public struct NamedColor
    {
        public string Name;
        public Color Value;

        public NamedColor(string name, Color value)
        {
            Name = name;
            Value = value;
        }
    }
}
