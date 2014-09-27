using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConvertMovies
{
    public  class Result
    {
        public Result()
        {
            HasErrors = false;
        }

        public bool HasErrors { get; set; }
        public string ErrorMessage { get; set; }
        public string Message { get; set; }
        public string File { get; set; }
    }
}
