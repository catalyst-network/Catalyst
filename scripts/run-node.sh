#!/usr/bin/env bash

openssl genrsa 2048 >> /root/.catalyst/private.pem
openssl req -x509 -new -key /root/.catalyst/private.pem -out /root/.catalyst/public.pem -subj "/C=US/ST=dsfd/L=dsfdsf/O=dfdsf/OU=dsfdsf/CN=dsfds"
openssl pkcs12 -export -in /root/.catalyst/public.pem -inkey /root/.catalyst/private.pem -out /root/.catalyst/mycert.pfx -passout pass:"test"

if [[ -z "${NODE_ENV}" ]]; then
  NODE_CONFIG_FILE="devnet.json"
else
  NODE_CONFIG_FILE="${NODE_ENV}"
fi

dotnet /opt/Catalyst.Node.dll --ipfs-password test --ssl-cert-password test --node-password test --config $NODE_CONFIG_FILE -o true