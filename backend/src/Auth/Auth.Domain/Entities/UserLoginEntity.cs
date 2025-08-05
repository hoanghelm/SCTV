using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Auth.Domain.Entities
{
    public class UserLoginEntity : IdentityUserLogin<string>
    {
        public UserEntity User { get; set; }
    }
}
