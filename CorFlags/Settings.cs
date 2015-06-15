//
// Settings.cs: All CorFlag settings
//
// Author: RobertN (sushihangover@outlook.com)
//
// Copyright 2015 RobertN (https://github.com/sushihangover)
//

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using System;

namespace CorFlags {

	public class Message
	{
		public Message(string MessageType, int Code, string Text, bool IsWarning = false) {
			this.MessageType = MessageType;
			this.Code = Code;
			this.Text = Text;
			this.IsWarning = IsWarning;
		}

		readonly public string MessageType;
		readonly public int Code;
		readonly public string Text;
		readonly public bool IsWarning;
	}

	public class Report 
	{
		readonly TextWriter output;

		public Report (TextWriter output) {
			this.output = output;
		}

		public void Warning (int code, string warning)
		{
			Print (new Message("corflags : Warning", code, warning, true));
		}

		public void Warning (int code, string format, params string[] args)
		{
			Warning (code, String.Format (format, args));
		}

		public void Error (int code, string error)
		{
			Print (new Message("corflags : Error", code, error, false));
		}

		public void Error (int code, string format, string arg)
		{
			Error (code, String.Format (format, arg));
		}

		protected void Print (Message msg)
		{
			StringBuilder txt = new StringBuilder ();
			txt.AppendFormat ("{0} CF{1:000}: {2}", msg.MessageType, msg.Code, msg.Text);

//			if (!msg.IsWarning)
//				output.WriteLine (FormatText (txt.ToString ()));
//			else
//				output.WriteLine (txt.ToString ());
			output.WriteLine (txt.ToString ());
		}
	}

	public class CorFlagsSettings
	{
		public bool InfoOnly = true;

		public bool Force;

		public bool THIRTYTWO_BITREQUIRED;
		public bool THIRTYTWO_NOT_BITREQUIRED;

		public bool THIRTYTWO_BITPREFERRED;
		public bool THIRTYTWO_NOT_BITPREFERRED;

		public bool ILONLY;
		public bool NOT_ILONLY;
	
		public bool RevertCLRHeader;
		public bool UpgradeCLRHeader;

		public bool NoLogo;
		public bool Help;

		readonly List<string> source_files;
		readonly List<string> arg_list;


		public CorFlagsSettings ()
		{
			source_files = new List<string> ();
			arg_list = new List<string> ();
		}
			
		public string FirstSourceFile {
			get {
				return source_files.Count > 0 ? source_files [0] : null;
			}
		}

		public List<string> SourceFiles {
			get {
				return source_files;
			}
		}

		public List<string> ArgList {
			get {
				return arg_list;
			}
		}
	}

	public class CommandLineParser
	{
		enum ParseResult
		{
			Success,
			Error,
			Stop,
			UnknownOption
		}

		#pragma warning disable 0414
		static readonly char[] argument_value_separator = new char[] { ';', ',' };
		static readonly char[] numeric_value_separator = new char[] { ';', ',', ' ' };
		#pragma warning restore 0414

		readonly TextWriter output;
		readonly Report report;
		bool stop_argument;

		Dictionary<string, int> source_file_index;

		public event Func<string[], int, int> UnknownOptionHandler;

		public CommandLineParser (TextWriter errorOutput)
			: this (errorOutput, Console.Out)
		{
		}

		public CommandLineParser (TextWriter errorOutput, TextWriter messagesOutput)
		{
			//var rp = new StreamReportPrinter (errorOutput);

			this.output = messagesOutput;
			this.report = new Report (messagesOutput);
		}

		public bool HasBeenStopped {
			get {
				return stop_argument;
			}
		}

		public CorFlagsSettings ParseArguments (string[] args)
		{
			var settings = new CorFlagsSettings ();
			if (!ParseArguments (settings, args))
				return null;

			return settings;
		}

		public bool ParseArguments (CorFlagsSettings settings, string[] args)
		{
			if (settings == null)
				throw new ArgumentNullException ("settings");

			bool parsing_options = true;
			stop_argument = false;
			source_file_index = new Dictionary<string, int> ();

			for (int i = 0; i < args.Length; i++) {
				string arg = args[i];
				if (arg.Length == 0)
					continue;

				if (arg[0] == '@') {
					string[] extra_args;
					string response_file = arg.Substring (1);

					extra_args = LoadArgs (response_file);
					if (extra_args == null) {
//						report.Error (2011, "Unable to open response file: " + response_file);
						return false;
					}

					args = AddArgs (args, extra_args);
					continue;
				}

				if (parsing_options) {
					if (arg == "--") {
						parsing_options = false;
						continue;
					}

					bool dash_opt = arg[0] == '-';
					bool slash_opt = arg[0] == '/';
					if (dash_opt || slash_opt) {
						string csc_opt = dash_opt ? "/" + arg.Substring (1) : arg;
						settings.ArgList.Add (arg);
						switch (ParseOption (csc_opt, ref args, settings)) {
						case ParseResult.Error:
						case ParseResult.Success:
							continue;
						case ParseResult.UnknownOption:
							if ((slash_opt && arg.Length > 3 && arg.IndexOf ('/', 2) > 0))
								break;

							if (UnknownOptionHandler != null) {
								var ret = UnknownOptionHandler (args, i);
								if (ret != -1) {
									i = ret;
									continue;
								}
							}

							Error_WrongOption (arg);
							return false;

						case ParseResult.Stop:
							stop_argument = true;
							return true;
						}
					}
				}

				ProcessSourceFiles (arg, false, settings.SourceFiles);
			}

			return true;
		}

