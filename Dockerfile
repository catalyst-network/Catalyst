FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build-env

WORKDIR /opt
COPY ./ ./
RUN mkdir output
WORKDIR /opt/src/Catalyst.Node
RUN dotnet restore

# Copy everything else and build
WORKDIR /opt
COPY . ./
RUN dotnet publish src/Catalyst.Node/Catalyst.Node.csproj -c Debug -o output

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/sdk:2.2

WORKDIR /opt

RUN apt update -y; apt-get install dnsutils lsof htop -y

RUN mkdir /root/.catalyst
COPY scripts/run-node.sh /tmp/run-node.sh
RUN chmod +x /tmp/run-node.sh
COPY --from=build-env /opt/output .

CMD ["/tmp/run-node.sh"]
