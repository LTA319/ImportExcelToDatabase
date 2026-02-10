# Excel Database Import Tool

一个用于将 Excel 文件导入到数据库的 WPF 应用程序。

## 项目结构

该解决方案包含两个项目：

### ExcelDatabaseImportTool
主应用程序项目，包含：
- WPF 用户界面
- 业务逻辑和服务
- 数据模型和仓储
- 数据库连接管理

### ExcelDatabaseImportTool.Tests
测试项目，包含：
- 属性测试（Property-Based Tests）
- 单元测试
- 集成测试

## 构建和运行

### 构建整个解决方案
```bash
cd ExcelDatabaseImportTool
dotnet build ExcelDatabaseImportTool.sln
```

### 运行应用程序
```bash
cd ExcelDatabaseImportTool
dotnet run
```

或者直接运行编译后的可执行文件：
```
ExcelDatabaseImportTool\bin\Debug\net8.0-windows\ExcelDatabaseImportTool.exe
```

### 运行测试
```bash
cd ExcelDatabaseImportTool.Tests
dotnet test
```

或者从解决方案级别运行所有测试：
```bash
cd ExcelDatabaseImportTool
dotnet test ExcelDatabaseImportTool.sln
```

## 技术栈

- .NET 8.0
- WPF (Windows Presentation Foundation)
- Entity Framework Core (SQLite)
- EPPlus (Excel 文件处理)
- NUnit + FsCheck (测试框架)
- MySQL 和 SQL Server 支持

## 功能特性

1. **数据库配置管理** - 管理多个数据库连接配置
2. **导入配置** - 配置字段映射和外键关系
3. **Excel 导入** - 将 Excel 数据导入到数据库
4. **导入历史** - 查看导入日志和错误信息
5. **数据验证** - 验证数据类型和约束
6. **事务管理** - 确保数据完整性

## 开发说明

- 主项目不包含测试代码，保持清洁
- 所有测试代码都在独立的测试项目中
- 使用属性测试（Property-Based Testing）确保代码正确性
