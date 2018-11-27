#!/usr/bin/env bash
# Generates the .cs files from the proto files.

# Exit on error.
set -eu

readonly workspace=$(dirname $0)/..
readonly proto_tools=${HOME}/.nuget/packages/grpc.tools/1.16.0/tools/linux_x86

readonly greeter_protos_dir=${workspace}/Proto
readonly generated_dir=${greeter_protos_dir}/Server

mkdir -p ${generated_dir}

find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs %{} ${proto_tools}/protoc {} \
      --csharp_out ${generated_dir} \
      --proto_path ${greeter_protos_dir}

find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs %{} ${proto_tools}/protoc {} \
      --proto_path ${greeter_protos_dir} \
      --grpc_out ${generated_dir} \
      --plugin=protoc-gen-grpc=${proto_tools}/grpc_csharp_plugin
