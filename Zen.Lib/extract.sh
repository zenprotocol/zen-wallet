#!/bin/bash

# records hints
../../externals/FStar/bin/fstar.exe --prims prims.fst --record_hints Zen.Types.fst
../../externals/FStar/bin/fstar.exe --prims prims.fst --record_hints Zen.Realized.fst

# verification and extraction of FStar to FSharp code
../../externals/FStar/bin/fstar.exe --codegen FSharp --prims prims.fst --extract_module Zen.Types --use_hints --n_cores 4 --odir fs Zen.Types.fst


#TODO: rest source files
