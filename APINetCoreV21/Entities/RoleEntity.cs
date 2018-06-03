using Microsoft.AspNetCore.Identity;
using System;

namespace APINetCoreV21.Entities
{
    public class RoleEntity : IdentityRole<Guid>
    {
        public RoleEntity() : base()
        {
        }

        public RoleEntity(string roleName) : base(roleName)
        {
        }
    }

}