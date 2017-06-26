#!/bin/bash

pwd=`pwd`
cd "$(dirname "$0")"

rm -rf zen
mkdir zen
cd zen

ZENPATH=../../../Zen/bin/Debug

cp $ZENPATH/*.dll ./
cp $ZENPATH/zen.exe ./
cp $ZENPATH/zen.exe.config ./
cp $ZENPATH/*.json ./
cp $ZENPATH/d3.min.js ./
cp ../run-zen ./zen

cd ..

rm -f zen.tar.gz
tar cvzf zen.tar.gz zen

rm -rf zen

cd "$pwd"
