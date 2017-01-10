#!/bin/bash

rsync -az --no-r bin/Debug/*.* ubuntu@54.218.251.235:/home/ubuntu/Debug4

# rsync -az --no-r bin/Debug/*.* --exclude *.xml ubuntu@54.218.251.235:/home/ubuntu/Debug3