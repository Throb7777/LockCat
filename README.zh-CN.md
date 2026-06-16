# LockCat

![LockCat 像素标题](src/LockPig/Assets/Pixel/target-logo.png)

你好，我是 LockCat，一只会待在 Windows 桌面上的像素小猫。我平时安安静静，偶尔动动尾巴；需要暂时锁住键盘和屏幕的时候，我会认真值班。

![LockCat 桌面小猫](src/LockPig/Assets/Cat/Sprites/cat-idle.png)

[English](README.md) | [日本語](README.ja.md) | [提交问题](https://github.com/Throb7777/LockCat/issues/new/choose)

## 我能做什么

- 用快捷键暂时锁住键盘。
- 锁定时可以顺手关闭屏幕。
- 用另一个快捷键恢复。
- 待在系统托盘里，随时可以找到。
- 在桌面上显示一只像素小猫。
- 记住你的快捷键、语言和小猫设置。
- 安装和卸载时只管理自己的 `LockCat` 文件夹。

## 系统要求

- Windows 10 或更新版本。
- x64 Windows。
- 日常使用不需要账号，也不需要联网。

## 下载和安装

1. 从 [GitHub Releases](https://github.com/Throb7777/LockCat/releases) 下载 `LockCatInstaller.exe`。
2. 运行安装器。
3. 如果需要，可以修改安装位置。
4. 选择是否登录系统后自动运行 LockCat。
5. 完成安装后，我会待在托盘里；如果开启桌面小猫，我也会出现在桌面上。

你选择安装位置时，我会自动在里面创建自己的 `LockCat` 文件夹：

```text
你选择：D:\Apps
实际安装到：D:\Apps\LockCat
```

这样我的文件都会住在自己的小窝里，不会散落到你的文件夹中。

## 默认快捷键

- 锁定：`Ctrl + Alt + K`
- 恢复：`Ctrl + Alt + P`

这两个快捷键都可以在设置里重新指定。

## 桌面小猫怎么用

桌面小猫不仅是装饰，也是一扇小入口。

- 单击我，我会回应一下。
- 连续点击我 3 次，进入锁定。前两次我会先弹出提示，让你确认这次要不要锁定。
- 右键我，打开快捷菜单。
- 按住拖动我，可以把我放到更顺眼的位置。
- 如果把我隐藏了，可以从托盘菜单或设置里重新显示。
- 如果你希望我一直在其他窗口上方，可以开启“小猫保持置顶”。

## 设置里可以调整什么

可以从系统托盘或小猫右键菜单打开设置。

你可以调整：

- 锁定快捷键。
- 恢复快捷键。
- 锁定时是否关闭屏幕。
- 是否显示桌面小猫。
- 小猫是否保持置顶。
- 是否开机自启动。
- 界面语言。

## 卸载说明

可以从开始菜单的卸载入口，或者运行安装目录里的 `LockCatUninstaller.exe`。

小猫认真保证：卸载器只会删除 LockCat 自己安装的文件，不会因为你安装时选过某个文件夹，就把整个文件夹清掉。如果选择保留设置，下次安装时还可以继续使用原来的快捷键和偏好。

## 隐私说明

LockCat 不会读取你输入的具体内容。它只监听你设置的快捷键，并使用一些简单的活动状态来驱动桌面小猫动画。设置保存在你自己的电脑上。

## 从源码构建

先安装 .NET 8 SDK，然后运行：

```powershell
dotnet build src\LockPig\LockPig.csproj -c Release
dotnet build src\LockCat.Installer\LockCat.Installer.csproj -c Release
dotnet build src\LockCat.Uninstaller\LockCat.Uninstaller.csproj -c Release
dotnet run --project qa-uninstall-safety\LockCatUninstallSafetyProbe.csproj -c Release
```

主程序在 `src/LockPig`，安装器和卸载器分别在 `src/LockCat.Installer`、`src/LockCat.Uninstaller`。

## 反馈

如果你发现哪里不对劲，或者希望我学会新的小动作，可以在这里提交：

https://github.com/Throb7777/LockCat/issues/new/choose

我会带着小本本认真看。

## 许可证

LockCat 使用 MIT License 开源。第三方字体和素材说明见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。
