FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies

COPY \*.csproj ./
RUN dotnet restore

# Copy all remaining source files and build

COPY . .
RUN dotnet publish -c Release -o /app/publish

# Final runtime image

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port and set ASPNETCORE_URLS for dynamic cloud port binding

ENV PORT=8080
EXPOSE 8080

ENTRYPOINT \["dotnet", "ProyectoUnisinu.dll"\]