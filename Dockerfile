FROM microsoft/dotnet:2.1-sdk AS publish
#RUN apk update && apk add libc6-compat libnsl libnsl-dev
WORKDIR /srv/
COPY ./ ./
WORKDIR /srv/src
RUN dotnet restore
WORKDIR /srv/Catalyst.Node
#RUN dotnet add package ILLink.Tasks -v 0.1.5-preview-1841731 -s https://dotnet.myget.org/F/dotnet-core/api/v3/index.json
RUN dotnet publish -c release -o out --self-contained --runtime linux-x64 --framework netcoreapp2.1

# test application
FROM publish AS testrunner
WORKDIR /srv/src
ENTRYPOINT ["dotnet", "test", "--logger:trx"]

# ADL runtime
FROM microsoft/dotnet:2.1-runtime AS runtime
#RUN apk update && apk add libc6-compat libnsl libnsl-dev
WORKDIR /srv/src/Catalyst.Node
COPY --from=publish /srv/src/Catalyst.Node/out ./

COPY certificate.pem ./
COPY mycert.pfx ./
COPY mykey.pem ./
#RUN ls -la
#RUN ldd /usr/lib/libnsl.so
#RUN cp /usr/lib/libnsl.so /usr/lib/libnsl.so.1
#RUN ldd /usr/lib/libnsl.so.1
#RUN ldd /usr/lib/libnsl.so.2
#RUN ldd libgrpc_csharp_ext.x64.so

COPY entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh
ENTRYPOINT ["/entrypoint.sh"]

#ENTRYPOINT ["./Node", "--public-key", "jem832p1uajfnc73kfhct", "--payout-address", "kek", "--disable-dfs", "--disable-gossip", "--disable-consensus", "-d", "--data-dir", "/srv/Node"]
