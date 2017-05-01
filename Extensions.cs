using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MsBuild.ProtocolBuffers
{
    public static class Extensions
    {
        public static string SnakeToCamelCase(this string snakeCase)
            => Regex.Replace(snakeCase, "_([A-Za-z0-9][^_]*)", m => m.Groups[1].Value.ModFirst(x => x.ToUpper())).ModFirst(x => x.ToLower());

        public static string SnakeToPascalCase(this string snakeCase)
            => Regex.Replace(snakeCase, "_([A-Za-z0-9][^_]*)", m => m.Groups[1].Value.ModFirst(x => x.ToUpper())).ModFirst(x => x.ToUpper());

        public static string ModFirst(this string input, Func<string, string> modify)
            => input.Length == 0 ? input
            : input.Length == 1 ? modify(input)
            : modify(input.Substring(0, 1)) + input.Substring(1);
    }
}
