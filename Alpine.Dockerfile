FROM microsoft/dotnet:2.1-sdk AS publish
RUN apk update && apk add libc6-compat libnsl libnsl-dev
WORKDIR /srv/
COPY ./ ./
WORKDIR /srv/src
RUN dotnet restore
WORKDIR Catalyst.Node
RUN dotnet publish -c release -o out --self-contained --runtime linux-x64 --framework netcoreapp2.1

# test application
FROM publish AS testrunner
WORKDIR /srv/src
ENTRYPOINT ["dotnet", "test", "--logger:trx"]

# ADL runtime
FROM microsoft/dotnet:2.1-runtime AS runtime
RUN apk update && apk add libc6-compat libnsl libnsl-dev
WORKDIR /srv/src/Catalyst.Node
COPY --from=publish /srv/src/Catalyst.Node/out ./

RUN mkdir ~/.Catalyst
COPY certificate.pem ~/.Catalyst
COPY mycert.pfx ~/.Catalyst
COPY mykey.pem ~/.Catalyst

RUN ldd /usr/lib/libnsl.so
RUN cp /usr/lib/libnsl.so /usr/lib/libnsl.so.1
RUN ldd /usr/lib/libnsl.so.1
RUN ldd /usr/lib/libnsl.so.2
RUN ldd libgrpc_csharp_ext.x64.so

COPY entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh
RUN ls -la
RUN pwd
ENTRYPOINT ["/srv/src/Catalyst.Node/entrypoint.sh"]

#ENTRYPOINT ["./Node", "--public-key", "jem832p1uajfnc73kfhct", "--payout-address", "kek", "--disable-dfs", "--disable-gossip", "--disable-consensus", "-d", "--data-dir", "/srv/Node"]