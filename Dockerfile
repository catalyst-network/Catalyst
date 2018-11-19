# develop container
FROM microsoft/dotnet:2.1-sdk-alpine AS develop
WORKDIR /srv
COPY . ./
RUN dotnet build
WORKDIR /srv/Cli

# publish container
FROM microsoft/dotnet:2.1-sdk-alpine AS publish
WORKDIR /srv
COPY . ./
RUN dotnet restore
WORKDIR /srv/Cli
RUN dotnet add package ILLink.Tasks -v 0.1.5-preview-1841731 -s https://dotnet.myget.org/F/dotnet-core/api/v3/index.json
RUN dotnet publish -c Release -r osx-x64 -o out /p:ShowLinkerSizeComparison=true

# test application
FROM publish AS testrunner
WORKDIR /srv
ENTRYPOINT ["dotnet", "test", "--logger:trx"]


# ADL runtime
FROM microsoft/dotnet:2.1-runtime-deps-alpine AS runtime
WORKDIR /usr/local/bin
COPY --from=publish /srv/Cli/out ./
ENTRYPOINT ["./dotnetapp"]
