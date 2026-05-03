# HOWTO — Guía de desarrollo y releases

## Flujo de desarrollo

1. **Cambios en el código** → editar archivos en `rgb2cmyk-gui/src/`
2. **Compilación** → `source setup.sh && ./build.sh`
3. **Verificación local** → `./run.sh` (en Linux)
4. **Distribución** → subir los binarios como GitHub Release

## Estructura del proyecto

```
rgb2cmyk/
├── README.md                    # Documentación pública
├── AGENTS.md                    # Notas para agentes IA (OpenCode)
├── HOWTO.md                     # Este archivo
├── img2cmyk.sh                  # Script CLI: CMYK completo
├── img2cmyk-black-only.sh       # Script CLI: solo K
├── srgb-color-space-profile.icm # Perfil RGB fuente
├── psouncoated_v3_fogra52.icc   # Perfil CMYK destino
└── rgb2cmyk-gui/                # Aplicación gráfica
    ├── setup.sh                 # Descarga .NET SDK local
    ├── build.sh                 # Compila las 3 plataformas
    ├── run.sh                   # Ejecuta en desarrollo (Linux)
    ├── .gitignore
    ├── nuget.config
    └── src/
        ├── rgb2cmyk-gui.csproj  # Configuración del proyecto
        ├── Program.cs
        ├── App.axaml / .cs
        ├── MainWindow.axaml / .cs
        ├── Conversion/
        │   └── CmykConverter.cs # Lógica de conversión
        ├── Assets/
        │   ├── rgb2cmyk.png     # Icono
        │   ├── rgb2cmyk.ico     # Icono Windows
        │   └── Info.plist       # Metadatos macOS .app
        └── Resources/
            ├── srgb-color-space-profile.icm   # Embebido en binario
            └── psouncoated_v3_fogra52.icc     # Embebido en binario
```

## Compilación

### Requisitos previos (solo primera vez)

1. Ejecutar `source setup.sh` — descarga el SDK de .NET 8 en `.dotnet/` (local, no afecta al sistema)
2. Dependencias del sistema para ejecutar en Linux:

   ```bash
   sudo apt install libfontconfig1 libice6 libsm6
   ```

### Compilar todo

```bash
cd rgb2cmyk-gui
source setup.sh
./build.sh
```

Genera:

- `publish/linux-x64/` — carpeta con binario ELF + `run.sh`
- `publish/win-x64/` — carpeta con `.exe` + DLLs
- `publish/osx-arm64/rgb2cmyk-gui.app/` — bundle macOS
- `publish/rgb2cmyk-gui-macOS-arm64.zip` — zip del .app

### Compilar una sola plataforma

```bash
source setup.sh
unset LD_PRELOAD

# Solo Linux
dotnet publish src/rgb2cmyk-gui.csproj -c Release -r linux-x64 --self-contained -o publish/linux-x64

# Solo Windows
dotnet publish src/rgb2cmyk-gui.csproj -c Release -r win-x64 --self-contained -o publish/win-x64

# Solo macOS ARM64
dotnet publish src/rgb2cmyk-gui.csproj -c Release -r osx-arm64 --self-contained -o publish/osx-arm64
cd publish/osx-arm64 && zip -r ../rgb2cmyk-gui-macOS-arm64.zip rgb2cmyk-gui.app && cd ../..
```

## Crear un release en GitHub

### 1. Actualizar versión

Editar el número de versión en:

- `src/MainWindow.axaml` — barra de estado inferior: `Versión XXXXXXXX`
- `src/Assets/Info.plist` — `CFBundleVersion`
- `README.md` — si hay cambios en las instrucciones

### 2. Compilar

```bash
cd rgb2cmyk-gui && source setup.sh && ./build.sh
```

### 3. Empaquetar los binarios para distribución

```bash
cd rgb2cmyk-gui/publish

# Linux
tar czf rgb2cmyk-gui-linux-x64.tar.gz -C linux-x64 .

# Windows
cd win-x64 && zip -r ../rgb2cmyk-gui-win-x64.zip . && cd ..

# macOS (ya generado por build.sh)
ls -la rgb2cmyk-gui-macOS-arm64.zip
```

### 4. Crear el release

```bash
# Opción A: con gh CLI
gh release create v20260503 \
  publish/rgb2cmyk-gui-linux-x64.tar.gz \
  publish/rgb2cmyk-gui-win-x64.zip \
  publish/rgb2cmyk-gui-macOS-arm64.zip \
  --title "v20260503" \
  --notes "Primera versión pública"

# Opción B: desde la web de GitHub
# 1. Ir a Releases → Draft a new release
# 2. Crear tag (ej: v20260503)
# 3. Subir los 3 ficheros
# 4. Publicar
```

## Convenciones de versión

Usar el formato `vYYYYMMDD` (fecha de compilación). Ejemplos:

- `v20260503` — 3 de mayo de 2026
- `v20260503b` — segunda compilación el mismo día

## Solicitar cambios a OpenCode

Para pedir cambios futuros a un agente IA (OpenCode), describir:

1. **Qué** quieres cambiar (en español o inglés)
2. **Dónde** si se sabe (archivo concreto)
3. **Comportamiento esperado** vs actual

Ejemplo de buena instrucción:

> "En MainWindow.axaml.cs, añadir un botón 'Abrir carpeta de salida' que abra el directorio donde se guardó el último archivo convertido usando Process.Start()"

Ejemplo de mala instrucción:

> "Mejorar la app"

## Notas técnicas importantes

- **Magick.NET-Q8-arm64** lleva `<ExcludeAssets>compile</ExcludeAssets>` en el csproj — sin eso, da error de tipos duplicados con el paquete x64
- **SkiaSharp NoDependencies**: el csproj tiene un post-publish target que reemplaza `libSkiaSharp.so` con la versión sin dependencia en fontconfig, solo para Linux x64
- **LD_PRELOAD**: `gtk3-nocsd` crashea Magick.NET. El wrapper `run.sh` hace `unset LD_PRELOAD`
- **Info.plist** para macOS se copia desde `Assets/Info.plist` como fichero, no se genera inline
- Los perfiles ICC están como `EmbeddedResource` (para lectura programática) y los iconos como `AvaloniaResource` (para avares://). El URI del icono en XAML usa el nombre del assembly: `avares://rgb2cmyk-gui/Assets/rgb2cmyk.ico` (con guiones, no el namespace)
