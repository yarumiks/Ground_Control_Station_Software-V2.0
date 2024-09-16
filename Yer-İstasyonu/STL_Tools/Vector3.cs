using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yer_İstasyonu.STL_Tools
{
    public class Vector3
    {
        public float x;
        public float y;
        public float z;

        /**
        * @brief  Class instance constructor
        * @param  none
        * @retval none
        */
        public Vector3(float xVal = 0, float yVal = 0, float zVal = 0)
        {
            x = xVal;
            y = yVal;
            z = zVal;
        }
    }
}
