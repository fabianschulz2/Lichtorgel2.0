using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Devices.Gpio;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Media.Audio;
using Windows.Media.Capture;


namespace Lichtorgel2._0
{
    class GPIOControll
    {
        public void Init()
        {
            var controller = GpioController.GetDefault();
            if (controller != null)
            {
                Console.WriteLine(controller.PinCount);
                //  textBox.Text = controller.PinCount.ToString();
            }

            //GpioPin an die richtigen Pins Koppeln
            GpioPin green = controller.OpenPin(17);
            GpioPin yellow = controller.OpenPin(27);
            GpioPin red = controller.OpenPin(22);

            //GpioPin Funktion festlegen
            green.SetDriveMode(GpioPinDriveMode.Output);
            yellow.SetDriveMode(GpioPinDriveMode.Output);
            red.SetDriveMode(GpioPinDriveMode.Output);

            // alle an
            green.Write(GpioPinValue.High);
            yellow.Write(GpioPinValue.High);
            red.Write(GpioPinValue.High);

            //   textBox.Text = green.Read().ToString() + " " + yellow.Read().ToString() + " " + red.Read().ToString();
        }

        public void SetAudio(AudioGraph _graph)
        {
          //  EqualizerEffectDefinition eed = new EqualizerEffectDefinition(_graph);
         //   IReadOnlyList<EqualizerBand> list = eed.Bands;
          //  Console.WriteLine(list.Count);
        }
    }
}
