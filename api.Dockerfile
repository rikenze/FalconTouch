# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os csproj primeiro para aproveitar cache
COPY ./src/FalconTouch.Domain/FalconTouch.Domain.csproj ./FalconTouch.Domain/
COPY ./src/FalconTouch.Infrastructure/FalconTouch.Infrastructure.csproj ./FalconTouch.Infrastructure/
COPY ./src/FalconTouch.Application/FalconTouch.Application.csproj ./FalconTouch.Application/
COPY ./src/FalconTouch.Api/FalconTouch.Api.csproj ./FalconTouch.Api/

RUN dotnet restore ./FalconTouch.Api/FalconTouch.Api.csproj

# Copia o restante do código
COPY ./src ./ 

WORKDIR /src/FalconTouch.Api
RUN dotnet publish FalconTouch.Api.csproj -c Release -o /app/publish

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FalconTouch.Api.dll"]
