using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cw5.DTOs.Requests
{
    public class LoginRequest
    {
        public string IndexNumber { get; set; }
        public string Password { get;set; }
    }
}
