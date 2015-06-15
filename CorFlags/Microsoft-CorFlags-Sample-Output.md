Note: Mono sets the CorFlags with mcs/mcs/assembly.cs

anycpu (default) compiles your assembly to run on any platform. Your application runs as a 64-bit process whenever possible and falls back to 32-bit when only that mode is available.
anycpu32bitpreferred compiles your assembly to run on any platform. Your application runs in 32-bit mode on systems that support both 64-bit and 32-bit applications. You can specify this option only for projects that target the .NET Framework 4.5.
ARM compiles your assembly to run on a computer that has an Advanced RISC Machine (ARM) processor.
x64 compiles your assembly to be run by the 64-bit common language runtime on a computer that supports the AMD64 or EM64T instruction set.
x86 compiles your assembly to be run by the 32-bit, x86-compatible common language runtime.
Itanium compiles your assembly to be run by the 64-bit common language runtime on a computer with an Itanium processor.



//These combine to specify the assembly types. Here is how they would look like for:
//
//	anycpu: PE = PE32    and  32BIT = 0
//		x86:      PE = PE32    and  32BIT = 1
//		64-bit:  PE = PE32+  and  32BIT = 0

PS> .\CorFlags.exe .\Test\anycpu\Release\DllForTesting.dll
Microsoft (R) .NET Framework CorFlags Conversion Tool.  Version  4.0.30319.17929
Copyright (c) Microsoft Corporation.  All rights reserved.

Version   : v4.0.30319
CLR Header: 2.5
PE        : PE32
CorFlags  : 0x1
ILONLY    : 1
32BITREQ  : 0
32BITPREF : 0
Signed    : 0
PS> .\CorFlags.exe .\Test\anycpu\Release\ExeForTesting.exe
Microsoft (R) .NET Framework CorFlags Conversion Tool.  Version  4.0.30319.17929
Copyright (c) Microsoft Corporation.  All rights reserved.

Version   : v4.0.30319
CLR Header: 2.5
PE        : PE32
CorFlags  : 0x1
ILONLY    : 1
32BITREQ  : 0
32BITPREF : 0
Signed    : 0
PS> .\CorFlags.exe .\Test\itanium\Release\DllForTesting.dll
Microsoft (R) .NET Framework CorFlags Conversion Tool.  Version  4.0.30319.17929
Copyright (c) Microsoft Corporation.  All rights reserved.

Version   : v4.0.30319
CLR Header: 2.5
PE        : PE32+
CorFlags  : 0x1
ILONLY    : 1
32BITREQ  : 0
32BITPREF : 0
Signed    : 0
PS> .\CorFlags.exe .\Test\itanium\Release\ExeForTesting.exe
Microsoft (R) .NET Framework CorFlags Conversion Tool.  Version  4.0.30319.17929
Copyright (c) Microsoft Corporation.  All rights reserved.

Version   : v4.0.30319
CLR Header: 2.5
PE        : PE32+
CorFlags  : 0x1
ILONLY    : 1
32BITREQ  : 0
32BITPREF : 0
Signed    : 0
PS> .\CorFlags.exe .\Test\x64\Release\DllForTesting.dll
Microsoft (R) .NET Framework CorFlags Conversion Tool.  Version  4.0.30319.17929
Copyright (c) Microsoft Corporation.  All rights reserved.

Version   : v4.0.30319
CLR Header: 2.5
PE        : PE32+
CorFlags  : 0x1
ILONLY    : 1
32BITREQ  : 0
32BITPREF : 0
Signed    : 0
PS> .\CorFlags.exe .\Test\x64\Release\ExeForTesting.exe
Microsoft (R) .NET Framework CorFlags Conversion Tool.  Version  4.0.30319.17929
Copyright (c) Microsoft Corporation.  All rights reserved.

Version   : v4.0.30319
CLR Header: 2.5
PE        : PE32+
CorFlags  : 0x1
ILONLY    : 1
32BITREQ  : 0
32BITPREF : 0
Signed    : 0
PS> .\CorFlags.exe .\Test\anycpu32bitpreferred\Release\DllForTesting.dll
Microsoft (R) .NET Framework CorFlags Conversion Tool.  Version  4.0.30319.17929
Copyright (c) Microsoft Corporation.  All rights reserved.

Version   : v4.0.30319
CLR Header: 2.5
PE        : PE32
CorFlags  : 0x1
ILONLY    : 1
32BITREQ  : 0
32BITPREF : 0
Signed    : 0
PS>



If the assembly contains only pure MSIL the "ilonly" flag will be present. For mixed-mode assemblies the "contains native" flag will be present instead. The flag "32bits" means that it will run as a native 32-bit app on 32-bit systems and run as a native 32-bit app within the WoW64 subsystem 64-bit systems. Platform independent assemblies will have the "32/64" flag instead.

For AnyCPU assemblies : PE = PE32, 32BIT = 0, and ILONLY = 1

For x86 assemblies : PE = PE32 and 32BIT = 1

For x64 assmblies : PE = PE32+ and 32BIT=0

If ILONLY = 0 that means the assembly has unmanaged codes as well (so it cannot be anycpu).

There is no way (for now) to demand execution in a 64-bit process because this thing is not controled by a metadata flag. Rather it's controled at compile time and compiler has to emit PE32+ compatible code.


corflags.exe /32BIT+ testme.exe

Now, this assembly would always execute under WoW64 (emulated 32-bit environment on a 64-bit box). In a case where this execution would not be possible - ie. run against an ia64 box - one would get a BadImageFormatException exception.

http://blogs.msdn.com/b/dsvc/archive/2008/06/28/x86-x64-ia64-any-cpu.aspx



C:\Users\Robert\Desktop>CorFlags.exe foo.exe
Microsoft (R) .NET Framework CorFlags Conversion Tool.  Version  4.0.30319.17929
Copyright (c) Microsoft Corporation.  All rights reserved.

corflags : error CF002 : Could not open file for reading