		void ProcessSourceFiles (string spec, bool recurse, List<string> sourceFiles)
		{
			string path, pattern;

			SplitPathAndPattern (spec, out path, out pattern);
			if (pattern.IndexOf ('*') == -1) {
				AddSourceFile (spec, sourceFiles);
				return;
			}

			string[] files = null;
			try {
				files = Directory.GetFiles (path, pattern);
			} catch (System.IO.DirectoryNotFoundException) {
//				report.Error (2001, "Source file `" + spec + "' could not be found");
				return;
			} catch (System.IO.IOException) {
//				report.Error (2001, "Source file `" + spec + "' could not be found");
				return;
			}
			foreach (string f in files) {
				AddSourceFile (f, sourceFiles);
			}

			if (!recurse)
				return;

			string[] dirs = null;

			try {
				dirs = Directory.GetDirectories (path);
			} catch {
			}

			foreach (string d in dirs) {

				// Don't include path in this string, as each
				// directory entry already does
				ProcessSourceFiles (d + "/" + pattern, true, sourceFiles);
			}
		}

		static string[] AddArgs (string[] args, string[] extra_args)
		{
			string[] new_args;
			new_args = new string[extra_args.Length + args.Length];

			// if args contains '--' we have to take that into account
			// split args into first half and second half based on '--'
			// and add the extra_args before --
			int split_position = Array.IndexOf (args, "--");
			if (split_position != -1) {
				Array.Copy (args, new_args, split_position);
				extra_args.CopyTo (new_args, split_position);
				Array.Copy (args, split_position, new_args, split_position + extra_args.Length, args.Length - split_position);
			} else {
				args.CopyTo (new_args, 0);
				extra_args.CopyTo (new_args, args.Length);
			}

			return new_args;
		}

		void AddSourceFile (string fileName, List<string> sourceFiles)
		{
			string path = Path.GetFullPath (fileName);

			int index;
			if (source_file_index.TryGetValue (path, out index)) {
				string other_name = sourceFiles[index - 1];
				if (fileName.Equals (other_name))
					report.Warning (1004, "Source file `{0}' specified multiple times", other_name);
				else
					report.Warning (1004, "Source filenames `{0}' and `{1}' both refer to the same file: {2}", fileName, other_name, path);

				return;
			}

			sourceFiles.Add (fileName);
		}

		void Error_WrongOption (string option)
		{
			report.Error (1003, "Unrecognized command-line option: `{0}'", option);
			Environment.Exit ((int)ExitCodes.Error);
		}

		static string[] LoadArgs (string file)
		{
			StreamReader f;
			var args = new List<string> ();
			string line;
			try {
				f = new StreamReader (file);
			} catch {
				return null;
			}

			StringBuilder sb = new StringBuilder ();

			while ((line = f.ReadLine ()) != null) {
				int t = line.Length;

				for (int i = 0; i < t; i++) {
					char c = line[i];

					if (c == '"' || c == '\'') {
						char end = c;

						for (i++; i < t; i++) {
							c = line[i];

							if (c == end)
								break;
							sb.Append (c);
						}
					} else if (c == ' ') {
						if (sb.Length > 0) {
							args.Add (sb.ToString ());
							sb.Length = 0;
						}
					} else
						sb.Append (c);
				}
				if (sb.Length > 0) {
					args.Add (sb.ToString ());
					sb.Length = 0;
				}
			}

			return args.ToArray ();
		}

