# RePKG-GUI

基于 **Python + Flask + pywebview** 的 Wallpaper Engine 创意工坊内容提取工具。支持四种项目类型的批量提取、输出目录管理、桌面窗口化运行。

## 使用指南

### 1. 路径配置

首次使用需配置输入/输出路径：

1. 打开 **设置** 页面
   
3. 点击 **自动检测 Steam 路径**，程序将自动识别 Wallpaper Engine 的创意工坊目录和我的项目目录
4. 也可手动填写路径

| 路径 | 说明 | 典型位置 |
|------|------|----------|
| 输入路径 | Wallpaper Engine 创意工坊内容目录 | `Steam\steamapps\workshop\content\431960` |
| 输出路径 | 提取后的项目输出位置 | `Steam\steamapps\common\wallpaper_engine\projects\myprojects` |

### 2. 扫描项目

路径配置完成后，点击 **扫描** 按钮。程序将遍历输入目录，解析每个项目的 `project.json` 获取：

- 项目名称与描述
- 类型（scene/video/web/application）
- 预览图
- 标签、版本号等信息

### 3. 提取项目

- **单个提取**：点击卡片上的提取按钮，或右键 → 提取
- **批量提取**：勾选多个项目卡片，点击顶部操作栏的"提取选中"
- **全选/取消**：点击操作栏的全选复选框

提取时 scene 类型会调用 RePKG.exe 解包 `.pkg` 文件，其余类型直接复制。进度条和日志实时更新。

### 4. 输出目录管理

切换到 **输出目录** 视图：

- 查看已提取的项目列表
- 单个或批量删除已提取的项目
- 支持搜索与类型筛选

### 5. 搜索与筛选

输入目录和输出目录均支持：

- **关键词搜索**：输入项目名称实时过滤
- **类型筛选**：按 scene / video / web / application 筛选
- **组合使用**：搜索和筛选可同时生效

### 6. 外观设置

在设置页面的 **外观设置** 中：

- 上传自定义背景图片（支持 PNG / JPG / GIF / WebP / BMP）
- 从背景库中选择已有图片
- 调整背景透明度和模糊度
- 删除不需要的背景图片

### 7. 提取选项

| 选项 | 说明 |
|------|------|
| 转换 TEX 纹理 | 将 `.tex` 文件转换为通用图片格式 |
| 复制项目文件 | 将 `project.json` 等配置文件一并复制 |
| 覆盖已有文件 | 提取时覆盖输出目录中已存在的同名文件 |
| 递归搜索子目录 | 扫描时递归遍历所有子文件夹 |
| 复制预览图像 | 将项目预览图复制到输出目录 |

### 8.提取后的文件

若使用了Steam路径检测的结果作为输出路径，则打开WallpaperEngine，在筛选的来源中选择：“我的壁纸”，即可查看到转换后的项目。

（通常一个壁纸是由多个图层和特效组合而成的，因此转换后的结果并不会是一个独立的图片PNG类或视频MP4类文件，当然，相关的结果都可以在项目文件夹内去查找）

转换后的项目是支持通过WallpaperEngine的“在编辑器中打开”进行相关的用户配置、图像、特效修改的。

## 项目结构

```
RePKG-GUI/
├── app.py                        # Flask 应用入口
├── config.json                   # 用户配置文件
├── requirements.txt              # Python 依赖
├── RePKG-GUI.spec                # PyInstaller 打包配置
├── installer.iss                 # Inno Setup 6 安装脚本
├── build.bat                     # 一键构建脚本
├── backend/                      # 后端模块
│   ├── __init__.py
│   ├── api.py                    # API 路由处理（业务逻辑）
│   ├── config.py                 # 配置读写管理
│   ├── executor.py               # RePKG 执行器（提取任务线程）
│   ├── paths.py                  # 路径工具（开发/打包环境统一）
│   ├── scanner.py                # 文件扫描器（项目发现与解析）
│   ├── server.py                 # 服务器启动（Flask + pywebview）
│   └── steam.py                  # Steam 路径检测
├── static/                       # 前端静态资源
│   ├── assets/
│   │   ├── RePKG.exe             # RePKG 解包工具
│   │   └── images/
│   │       ├── icon.ico          # 应用图标
│   │       └── background/       # 背景图片库
│   ├── css/
│   │   └── custom.css            # 自定义样式
│   └── js/
│       └── app.js                # Alpine.js 前端逻辑
└── templates/                    # HTML 模板
    ├── base.html                 # 主页面（双视图面板）
    ├── index.html                # 索引页
    └── settings.html             # 设置页
```
## 致谢

- [RePKG](https://github.com/notscuffed/RePKG) — Wallpaper Engine `.pkg` 解包工具


## 许可证

[MIT License](LICENSE)
