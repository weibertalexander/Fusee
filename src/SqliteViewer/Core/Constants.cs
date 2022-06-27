using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusee.Examples.SQLiteViewer.Core
{
    public static class Constants
    {
        private static int _footpulseAmount = 10;  // How many footpulses are in one file
        public static int FootpulseAmount
        {
            get
            {
                return _footpulseAmount;
            }
        }

    }
}
