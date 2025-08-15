using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI.C;
using Rampastring.XNAUI.C.XNAControls;
using System;
namespace TSMapEditor.UI.Controls
{
    /// <summary>
    /// 富文本工具提示类，继承自ToolTip。
    /// </summary>
    public class RichToolTip : ToolTip
    {
        private RichTextLayout layout;
        /// <summary>
        /// 创建一个新的富文本工具提示并将其附加到指定的控件上。
        /// </summary>
        /// <param name="windowManager">The window manager.</param>
        /// <param name="masterControl">The control to attach the tool tip to.</param>
        public RichToolTip(WindowManager windowManager, XNAControl masterControl) : base(windowManager, masterControl)
        {
            // Additional initialization for RichToolTip can be added here
            layout = new RichTextLayout
            {
                Font = Renderer.GetFont(),
                Text = ""
            };
        }
        public override string Text
        {
            get => base.Text;
            set
            {
                layout.Text = value;
                // 尽管宽度和高度会被重写，但我们仍然需要设置Text
                base.Text = value;
                Point textSize = layout.Size;
                Width = textSize.X + ToolTipMargin * 2;
                Height = textSize.Y + ToolTipMargin * 2;
                Console.WriteLine($"RichToolTip Text set: {value}, Size: {textSize.X}, {textSize.Y}");
            }
        }
        /// <summary>
        /// 在指定位置显示工具提示。
        /// 与Tooltip不同的是，这里优先将左上角放置到鼠标下方。
        /// </summary>
        /// <param name="location"></param>
        public override void DisplayAtLocation(Point location)
        {
            X = location.X + Width > WindowManager.RenderResolutionX ?
                WindowManager.RenderResolutionX - Width : location.X;
            Y = location.Y + Height > WindowManager.RenderResolutionY ?
                WindowManager.RenderResolutionY - Height : location.Y;
        }
        public override void Draw(GameTime gameTime)
        {
            Renderer.FillRectangle(ClientRectangle,
                UISettings.ActiveSettings.BackgroundColor * Alpha);
            Renderer.DrawRectangle(ClientRectangle,
                UISettings.ActiveSettings.AltColor * Alpha);
            Renderer.DrawRichText(layout, new Vector2(X + ToolTipMargin, Y + ToolTipMargin), Color.White);
        }

    }
}