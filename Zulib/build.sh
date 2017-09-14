#!/bin/sh

mono .paket/paket.exe restore

exit_code=$?
  if [ $exit_code -ne 0 ]; then
    exit $exit_code
  fi

TARGET="$@"

if [[ "${TARGET}" = "" ]]
then
  TARGET="Default"
fi  

mono packages/FAKE/tools/FAKE.exe $TARGET --fsiairgs -d:MONO build.fsx
