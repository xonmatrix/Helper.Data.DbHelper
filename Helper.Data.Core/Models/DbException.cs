using System;
using System.Collections.Generic;
using System.Text;

namespace Helper.Data
{
    public class DbException : Exception
    {
        public int ErrorCode = 0;
        public DbException(Exception e) : base(e.Message, e)
        { 
        }
    }
}
