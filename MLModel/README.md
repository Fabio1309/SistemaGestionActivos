Instrucciones rápidas

1) Desde PowerShell en la raíz del repo (donde está `SistemaGestionActivos.sln`) ejecutar:

   dotnet restore
   dotnet add MLModel package Microsoft.ML
   dotnet run --project MLModel

2) El programa leerá `ot-data.csv` (ruta relativa `..\\ot-data.csv` desde la carpeta `MLModel`), entrenará un clasificador multicategoría y guardará `Model.zip` en la carpeta del proyecto.

3) Si hay errores al ejecutar, copia aquí la salida completa del terminal y lo reviso.
