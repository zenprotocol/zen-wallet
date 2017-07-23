#!/bin/bash

pwd=`pwd`
cd "$(dirname "$0")"

APP_NAME="Zen"

rm -rf "$APP_NAME.app"
mkdir "$APP_NAME.app"
cd "$APP_NAME.app"

mkdir Contents
cd Contents

cp ../../Info.plist .

mkdir MacOS
cd MacOS
cp ../../../zen .
cd ..

mkdir Resources
cd Resources

MODE="${1:-Release}"
echo "Packing $MODE mode"

ZENPATH="../../../../../Zen/bin/$MODE"

cp $ZENPATH/*.dll ./
cp $ZENPATH/zen.exe ./
cp $ZENPATH/zen.exe.config ./
cp ../../../Zen.icns ./
cp $ZENPATH/*.json ./
cp $ZENPATH/d3.min.js ./
cp $ZENPATH/graph.html ./
cp /usr/local/lib/libsodium.dylib ./

cd ../../..

rm -f Zen.dmg
appdmg ./appdmg.json ./Zen.dmg

cd "$pwd"
