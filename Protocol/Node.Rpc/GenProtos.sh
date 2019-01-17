#!/usr/bin/env bash
# Generates the .cs files from the proto files.

# Exit on error.
set -eu

readonly workspace=$(dirname $0)
readonly outdir=$(dirname $0)/../../Sdk
readonly greeter_protos_dir=${workspace}
readonly proto_tools=${HOME}/.nuget/packages/grpc.tools/1.16.0/tools/macosx_x64

mkdir -p ${outdir}/Catalyst.Protocol.Node.Rpc/csharp
mkdir -p ${outdir}/Catalyst.Protocol.Node.Rpc/javascript
mkdir -p ${outdir}/Catalyst.Protocol.Node.Rpc/cpp
mkdir -p ${outdir}/Catalyst.Protocol.Node.Rpc/java
mkdir -p ${outdir}/Catalyst.Protocol.Node.Rpc/python
mkdir -p ${outdir}/Catalyst.Protocol.Node.Rpc/ruby
mkdir -p ${outdir}/Catalyst.Protocol.Node.Rpc/php
mkdir -p ${outdir}/Catalyst.Protocol.Node.Rpc/objc

find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --csharp_out ${outdir}/Catalyst.Protocol.Node.Rpc/csharp \
      --proto_path ${greeter_protos_dir}
      
find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --js_out ${outdir}/Catalyst.Protocol.Node.Rpc/javascript \
      --proto_path ${greeter_protos_dir}    

find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --cpp_out ${outdir}/Catalyst.Protocol.Node.Rpc/cpp \
      --proto_path ${greeter_protos_dir}     
      
find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --java_out ${outdir}/Catalyst.Protocol.Node.Rpc/java \
      --proto_path ${greeter_protos_dir}  
      
find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --python_out ${outdir}/Catalyst.Protocol.Node.Rpc/python \
      --proto_path ${greeter_protos_dir}      
      
find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --ruby_out ${outdir}/Catalyst.Protocol.Node.Rpc/ruby \
      --proto_path ${greeter_protos_dir}                        

find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --php_out ${outdir}/Catalyst.Protocol.Node.Rpc/php \
      --proto_path ${greeter_protos_dir}     

find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --objc_out ${outdir}/Catalyst.Protocol.Node.Rpc/objc \
      --proto_path ${greeter_protos_dir}   
