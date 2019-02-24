#!/bin/bash

for i in $(find ./src -name '*.cs')
do
  if ! grep -q Copyright $i
  then
    cat COPYING $i >$i.new && mv $i.new $i
  fi
done