#!/usr/bin/env bash
# Generates the .cs files from the proto files.

# Exit on error.
set -eu

readonly current=$(dirname $0)
readonly workspace=$(dirname $0)/../../../
readonly proto_tools=${HOME}/.nuget/packages/grpc.tools/1.16.0/tools/macosx_x64

readonly greeter_protos_dir=${workspace}/Protocols/Rpc/Node
readonly generated_dir=${workspace}/Node/Core/Modules/Rpc

mkdir -p ${generated_dir}
mkdir -p ${greeter_protos_dir}/dist/csharp

find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --proto_path ${greeter_protos_dir} \
      --grpc_out ${greeter_protos_dir}/dist/csharp \
      --plugin=protoc-gen-grpc=${proto_tools}/grpc_csharp_plugin
