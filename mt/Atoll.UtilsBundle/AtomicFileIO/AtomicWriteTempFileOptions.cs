#if NETSTANDARD2_0
#endif

namespace Coral.Atoll.Utils
{
    public class AtomicWriteTempFileOptions
    {
        public string TempFileSuffix { get; set; }
        public TempFileDestination Destination { get; set; }

        public AtomicWriteTempFileOptions()
        {
            this.Destination = TempFileDestination.NextToMainFile;
            this.TempFileSuffix = ".tmp";
        }
    }
}
