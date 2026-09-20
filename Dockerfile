FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["CompanyMng/CompanyMng.csproj", "CompanyMng/"]
RUN dotnet restore "CompanyMng/CompanyMng.csproj"

COPY . .

WORKDIR "/src/CompanyMng"
RUN dotnet publish "CompanyMng.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CompanyMng.dll"]