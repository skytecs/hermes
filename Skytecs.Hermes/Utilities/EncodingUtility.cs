using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Utilities
{
    public static class EncodingUtility
    {
        public static string EncodeForPrinter(string description)
        {
            var bytes = Encoding.GetEncoding(866).GetBytes(description);
            return Encoding.Default.GetString(bytes);
        }
    }
}
