FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["FGLairControl.csproj", "."]
RUN dotnet restore "./FGLairControl.csproj"
COPY . .
RUN dotnet build "FGLairControl.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FGLairControl.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Define environment variables with default values
ENV FGLair__LouverPositions="7,8"
ENV FGLair__Interval="20"

ENTRYPOINT ["dotnet", "FGLairControl.dll"]
