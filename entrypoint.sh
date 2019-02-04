#!/usr/bin/env sh
PUBLIC_KEY=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 20 | head -n 1)
PAYOUT_ADDRESS=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 20 | head -n 1)
dotnet Catalyst.Node.dll --public-key $PUBLIC_KEY --payout-address $PAYOUT_ADDRESS --disable-dfs --disable-rpc -d
