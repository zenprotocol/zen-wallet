#!/bin/bash

# records hints
../../externals/FStar/bin/fstar.exe --prims prims.fst --record_hints Consensus.Types.fst
../../externals/FStar/bin/fstar.exe --prims prims.fst --record_hints Consensus.Realized.fst

# verification and extraction of FStar to FSharp code 
../../externals/FStar/bin/fstar.exe --codegen FSharp --prims prims.fst --extract_module Consensus.Types --use_hints --n_cores 4 --odir fs Consensus.Types.fst


#TODO: rest source files