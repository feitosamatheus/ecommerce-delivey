FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Ecommerce.MVC/Ecommerce.MVC.csproj Ecommerce.MVC/
RUN dotnet restore Ecommerce.MVC/Ecommerce.MVC.csproj

COPY . .
WORKDIR /src/Ecommerce.MVC
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 🔥 porta fixa
ENV ASPNETCORE_URLS=http://+:8000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ecommerce.MVC.dll"]
