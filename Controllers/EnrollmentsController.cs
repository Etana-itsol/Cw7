using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Cw5.DTOs.Requests;
using Cw5.DTOs.Responses;
using Cw5.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Cw5.Controllers
{
    [Route("api/enrollments")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private IStudentDbServices _service;
        public IConfiguration Configuration { get; set; }

        public EnrollmentsController(IStudentDbServices service, IConfiguration configuration)
        {
            _service = service;
            Configuration = configuration;
        }
      

        [HttpPost]
       // [Authorize(Roles ="Employee")]
        public IActionResult EnrollStudent(EnrollStudentRequest request)
        {
            var response = _service.EnrollStudent(request);

            return Ok(response);
        }

        //..

        //..
        [HttpPost("promotions")]
       // [Authorize(Roles = "Employee")]
        public IActionResult PromoteStudents(PromoteStudentRequest request)
        {
            PromoteStudentRequest res = _service.PromoteStudents(request);
            res.Semester = res.Semester + 1;
            return Ok(res);
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest req)
        {
            
                using var con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18725;Integrated Security=True");
                con.Open();
                using var transaction = con.BeginTransaction();

              if (!_service.CheckLogin(req.IndexNumber, req.Password))
              {
              transaction.Rollback();
              throw new Exception("Wrong Login");
              }

            /*
                 var claims = new[]
                 {
                 new Claim(ClaimTypes.NameIdentifier, req.IndexNumber),
                 new Claim(ClaimTypes.Name, "jan123"),
                 new Claim(ClaimTypes.Role, _service.GetRole(req.IndexNumber)),

                 new Claim(ClaimTypes.Role, "student")
                 };
                 */


                var claims = _service.GetClaims(req.IndexNumber);
            if (claims == null)
            {
                transaction.Rollback();
                throw new Exception("null claims");
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken
                (
                    issuer: "Gakko",
                    audience: "Students",
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(15),
                    signingCredentials: creds
                );
                    var RefreshToken = Guid.NewGuid();
                    _service.SetRefreshToken(RefreshToken.ToString(), req.IndexNumber);

            return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    refreshToken = RefreshToken
                });
            }


        [HttpPost("refresh_token/{refreshToken}")]
        public IActionResult RefreshToken(LoginRequest request, string refreshToken)
        {
            if (_service.CheckRefreshToken(refreshToken, request.IndexNumber))
            {
                var claims = _service.GetClaims(request.IndexNumber);
                if (claims == null) 
                {
                    throw new Exception("null claims");
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken
                (
                    issuer: "Gakko",
                    audience: "Students",
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(10),
                    signingCredentials: creds
                );

                return Ok(new
                {
                    newtoken = new JwtSecurityTokenHandler().WriteToken(token)
                });

            }
            return StatusCode(401);
        }
    }
}