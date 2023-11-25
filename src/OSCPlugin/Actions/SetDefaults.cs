namespace Loupedeck.OSCPlugin.Actions
{
    using System;
    using System.IO;

    internal class SetDefaults : ActionEditorCommand
    {
        public SetDefaults()
        {
            this.Name = "SetDefaults";
            this.DisplayName = "Set Defaults";
            this.Description = "Set default IP and port for OSC. DO THIS FIRST BEFORE SETTING ANYTHING!";
            this.GroupName = "Initialization";

            this.ActionEditor.AddControlEx(
                new ActionEditorTextbox(name: "sender_ip", labelText: "Sender IP")
                .SetPlaceholder("127.0.0.1")
                .SetRequired()
             );

            this.ActionEditor.AddControlEx(
                new ActionEditorTextbox(name: "sender_port", labelText: "Sender Port")
                .SetPlaceholder("9000")
                .SetRequired()
             );
        }

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            var sender_ip = "127.0.0.1";
            actionParameters.TryGetString("sender_ip", out sender_ip);
            var sender_port = "9000";
            actionParameters.TryGetString("sender_port", out sender_port);

            var pluginDataDirectory = this.Plugin.GetPluginDataDirectory();
            if(IoHelpers.EnsureDirectoryExists(pluginDataDirectory))
            {
                var ipPath = Path.Combine(pluginDataDirectory, "sender_ip");
                using(var streamWriter = new StreamWriter(ipPath))
                {
                    streamWriter.WriteLine(sender_ip);
                }

                var portPath = Path.Combine(pluginDataDirectory, "sender_port");
                using (var streamWriter = new StreamWriter(portPath))
                {
                    streamWriter.WriteLine(sender_port);
                }
            }
            ((OSCPlugin)this.Plugin).restartOSC();
            return true;
        }
    }
}
