using System;
using System.Collections.Generic;
using System.Text;

namespace Helper.Data.Net4
{
    public class DbExpression
    {
        public DbExpression(string expression)
        {
            this.Expression = expression;
        }
        public string Expression { get; private set; }
    }

}
