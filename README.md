## 🚀 Pasos de instalación

### 1. Clonar el repositorio
```bash
git clone https://github.com/tu_usuario/tu_repositorio.git
```

### 2. Descargar archivos adicionales
Descarga los archivos pesados desde el siguiente enlace de Google Drive:
[🔗 Archivos necesarios (resources y plugins)](https://drive.google.com/drive/folders/1Cd4vT89S7XLLWE1DgatG8zvlWynwcqMa?usp=drive_link)

Copia las carpetas resources y plugins.

Pégalos dentro de la ruta:

Assets > Alteruna

### 3. Instalar el paquete de MediaPipe
Descarga el paquete Unity de MediaPipe (versión v0.16.1) desde:

[🔗 MediaPipeUnityPlugin v0.16.1](https://github.com/homuler/MediaPipeUnityPlugin/releases/tag/v0.16.1)

Luego, impórtalo en tu proyecto Unity:

Assets > Import Package > Custom Package

### 4. Modificar archivo VisionTaskApiRunner.cs
Accede al archivo:

Assets > MediaPipeUnity > Samples > Common > Scripts > VisionTaskApiRunner.cs
Busca la línea:

[SerializeField] protected Screen screen;
Y agrega justo debajo:

[SerializeField] protected Screen2D screen2D;