		//
		// This parses the -arg and /arg options
		//
		ParseResult ParseOption (string option, ref string[] args, CorFlagsSettings settings)
		{
			int idx = option.IndexOf (':');
			string arg, value;

			if (idx == -1) {
				arg = option;
				value = "";
			} else {
				arg = option.Substring (0, idx);

				value = option.Substring (idx + 1);
			}

			switch (arg.ToUpperInvariant ()) {
			case "/NOLOGO":
				settings.NoLogo = true;
				return ParseResult.Success;

			case "/HELP":
			case "/?":
				Header ();
				Usage ();
				return ParseResult.Stop;

			case "/ABOUT":
				About ();
				return ParseResult.Stop;

			case "/VERSION":
			case "/V":
				Version ();
				return ParseResult.Stop;

			case "/ISSUE":
			case "/BUGREPORT":
				output.WriteLine ("To file bug reports, please visit: https://github.com/sushihangover/CorFlags/issues");
				return ParseResult.Stop;

			case "/FORCE":
				settings.InfoOnly = false;
				settings.Force = true;
				return ParseResult.Success;

			case "/32BIT":
			case "/32BIT+":
			case "/32BITREQ+":
				settings.InfoOnly = false;
				settings.THIRTYTWO_BITREQUIRED = true;
				settings.THIRTYTWO_NOT_BITREQUIRED = false;
				return ParseResult.Success;

			case "/32BIT-":
			case "/32BITREQ-":
				settings.InfoOnly = false;
				settings.THIRTYTWO_BITREQUIRED = false;
				settings.THIRTYTWO_NOT_BITREQUIRED = true;
				return ParseResult.Success;

			case "/32BITPREF":
			case "/32BITPREF+":
				settings.InfoOnly = false;
				settings.THIRTYTWO_BITPREFERRED = true;
				settings.THIRTYTWO_NOT_BITPREFERRED = false;
				return ParseResult.Success;
			
			case "/32BITPREF-":
				settings.InfoOnly = false;
				settings.THIRTYTWO_BITPREFERRED = false;
				settings.THIRTYTWO_NOT_BITPREFERRED = true;
				return ParseResult.Success;

			case "/ILONLY":
			case "/ILONLY+":
				settings.InfoOnly = false;
				settings.ILONLY = true;
				settings.NOT_ILONLY = false;
				return ParseResult.Success;

			case "/ILONLY-":
				settings.InfoOnly = false;
				settings.ILONLY = false;
				settings.NOT_ILONLY = true;
				return ParseResult.Success;
			
			case "/REVERTCLRHEADER":
				settings.InfoOnly = false;
				settings.RevertCLRHeader = true;
				settings.UpgradeCLRHeader = false;
				return ParseResult.Success;

			case "/UPGRADECLRHEADER":
				settings.InfoOnly = false;
				settings.RevertCLRHeader = false;
				settings.UpgradeCLRHeader = true;
				return ParseResult.Success;


			case "/debug":
				if (value.Equals ("+", StringComparison.OrdinalIgnoreCase) || value.Equals ("full", StringComparison.OrdinalIgnoreCase) || value.Equals ("pdbonly", StringComparison.OrdinalIgnoreCase) || idx < 0) {
					return ParseResult.Success;
				}

				return ParseResult.Error;

			default:
				return ParseResult.UnknownOption;
			}
		}

		//
		// Given a path specification, splits the path from the file/pattern
		//
		static void SplitPathAndPattern (string spec, out string path, out string pattern)
		{
			int p = spec.LastIndexOf ('/');
			if (p != -1) {
				//
				// Windows does not like /file.cs, switch that to:
				// "\", "file.cs"
				//
				if (p == 0) {
					path = "\\";
					pattern = spec.Substring (1);
				} else {
					path = spec.Substring (0, p);
					pattern = spec.Substring (p + 1);
				}
				return;
			}

			p = spec.LastIndexOf ('\\');
			if (p != -1) {
				path = spec.Substring (0, p);
				pattern = spec.Substring (p + 1);
				return;
			}

			path = ".";
			pattern = spec;
		}

		public void Header ()
		{
			output.WriteLine (
				"Mono/.NET Framework CorFlags Conversion Tool.  Version  {0}\n" +
				"Copyright (c) SushiHangover.  All rights reserved.\n", GetVersion()
			);
		}
		public void Usage ()
		{
			output.WriteLine (
				"Windows Usage: xCorflags.exe Assembly [options]\n" +
				" X-Plat Usage: mono xcorflags.exe Assembly [options]\n" +
				"\n" +
				"If no options are specified, the flags for the given image are displayed.\n" + 
				"\n" +
				" 	 Options: (/ or - prefixed\n" +
				"	 /ILONLY+ /ILONLY-       Sets/clears the ILONLY flag\n" +
				"	 /32BITREQ+ /32BITREQ-   Sets/clears the bits indicating 32-bit x86 only\n" +
				"	 /32BITPREF+ /32BITPREF- Sets/clears the bits indicating 32-bit preferred\n" +
				"	 /UpgradeCLRHeader       Upgrade the CLR Header to version 2.5\n" +
				"	 /RevertCLRHeader        Revert the CLR Header to version 2.0\n" +
				"	 /Force                  Force an assembly update even if the image is\n" +
				"		 strong name signed.\n" +
				"		 WARNING: Updating a strong name signed assembly\n" +
				"		 will require the assembly to be resigned before\n" +
				"		 it will execute properly.\n" +
				"	 /nologo                 Prevents corflags from displaying logo\n",
				GetVersion());
		}

		void About ()
		{
			output.WriteLine (
				"xCorFlags is Copyright 2015, SushiHangover/RobertN\n\n" +
				"The xCorFlags source code is released under the terms of MIT License (MIT)\n\n" +
				"For more information on cCorFlags, visit the project site:\n" +
				"   https://github.com/sushihangover/CorFlags\n\n" +
				"xCorFlags written by SushiHangover/RobertN");
		}
			
		string GetVersion ()
		{
			return System.Reflection.MethodBase.GetCurrentMethod ().DeclaringType.Assembly.GetName ().Version.ToString ();
		}

		void Version ()
		{
			output.WriteLine ("CorFlags version {0}", GetVersion());
		}
	}
}
