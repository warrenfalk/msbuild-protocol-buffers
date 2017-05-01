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
            withOutput.SetMetadata("OutputSpec", Path.Combine(Path.GetDirectoryName(input.ItemSpec), Path.GetFileNameWithoutExtension(input.ItemSpec).SnakeToPascalCase() + Path.GetExtension(input.ItemSpec) + ".cs"));
            return withOutput;
        }
    }
}
