namespace Loupedeck.OSCPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Threading;

    // This class implements an example command that counts button presses.

    public class ButtonOSC : ActionEditorCommand
    {
        private Dictionary<string, decimal> _values = new Dictionary<string, decimal>();

        // Initializes the command class.
        public ButtonOSC()
        {
            this.Name = "ButtonOSC";
            this.DisplayName = "OSC Button";
            this.GroupName = "Button";
            this.ActionEditor.AddControlEx(
                    new ActionEditorTextbox("label", "Button Label")
                    .SetPlaceholder("Label on icon image")
            );
            this.ActionEditor.AddControlEx(
                    new ActionEditorTextbox("address", "Address").SetRequired()
                    .SetPlaceholder("OSC Address")
            );
            this.ActionEditor.AddControlEx(
                    new ActionEditorListbox("type", "Button Type").SetRequired()
            );
            this.ActionEditor.ListboxItemsRequested += OnActionEditorListboxItemsRequested;
            this.ActionEditor.ControlValueChanged += OnActionEditorControlValueChanged;
        }

        private void OnActionEditorControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e)
        {
            if (e.ControlName.EqualsNoCase("type"))
            {
                this.ActionEditor.ListboxItemsChanged("type");
            }
            
        }

        private void OnActionEditorListboxItemsRequested(Object sender, ActionEditorListboxItemsRequestedEventArgs e)
        {
            if (e.ControlName.EqualsNoCase("type"))
            {
                e.AddItem("basic", "Basic", "Sends 1 to address");
                e.AddItem("toggle", "Toggle", "Alternates 1 and 0 to address");
                e.AddItem("momentary", "Momentary", "Sends 1 and then 0 to address");
            }
            else
            {
                this.Plugin.Log.Error($"Unexpected control name '{e.ControlName}'");
            }
        }

        // This method is called when the user executes the command.
        protected override Boolean RunCommand(ActionEditorActionParameters actionParameter)
        {
            var type = actionParameter.GetString("type");
            var address = actionParameter.GetString("address");
            if (!_values.ContainsKey(address))
            {
                _values[address] = 0;
            }
            switch (type)
            {
                case "basic":
                    OSCPlugin.sendOSC(address, 1);
                    this.ActionImageChanged();
                    break;
                case "toggle":
                    _values[address] = _values[address] == 1 ? 0 : 1;
                    OSCPlugin.sendOSC(address, _values[address]);
                    this.ActionImageChanged();
                    break;
                case "momentary":
                    OSCPlugin.sendOSC(address, 1);
                    this.ActionImageChanged();
                    Thread.Sleep(250);
                    OSCPlugin.sendOSC(address, 0);
                    this.ActionImageChanged();
                    break;
            }
            
            return true;
        }

        // This method is called when Loupedeck needs to show the command on the console or the UI.
        //protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
        //    $"Press Counter{Environment.NewLine}{this._value}";
        

        protected override BitmapImage GetCommandImage(ActionEditorActionParameters actionParameter, int width, int height)
        {
            var pName = actionParameter.GetString("label");
            var address = actionParameter.GetString("address");
            if (!_values.ContainsKey(address))
            {
                _values[address] = 0;
            }
            return (this.Plugin as OSCPlugin).BuildImage(PluginImageSize.Width90, "Loupedeck.OSCPlugin.Sprites.Button.Button" + (_values[address] == 1 ? "On" : "Off") + ".png", pName, false).ToImage();
        }
    }
}
