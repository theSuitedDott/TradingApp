FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY ["TradingApp.csproj", "./"]
RUN dotnet restore "./TradingApp.csproj"

# copy everything and publish
COPY . .
WORKDIR /src
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5181
EXPOSE 5181
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TradingApp.dll"]

