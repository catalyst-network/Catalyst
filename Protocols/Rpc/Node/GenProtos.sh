#!/usr/bin/env bash
# Generates the .cs files from the proto files.

# Exit on error.
set -eu

readonly workspace=$(dirname $0)
readonly proto_tools=${HOME}/.nuget/packages/grpc.tools/1.16.0/tools/macosx_x64

readonly greeter_protos_dir=${workspace}

mkdir -p ${greeter_protos_dir}/dist/
mkdir -p ${greeter_protos_dir}/dist/csharp
mkdir -p ${greeter_protos_dir}/dist/csharp
mkdir -p ${greeter_protos_dir}/dist/javascript
mkdir -p ${greeter_protos_dir}/dist/cpp
mkdir -p ${greeter_protos_dir}/dist/java
mkdir -p ${greeter_protos_dir}/dist/python
mkdir -p ${greeter_protos_dir}/dist/ruby
mkdir -p ${greeter_protos_dir}/dist/php
mkdir -p ${greeter_protos_dir}/dist/objc

find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --csharp_out ${greeter_protos_dir}/dist/csharp \
      --proto_path ${greeter_protos_dir}
      
find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --js_out ${greeter_protos_dir}/dist/javascript \
      --proto_path ${greeter_protos_dir}    

find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --cpp_out ${greeter_protos_dir}/dist/cpp \
      --proto_path ${greeter_protos_dir}     
      
find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --java_out ${greeter_protos_dir}/dist/java \
      --proto_path ${greeter_protos_dir}  
      
find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --python_out ${greeter_protos_dir}/dist/python \
      --proto_path ${greeter_protos_dir}      
      
find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --ruby_out ${greeter_protos_dir}/dist/ruby \
      --proto_path ${greeter_protos_dir}                        

find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --php_out ${greeter_protos_dir}/dist/php \
      --proto_path ${greeter_protos_dir}     

find ${greeter_protos_dir} -type f -name '*.proto' | \
    xargs -J{} ${proto_tools}/protoc {} \
      --objc_out ${greeter_protos_dir}/dist/objc \
      --proto_path ${greeter_protos_dir}   
