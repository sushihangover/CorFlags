using System;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace CorFlags
{

	enum ExitCodes : int
	{
		Success = 0,
		Error = 1
	}

	public class CorFlagsInformation {
		public string assemblyVersion;
		public string version;
		public TargetRuntime clr_header;
		public string pe;
		public int corflags;
		public bool ilonly;
		public bool x32bitreq;
		public bool x32bitpref;
		public bool signed;
	}

	public class AssemblyInfo 
	{
		readonly TextWriter output;

		public AssemblyInfo (TextWriter errorOutput)
			: this (errorOutput, Console.Out)
		{
		}

		public AssemblyInfo (TextWriter errorOutput, TextWriter messagesOutput)
		{
			this.output = messagesOutput;
		}

		bool IsMarked(ModuleDefinition targetModule)
		{
			return targetModule.Types.Any(t => t.Name == "IMarked");
		}

		public void AssemblyInfoOutput (CorFlagsInformation info) 
		{
			output.WriteLine ("Version   : {0}", info.version);
			output.WriteLine ("CLR Header: {0}", (info.clr_header <= TargetRuntime.Net_1_1) ? "2.0" : "2.5");
			output.WriteLine ("PE        : {0}", info.pe);
			output.WriteLine ("CorFlags  : 0x{0:X}", info.corflags);
			output.WriteLine ("ILONLY    : {0}", info.ilonly ? 1 : 0);
			output.WriteLine ("32BITREQ  : {0}", info.x32bitreq ? 1 : 0);
			output.WriteLine ("32BITPREF : {0}", info.x32bitpref ? 1 : 0);
			output.WriteLine ("Signed    : {0}", info.signed ? 1 : 0);
			//	anycpu: PE = PE32  and  32BIT = 0
			//	   x86: PE = PE32  and  32BIT = 1
			//	64-bit: PE = PE32+ and  32BIT = 0
		}

		public bool ChangeInfo (ModuleDefinition assembly, CorFlagsSettings args)
		{
			var flags = assembly.Attributes;
			#if DEBUG
			Console.WriteLine (assembly.Attributes);
			#endif
			if (args.ILONLY) 
				assembly.Attributes |= ModuleAttributes.ILOnly;
			else if (args.NOT_ILONLY)
				assembly.Attributes &= ~ModuleAttributes.ILOnly;

			if (args.THIRTYTWO_BITPREFERRED)
				assembly.Attributes |= ModuleAttributes.Preferred32Bit;
			else if (args.THIRTYTWO_NOT_BITPREFERRED)
				assembly.Attributes &= ~ModuleAttributes.Preferred32Bit;

			if (args.THIRTYTWO_BITREQUIRED)
				assembly.Attributes |= ModuleAttributes.Required32Bit;
			else if (args.THIRTYTWO_NOT_BITREQUIRED)
				assembly.Attributes &= ~ModuleAttributes.Required32Bit;
			// CLR Header: 2.0 indicates a .Net 1.0 or .Net 1.1 (Everett) image 
			//		 while 2.5 indicates a .Net 2.0 (Whidbey) image.
			// corflags : error CF014 : It is invalid to revert the CLR header on an image with a version of v4.0.30319
			bool runtimechanged = false;
			if (args.UpgradeCLRHeader) {
				assembly.RuntimeVersion = "4.0";
				assembly.Runtime = TargetRuntime.Net_4_0;
				runtimechanged = true;
			} else if (args.RevertCLRHeader) {
				assembly.RuntimeVersion = "1.1";
				assembly.Runtime = TargetRuntime.Net_1_1;
				runtimechanged = true;
			}

			#if DEBUG
			Console.WriteLine (assembly.Attributes);
			#endif

			return (flags != assembly.Attributes || runtimechanged);
		}

		public CorFlagsInformation ExtractInfo (ModuleDefinition assembly)
		{
			var info = new CorFlagsInformation ();

			// The user defined version of the assembly
			info.assemblyVersion = assembly.Assembly.Name.Version.ToString ();

			//Version of the mscorlib.dll that was assembly was compiled with and now should run against
			info.version = assembly.RuntimeVersion;

			info.clr_header = assembly.Runtime;

			info.pe = (assembly.Architecture == TargetArchitecture.AMD64 || assembly.Architecture == TargetArchitecture.IA64) ? "PE32+" : "PE32";

			info.corflags = (int)assembly.Attributes;

			info.ilonly = (assembly.Attributes & ModuleAttributes.ILOnly) == ModuleAttributes.ILOnly;

			info.x32bitreq = (assembly.Attributes & ModuleAttributes.Required32Bit) == ModuleAttributes.Required32Bit;

			info.x32bitpref = (assembly.Attributes & ModuleAttributes.Preferred32Bit) == ModuleAttributes.Preferred32Bit;

			info.signed = (assembly.Attributes & ModuleAttributes.StrongNameSigned) == ModuleAttributes.StrongNameSigned;

			return info;
		}

		public ModuleDefinition OpenAssembly (CorFlagsSettings setting,  string fullPath, Report report) {
			if (!File.Exists (fullPath)) {
				#if DEBUG
				output.WriteLine (fullPath);
				#endif
				throw new FileNotFoundException();
			}

			var targetModule = ModuleDefinition.ReadModule (fullPath);
			if (!IsMarked (targetModule)) {
				// Console.WriteLine("isMarked?");
			}
			return targetModule;
		}
	}

	class MainClass
	{
		public static void Main (string[] args)
		{
			var output = Console.Out;

			var report = new Report (output);
			var cmd = new CommandLineParser (output);
			var cmdArguments = cmd.ParseArguments (args);

			if (cmdArguments.ArgList.Count == 0 && cmdArguments.SourceFiles.Count == 0) {
				// MS's CorFlags displays help whe no args are supplied and exits success
				cmd.Header ();
				cmd.Usage ();
				Environment.Exit ((int)ExitCodes.Success);
			} else if (cmdArguments.SourceFiles.Count > 0) {
				var assemblyInfo = new AssemblyInfo (output);
				foreach (var assemblyFileName in cmdArguments.SourceFiles) {
					if (!cmdArguments.NoLogo)
						cmd.Header ();
				    var backupFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(assemblyFileName));
				    try {
					    //var dataPath = Path.GetDirectoryName(Environment.GetEnvironmentVariable("PWD")) ?? Directory.GetCurrentDirectory();
					    var assemblyFile = Path.GetFullPath(assemblyFileName);
					    var pwd = Environment.GetEnvironmentVariable("PWD");
                        if (!File.Exists(assemblyFile) && null != pwd) {
					        assemblyFile = Path.Combine(pwd, assemblyFileName);
					    }

                        File.Copy(assemblyFile, backupFile, true);
					    using (var modDef = assemblyInfo.OpenAssembly(cmdArguments, backupFile, report)) {

                            if (modDef == null) {
                                report.Error(998, "Unknown error with no exception opening: {0}", assemblyFileName);
                                Environment.Exit((int)ExitCodes.Error);
                            } else {
                                var corFlags = assemblyInfo.ExtractInfo(modDef);
                                if (cmdArguments.InfoOnly) {
                                    assemblyInfo.AssemblyInfoOutput(corFlags);
                                } else {
                                    var changed = assemblyInfo.ChangeInfo(modDef, cmdArguments);
                                    if (changed && ((corFlags.signed && cmdArguments.Force) || !corFlags.signed)) {
                                        try {
                                            // corflags : warning CF011 : The specified file is strong name signed.  Using /Force will invalidate the signature of this
                                            // image and will require the assembly to be resigned.
                                            modDef.Write(assemblyFile);
                                        } catch (Exception) {
                                            // If exception on Cecil writing, 'restore' backup
                                            File.Copy(backupFile, assemblyFile, true);
                                            throw;
                                        }
                                    } else if (changed && corFlags.signed && !cmdArguments.Force) {
                                        // Strong name signed, but no Force argument passed
                                        // corflags : error CF012 : The specified file is strong name signed.  Use /Force to force the update.
                                        throw new Exception("The specified file is strong name signed.  Use /Force to force the update.");
                                    }
                                }
                                output.WriteLine();
                            }
					    }
                    } catch (FileNotFoundException) {
						// corflags : error CF002 : Could not open file for reading
						report.Error (2, "{0}", "Could not open file for reading");
						Environment.Exit ((int)ExitCodes.Error);
					} catch (BadImageFormatException) {
						// System.BadImageFormatException: Format of the executable (.exe) or library (.dll) is invalid.
						// The specified file does not have a valid managed header
						report.Error (8, "{0}", "The specified file does not have a valid managed header");
						Environment.Exit ((int)ExitCodes.Error);
					} catch (Exception e) {
						// i.e. /ILONLY- Cecil; Writing mixed-mode assemblies is not supported
						report.Error (999, "Unknown exception: {0}", e.Message);
						#if DEBUG
						output.WriteLine (e);
						#endif 
						Environment.Exit ((int)ExitCodes.Error);
					} finally {
						// Delete backup
						if (File.Exists (backupFile))
							File.Delete (backupFile);
					}
				}
				Environment.Exit ((int)ExitCodes.Success);
			}
			Environment.Exit ((int)ExitCodes.Error);
		}
	}
}
