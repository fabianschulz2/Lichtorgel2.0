using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lichtorgel2._0
{
    class GPIOControlVirtual : GPIOControl
    {
        public override void Init()
        {
            Debug.WriteLine("GPIOControlVirtual initialisiert");
        }
        public override void SetRed(Boolean on)
        {
            Debug.WriteLine("Red: " + on);
        }
        public override void SetYellow(Boolean on)
        {
            Debug.WriteLine("Yellow: " + on);
        }
        public override void SetGreen(Boolean on)
        {
            Debug.WriteLine("Green: " + on);
        }

    }
}
