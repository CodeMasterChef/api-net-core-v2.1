using Microsoft.AspNetCore.Identity;
using System;

namespace APINetCoreV21.Entities
{
    public class UserEntity : IdentityUser<Guid>
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public bool IsActive { get; set; }

    }
}
