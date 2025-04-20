# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
RUN apt upgrade
RUN apt update
RUN apt-get install -y nodejs npm
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["sfa-chat-graph.Server/sfa-chat-graph.Server.csproj", "sfa-chat-graph.Server/"]
COPY ["AwosFramework.ApiClients.Jupyter/AwosFramework.ApiClients.Jupyter.csproj", "AwosFramework.ApiClients.Jupyter/"]
COPY ["AwosFramework.Generators.FunctionCalling/AwosFramework.Generators.FunctionCalling.csproj", "AwosFramework.Generators.FunctionCalling/"]
COPY ["sfa-chat-graph.client/sfa-chat-graph.client.esproj", "sfa-chat-graph.client/"]
COPY ["sfa-chat-graph.client/package.json", "sfa-chat-graph.client/"]
RUN mkdir -p /src/sfa-chat-graph.client/obj/Debug/
RUN npm install ./sfa-chat-graph.client/

RUN dotnet restore "./sfa-chat-graph.Server/sfa-chat-graph.Server.csproj"
COPY . .
WORKDIR "/src/sfa-chat-graph.Server"
RUN dotnet build "./sfa-chat-graph.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./sfa-chat-graph.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "sfa-chat-graph.Server.dll"]