#!/usr/bin/env bash
# Generates the .cs files from the proto files.

# Exit on error.
set -eu

readonly workspace=$(dirname $0)
readonly outdir=$(dirname $0)/../../Sdk/Catalyst.Protocol.Node.Rpc.Grpc
readonly greeter_protos_dir=${workspace}
readonly proto_tools=${HOME}/.nuget/packages/grpc.tools/1.16.0/tools/macosx_x64

mkdir -p ${outdir}

find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --proto_path ${workspace} \
      --grpc_out ${outdir} \
      --plugin=protoc-gen-grpc=${proto_tools}/grpc_csharp_plugin
