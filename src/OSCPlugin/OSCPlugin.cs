namespace Loupedeck.OSCPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using SharpOSC;
    using SkiaSharp;

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
            var listenerPortPath = Path.Combine(pluginDataDirectory, "listener_port");

            PluginLog.Info("CHECKING DIRS");
            var ipSet = IoHelpers.FileExists(ipPath);
            var portSet = IoHelpers.FileExists(portPath);
            PluginLog.Info($"ipSet: {ipSet} : portSet: {portSet}");

            if (ipSet == false || portSet == false)
            {
                this._senderIP = "";
                this._senderPort = 0;
                //this._listenerPort = 0;
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

                /*using (var streamReader = new StreamReader(listenerPortPath))
                {
                    var rawListenerPort = streamReader.ReadLine();
                    this._listenerPort = Int32.Parse(rawListenerPort);
                }*/
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

            /*try
            {
                if (_listenerPort != 0)
                {
                    _udpListener = new UDPListener(_listenerPort);
                }
            }
            catch
            {
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Error, $"OSC Listener failed to start! IP:{_senderIP} PORT:{_senderPort} LISTENER PORT: {_listenerPort}");
            }*/

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

        public static Dictionary<string, BitmapColor> ControlColors = new Dictionary<string, BitmapColor>
        {
            {"white",new BitmapColor(255, 255, 255) },
            {"red",new BitmapColor(255, 0, 0) },
            {"green",new BitmapColor(0, 255, 0) },
            {"blue",new BitmapColor(0, 0, 255) },
            {"yellow",new BitmapColor(255, 255, 0) },
            {"cyan",new BitmapColor(0, 255, 255) },
            {"magenta",new BitmapColor(255, 0, 255) },

            {"white-dark",new BitmapColor(140, 140, 140) },
            {"red-dark",new BitmapColor(140, 0, 0) },
            {"green-dark",new BitmapColor(0, 140, 0) },
            {"blue-dark",new BitmapColor(0, 0, 140) },
            {"yellow-dark",new BitmapColor(140, 140, 0) },
            {"cyan-dark",new BitmapColor(0, 140, 140) },
            {"magenta-dark",new BitmapColor(140, 0, 140) }
        };

        public BitmapImage DrawProgressArc(PluginImageSize imageSize, BitmapColor backgroundColor, BitmapColor foregroundColor, Decimal currentValue, Decimal minValue, Decimal maxValue,
            String name = "", Boolean isButton=true)
        {
            // Prepare variables
            var dim = imageSize == PluginImageSize.Width60 ? 50 : 80;
            var percentage = (currentValue - minValue) / (maxValue - minValue) * 100;
            var height = (Int32)(dim * 0.6);
            var width = (Int32)(dim * 0.6);
            var calculatedHeight = (Int32)(360 * percentage / 100);
            var xCenter = dim / 2;
            var yCenter = (Int32)(dim * 0.375);
            var builder = new BitmapBuilder(dim, dim);

            // Reset to black
            builder.Clear(BitmapColor.Black);

            // Draw volume bar and border
            
            const Int32 fontSize = 16;

            var cmdSize = GetFontSize(fontSize, name, dim);

            if (isButton == true)
            {
                if (currentValue > 0)
                {
                    builder.FillCircle(xCenter, yCenter, width / 2, foregroundColor);
                }
                builder.DrawArc(xCenter, yCenter, width / 2, 0.0f, 360, backgroundColor, 5.0f);
                builder.DrawText(name, 0, dim / 2 - Convert.ToInt32(cmdSize * 0.6), dim, dim, foregroundColor, cmdSize);
            }
            else
            {
                calculatedHeight = (Int32)(height * percentage / 100);
                
                builder.FillRectangle(xCenter - (width / 2), yCenter + (height / 2), width, -calculatedHeight, backgroundColor);

                var strokeSize = 3;
                DrawRectangleOutline(builder, foregroundColor, xCenter, yCenter, width, -height, strokeSize);
                
                builder.DrawText((currentValue).ToString(CultureInfo.CurrentCulture), foregroundColor, cmdSize);
                builder.DrawText(name, 0, dim / 2 - cmdSize/2, dim, dim, foregroundColor, cmdSize);
            }

            return builder.ToImage();
        }

        public void DrawRectangleOutline(BitmapBuilder builder, BitmapColor color, float xCenter, float yCenter, float width, float height, float strokeSize)
        {
            // Calculate half width and half height
            float halfWidth = width / 2;
            float halfHeight = height / 2;

            // Calculate the coordinates of the rectangle corners
            float left = xCenter - halfWidth;
            float right = xCenter + halfWidth;
            float top = yCenter - halfHeight;
            float bottom = yCenter + halfHeight;

            // Draw the four sides of the rectangle using DrawLine
            builder.DrawLine(left, top, right, top, color, strokeSize);      // Top side
            builder.DrawLine(right, top, right, bottom, color, strokeSize);  // Right side
            builder.DrawLine(left, bottom, right, bottom, color, strokeSize);// Bottom side
            builder.DrawLine(left, top, left, bottom, color, strokeSize);    // Left side
        }

        private static Int32 GetFontSize(Int32 fontSize, String text, Int32 dim)
        {
            // create a SKPaint object for measuring the text
            var paint = new SKPaint
            {
                TextSize = fontSize,
                IsAntialias = true
            };

            // measure the size of the text
            var textBounds = new SKRect();
            paint.MeasureText(text, ref textBounds);

            // adjust the font size until the text fits within the bounds of the image
            while (textBounds.Width > dim || textBounds.Height > dim)
            {
                fontSize -= 1;
                paint.TextSize = fontSize;
                paint.MeasureText(text, ref textBounds);
            }

            return fontSize;
        }
    }
}
