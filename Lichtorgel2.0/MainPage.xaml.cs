using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using NAudio;
using Unosquare.RaspberryIO;
using Unosquare.WiringPi;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace Lichtorgel2._0
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // NAudio.Wave.WaveFileReader wave = new NAudio.Wave.WaveFileReader("test.wav");
            Pi.Init<BootstrapWiringPi>();    //doku : https://unosquare.github.io/raspberryio/
            var blinkingPin = Pi.Gpio[0];
            blinkingPin = Pi.Gpio[bcmPin:0];

            blinkingPin.PinMode = Unosquare.RaspberryIO.Abstractions.GpioPinDriveMode.Output;
            bool ison = false;
            for(int i = 0; i < 20; i++)
            {
                ison = !ison;
                blinkingPin.Write(ison);
                System.Threading.Thread.Sleep(500);
            }
            


            
        }
    }
}
