FROM microsoft/dotnet:2.2-sdk AS publish
WORKDIR /srv/
COPY ./ ./
WORKDIR /srv/src
RUN dotnet restore
RUN dotnet publish -c debug -o out --self-contained --runtime linux-x64 --framework netcoreapp2.2

# test application
FROM publish AS testrunner
WORKDIR /srv/src
ENTRYPOINT ["dotnet", "test", "--logger:trx"]

# ADL runtime
FROM microsoft/dotnet:2.2-runtime AS runtime
WORKDIR /srv/src/Catalyst.Node
COPY --from=publish /srv/src/Catalyst.Node/out ./

RUN mkdir /root/.Catalyst
# COPY certificate.pem /root/.Catalyst
# COPY mycert.pfx /root/.Catalyst
# COPY mykey.pem /root/.Catalyst

COPY entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh
RUN ls -la
RUN pwd
ENTRYPOINT ["/srv/src/Catalyst.Node/entrypoint.sh"]

FROM microsoft/dotnet:2.2-sdk AS publish-alpine
RUN apk update && apk add libc6-compat libnsl libnsl-dev
WORKDIR /srv/
COPY ./ ./
WORKDIR /srv/src
RUN dotnet restore
WORKDIR ./Catalyst.Node
RUN dotnet publish -c release -o out --self-contained --runtime linux-x64 --framework netcoreapp2.2

# test application
FROM publish-alpine AS testrunner-alpine
WORKDIR /srv/src
ENTRYPOINT ["dotnet", "test", "--logger:trx"]

# ADL runtime
FROM microsoft/dotnet:2.2-runtime AS runtime-alpine
RUN apk update && apk add libc6-compat libnsl libnsl-dev
WORKDIR /srv/src/Catalyst.Node
COPY --from=publish-alpine /srv/src/Catalyst.Node/out ./

RUN mkdir /root/.Catalyst
COPY certificate.pem /root/.Catalyst
COPY mycert.pfx /root/.Catalyst
COPY mykey.pem /root/.Catalyst

RUN ldd /usr/lib/libnsl.so
RUN cp /usr/lib/libnsl.so /usr/lib/libnsl.so.1
RUN ldd /usr/lib/libnsl.so.1
RUN ldd /usr/lib/libnsl.so.2
RUN ldd libgrpc_csharp_ext.x64.so

COPY entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh
ENTRYPOINT ["/srv/src/Catalyst.Node/entrypoint.sh"]