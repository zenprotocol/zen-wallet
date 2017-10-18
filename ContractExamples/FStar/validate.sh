#!/bin/bash

mono ../../tools/fstar/mono/fstar.exe --prims ../../Zulib/fstar/prims.fst --include ../../Zulib/fstar --no_default_includes $PWD/$1
