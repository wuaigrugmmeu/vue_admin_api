# 用户权限管理系统 (UserPermissionSystem) 项目结构分析

## 项目概述

该项目是一个基于.NET 8.0开发的用户权限管理系统，采用领域驱动设计(DDD)架构风格实现。系统提供了用户、角色、权限和菜单的管理功能，支持基于JWT的身份验证和基于权限的授权控制。

## 技术栈

- **框架**：.NET 8.0
- **数据库**：SQLite
- **ORM**：Entity Framework Core 8.0
- **认证方式**：JWT (JSON Web Token)
- **API文档**：Swagger / OpenAPI
- **架构模式**：领域驱动设计 (DDD)
- **依赖注入**：.NET内置DI容器

## 项目架构

项目采用了经典的DDD分层架构：

### 1. 领域层 (Domain)

包含业务领域模型、领域服务和核心业务逻辑。

#### 主要组件：

- **实体 (Entities)**
  - User：用户实体
  - Role：角色实体
  - Permission：权限实体
  - Menu：菜单实体
  - UserRole：用户角色关联实体
  - RolePermission：角色权限关联实体

- **值对象 (Value Objects)**
  - Password：密码值对象，处理密码的加密和验证

- **聚合根 (Aggregate Roots)**
  - 实现IAggregateRoot接口的实体：User, Role, Menu

- **领域事件 (Domain Events)**
  - 用户相关事件：UserCreatedDomainEvent, UserUpdatedDomainEvent等
  - 角色相关事件：RoleCreatedDomainEvent, RoleUpdatedDomainEvent等
  - 权限相关事件：PermissionCreatedDomainEvent, PermissionUpdatedDomainEvent等
  - 菜单相关事件：MenuCreatedDomainEvent, MenuUpdatedDomainEvent等

- **领域服务 (Domain Services)**
  - IAuthDomainService：认证领域服务

- **领域异常 (Domain Exceptions)**
  - DomainException：领域异常基类

### 2. 应用层 (Application)

处理用例和协调领域层与基础设施层。

#### 主要组件：

- **命令 (Commands)**
  - LoginCommand：登录命令
  - PasswordCommands：密码相关命令

- **查询 (Queries)**
  - UserQuery：用户查询

- **CQRS框架**
  - ICommand：命令接口
  - IQuery：查询接口
  - IDispatcher：调度器接口
  - Dispatcher：调度器实现

- **服务 (Services)**
  - IAuthService：认证服务接口
  - AuthService：认证服务实现

### 3. 基础设施层 (Infrastructure)

处理技术实现细节，如数据库访问、身份验证等。

#### 主要组件：

- **持久化 (Persistence)**
  - AppDbContext：EF Core数据上下文
  - Repository：通用仓储实现
  - UnitOfWork：工作单元实现

- **认证 (Authentication)**
  - JwtSettings：JWT配置

- **服务 (Services)**
  - AuthDomainService：认证领域服务实现
  - AuthService：认证服务实现

- **数据初始化 (Seeders)**
  - DatabaseSeeder：数据库初始化种子类

### 4. 表现层 (Presentation/API)

处理用户交互，包括API控制器和数据传输对象。

#### 主要组件：

- **控制器 (Controllers)**
  - AuthController：认证控制器
  - UsersController：用户控制器
  - RolesController：角色控制器
  - PermissionsController：权限控制器
  - MenusController：菜单控制器

- **数据传输对象 (DTOs)**
  - AuthDTOs：认证相关DTO
  - UserDTOs：用户相关DTO
  - RoleDTOs：角色相关DTO
  - PermissionDTOs：权限相关DTO
  - MenuDTOs：菜单相关DTO

## 权限设计

系统采用了基于代码的权限设计，主要权限模块包括：

1. **用户管理**：user:list, user:read, user:create, user:update, user:delete, user:assignRoles, user:resetPassword
2. **角色管理**：role:list, role:read, role:create, role:update, role:delete, role:assignPermissions
3. **权限管理**：permission:list
4. **菜单管理**：menu:list, menu:read, menu:create, menu:update, menu:delete

权限代码遵循 `模块:操作` 的命名规则，通过角色-权限关联赋予用户相应的访问控制。

## 身份验证与授权

### 身份验证

- 使用JWT (JSON Web Token)进行身份验证
- 支持用户名/密码登录方式
- Token包含用户ID、用户名和权限信息

### 授权

- 基于声明的授权策略
- 每个API接口通过[Authorize]特性和权限策略进行保护
- 权限策略格式："RequirePermission:{permissionCode}"

## 菜单管理

- 支持树形菜单结构，包含父子菜单关系
- 菜单可以关联权限代码，实现动态权限控制
- 支持菜单的可见性控制
- 提供用户可访问菜单树的API

## 数据初始化

系统启动时通过DatabaseSeeder类初始化基础数据：

1. 创建基本权限
2. 创建管理员和普通用户角色
3. 创建系统管理员用户(admin/admin123)
4. 创建基本菜单结构

## API接口

系统集成了Swagger，提供了完整的API文档和测试界面，主要API包括：

1. **认证相关**：登录、获取用户信息、修改密码
2. **用户管理**：用户CRUD、分配角色、重置密码
3. **角色管理**：角色CRUD、分配权限
4. **权限管理**：获取权限列表
5. **菜单管理**：菜单CRUD、获取菜单树

## 部署与运行

项目使用SQLite作为数据库，便于部署和演示。在首次运行时会自动创建数据库并初始化系统数据。

### 默认账号

- 用户名：admin
- 密码：admin123

## 项目特点

1. **领域驱动设计**：清晰的分层架构，富领域模型
2. **工厂方法模式**：实体通过工厂方法创建，确保数据一致性
3. **领域事件**：核心业务操作触发领域事件，支持事件驱动架构
4. **CQRS模式**：命令和查询分离，提高性能和可维护性
5. **策略模式**：灵活的权限策略配置
6. **资源库模式**：通用Repository实现领域对象的持久化

## 进一步优化方向

1. **缓存优化**：添加分布式缓存，提高性能
2. **数据库扩展**：支持更多数据库类型
3. **操作审计**：添加操作日志记录
4. **前端集成**：开发配套的前端管理界面
5. **单元测试**：增加单元测试覆盖率