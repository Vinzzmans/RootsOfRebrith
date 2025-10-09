using System.Runtime.InteropServices;

namespace FarmingEngine
{
    public class WebGLTool
    {

        public static bool isMobile()
        {
            return UnityEngine.Device.Application.isMobilePlatform;
        }

    }

}