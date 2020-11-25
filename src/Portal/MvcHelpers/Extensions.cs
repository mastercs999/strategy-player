using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Portal.MvcHelpers
{
    public static class Extensions
    {
        public static string NumberHighlightClass(this decimal value)
        {
            return value >= 0 ? "number-positive" : "number-negative";
        }
    }
}