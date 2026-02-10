# 日志文件说明

## 日志位置

日志文件现在存储在**程序目录**中，便于查看和管理：

```
程序目录/
├── Logs/                    # 应用程序日志
│   └── application-YYYYMMDD.log
├── CrashReports/            # 崩溃报告
│   └── CrashReport_YYYYMMDD_HHMMSS.txt
└── ExcelDatabaseImportTool.exe
```

### 开发环境
- **日志目录**: `ExcelDatabaseImportTool\bin\Debug\net8.0-windows\Logs\`
- **崩溃报告**: `ExcelDatabaseImportTool\bin\Debug\net8.0-windows\CrashReports\`

### 发布后
- **日志目录**: `[安装目录]\Logs\`
- **崩溃报告**: `[安装目录]\CrashReports\`

## 日志功能

### 自动管理
- ✅ 每天自动创建新的日志文件
- ✅ 自动清理超过 30 天的旧日志
- ✅ 单个日志文件最大 10MB，超过后自动滚动
- ✅ 应用程序启动时自动清理旧日志

### 日志内容
每条日志包含：
- **时间戳**: 精确到毫秒
- **日志级别**: INF (信息), WRN (警告), ERR (错误), CRI (严重)
- **来源**: 记录日志的类名
- **消息**: 详细的日志信息
- **异常**: 如果有错误，包含完整的堆栈跟踪

### 日志级别
- **Information**: 正常操作信息（应用启动、数据库初始化等）
- **Warning**: 警告信息（非关键错误）
- **Error**: 错误信息（操作失败但应用继续运行）
- **Critical**: 严重错误（可能导致应用崩溃）

## 崩溃报告

当应用程序发生严重错误时，会自动生成崩溃报告，包含：
- 异常详细信息
- 堆栈跟踪
- 系统信息（操作系统、内存、CPU等）
- 应用程序状态

## 查看日志

### 方法 1: 直接打开文件
在程序目录中找到 `Logs` 文件夹，使用任何文本编辑器打开 `.log` 文件。

### 方法 2: 使用记事本
```cmd
notepad Logs\application-20260210.log
```

### 方法 3: 使用 PowerShell 查看最新日志
```powershell
Get-Content .\Logs\application-*.log -Tail 50
```

## 日志保留策略

- **保留时间**: 30 天
- **最大总大小**: 100 MB
- **单文件大小**: 10 MB
- **清理时机**: 应用程序启动时

## 故障排查

如果遇到问题，请：
1. 查看最新的日志文件
2. 搜索 `[ERR]` 或 `[CRI]` 标记的错误
3. 检查 CrashReports 目录中的崩溃报告
4. 将相关日志提供给技术支持

## 注意事项

⚠️ **重要**: 
- 日志文件可能包含敏感信息（在 DEBUG 模式下）
- 分享日志前请检查是否包含敏感数据
- 定期备份重要的日志文件
