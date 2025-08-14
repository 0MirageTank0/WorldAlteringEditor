using Rampastring.XNAUI.C;
using Rampastring.XNAUI.C.XNAControls;

namespace TSMapEditor.UI.Controls
{
    public class EditorSuggestionTextBox : XNASuggestionTextBox
    {
        public EditorSuggestionTextBox(WindowManager windowManager) : base(windowManager)
        {
            Height = Constants.UITextBoxHeight;
        }
    }
}
