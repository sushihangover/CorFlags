#!/bin/bash
function build {
	xbuild /property:Configuration=Release /property:Platform=x64 /target:Build  >/dev/null
	xbuild /property:Configuration=Release /property:Platform=x86 /target:Build  >/dev/null
	xbuild /property:Configuration=Release /property:Platform=anycpu /target:Build  >/dev/null
	xbuild /property:Configuration=Release /property:Platform=anycpu32bitpreferred /target:Build  >/dev/null
	xbuild /property:Configuration=Release /property:Platform=itanium /target:Build  >/dev/null
}
pushd ../Test/ExeForTesting/
echo "Building 5 EXE test targets"
build
popd
pushd ../Test/DllForTesting
echo "Building 5 DLL test targets"
build
popd
