# =========================
# STAGE 1 - Build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia o csproj e restaura
COPY Ecommerce.MVC/Ecommerce.MVC.csproj Ecommerce.MVC/
RUN dotnet restore Ecommerce.MVC/Ecommerce.MVC.csproj

# Copia o restante do código
COPY . .
WORKDIR /src/Ecommerce.MVC

RUN dotnet publish -c Release -o /app/publish

# =========================
# STAGE 2 - Runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Ecommerce.MVC.dll"]
