#!/bin/bash

mono ../../ZenFSharpC/bin/Debug/ZenFSharpC.exe --mlcompatibility -a ZenModule.fs -r ../../Zulib/bin/Zulib.dll -r ../../tools/fstar/mono/FSharp.Compatibility.OCaml.dll
