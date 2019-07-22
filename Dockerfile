FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS dev-build-env
WORKDIR /app

RUN mkdir output

COPY ./ ./
WORKDIR /app/src
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Debug -o output

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 as dev-env
RUN useradd -ms /bin/bash kat
WORKDIR /home/kat

RUN mkdir /home/kat/.Catalyst
COPY --from=dev-build-env /app/src/Catalyst.Node/output .
COPY mycert.pfx /home/kat/.Catalyst/
COPY private.pem /home/kat/.Catalyst/
COPY public.pem /home/kat/.Catalyst/
RUN ls -la /home/kat/.Catalyst/
RUN chown -Rf kat:kat /home/kat
USER kat
ENTRYPOINT ["dotnet", "Catalyst.Node.dll"]

## test application
#FROM build-env AS test-env
#WORKDIR /srv/src
#ENTRYPOINT ["dotnet", "test"]