# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia el archivo del proyecto y restaura dependencias
COPY ["BarcodeShippingSystem.csproj", "."]
RUN dotnet restore "BarcodeShippingSystem.csproj"

# Copia todo el código fuente
COPY . .

# Build del proyecto
RUN dotnet build "BarcodeShippingSystem.csproj" -c Release -o /app/build

# Publicar la aplicación
RUN dotnet publish "BarcodeShippingSystem.csproj" -c Release -o /app/publish

# Etapa 2: Runtime (imagen más pequeña)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copia los archivos publicados desde la etapa de build
COPY --from=build /app/publish .

# Variables de entorno por defecto (Render las sobreescribirá)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://*:8080

# Expone el puerto que usa Render
EXPOSE 8080

# Comando de inicio
ENTRYPOINT ["dotnet", "BarcodeShippingSystem.dll"]