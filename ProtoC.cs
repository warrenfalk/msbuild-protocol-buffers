using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System;

namespace MsBuild.ProtocolBuffers
{
    public class ProtoC : Task
    {
        [Required]
        public ITaskItem[] Inputs { get; set; }

        [Output]
        public ITaskItem[] OutputFiles { get; set; }

        public string ProtoToolsPath { get; set; }

        public string OutputFolder { get; set; } = ".";

        public string Includes { get; set; } = ".";

        /* Some noteworthy observations:
         * 
         * Output Directory structure:
         * protoc seems designed so that you invoke it once with a list of all .proto source files, but it provides
         * no mechanisms for configuring the structure of the output directory.  The only option you have is to
         * specify base_namespace, in which case the output directory structure is based on the "package" line
         * within the .proto files, or to not specify base_namespace in which case the output directory is flat with
         * no structure at all.  Neither of these options lends itself to allowing protoc to be used in a make-like
         * system which expects to know where to find inputs and outputs so that it can tell when they are out of
         * date.  In order to have msbuild be able to determine the output, given the input, we'd either need to
         * create a task to parse the file (thus eliminating much of the benefit of change-detection in the build
         * system) or have it dump everything in the same directory.  Both options prevent using "DependentUpon"
         * in visual studio which requires that source and target be in the same directory.  In order to mirror
         * the directory structure of the source in the output, we will need to invoke protoc at least once for
         * every unique source path.  Furthermore, even if we used the whole output folder as the output, this
         * will cause problems when .proto files are deleted but their previously-generated .cs files are still
         * around.  Normally we'd instruct msbuild not to compile only the .cs files associated with an extant .proto
         * file but we must know how to map the one to the other in order to do this.
         */
        public override bool Execute()
        {
            OutputFiles = Inputs.Select(inc => new TaskItem
            {
                ItemSpec = inc.GetMetadata("OutputSpec") ?? Path.Combine(Path.GetDirectoryName(inc.ItemSpec), Path.GetFileNameWithoutExtension(inc.ItemSpec).SnakeToPascalCase() + Path.GetExtension(inc.ItemSpec) + ".cs"),
            })
            .ToArray();

            var arch = (RuntimeInformation.ProcessArchitecture.HasFlag(Architecture.X64) ? "x64" : "x86");
            string environment = $"windows_{arch}";
            string executable = "protoc.exe";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                environment = $"macosx_{arch}";
                executable = "protoc";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                environment = $"linux_{arch}";
                executable = "protoc";
            }

            var protocPath = Path.Combine(ProtoToolsPath, "tools", environment, executable);
            var protocInclude = Path.Combine(ProtoToolsPath, "tools");
            Log.LogMessage("ProtoToolsPath: {0}", protocPath);

            // We want to mirror the input directory structure to the output directory structure
            // see note above about why we need this and how protoc doesn't support it
            // to do this we must find the distinct list of directories
            var directories = new Directories();
            foreach (var item in Inputs)
                directories.Add(item);

