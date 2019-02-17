FROM microsoft/dotnet:2.1-sdk AS publish
WORKDIR /srv/
COPY ./ ./
WORKDIR /srv/src
RUN dotnet restore
WORKDIR ./Catalyst.Node
RUN dotnet publish -c release -o out --self-contained --runtime linux-x64 --framework netcoreapp2.1

# test application
FROM publish AS testrunner
WORKDIR /srv/src
ENTRYPOINT ["dotnet", "test", "--logger:trx"]

# ADL runtime
FROM microsoft/dotnet:2.1-runtime AS runtime
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
