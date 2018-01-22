using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System;
using System.Collections;

namespace MsBuild.ProtocolBuffers
{
    public class CalcProtoOutputs : Task
    {
        [Required]
        public ITaskItem[] Inputs { get; set; }

        [Output]
        public ITaskItem[] InputsWithOutput { get; set; }

        public override bool Execute()
        {
            InputsWithOutput = Inputs.Select(inp => WithOutput(inp)).ToArray();
            return true;
        }

        public static ITaskItem WithOutput(ITaskItem input)
        {
            var withOutput = new TaskItem(input);
            var outDir = input.GetMetadata("RelativeDir");
            var inputBase = input.GetMetadata("InputBase");
            if (!string.IsNullOrEmpty(inputBase))
                outDir = Relative(outDir, inputBase);
            var outputBase = input.GetMetadata("OutputBase");
            if (outputBase != null)
                outDir = Path.Combine(outputBase, outDir);
            var outName = Path.GetFileNameWithoutExtension(input.ItemSpec).SnakeToPascalCase() + Path.GetExtension(input.ItemSpec) + ".cs";
            withOutput.SetMetadata("OutputSpec", Path.Combine(outDir, outName));
            return withOutput;
        }

        public static string Relative(string inputPath, string contextPath)
        {
            var input = Regex.Split(inputPath, @"[/\\]+").Where(s => s != "").ToArray();
            var context = Regex.Split(contextPath, @"[/\\]+").Where(s => s != "").ToArray();
            while (input.Length > 0 && context.Length > 0 && input[0] == context[0])
            {
                input = Shift(input);
                context = Shift(context);
            }
            while (context.Length > 0)
            {
                context = Shift(context);
                input = Unshift(input, "..");
            }
            return string.Join("/", input);
        }

        public static string[] Shift(string[] input)
        {
            var shifted = new string[input.Length - 1];
            Array.Copy(input, 1, shifted, 0, shifted.Length);
            return shifted;
        }
        public static string[] Unshift(string[] input, string item)
        {
            var unshifted = new string[input.Length + 1];
            unshifted[0] = item;
            Array.Copy(input, 0, unshifted, 1, input.Length);
            return unshifted;
        }
    }
}
