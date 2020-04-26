using Cw5.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Cw5.DTOs.Requests;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Cw5.DTOs.Responses;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace Cw5.Services
{
    public class SqlServerStudentDbServices : IStudentDbServices
    {
        public IConfiguration Configuration { get; set; }
        public string sql="Data Source=db-mssql;Initial Catalog=s18725;Integrated Security=True";
        public SqlServerStudentDbServices(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        /*
        public Enrollment EnrollStudent(EnrollStudentRequest request)
        {
            using (var con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18725;Integrated Security=True"))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();
                var tran = con.BeginTransaction();
                try
                {
                    com.CommandText = "Exec EnrollStudent @IndexNumber, @FirstName, @LastName, @BirthDate, @Studies";
                    com.Parameters.AddWithValue("IndexNumber", request.IndexNumber);
                    com.Parameters.AddWithValue("FirstName", request.FirstName);
                    com.Parameters.AddWithValue("LastName", request.LastName);
                    com.Parameters.AddWithValue("BirthDate", request.Birthdate);
                    com.Parameters.AddWithValue("Studies", request.Studies);
                }
                catch 
                {
                    tran.Rollback();
                }
                tran.Commit();
                com.ExecuteNonQuery();

            }
            return new Enrollment() { Studies = request.Studies, Semester = 1, StartDate = DateTime.Today };
        }
        */
        public Enrollment EnrollStudent(EnrollStudentRequest request)
        {
            using var con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18725;Integrated Security=True");
            con.Open();
            using var transaction = con.BeginTransaction();

            //check if studies exists
            if (!CheckStudies(request.Studies, con, transaction))
            {
                transaction.Rollback();
                throw new Exception ( "Studies does not exist.");
            }

            //get (or create and get) the enrollment
            var enrollment = NewEnrollment(request.Studies, 1, con, transaction);
            if (enrollment == null)
            {
                CreateEnrollment(request.Studies, 1, DateTime.Now, con, transaction);
                enrollment = NewEnrollment(request.Studies, 1, con, transaction);
            }

            //check if provided index number is unique
            if (GetStudent(request.IndexNumber) != null)
            {
                transaction.Rollback();
                throw new Exception( $"Index number ({request.IndexNumber}) is not unique.");
            }

            //create a student and commit the transaction
            CreateStudent(request.IndexNumber, request.FirstName, request.LastName, request.BirthDate, enrollment.IdEnrollment, con, transaction);
            transaction.Commit();

            //return Enrollment object
            return enrollment;
        }
    
        public PromoteStudentRequest PromoteStudents(PromoteStudentRequest request)
        {
            using (var con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18725;Integrated Security=True"))
            using (var com = new SqlCommand("Execute PromoteStudents @name, @semester;", con))
            {
                Console.WriteLine("open");
                con.Open();

                var tran = con.BeginTransaction();
                try 
                {
                    Console.WriteLine("try");
            
                    com.Parameters.AddWithValue("name", request.Studies);
                    com.Parameters.AddWithValue("semester", request.Semester);

                    tran.Commit();
                    com.ExecuteNonQuery();
                }
                catch (SqlException exc)
                {
                    tran.Rollback();
                    Console.WriteLine("err");         
                }
            }
           return request;
        }

        public Student GetStudent(string id)
        {

            using (SqlConnection con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18725;Integrated Security=True"))
            using (SqlCommand com = new SqlCommand())
            {
                com.Connection = con;
                com.CommandText = "SELECT IndexNumber,FirstName,LastName,BirthDate,Name,Semester FROM Student S JOIN Enrollment E on S.IdEnrollment = E.IdEnrollment JOIN Studies St on E.IdStudy = St.IdStudy WHERE IndexNumber = @index";
                com.Parameters.AddWithValue("index", id);

                con.Open();
                var dr = com.ExecuteReader();
                if (dr.Read())
                {
                    Student st = new Student
                    {
                        IndexNumber = dr["IndexNumber"].ToString(),
                        FirstName = dr["FirstName"].ToString(),
                        LastName = dr["LastName"].ToString(),
                        BirthDate = DateTime.Parse(dr["BirthDate"].ToString()),
                        Studies = dr["Name"].ToString(),
                        Semester = int.Parse(dr["Semester"].ToString()),
                    };
                    return st;
                }
                return null;
            }
        }
        public IEnumerable<Student> GetStudents() 
        {
            var list = new List<Student>();

            using (SqlConnection con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18725;Integrated Security=True"))
            using (SqlCommand com = new SqlCommand())
            {
                com.Connection = con;
                com.CommandText = "select IndexNumber,FirstName,LastName,BirthDate ,Name,Semester from Student s join Enrollment e on e.IdEnrollment=s.IdEnrollment join Studies st on st.IdStudy=e.IdStudy";

                con.Open();
                SqlDataReader dr = com.ExecuteReader();

                while (dr.Read())
                {
                    var st = new Student();
                    st.IndexNumber = dr["IndexNumber"].ToString();
                    st.FirstName = dr["FirstName"].ToString();
                    st.LastName = dr["LastName"].ToString();
                    st.BirthDate = DateTime.Parse(dr["BirthDate"].ToString());
                    //st.BirthDate = DateTime.Now;
                    st.Studies = dr["Name"].ToString();
                    st.Semester = int.Parse(dr["Semester"].ToString());
                    list.Add(st);
                }
                con.Dispose();
            }
            return list;
        }

        public bool CheckLogin(string login, string haslo) 
        {
            bool b;
            using (SqlConnection con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18725;Integrated Security=True"))
            using (SqlCommand com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();

                var salt = GetSalt(login);
                Console.WriteLine(salt);
                var hashhaslo = Create(haslo,salt);
                com.Parameters.AddWithValue("id", login);
                com.Parameters.AddWithValue("haslo", hashhaslo);
                com.CommandText = "select 1 from Student where IndexNumber=@id and password=@haslo";

                var dr = com.ExecuteReader();
               b= dr.Read();

            }

            return b;
        }

        public bool SetRefreshToken(string refreshToken, string IndexNumber)
        {
            using (var con = new SqlConnection(sql))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();
                try
                {
                    com.Parameters.AddWithValue("token", refreshToken);
                    com.Parameters.AddWithValue("IndexNumber", IndexNumber);
                    com.CommandText = "update student set token = @token where IndexNumber = @IndexNumber";
                    com.ExecuteNonQuery();
                    return true;
                }
                catch (SqlException ex)
                {
                    return false;
                }
            }
        }

        public bool CheckRefreshToken(string refreshToken, string IndexNumber)
        {
            using (var connection = new SqlConnection(sql))
            using (var command = new SqlCommand())
            {
                command.Connection = connection;
                connection.Open();
                try
                {
                    command.Parameters.AddWithValue("token", refreshToken);
                    command.Parameters.AddWithValue("IndexNumber", IndexNumber);
                    Console.WriteLine(refreshToken);
                    command.CommandText = "SELECT 1 FROM Student WHERE IndexNumber = @IndexNumber AND token = @token";
                    return command.ExecuteReader().Read();
                }
                catch (SqlException ex)
                {
                    return false;
                }
            }
        }

        public string Create(string value, string salt) 
        {
            var valueBytes = KeyDerivation.Pbkdf2(
                password: value,
                salt: Encoding.UTF8.GetBytes(salt),
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: 10000,
                numBytesRequested: 256 / 8);

            return Convert.ToBase64String(valueBytes);
        }

        public string CreateSalt() 
        {
            byte[] randomBytes = new byte[128/8];
            using(var generator=RandomNumberGenerator.Create())
            {
                generator.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
        }
        public string GetSalt(string index) 
        {
            using (var con = new SqlConnection(sql))
            using (var com = new SqlCommand())
            {
                com.Connection = con;
                con.Open();
                try
                {
                    com.Parameters.AddWithValue("IndexNumber", index);
                    com.CommandText = "select salt from student where indexnumber =@IndexNumber";
                    var dr = com.ExecuteReader();
                    dr.Read();
                    string salt= dr["salt"].ToString();
                    return salt; 
                }
                catch (SqlException ex)
                {
                    return null;
                }
            }
        }
        public bool Validate(string value, string salt,string hash)
        {
            return Create(value, salt).Equals(hash);
        }

        public void hashEvryone()
        {
            var con = new SqlConnection(sql);
            var com = new SqlCommand();

            com.Connection = con;
            com.CommandText = "SELECT IndexNumber,Password FROM Student ";

            con.Open();
            var dr = com.ExecuteReader();
            while (dr.Read())
            {
                var password = dr["Password"].ToString();
                var index = dr["IndexNumber"].ToString();
                var salt = CreateSalt();

                //dodajemy salt
                var con2 = new SqlConnection(sql);
                var com2 = new SqlCommand();
                com2.Connection = con2;
                con2.Open();
                com2.CommandText = "UPDATE Student SET salt = @salt WHERE IndexNumber = @index";
                com2.Parameters.AddWithValue("index", index);
                com2.Parameters.AddWithValue("salt", salt);
                com2.ExecuteNonQuery();

                //dodajemy hash
                string pass = Create(dr["Password"].ToString(), salt);
                var con3 = new SqlConnection(sql);
                var com3 = new SqlCommand();

                com3.Connection = con3;
                com3.CommandText = "UPDATE Student SET Password = @pass WHERE Student.IndexNumber = @indeks";
                com3.Parameters.AddWithValue("indeks", index);
                com3.Parameters.AddWithValue("pass", pass);

                con3.Open();
                com3.ExecuteNonQuery();
            }
            dr.Close();
        }
        public Claim[] GetClaims(string id)
        {
            using var con = new SqlConnection(sql);
            con.Open();
            using var com = new SqlCommand
            {
                Connection = con,
                CommandText = "select IndexNumber,FirstName,LastName,Rola from S_Rola sr Join Uprawnienia u on sr.Rola_IdRola = u.IdRola join Student s on s.IndexNumber = sr.Student_IndexNumber where s.IndexNumber=@index"
            };
            com.Parameters.AddWithValue("index", id);
            using var dr = com.ExecuteReader();
            if (dr.Read())
            {
                var claim = new List<Claim>();
                claim.Add(new Claim(ClaimTypes.NameIdentifier, dr["IndexNumber"].ToString()));
                claim.Add(new Claim(ClaimTypes.Name, dr["FirstName"].ToString() + " " + dr["LastName"].ToString()));
                claim.Add(new Claim(ClaimTypes.Role, dr["Rola"].ToString()));

                while (dr.Read())//jeśli osoba ma więcej niż 1 uprawnienie
                {
                    claim.Add(new Claim(ClaimTypes.Role, dr["Rola"].ToString()));
                }
                return claim.ToArray<Claim>();
            }
            else
                return null;

        }
        /*
        public IActionResult Login(string id, string haslo)
        {
            using var con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18725;Integrated Security=True");
            con.Open();
            using var transaction = con.BeginTransaction();

            if (!CheckLogin(id,haslo, con, transaction))
            {
                    transaction.Rollback();
                    throw new Exception("Wrong Login");
            }

            var claims = new[]
   {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "jan123"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim(ClaimTypes.Role, "student")
            };

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
                token = new JwtSecurityTokenHandler().WriteToken(token),
                refreshToken = Guid.NewGuid()
            });
        }
        */
        /////////////////////////////////////////////////////////////////////////////////////////////
        private bool CheckStudies(string name, SqlConnection con, SqlTransaction transaction) 
        {
            using var cmd = new SqlCommand
            {
                Connection = con,
                Transaction = transaction,
                CommandText = @"SELECT 1 from Studies s WHERE s.Name = @name;"
            };
            cmd.Parameters.AddWithValue("name", name);
            using var dr = cmd.ExecuteReader();
            return dr.Read();
        }
        private Enrollment NewEnrollment(string studiesName, int semester, SqlConnection con, SqlTransaction transaction)
        {
            using var cmd = new SqlCommand
            {
                Connection = con,
                Transaction = transaction,
                CommandText = @"SELECT TOP 1 e.IdEnrollment, e.IdStudy, e.StartDate
                                FROM Enrollment e JOIN Studies s ON e.IdStudy=s.IdStudy
                                WHERE e.Semester = @Semester AND s.Name = @Name
                                ORDER BY IdEnrollment DESC;"
            };

            cmd.Parameters.AddWithValue("Name", studiesName);
            cmd.Parameters.AddWithValue("Semester", semester);

            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new Enrollment
                {
                    IdEnrollment = int.Parse(dr["IdEnrollment"].ToString()),
                    Semester = semester,
                    IdStudy = int.Parse(dr["IdStudy"].ToString()),
                    StartDate = DateTime.Parse(dr["StartDate"].ToString()),
                };
            }
            return null;
        }
        private void CreateEnrollment(string studiesName, int semester, DateTime startDate, SqlConnection con, SqlTransaction transaction)
        {
            using var cmd = new SqlCommand
            {
                Connection = con,
                Transaction = transaction,
                CommandText = @"INSERT INTO Enrollment(IdEnrollment, IdStudy, StartDate, Semester)
                                VALUES ((SELECT ISNULL(MAX(e.IdEnrollment)+1,1) FROM Enrollment e), 
		                                (SELECT s.IdStudy FROM Studies s WHERE s.Name = @Name), 
		                                @StartDate,
		                                @Semester);"
            };

            cmd.Parameters.AddWithValue("Name", studiesName);
            cmd.Parameters.AddWithValue("Semester", semester);
            cmd.Parameters.AddWithValue("StartDate", startDate);
            cmd.ExecuteNonQuery();
        }
        private void CreateStudent(string indexNumber, string firstName, string lastName, DateTime BirthDate, int idEnrollment, SqlConnection sqlConnection = null, SqlTransaction transaction = null)
        {
            using var cmd = new SqlCommand
            {
                CommandText = @"INSERT INTO Student(IndexNumber, FirstName, LastName, BirthDate, IdEnrollment)
                                VALUES (@IndexNumber, @FirstName, @LastName, @BirthDate, @IdEnrollment);"
            };
            cmd.Parameters.AddWithValue("IndexNumber", indexNumber);
            cmd.Parameters.AddWithValue("FirstName", firstName);
            cmd.Parameters.AddWithValue("LastName", lastName);
            cmd.Parameters.AddWithValue("BirthDate", BirthDate);
            cmd.Parameters.AddWithValue("IdEnrollment", idEnrollment);

            if (sqlConnection == null)
            {
                using var con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18725;Integrated Security=True");
                con.Open();
                cmd.Connection = con;
                cmd.ExecuteNonQuery();
            }
            else
            {
                cmd.Connection = sqlConnection;
                cmd.Transaction = transaction;
                cmd.ExecuteNonQuery();
            }
        }
        /*
        public bool CheckLogin(string id, string haslo, SqlConnection con, SqlTransaction transaction)
        {

            using var com = new SqlCommand
            {
                Connection = con,
                Transaction = transaction,
                CommandText = "select 1 from Student where IndexNumber=@id and password=@haslo",
            };
            com.Parameters.AddWithValue("id", id);
            com.Parameters.AddWithValue("haslo", haslo);
            using var dr = com.ExecuteReader();
            return dr.Read();
        }
    */
    }
    
}
