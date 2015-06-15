# xCorFlags.exe (CorFlags Conversion Tool)
## Mono and .NET Framework 4.5 

### The CorFlags Conversion tool allows you to configure the CorFlags section of the header of a portable executable image.

This is a **cross-platform** version of the [Microsoft CorFlags](https://msdn.microsoft.com/en-us/library/ms164699%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396) tool that is included in the .Net Framework SDK installations.

* [MSDN Documentation for CorFlag.exe](https://msdn.microsoft.com/en-us/library/ms164699%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396)

This xplat version strives to be argument option and output compatible (using /nologo) with the Microsoft version.

*Note: Platform independent argument prefixes can be used: **'/' or '-'** *

## Usage:

    Mono/.NET Framework CorFlags Conversion Tool.  Version  0.9.5643.21314
    Copyright (c) SushiHangover.  All rights reserved.
    
    Windows Usage: xCorflags.exe Assembly [options]
     X-Plat Usage: mono xcorflags.exe Assembly [options]
    
    If no options are specified, the flags for the given image are displayed.
    
     	 Options: (/ or - prefixed
    	 /ILONLY+ /ILONLY-       Sets/clears the ILONLY flag
    	 /32BITREQ+ /32BITREQ-   Sets/clears the bits indicating 32-bit x86 only
    	 /32BITPREF+ /32BITPREF- Sets/clears the bits indicating 32-bit preferred
    	 /UpgradeCLRHeader       Upgrade the CLR Header to version 2.5
    	 /RevertCLRHeader        Revert the CLR Header to version 2.0
    	 /Force                  Force an assembly update even if the image is
    		 strong name signed.
    		 WARNING: Updating a strong name signed assembly
    		 will require the assembly to be resigned before
    		 it will execute properly.
    	 /nologo                 Prevents corflags from displaying logo
	 

## Output:

When running with the nologo option, the output should be compatible with Microsoft's version, a nice to have for those who already are used the output to CorFlags.exe or those that have existing scripts.

    mono ./CorFlags.exe /nologo CorFlags.exe
    Version   : v4.0.30319
    CLR Header: 2.5
    PE        : PE32
    CorFlags  : 0x3
    ILONLY    : 1
    32BITREQ  : 1
    32BITPREF : 0
    Signed    : 0
    

## Without using the nologo argument:

    mono ./CorFlags.exe CorFlags.exe SudoKnights.exe
    Mono/.NET Framework CorFlags Conversion Tool.  Version  0.9.5643.21314
    Copyright (c) SushiHangover.  All rights reserved.
    
    Version   : v4.0.30319
    CLR Header: 2.5
    PE        : PE32
    CorFlags  : 0x20003
    ILONLY    : 1
    32BITREQ  : 1
    32BITPREF : 1
    Signed    : 0

### versus:

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


Built and tested using Mono 4.0.1 on:

* Windows
* OS-X
* Linux
