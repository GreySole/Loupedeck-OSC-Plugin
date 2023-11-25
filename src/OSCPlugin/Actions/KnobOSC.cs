namespace Loupedeck.OSCPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Windows.Markup;

    // This class implements an example adjustment that counts the rotation ticks of a dial.

    public class KnobOSC : ActionEditorAdjustment
    {
        // This variable holds the current value of the counter.
        private Dictionary<string, decimal> _values = new Dictionary<string, decimal>();

        // Initializes the adjustment class.
        // When `hasReset` is set to true, a reset command is automatically created for this adjustment.
        public KnobOSC() : base(hasReset:true, supportedDevices:DeviceType.All)
        {
            this.Name = "oscknob";
            this.DisplayName = "OSC Knob";
            this.GroupName = "Knob";

            this.ActionEditor.AddControlEx(
                    new ActionEditorTextbox("label", "Button Label")
                    .SetPlaceholder("Label on icon image")
            );
            this.ActionEditor.AddControlEx(
                    new ActionEditorTextbox("address", "Address").SetRequired()
                    .SetPlaceholder("OSC Address")
            );
            this.ActionEditor.AddControlEx(
                    new ActionEditorListbox("type", "Knob Type").SetRequired()
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
                e.AddItem("raw", "Raw", "Raw value, no constraints to address");
                e.AddItem("normal", "Normal", "Simple 0-100 value to address");
            }
            else
            {
                this.Plugin.Log.Error($"Unexpected control name '{e.ControlName}'");
            }
        }

        // This method is called when the adjustment is executed.
        protected override Boolean ApplyAdjustment(ActionEditorActionParameters actionParameter, Int32 diff)
        {
            var type = actionParameter.GetString("type");
            var address = actionParameter.GetString("address");
            if (!_values.ContainsKey(address))
            {
                _values[address] = 0;
            }
            //PluginLog.Info($"Value:{_values[address]}");
            switch (type)
            {
                case "raw":
                    _values[address] += diff;
                    OSCPlugin.sendOSC(address, _values[address]);
                    break;
                case "normal":
                    _values[address] += diff;
                    if (_values[address] <= 0)
                    {
                        _values[address] = 0;
                    }
                    if (_values[address] >= 100)
                    {
                        _values[address] = 100;
                    }
                    OSCPlugin.sendOSC(address, _values[address]);
                    break;
            }

            this.AdjustmentValueChanged(); // Notify the Loupedeck service that the adjustment value has changed.
            return true;
        }

        // This method is called when the reset command related to the adjustment is executed.
        protected override Boolean RunCommand(ActionEditorActionParameters actionParameter)
        {
            var address = actionParameter.GetString("address");
            _values[address] = 0;
            OSCPlugin.sendOSC(address, _values[address]);
            
            this.AdjustmentValueChanged(); // Notify the Loupedeck service that the adjustment value has changed.
            return true;
        }

        protected override BitmapImage GetCommandImage(ActionEditorActionParameters actionParameter, int width, int height)
        {
            var address = actionParameter.GetString("address");
            var pName = actionParameter.GetString("label");
            var type = actionParameter.GetString("type");

            if (!_values.ContainsKey(address))
            {
                _values[address] = 0;
            }

            if (type == "normal")
            {
                return (this.Plugin as OSCPlugin).BuildImage(PluginImageSize.Width90, "Loupedeck.OSCPlugin.Sprites.NormalKnobOSC.Knob" + _values[address].ToString("000") + ".png", pName, false).ToImage();
            }
            else
            {
                return (this.Plugin as OSCPlugin).BuildImage(PluginImageSize.Width90, null, pName, false).ToImage();
            }
        }

        // Returns the adjustment value that is shown next to the dial.
        protected override string GetAdjustmentDisplayName(ActionEditorActionParameters actionParameter)
        {
            return actionParameter.GetString("label");
        }
    }
}