            foreach (var entry in directories)
            {
                var dirPair = entry.Key;
                var files = entry.Value;
                var inputs = string.Join(" ", files.Select(file => $"\"{Path.Combine(dirPair.InputDir, file)}\""));
                var outputDir = Path.Combine(OutputFolder, dirPair.OutputDir);
                var arguments = $" --error_format=msvs -I\"{protocInclude}\" {string.Join(" ", Includes.Split(';').Select(path => $"-I\"{path}\""))} --csharp_out={outputDir} --csharp_opt=file_extension=.proto.cs {inputs}";
                var cmdLine = $"\"{protocPath}\" {arguments}";
                Log.LogCommandLine(cmdLine);

                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                var psi = new ProcessStartInfo
                {
                    FileName = protocPath,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };
                var proc = Process.Start(psi);
                //gcc error format
                var errorPattern = new Regex("^(?<file>.*)\\((?<line>[0-9]+)\\) : error in column=(?<column>[0-9]+): (?<message>.*)$|^(?<file>.*):(?<line>[0-9]+):(?<column>[0-9]+): (?<message>.*)$", RegexOptions.Compiled);
                var noLinePattern = new Regex("^(?<file>[^:]+): (?<message>.*)$", RegexOptions.Compiled);
                var warnPattern = new Regex("^\\[(?<sourcemodule>.*) (?<level>.*) (?<sourcefile>.*):(?<sourceline>[0-9]+)\\] (?<message>.*)", RegexOptions.Compiled);
                var protoFilePattern = new Regex("proto file: (?<filename>.*\\.proto)", RegexOptions.Compiled);
                var fallbackErrorPattern = new Regex("^(?<option>.*): (?<file>.*): (?<message>.*)$", RegexOptions.Compiled);
                var warningPrefixPattern = new Regex("^warning:\\s?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var errors = 0;
                var stdErrTask = System.Threading.Tasks.Task.Run(() =>
                {
                    string line;
                    while (null != (line = proc.StandardError.ReadLine()))
                    {
                        var match = errorPattern.Match(line);
                        if (match.Success)
                        {
                            var filename = match.Groups["file"].Value;
                            var lineNum = ParseInt(match.Groups["line"].Value, 0);
                            var columnNum = ParseInt(match.Groups["column"].Value, 0);
                            var message = match.Groups["message"].Value;
                            errors++;
                            Log.LogError("protobuf", null, null, filename, lineNum, columnNum, lineNum, columnNum, message, messageArgs: new string[0]);
                            continue;
                        }
                        match = warnPattern.Match(line);
                        if (match.Success)
                        {
                            var message = match.Groups["message"].Value;
                            var filename = protoFilePattern.Match(message).Groups["filename"].Value;
                            if (filename != null)
                                Log.LogWarning("protobuf", null, null, filename, 0, 0, 0, 0, "{0}", message);
                            else
                                Log.LogWarning("{0}", message);
                            continue;
                        }
                        match = noLinePattern.Match(line);
                        if (match.Success)
                        {
                            var filename = match.Groups["file"].Value;
                            var message = match.Groups["message"].Value;
                            var warnPrefixMatch = warningPrefixPattern.Match(message);
                            if (warnPrefixMatch.Success)
                            {
                                message = warningPrefixPattern.Replace(message, "");
                                Log.LogWarning("protobuf", null, null, filename, 0, 0, 0, 0, message, messageArgs: new string[0]);
                            }
                            else
                            {
                                errors++;
                                Log.LogError("protobuf", null, null, filename, 0, 0, 0, 0, message, messageArgs: new string[0]);
                            }
                            continue;
                        }
                        match = fallbackErrorPattern.Match(line);
                        if (match.Success)
                        {
                            var filename = match.Groups["file"].Value;
                            var lineNum = 0;
                            var columnNum = 0;
                            var message = match.Groups["message"].Value;
                            errors++;
                            Log.LogError("protobuf", null, null, filename, lineNum, columnNum, lineNum, columnNum, message, messageArgs: new string[0]);
                            continue;
                        }
                        Log.LogMessageFromText(line, MessageImportance.High);
                    }
                });
                var stdInTask = System.Threading.Tasks.Task.Run(() =>
                {
                    Log.LogMessagesFromStream(proc.StandardOutput, MessageImportance.High);
                });
                proc.WaitForExit();
                System.Threading.Tasks.Task.WaitAll(stdErrTask, stdInTask);
                var exitCode = proc.ExitCode;
                if (exitCode != 0)
                {
                    // if we didn't catch any errors being logged from the output
                    // then just explain that protoc returned a non-zero exit code without telling us anything
                    if (errors == 0)
                        Log.LogError("protoc returned {0}", exitCode);
                    return false;
                }
            }
            return true;
        }

        private int ParseInt(string str, int defaultTo)
        {
            if (String.IsNullOrEmpty(str))
                return defaultTo;
            if (!int.TryParse(str, out int result))
                return defaultTo;
            return result;
        }
    }
}
