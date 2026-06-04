FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Confeccao.sln ./
COPY src/Confeccao.Api/Confeccao.Api.csproj src/Confeccao.Api/
COPY src/Confeccao.Domain/Confeccao.Domain.csproj src/Confeccao.Domain/
COPY tests/Confeccao.UnitTests/Confeccao.UnitTests.csproj tests/Confeccao.UnitTests/
RUN dotnet restore src/Confeccao.Api/Confeccao.Api.csproj

COPY . ./
RUN dotnet publish src/Confeccao.Api/Confeccao.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Confeccao.Api.dll"]
