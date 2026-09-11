# RePKG Workbench

[简体中文](README.zh-CN.md) | English

RePKG Workbench is a Windows desktop application that provides a native WPF interface for RePKG, allowing users to browse, extract, and manage local Wallpaper Engine Workshop projects.

![RePKG Workbench home screen](image/程序首页.png)

## Highlights

- Finds Wallpaper Engine projects automatically or scans a directory you choose
- Recognizes scene, video, web, and application projects
- Displays metadata and static, animated GIF, or muted looping video previews
- Searches and filters projects, with individual or batch selection
- Extracts scene packages through the bundled RePKG command-line tool
- Copies video, web, and application projects into a usable output structure
- Shows live progress and detailed task logs, with cancellation support
- Manages extracted projects in a separate output view
- Supports English and Simplified Chinese (additional languages can be added by creating a JSON configuration in the `Languages` directory), light/dark themes, and custom backgrounds
- Stores settings locally under `%LocalAppData%\RePKG-Workbench`

## Download

Choose one of the packages on the project Releases page:

- **`RePKG-Workbench-Setup-x64.exe`** — standard Windows installer. Requires the .NET 10 Desktop Runtime (x64); the installer can direct you to Microsoft if it is missing.
- **`RePKG-Workbench-Portable-x64.zip`** — recommended portable package. It includes the required .NET runtime and works without a separate .NET installation.
- **`RePKG-Workbench-Portable-FDD-x64.zip`** — smaller portable package for computers that already have the .NET 10 Desktop Runtime (x64).

All current packages target 64-bit Windows. If you are unsure which package to use, choose the self-contained **Portable** package.

## Quick start

1. Install the application, or extract the portable archive to a writable directory.
2. Start `RePKG-Workbench.exe`.
3. Set the **Input directory** to your Wallpaper Engine Workshop content folder. A common location is:

   ```text
   C:\Program Files (x86)\Steam\steamapps\workshop\content\431960
   ```

   Use **Detect Steam paths** if Wallpaper Engine is installed in a detected Steam library.
4. Set the **Output directory** to the folder where extracted projects should be created. The Wallpaper Engine **My Projects** directory is recommended; **Detect Steam paths** fills in this location automatically.

   Save the settings after completing the configuration.
5. Keep the bundled `RePKG.exe` detected by the application, or select another RePKG executable that you provide.
6. Choose the extraction options you need and click **Scan / Refresh**.
7. Select one or more projects, then click **Extract selected**.

### 1. Scan and preview

Project cards show the preview, title, type, size, and relevant paths. Use search and the type filter to narrow the list.

![Scanned Wallpaper Engine projects](image/扫描结果.png)

### 2. Extract projects

Select the required cards and start extraction. The task log reports each operation and its result.

> Other methods:
>
> - Right-click a project card to extract that project.
> - Drag a complete project folder, or a folder containing multiple projects, into the application window. Extraction starts automatically after the folders are dropped.

![Project extraction and task log](image/执行.png)

### 3. Manage output

Switch to **Output projects** to review the extracted results. Deleting a project here permanently removes its output directory, so check the selection carefully.

![Extracted project management](image/输出目录结果.png)

## Extraction options

- **Scan subdirectories** — searches recursively for project folders containing `project.json`.
- **Convert TEX textures** — converts supported Wallpaper Engine TEX textures while extracting scene projects.
- **Copy project.json** — keeps the source project metadata in the output.
- **Copy alternate preview images** — copies additional preview images when available.
- **Overwrite existing output** — replaces files in an existing output project; leave this disabled if existing output must be preserved.

Scene projects are unpacked with RePKG. Video, web, and application projects are copied to the output directory rather than unpacked as scene packages. The default options are suitable for most users; refer to the RePKG project for details.

## Requirements and troubleshooting

### Windows reports that .NET 10 Desktop Runtime is required

The installer and FDD portable package need the **.NET 10 Desktop Runtime (x64)**. This is different from the .NET Framework included with Windows. Install the x64 Desktop Runtime from Microsoft, or use `RePKG-Workbench-Portable-x64.zip`, which includes it.

### No projects are found

- Confirm that the input directory contains project folders with a `project.json` file.
- Enable **Scan subdirectories** when scanning the Workshop root.
- Wallpaper Engine Workshop content normally uses Steam app ID `431960`.
- Try **Detect Steam paths** if the Steam library is on another drive.

### A scene project cannot be extracted

- Confirm that the configured `RePKG.exe` exists and can be opened.
- Check the expanded task log for the package name and RePKG error output.
- Try extracting to a writable directory with sufficient free space.

### No wallpaper image appears in the output directory

Scene projects are often assembled from multiple assets using tools such as Spine, so **a separate, complete wallpaper image may not exist**. Open the project in the Wallpaper Engine editor through **My Wallpapers** to view it.

![Open a project in the Wallpaper Engine editor](image/在编辑器中打开.png)
