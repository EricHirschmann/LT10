
namespace Evel.share {
    public class Performancer {

        [System.Runtime.InteropServices.DllImport("KERNEL32")]
        public static extern bool QueryPerformanceCounter(
            ref long lpPerformanceCount);

        [System.Runtime.InteropServices.DllImport("KERNEL32")]
        public static extern bool QueryPerformanceFrequency(
            ref long lpFrequency);

    }
}
