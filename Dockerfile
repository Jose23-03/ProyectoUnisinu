FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ProyectoUnisinu.csproj", "./"]
RUN dotnet restore "ProyectoUnisinu.csproj"
COPY . .
RUN dotnet publish "ProyectoUnisinu.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProyectoUnisinu.dll"]
