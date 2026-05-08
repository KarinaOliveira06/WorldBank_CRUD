FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["WorldBank_CRUD.csproj", "./"]
RUN dotnet restore "WorldBank_CRUD.csproj"

COPY . .
RUN dotnet publish "WorldBank_CRUD.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WorldBank_CRUD.dll"]