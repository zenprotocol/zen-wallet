#!/bin/bash

pwd=`pwd`
cd "$(dirname "$0")"

rm -rf zen
mkdir zen
cd zen

MODE="${1:-Release}"

echo "Packing $MODE mode"

# get some files needed
ZENPATH="../../../Zen/bin/$MODE"

cp $ZENPATH/*.dll ./
cp $ZENPATH/zen.exe ./
cp $ZENPATH/zen.exe.config ./
cp $ZENPATH/*.json ./
cp $ZENPATH/d3.min.js ./
cp $ZENPATH/graph.html ./

# get 3rd party tools and libs
TOOLSPATH="../../../tools"

# z3
cp -r $TOOLSPATH/z3/linux ./z3
# fstar
cp -r $TOOLSPATH/fstar/mono ./fstar
# Zulib-fstar
ZULIBPATH="../../../Zulib/fstar"
cp -r $ZULIBPATH ./zulib

# configure
# todo: use args
CONFIG="zen.exe.config"
xmlstarlet edit -L -u "/configuration/appSettings/add[@key='network']/@value" -v 'staging_client' $CONFIG
xmlstarlet edit -L -u "/configuration/appSettings/add[@key='assetsDiscovery']/@value" -v 'staging.zenprotocol.com' $CONFIG
xmlstarlet edit -L -u "/configuration/appSettings/add[@key='fstar']/@value" -v 'fstar' $CONFIG
xmlstarlet edit -L -u "/configuration/appSettings/add[@key='zulib']/@value" -v 'zulib' $CONFIG

cp ../run-zen ./zen

cd ..

rm -f zen.tar.gz
tar czf zen.tar.gz zen

rm -rf zen

cd "$pwd"

#rsync -azr zen.tar.gz ubuntu@staging:/home/ubuntu/www
