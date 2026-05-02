FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

ARG ASPNETCORE_ENVIRONMENT=Development
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["PharmaGo.WebApi/PharmaGo.WebApi.csproj", "PharmaGo.WebApi/"]
COPY ["PharmaGo.Factory/PharmaGo.Factory.csproj", "PharmaGo.Factory/"]
COPY ["PharmaGo.DataAccess/PharmaGo.DataAccess.csproj", "PharmaGo.DataAccess/"]
COPY ["PharmaGo.BusinessLogic/PharmaGo.BusinessLogic.csproj", "PharmaGo.BusinessLogic/"]
COPY ["PharmaGo.Domain/PharmaGo.Domain.csproj", "PharmaGo.Domain/"]
COPY ["PharmaGo.Exceptions/PharmaGo.Exceptions.csproj", "PharmaGo.Exceptions/"]
COPY ["PharmaGo.IBusinessLogic/PharmaGo.IBusinessLogic.csproj", "PharmaGo.IBusinessLogic/"]
COPY ["PharmaGo.IDataAccess/PharmaGo.IDataAccess.csproj", "PharmaGo.IDataAccess/"]
COPY ["Instrumentation/Instrumentation.csproj", "Instrumentation/"]
COPY ["InstrumentationInterface/InstrumentationInterface.csproj", "InstrumentationInterface/"]
COPY ["JSONExporter/JSONExporter.csproj", "JSONExporter/"]
COPY ["ExportationModel/ExportationModel.csproj", "ExportationModel/"]

RUN dotnet restore "PharmaGo.WebApi/PharmaGo.WebApi.csproj"

COPY . .

RUN dotnet publish "PharmaGo.WebApi/PharmaGo.WebApi.csproj" \
    -c ${BUILD_CONFIGURATION} \
    -o /app/publish \
    -p:EnvironmentName=${ASPNETCORE_ENVIRONMENT}

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final

ARG ASPNETCORE_ENVIRONMENT=Development

LABEL maintainer="PharmaGo Team"
LABEL description="PharmaGo Backend API - .NET 6.0"
LABEL environment=${ASPNETCORE_ENVIRONMENT}

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
