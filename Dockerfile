# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/CloudKitchenChallenge
COPY ["./CloudKitchenChallenge/CloudKitchenChallenge.csproj", "."]
RUN dotnet restore "./CloudKitchenChallenge.csproj"
COPY ./CloudKitchenChallenge .
WORKDIR "/src/."
RUN dotnet build "./CloudKitchenChallenge/CloudKitchenChallenge.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Test stage
FROM build AS test
RUN touch ./runThisStageDefault.txt
WORKDIR ./CKTests
COPY ["./CKTests/CKTests.csproj", "."]
RUN dotnet restore .
COPY ["./CKTests/", "."]
RUN dotnet build .
RUN dotnet test

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CloudKitchenChallenge/CloudKitchenChallenge.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final

# Needed to run the test stage by default. Uncomment to disable running unit tests
COPY --from=test /src/runThisStageDefault.txt .
ENV SERVER_TEST="true"
ENV UTC_OFFSET=0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CloudKitchenChallenge.dll", "500000", "4000000", "8000000"]
