# UnityBinaryFileRewriter

## 介绍
本工具用于重写Unity引擎的二进制文件，在没有源码的情况下，可以用它实现一些功能、修复一些问题

## 使用
1. 引用包com.modx.enginebinaryfilerewriter，参考格式：https://github.com/AlanLiu90/UnityBinaryFileRewriter.git?path=Packages/com.modx.enginebinaryfilerewriter#v0.5.0
2. 在 Project Settings -> Engine Binary File Rewriter 中进行配置。配置方式可参考[文章](https://alanliu90.hatenablog.com/entry/2025/04/23/Unity%E4%B8%AD%E5%85%A8%E5%B1%80%E7%A6%81%E7%94%A8AssetBundle%E7%9A%84%E5%85%BC%E5%AE%B9%E6%80%A7%E6%A3%80%E6%9F%A5)

### 支持说明
支持Unity 2019~2022
* Android
  * 只支持ARMv7、ARM64
  * 支持直接构建apk/aab，也支持先导出Gradle工程，再构建apk/aab
* iOS
  * 只支持在设备上运行的版本，不支持模拟器版本
  * 支持在Windows、Mac和Linux上导出Xcode工程
    * 2019只支持在Mac上导出

## 已配置功能
项目可以将`ProjectSettings\EngineBinaryFileRewriterSettings.asset`拷贝到自己的工程中，并启用需要的功能

工程中只包含特定Unity版本的配置，但其他版本有可能可以直接使用这些配置。
可以将配置中的版本改成项目使用的Unity版本，然后运行本工程的单元测试（Window -> General -> Test Runner）验证工具是否能正常工作

### 禁用AssetBundle的兼容性检查
HybridCLR要求AssetBundle开启TypeTree或者禁用AssetBundle的兼容性检查，但Unity只对异步加载提供了接口禁用（[文档](https://hybridclr.doc.code-philosophy.com/en/docs/basic/monobehaviour)）：
```cs
AssetBundleCreateRequest req = AssetBundle. LoadFromFileAsync(path);
req.SetEnableCompatibilityChecks(false); // Non-public, needs to be called by reflection
```
本工具可以全局禁用掉AssetBundle的兼容性检查

工程中包含了Android、iOS的开发版本和发布版本的配置。已测试版本：
* 2022.3.60
* 2021.3.45
* 2020.3.48
* 2019.4.40

#### 在设备上测试配置的正确性（以Android为例，iOS类似）
1. 在Project Settings -> Engine Binary File Rewriter中，开启这个功能
2. 执行菜单项Build -> Android
3. 使用Android Studio打开Gradle工程，构建并安装到设备上。运行时可以看到一条日志：`[InstantiateByAsset] text:..., 这个脚本通过挂载到资源的方式实例化`
4. 修改Assets\HotUpdate\InstantiateByAsset.cs，注释第一行代码
5. 执行菜单项Build -> Android_Assets
6. 在Android Studio中，重新构建、安装。运行时可以看到新的日志：`[InstantiateByAsset] text:..., 这个脚本通过挂载到资源的方式实例化, count: 0`

### 修复异步上传导致线程卡死的bug
卡死时的调用栈示例（有其他的可能性，主要看是否是卡在`AsyncResourceUploadBlocking`里）：
```
#1  0x0000000190f53370 in _dispatch_sema4_wait ()
#2  0x0000000190f53a20 in _dispatch_semaphore_wait_slow ()
#3  0x000000011bceda44 in DarwinApi::detail::Acquire(UnityClassic::Baselib_SystemSemaphore_Handle, unsigned long long) [inlined] at /Users/bokken/build/output/unity/unity/External/baselib/builds/Source/Darwin/Baselib_SystemSemaphore_DarwinApi.inl.h:36
#4  0x000000011bceda38 in DarwinApi::Baselib_SystemSemaphore_Acquire(UnityClassic::Baselib_SystemSemaphore_Handle) [inlined] at /Users/bokken/build/output/unity/unity/External/baselib/builds/Source/Darwin/Baselib_SystemSemaphore_DarwinApi.inl.h:62
#5  0x000000011bceda38 in UnityClassic::Baselib_SystemSemaphore_Acquire(UnityClassic::Baselib_SystemSemaphore_Handle) at /Users/bokken/build/output/unity/unity/External/baselib/builds/Source/CProxy/Baselib_SystemSemaphore_CProxy.inl.h:19
#6  0x000000011b05c7bc in Baselib_CappedSemaphore_Acquire [inlined] at /Users/bokken/build/output/unity/unity/External/baselib/builds/Include/C/Internal/Baselib_CappedSemaphore_SemaphoreBased.inl.h:44
#7  0x000000011b05c7b4 in ::WaitForSignal() at /Users/bokken/build/output/unity/unity/Runtime/Threads/CappedSemaphore.h:29
#8  0x000000011b05c674 in ::AsyncResourceUploadBlocking() at /Users/bokken/build/output/unity/unity/Runtime/Graphics/AsyncUploadManager.cpp:524
#9  0x000000011b9312e0 in ::SyncAsyncResourceUpload() at /Users/bokken/build/output/unity/unity/Runtime/GfxDevice/GfxDevice.cpp:2320
#10 0x000000011bac2a44 in ::RunCommand() at /Users/bokken/build/output/unity/unity/Runtime/GfxDevice/threaded/GfxDeviceWorker.cpp:2521
#11 0x000000011bb6cc34 in GfxDeviceWorkerAutoreleasePoolProxy at /Users/bokken/build/output/unity/unity/Runtime/GfxDevice/metal/GfxDeviceMetal.mm:5876
#12 0x000000011babda08 in ::RunExt() at /Users/bokken/build/output/unity/unity/Runtime/GfxDevice/threaded/GfxDeviceWorker.cpp:375
#13 0x000000011babd928 in ::Run() at /Users/bokken/build/output/unity/unity/Runtime/GfxDevice/threaded/GfxDeviceWorker.cpp:353
#14 0x000000011babd5f0 in ::RunGfxDeviceWorker() at /Users/bokken/build/output/unity/unity/Runtime/GfxDevice/threaded/GfxDeviceWorker.cpp:332
#15 0x000000011b22562c in ::RunThreadWrapper() at /Users/bokken/build/output/unity/unity/Runtime/Threads/Thread.cpp:112
#16 0x00000001e5afe06c in _pthread_start ()
```

本工具通过调整信号量类中一个函数的指令规避这个问题，但是有性能损失

工程中包含了Android、iOS的发布版本的配置。已测试版本：
* 2022.3.60
* 2022.3.20

#### 在设备上测试配置的正确性
1. 使用[工程](https://discussions.unity.com/t/android-build-project-freezes-after-5-minutes-with-playerloop-in-profiler-at-60-000-ms/784527/337)在设备上复现问题
2. 集成工具，确定问题不再出现

## Todo
* 支持更多的Unity版本（2023、6+）
