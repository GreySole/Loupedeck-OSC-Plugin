namespace Loupedeck.OSCPlugin.Actions
{
    using System;
    using System.Collections.Generic;
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
                new ActionEditorTextbox(name: "address", labelText: "Address")
                .SetPlaceholder("/test/subtest")
                .SetRequired()
             );
            this.ActionEditor.AddControlEx(new ActionEditorTextbox("max", "Max").SetPlaceholder("1.0").SetRequired());
            this.ActionEditor.AddControlEx(new ActionEditorTextbox("min", "Min").SetPlaceholder("0.0").SetRequired());
            this.ActionEditor.AddControlEx(new ActionEditorTextbox("step", "Step").SetPlaceholder("0.1").SetRequired());
            this.ActionEditor.AddControlEx(new ActionEditorTextbox("reset", "Reset").SetPlaceholder("0.5").SetRequired());
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
            var address = actionParameter.GetString("address");
            var max = Convert.ToDecimal(actionParameter.GetString("max"));
            var min = Convert.ToDecimal(actionParameter.GetString("min"));
            var step = Convert.ToDecimal(actionParameter.GetString("step"));

            var name = actionParameter.GetString("label");
            
            var nameGet = actionParameter.TryGetString("actionControlName", out name);

            var totalRange = max - min;

            if (!_values.ContainsKey(address))
            {
                _values[address] = 0;
            }

            var percentage = ((_values[address] - min) / totalRange) * 100;
            return (this.Plugin as OSCPlugin).BuildImage(PluginImageSize.Width90, "Loupedeck.OSCPlugin.Sprites.NormalKnobOSC.Knob" + percentage.ToString("000") + ".png", name, false).ToImage();
        }
    }
}
