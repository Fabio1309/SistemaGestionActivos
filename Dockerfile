# Etapa 1: Compilar y publicar la aplicación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo .csproj y restaurar las dependencias primero
COPY ["SistemaGestionActivos.csproj", "."]
RUN dotnet restore "./SistemaGestionActivos.csproj"

# Copiar el resto del código fuente y construir
COPY . .
WORKDIR "/src/."
RUN dotnet build "SistemaGestionActivos.csproj" -c Release -o /app/build

# Publicar la aplicación
FROM build AS publish
RUN dotnet publish "SistemaGestionActivos.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Crear la imagen final de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Exponer el puerto que usa ASP.NET Core
EXPOSE 80
EXPOSE 443

# El ENTRYPOINT se asegura de que la aplicación se inicie.
# Las variables de entorno definidas en el dashboard de Render se pasarán automáticamente a este proceso.
ENTRYPOINT ["dotnet", "SistemaGestionActivos.dll"]