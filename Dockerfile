FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env



#Install snappy for RocksDB +
RUN apt-get update
RUN apt-get install -y nano 
RUN apt install -y dnsutils 
RUN apt-get install -y apt-utils
RUN apt-get install -y build-essential
RUN apt -y install libsnappy-dev

#Install MongoDB 
EXPOSE 27017/tcp
RUN apt-get -y install gnupg
RUN wget -qO - https://www.mongodb.org/static/pgp/server-4.2.asc | apt-key add -
RUN echo "deb [ arch=amd64,arm64 ] https://repo.mongodb.org/apt/ubuntu bionic/mongodb-org/4.2 multiverse" | tee /etc/apt/sources.list.d/mongodb-org-4.2.list
RUN apt-get update
RUN apt-get install -y mongodb-org

RUN git clone https://github.com/catalyst-network/Catalyst.git
WORKDIR Catalyst
RUN git checkout develop
RUN ls
COPY docker_resources.json /app/core/.dolittle/resources.json

RUN git submodule update --init --recursive --force

#Build the project 



WORKDIR src

RUN dotnet build Catalyst.sln
