# --- Fase 1: Compilación (Build Stage) ---
# Usamos la imagen oficial de Microsoft con el SDK completo de .NET 8
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos el archivo .csproj y restauramos las dependencias
COPY ["SistemaGestionActivos.csproj", "."]
RUN dotnet restore "./SistemaGestionActivos.csproj"

# Copiamos el resto del código fuente
COPY . .

# Publicamos la aplicación en modo Release en la carpeta /app/publish
RUN dotnet publish "SistemaGestionActivos.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Fase 2: Ejecución (Final Stage) ---
# Usamos la imagen ligera que solo contiene el runtime de ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Exponemos el puerto 80, que Render usará
EXPOSE 80

# Comando final para iniciar la aplicación
ENTRYPOINT ["dotnet", "SistemaGestionActivos.dll"]