#!/bin/bash

pwd=`pwd`

cd "$(dirname "$0")"

MODE="${1:-Release}"

echo "Cleaning up $MODE"
ZENPATH="../../Zen/bin/$MODE"
rm -r $ZENPATH

echo "Compiling $MODE mode"
eval "msbuild ../../unix.sln /p:Configuration=$MODE"
ret_code=$?
if [ $ret_code != 0 ]; then
  printf "Error compiling project.\n aborted.\n"
  exit $ret_code
fi

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

echo "Packing $MODE mode"

# get some files needed
ZENPATH="../../../../../Zen/bin/$MODE"

cp $ZENPATH/*.dll ./
cp $ZENPATH/zen.exe ./
cp $ZENPATH/zen.exe.config ./
cp ../../../Zen.icns ./
cp $ZENPATH/*.json ./
cp $ZENPATH/d3.min.js ./
cp $ZENPATH/graph.html ./

# get 3rd party tools and libs
TOOLSPATH="../../../../../tools"

mkdir tools
# z3
cp -r $TOOLSPATH/z3/osx ./z3
# fstar
cp -r $TOOLSPATH/fstar/mono ./fstar
# Zulib-fstar
ZULIBPATH="../../../../../Zulib/fstar"
cp -r $ZULIBPATH ./zulib
# libsodium
cp /usr/local/lib/libsodium.dylib ./

# configure
# todo: use args
CONFIG="zen.exe.config"
xmlstarlet edit -L -u "/configuration/appSettings/add[@key='network']/@value" -v 'alpha_client' $CONFIG
xmlstarlet edit -L -u "/configuration/appSettings/add[@key='assetsDiscovery']/@value" -v 'alpha.zenprotocol.com' $CONFIG
xmlstarlet edit -L -u "/configuration/appSettings/add[@key='fstar']/@value" -v 'fstar' $CONFIG
xmlstarlet edit -L -u "/configuration/appSettings/add[@key='zulib']/@value" -v 'zulib' $CONFIG


cd ../../..

rm -f Zen.dmg
appdmg ./appdmg.json ./Zen.dmg

cd "$pwd"
