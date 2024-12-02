# Stage 1: Base image with runtime dependencies
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080 8081

# Define a non-root user for security
ARG APP_UID=1001
RUN adduser --uid $APP_UID --disabled-password --gecos "" appuser && \
    chown -R appuser /app
USER appuser

# Stage 2: Build image with SDK for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["api.csproj", "./"]
RUN dotnet restore "api.csproj"

# Copy all source files and build the application
COPY . .
RUN dotnet build "api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final image with only runtime dependencies
FROM base AS final
WORKDIR /app

# Copy the published application from the previous stage
COPY --from=publish /app/publish .

# Optimize for containerized environments
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Start the application
ENTRYPOINT ["dotnet", "api.dll"]
