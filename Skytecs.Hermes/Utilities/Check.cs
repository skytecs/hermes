using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Utilities
{
    public static class Check
    {
        public static void NotNull(object argument, string name)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        public static void NotEmpty(string argument, string name)
        {
            if (String.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentNullException(name);
            }
        }

    }
}
