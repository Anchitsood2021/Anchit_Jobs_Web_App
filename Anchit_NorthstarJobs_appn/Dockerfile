FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/NorthstarJobs/NorthstarJobs.csproj src/NorthstarJobs/
RUN dotnet restore src/NorthstarJobs/NorthstarJobs.csproj
COPY src/NorthstarJobs/ src/NorthstarJobs/
RUN dotnet publish src/NorthstarJobs/NorthstarJobs.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /app/App_Data/keys /app/App_Data/mail && chown -R app:app /app/App_Data
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "NorthstarJobs.dll"]
