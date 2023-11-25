namespace Loupedeck.OSCPlugin
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;

    using Loupedeck.OSCPlugin.Actions;

    using SharpOSC;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class OSCPlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

        private static UDPSender _udpSender;
        private static UDPListener _udpListener;
        public String _senderIP;
        public Int32 _senderPort;
        public Int32 _listenerPort;

        // Initializes a new instance of the plugin class.
        public OSCPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            setDefaults();

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);
        }

        public void setDefaults()
        {
            var pluginDataDirectory = this.GetPluginDataDirectory();
            var ipPath = Path.Combine(pluginDataDirectory, "sender_ip");
            var portPath = Path.Combine(pluginDataDirectory, "sender_port");

            PluginLog.Info("CHECKING DIRS");
            var ipSet = IoHelpers.FileExists(ipPath);
            var portSet = IoHelpers.FileExists(portPath);
            PluginLog.Info($"ipSet: {ipSet} : portSet: {portSet}");

            if (ipSet == false || portSet == false)
            {
                this._senderIP = "";
                this._senderPort = 0;
                this._listenerPort = 0;
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning, "Defaults not set! Configure a SetDefaults button to set OSC ip and port. You only need to do this once.");
            }
            else
            {
                using (var streamReader = new StreamReader(ipPath))
                {
                    var rawIP = streamReader.ReadLine();
                    this._senderIP = rawIP;
                }

                using (var streamReader = new StreamReader(portPath))
                {
                    var rawPort = streamReader.ReadLine();
                    this._senderPort = Int32.Parse(rawPort);
                }
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Normal, $"Default set to {this._senderIP}:{this._senderPort}");
            }
        }

        public static void sendOSC(string address, int value)
        {
            if(_udpSender != null)
            {
                _udpSender.Send(new OscMessage(address, value));
            }
        }

        public static void sendOSC(string address, Decimal value)
        {
            if (_udpSender != null)
            {
                _udpSender.Send(new OscMessage(address, (float)value));
            }
        }

        public void restartOSC()
        {
            PluginLog.Info("RESTARTING OSC");
            this.setDefaults();
            
            if (_udpSender != null)
            {
                _udpSender.Close();
            }
            try
            {
                if (_senderIP != "" && _senderPort != 0)
                {
                    _udpSender = new UDPSender(_senderIP, _senderPort);
                }
            }
            catch
            {
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Error, $"OSC Sender failed to start! IP:{_senderIP} PORT:{_senderPort}");
            }
            
            PluginLog.Info($"Default set to {this._senderIP}:{this._senderPort}");
        }

        // This method is called when the plugin is loaded during the Loupedeck service start-up.
        public override void Load()
        {
            this.Info.Icon16x16 = EmbeddedResources.ReadImage("Loupedeck.OSCPlugin.metadata.Icon16x16.png");
            this.Info.Icon32x32 = EmbeddedResources.ReadImage("Loupedeck.OSCPlugin.metadata.Icon32x32.png");
            this.Info.Icon48x48 = EmbeddedResources.ReadImage("Loupedeck.OSCPlugin.metadata.Icon48x48.png");
            this.Info.Icon256x256 = EmbeddedResources.ReadImage("Loupedeck.OSCPlugin.metadata.Icon256x256.png");

            try
            {
                if (_senderIP != "" && _senderPort != 0)
                {
                    _udpSender = new UDPSender(_senderIP, _senderPort);
                }
            }
            catch
            {
                _udpSender = null;
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Error, $"OSC Sender failed to start! IP:{_senderIP} PORT:{_senderPort}");
            }

            //_udpListener = new UDPListener(_listenerPort);
        }

        // This method is called when the plugin is unloaded during the Loupedeck service shutdown.
        public override void Unload()
        {
            if (_udpSender != null)
            {
                _udpSender.Close();
            }
            
            //_udpListener.Close();
        }

        private static readonly BitmapColor BitmapColorPink = new BitmapColor(255, 192, 203);

        public BitmapBuilder BuildImage(PluginImageSize imageSize, String imageName, String text, Boolean selected)
        {
            var bitmapBuilder = new BitmapBuilder(imageSize);
            if(imageName != null)
            {
                try
                {
                    var image = EmbeddedResources.ReadImage(imageName);
                    bitmapBuilder.DrawImage(image);
                }
                catch (Exception ex)
                {
                    this.Log.Error($"Cannot load image {imageName}, exception {ex}");
                }
            }

            if (!String.IsNullOrEmpty(text))
            {
                var x1 = bitmapBuilder.Width * 0.1;
                var w = bitmapBuilder.Width * 0.8;
                var y1 = bitmapBuilder.Height * 0.60;
                var h = bitmapBuilder.Height * 0.3;

                bitmapBuilder.DrawText(text, (Int32)x1, (Int32)y1, (Int32)w, (Int32)h,
                                            BitmapColorPink,
                                            imageSize == PluginImageSize.Width90 ? 18 : 18,
                                            imageSize == PluginImageSize.Width90 ? 12 : 8, 1);
            }

            return bitmapBuilder;
        }
    }
}
