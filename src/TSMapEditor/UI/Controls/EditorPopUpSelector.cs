using Microsoft.Xna.Framework;
using Rampastring.XNAUI.C;

namespace TSMapEditor.UI.Controls
{
    class EditorPopUpSelector : EditorPanel
    {
        private const int TEXT_HORIZONTAL_MARGIN = 3;
        private const int TEXT_VERTICAL_MARGIN = 2;

        public EditorPopUpSelector(WindowManager windowManager) : base(windowManager)
        {
            Height = Constants.UITextBoxHeight;
            TextIdleColor = UISettings.ActiveSettings.TextColor;
            TextHoverColor = UISettings.ActiveSettings.AltColor;
            textColor = TextIdleColor;
        }

        public float FontSize { get; set; } = 16.0f;

        public Color TextIdleColor { get; set; }
        public Color TextHoverColor { get; set; }

        private string oldText;
        private string cachedText;

        private Color textColor;

        public override void OnLeftClick(InputEventArgs inputEventArgs)
        {
            inputEventArgs.Handled = true;
            base.OnLeftClick(inputEventArgs);
        }

        public override void OnMouseLeftDown(InputEventArgs e)
        {
            e.Handled = true;
            base.OnMouseLeftDown(e);
        }

        public override void OnMouseEnter()
        {
            textColor = TextHoverColor;
            base.OnMouseEnter();
        }

        public override void OnMouseLeave()
        {
            textColor = TextIdleColor;
            base.OnMouseLeave();
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            if (!string.IsNullOrWhiteSpace(Text))
            {
                if (!ReferenceEquals(oldText, Text))
                {
                    oldText = Text;
                    cachedText = Text;
                }

                DrawStringWithShadow(cachedText,
                    new Vector2(TEXT_HORIZONTAL_MARGIN, TEXT_VERTICAL_MARGIN),
                    textColor,FontSize);
            }

            base.Draw(gameTime);
        }
    }
}
