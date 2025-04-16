using System;
using System.Collections.Generic;
using System.Linq;
using UserPermissionSystem.Domain.AggregateModels;
using UserPermissionSystem.Domain.Events;
using UserPermissionSystem.Domain.Exceptions;

namespace UserPermissionSystem.Domain.Entities
{
    public class Menu : Entity, IAggregateRoot
    {
        public int Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Path { get; private set; } = string.Empty;
        public string ComponentPath { get; private set; } = string.Empty;
        public string Icon { get; private set; } = string.Empty;
        public int? ParentId { get; private set; }
        public int Order { get; private set; }
        public string? PermissionCode { get; private set; } = string.Empty; // 修改为可空类型
        public bool IsVisible { get; private set; } = true;
        
        public Menu Parent { get; private set; }
        // 添加Permission导航属性
        public Permission Permission { get; private set; }
        
        private readonly List<Menu> _children = new List<Menu>();
        public IReadOnlyCollection<Menu> Children => _children.AsReadOnly();

        // 私有构造函数，防止直接创建实例
        private Menu() { }

        // 工厂方法，创建新菜单
        public static Menu Create(
            string name, 
            string path, 
            string componentPath, 
            string icon, 
            int? parentId, 
            int order, 
            string? permissionCode, 
            bool isVisible)
        {
            ValidateName(name);
            ValidatePath(path);

            var menu = new Menu
            {
                Name = name,
                Path = path,
                ComponentPath = componentPath ?? string.Empty,
                Icon = icon ?? string.Empty,
                ParentId = parentId,
                Order = order,
                PermissionCode = permissionCode, // 修改：保留null值，不再转换为空字符串
                IsVisible = isVisible
            };
            
            menu.AddDomainEvent(new MenuCreatedDomainEvent(menu.Id, menu.Name));
            
            return menu;
        }

        // 用于从存储加载的工厂方法
        public static Menu Load(
            int id, 
            string name, 
            string path, 
            string componentPath, 
            string icon, 
            int? parentId, 
            int order, 
            string? permissionCode, 
            bool isVisible)
        {
            return new Menu
            {
                Id = id,
                Name = name,
                Path = path,
                ComponentPath = componentPath ?? string.Empty,
                Icon = icon ?? string.Empty,
                ParentId = parentId,
                Order = order,
                PermissionCode = permissionCode, // 修改：保留null值，不再转换为空字符串
                IsVisible = isVisible
            };
        }

        // 更新菜单信息
        public void Update(
            string name, 
            string path, 
            string componentPath, 
            string icon, 
            int? parentId, 
            int order, 
            string? permissionCode, 
            bool isVisible)
        {
            ValidateName(name);
            ValidatePath(path);

            // 防止创建循环引用
            if (parentId.HasValue && parentId.Value == Id)
                throw new MenuDomainException("菜单不能将自己设为父菜单");

            Name = name;
            Path = path;
            ComponentPath = componentPath ?? string.Empty;
            Icon = icon ?? string.Empty;
            ParentId = parentId;
            Order = order;
            PermissionCode = permissionCode; // 修改：保留null值，不再转换为空字符串
            IsVisible = isVisible;
            
            AddDomainEvent(new MenuUpdatedDomainEvent(Id, Name));
        }

        // 添加子菜单
        public void AddChild(Menu child)
        {
            if (child == null)
                throw new MenuDomainException("子菜单不能为空");

            if (child.Id == Id)
                throw new MenuDomainException("不能将自己添加为子菜单");

            if (!_children.Any(c => c.Id == child.Id))
            {
                child.ParentId = Id;
                _children.Add(child);
                
                AddDomainEvent(new MenuChildAddedDomainEvent(Id, child.Id));
            }
        }

        // 移除子菜单
        public void RemoveChild(int childId)
        {
            var child = _children.FirstOrDefault(c => c.Id == childId);
            if (child != null)
            {
                _children.Remove(child);
                
                AddDomainEvent(new MenuChildRemovedDomainEvent(Id, childId));
            }
        }

        // 显示菜单
        public void Show()
        {
            if (!IsVisible)
            {
                IsVisible = true;
                AddDomainEvent(new MenuVisibilityChangedDomainEvent(Id, true));
            }
        }

        // 隐藏菜单
        public void Hide()
        {
            if (IsVisible)
            {
                IsVisible = false;
                AddDomainEvent(new MenuVisibilityChangedDomainEvent(Id, false));
            }
        }

        // 验证菜单名称
        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new MenuDomainException("菜单名称不能为空");
                
            if (name.Length > 50)
                throw new MenuDomainException("菜单名称长度不能超过50个字符");
        }

        // 验证菜单路径
        private static void ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new MenuDomainException("菜单路径不能为空");
                
            if (path.Length > 100)
                throw new MenuDomainException("菜单路径长度不能超过100个字符");
        }
    }

    // 菜单异常
    public class MenuDomainException : DomainException
    {
        public MenuDomainException() { }
        public MenuDomainException(string message) : base(message) { }
        public MenuDomainException(string message, Exception innerException) : base(message, innerException) { }
    }

    // 菜单相关的领域事件
    public class MenuCreatedDomainEvent : DomainEvent
    {
        public int MenuId { get; }
        public string MenuName { get; }
        
        public MenuCreatedDomainEvent(int menuId, string menuName)
        {
            MenuId = menuId;
            MenuName = menuName;
        }
    }
    
    public class MenuUpdatedDomainEvent : DomainEvent
    {
        public int MenuId { get; }
        public string MenuName { get; }
        
        public MenuUpdatedDomainEvent(int menuId, string menuName)
        {
            MenuId = menuId;
            MenuName = menuName;
        }
    }
    
    public class MenuChildAddedDomainEvent : DomainEvent
    {
        public int ParentMenuId { get; }
        public int ChildMenuId { get; }
        
        public MenuChildAddedDomainEvent(int parentMenuId, int childMenuId)
        {
            ParentMenuId = parentMenuId;
            ChildMenuId = childMenuId;
        }
    }
    
    public class MenuChildRemovedDomainEvent : DomainEvent
    {
        public int ParentMenuId { get; }
        public int ChildMenuId { get; }
        
        public MenuChildRemovedDomainEvent(int parentMenuId, int childMenuId)
        {
            ParentMenuId = parentMenuId;
            ChildMenuId = childMenuId;
        }
    }
    
    public class MenuVisibilityChangedDomainEvent : DomainEvent
    {
        public int MenuId { get; }
        public bool IsVisible { get; }
        
        public MenuVisibilityChangedDomainEvent(int menuId, bool isVisible)
        {
            MenuId = menuId;
            IsVisible = isVisible;
        }
    }
}