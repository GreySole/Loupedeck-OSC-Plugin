namespace Loupedeck.OSCPlugin
{
    using System;
    using System.Collections.Generic;
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
                    new ActionEditorTextbox("buttonvalue", "Value")
                    .SetPlaceholder("Value to send when button pressed (1 if not set)")
            );
            this.ActionEditor.AddControlEx(
                    new ActionEditorListbox("type", "Button Type").SetRequired()
            );
            this.ActionEditor.AddControlEx(
                    new ActionEditorListbox("color", "Color")
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
            else if (e.ControlName.EqualsNoCase("color"))
            {
                this.ActionEditor.ListboxItemsChanged("color");
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
            else if (e.ControlName.EqualsNoCase("color"))
            {
                e.AddItem("white", "White", "");
                e.AddItem("red", "Red", "");
                e.AddItem("green", "Green", "");
                e.AddItem("blue", "Blue", "");
                e.AddItem("cyan", "Cyan", "");
                e.AddItem("yellow", "Yellow", "");
                e.AddItem("magenta", "Magenta", "");
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
            var value = actionParameter.GetInt32("buttonvalue");
            if(value == 0)
            {
                value = 1;
            }
            if (!_values.ContainsKey(address))
            {
                _values[address] = 0;
            }
            switch (type)
            {
                case "basic":
                    OSCPlugin.sendOSC(address, value);
                    this.ActionImageChanged();
                    break;
                case "toggle":
                    _values[address] = _values[address] == 0 ? value : 0;
                    OSCPlugin.sendOSC(address, _values[address]);
                    this.ActionImageChanged();
                    break;
                case "momentary":
                    _values[address] = value;
                    OSCPlugin.sendOSC(address, _values[address]);
                    this.ActionImageChanged();
                    Thread.Sleep(250);
                    _values[address] = 0;
                    OSCPlugin.sendOSC(address, 0);
                    this.ActionImageChanged();
                    break;
            }
            
            return true;
        }
        

        protected override BitmapImage GetCommandImage(ActionEditorActionParameters actionParameter, int width, int height)
        {
            var pName = actionParameter.GetString("label");
            var address = actionParameter.GetString("address");
            var color = actionParameter.GetString("color", "white");
            if (!_values.ContainsKey(address))
            {
                _values[address] = 0;
            }
            return (this.Plugin as OSCPlugin).DrawProgressArc(PluginImageSize.Width90, OSCPlugin.ControlColors[$"{color}-dark"], OSCPlugin.ControlColors[color], _values[address], 0, 1, name:pName, isButton:true);
        }
    }
}
