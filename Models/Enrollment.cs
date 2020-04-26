using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cw5.Models
{
    public class Enrollment
    {
        public int IdEnrollment { get; set; }
        //public string Studies { get; set; }
        public int IdStudy { get; set; }
        public int Semester { get; set; }
        public DateTime StartDate { get; set; }
    }
}
