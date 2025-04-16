using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserPermissionSystem.Data;
using UserPermissionSystem.DTOs;
using UserPermissionSystem.Models;

namespace UserPermissionSystem.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PermissionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PermissionsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Policy = "RequirePermission:permission:list")]
        public async Task<ActionResult<IEnumerable<PermissionDto>>> GetPermissions()
        {
            var permissions = await _context.Permissions.ToListAsync();

            var permissionDtos = permissions.Select(p => new PermissionDto
            {
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                Module = p.Module,
                Type = p.Type
            }).ToList();

            return Ok(permissionDtos);
        }

        [HttpGet("grouped")]
        [Authorize(Policy = "RequirePermission:permission:list")]
        public async Task<ActionResult<IEnumerable<PermissionGroupDto>>> GetPermissionsGrouped()
        {
            var permissions = await _context.Permissions.ToListAsync();

            var groupedPermissions = permissions
                .GroupBy(p => p.Module)
                .Select(g => new PermissionGroupDto
                {
                    Module = g.Key,
                    Permissions = g.Select(p => new PermissionDto
                    {
                        Code = p.Code,
                        Name = p.Name,
                        Description = p.Description,
                        Module = p.Module,
                        Type = p.Type
                    }).ToList()
                })
                .OrderBy(g => g.Module)
                .ToList();

            return Ok(groupedPermissions);
        }
    }
}