using Cw5.DTOs.Requests;
using Cw5.DTOs.Responses;
using Cw5.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cw5.Services
{
    public interface IStudentDbServices
    {
        Enrollment EnrollStudent(EnrollStudentRequest request);
        //public IActionResult GetStudents();
        IEnumerable<Student> GetStudents();

        PromoteStudentRequest PromoteStudents(PromoteStudentRequest request);
        public Student GetStudent(string id);

        //  public IActionResult Login(string id, String haslo);
        public bool CheckLogin(string id, string haslo);
        public bool SetRefreshToken(string refreshToken, string IndexNumber);
        public bool CheckRefreshToken(string refreshToken, string IndexNumber);
        public void hashEvryone();
        public Claim[] GetClaims( string id);
    }
}
