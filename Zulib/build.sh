#!/bin/sh

mono .paket/paket.exe restore

exit_code=$?
  if [ $exit_code -ne 0 ]; then
    exit $exit_code
  fi

ulimit -n 9001
mono packages/FAKE/tools/FAKE.exe $@ --fsiargs -d:MONO build.fsx
