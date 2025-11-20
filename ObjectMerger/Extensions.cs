using War3Net.Build.Object;
using War3Net.Common.Extensions;

namespace ObjectMerger
{
    /// <summary>
    /// Extension methods for War3Net objects
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Convert rawcode string to int
        /// Example: "h001" -> int
        /// </summary>
        public static int FromRawcode(this string rawcode)
        {
            return War3Net.Common.Extensions.RawcodeExtensions.FromRawcode(rawcode);
        }

        /// <summary>
        /// Convert int to rawcode string
        /// Example: int -> "h001"
        /// </summary>
        public static string ToRawcode(this int value)
        {
            return War3Net.Common.Extensions.RawcodeExtensions.ToRawcode(value);
        }
    }
}
