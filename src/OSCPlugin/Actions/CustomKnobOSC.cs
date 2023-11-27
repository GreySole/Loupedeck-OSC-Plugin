namespace Loupedeck.OSCPlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Net;

    internal class CustomKnobOSC : ActionEditorAdjustment
    {
        private Dictionary<string, decimal> _values = new Dictionary<string, decimal>();

        public CustomKnobOSC() : base(hasReset:true, supportedDevices:DeviceType.All)
        {
            this.Name = "CustomKnobOSC";
            this.DisplayName = "Custom Knob";
            this.Description = "Set your own min, max, step, and reset for a knob";
            this.GroupName = "Knob";

            this.ActionEditor.AddControlEx(
                    new ActionEditorTextbox("label", "Button Label")
                    .SetPlaceholder("Label on icon image")
            );

            this.ActionEditor.AddControlEx(
                new ActionEditorTextbox(name: "address", labelText: "Address")
                .SetPlaceholder("/test/subtest")
                .SetRequired()
             );
            this.ActionEditor.AddControlEx(new ActionEditorTextbox("max", "Max").SetPlaceholder("1.0").SetRequired());
            this.ActionEditor.AddControlEx(new ActionEditorTextbox("min", "Min").SetPlaceholder("0.0").SetRequired());
            this.ActionEditor.AddControlEx(new ActionEditorTextbox("step", "Step").SetPlaceholder("0.1").SetRequired());
            this.ActionEditor.AddControlEx(new ActionEditorTextbox("reset", "Reset").SetPlaceholder("0.5").SetRequired());
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
                e.AddItem("raw", "Raw", "Raw value, no constraints to address");
                e.AddItem("normal", "Normal", "Simple 0-100 value to address");
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

        protected override Boolean ApplyAdjustment(ActionEditorActionParameters actionParameter, Int32 diff)
        {
            var address = actionParameter.GetString("address");
            var max = Convert.ToDecimal(actionParameter.GetString("max"));
            var min = Convert.ToDecimal(actionParameter.GetString("min"));
            var step = Convert.ToDecimal(actionParameter.GetString("step"));

            if (!_values.ContainsKey(address))
            {
                _values[address] = 0;
            }

            _values[address] += Math.Round((diff > 1 ? step : -step), 3);
            
            if (_values[address] <= min)
            {
                _values[address] = min;
            }
            if (_values[address] >= max)
            {
                _values[address] = max;
            }

            OSCPlugin.sendOSC(address, _values[address]);
            this.AdjustmentValueChanged(); // Notify the Loupedeck service that the adjustment value has changed.
            return true;
        }

        // This method is called when the reset command related to the adjustment is executed.
        protected override Boolean RunCommand(ActionEditorActionParameters actionParameter)
        {
            var address = actionParameter.GetString("address");
            _values[address] = Convert.ToDecimal(actionParameter.GetString("reset"));

            OSCPlugin.sendOSC(address, _values[address]);

            this.AdjustmentValueChanged(); // Notify the Loupedeck service that the adjustment value has changed.
            return true;
        }

        protected override BitmapImage GetCommandImage(ActionEditorActionParameters actionParameter, int width, int height)
        {
            var pName = actionParameter.GetString("label");
            var address = actionParameter.GetString("address");
            var max = Convert.ToDecimal(actionParameter.GetString("max"));
            var min = Convert.ToDecimal(actionParameter.GetString("min"));
            var step = Convert.ToDecimal(actionParameter.GetString("step"));
            var color = actionParameter.GetString("color", "white");

            var name = actionParameter.GetString("label");
            
            var nameGet = actionParameter.TryGetString("actionControlName", out name);

            var totalRange = max - min;

            if (!_values.ContainsKey(address))
            {
                _values[address] = 0;
            }

            return (this.Plugin as OSCPlugin).DrawProgressArc(PluginImageSize.Width60, OSCPlugin.ControlColors[$"{color}-dark"], OSCPlugin.ControlColors[color], _values[address], min, max, name: pName, isButton: false);
        }
    }
}
