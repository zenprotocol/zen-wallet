#!/bin/bash

mono ../../tools/fstar/mono/fstar.exe --lax --codegen FSharp --prims ../../Zulib/fstar/prims.fst --extract_module ZenModule --include ../../Zulib/fstar --no_default_includes $PWD/$1 --verify_all
