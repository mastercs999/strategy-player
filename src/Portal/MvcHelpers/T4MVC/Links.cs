using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Links
{
    public static partial class Bundles
    {
        public static partial class Scripts
        {
            public static readonly string preinit = "~/Scripts/Preinit";
            public static readonly string scripts = "~/Scripts/Scripts";
        }

        public static partial class Styles
        {
            public static readonly string styles = "~/Styles/Styles";
        }
    }
}