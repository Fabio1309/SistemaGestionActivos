# --- Etapa 1: Build ---
# Usamos la imagen oficial del SDK de .NET 9 para compilar la aplicación.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiamos los archivos de proyecto (.csproj) y restauramos las dependencias primero.
# Esto aprovecha el caché de Docker y acelera las compilaciones futuras.
COPY ["SistemaGestionActivos.csproj", "./"]
RUN dotnet restore "./SistemaGestionActivos.csproj"

# Copiamos el resto del código fuente.
COPY . .
WORKDIR "/src/."

# Publicamos la aplicación en modo Release. La salida estará en /app/publish.
RUN dotnet publish "SistemaGestionActivos.csproj" -c Release -o /app/publish

# --- Etapa 2: Final ---
# Usamos una imagen más ligera que solo contiene el runtime de ASP.NET Core.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exponemos el puerto 80 del contenedor. Render se encargará de mapearlo.
EXPOSE 80

# Definimos el punto de entrada para ejecutar la aplicación cuando el contenedor inicie.
ENTRYPOINT ["dotnet", "SistemaGestionActivos.dll"]