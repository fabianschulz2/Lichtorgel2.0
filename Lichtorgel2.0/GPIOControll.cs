﻿using System;
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
        GpioPin green;
        GpioPin yellow;
        GpioPin red;
        public void Init()
        {
            var controller = GpioController.GetDefault();
            if (controller != null)
            {
                Console.WriteLine(controller.PinCount);
            }

            //GpioPin an die richtigen Pins Koppeln
            green = controller.OpenPin(17);
            yellow = controller.OpenPin(27);
            red = controller.OpenPin(22);

            //GpioPin Funktion festlegen
            green.SetDriveMode(GpioPinDriveMode.Output);
            yellow.SetDriveMode(GpioPinDriveMode.Output);
            red.SetDriveMode(GpioPinDriveMode.Output);

            // alle an
            //green.Write(GpioPinValue.High);
            //yellow.Write(GpioPinValue.High);
            // red.Write(GpioPinValue.High);

        }

        public void SetGreen(Boolean on)
        {
            if (on)
            {
                green.Write(GpioPinValue.High);
            }
            else
            {
                green.Write(GpioPinValue.Low);
            }
        }
        public void SetYellow(Boolean on)
        {
            if (on)
            {
                yellow.Write(GpioPinValue.High);
            }
            else
            {
                yellow.Write(GpioPinValue.Low);
            }
        }
        public void SetRed(Boolean on)
        {
            if (on)
            {
                red.Write(GpioPinValue.High);
            }
            else
            {
                red.Write(GpioPinValue.Low);
            }
        }
    }
}
