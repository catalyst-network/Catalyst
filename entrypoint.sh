#!/usr/bin/env sh
PUBLIC_KEY=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 20 | head -n 1)
dotnet Catalyst.Node.dll --public-key $PUBLIC_KEY --payout-address jw8dh3ns92p2msjs73jdnga --disable-dfs --disable-rpc -d
