using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionEvaluator
{
    public class ExpressionContainerAttribute : Attribute
    {
    }

    public class ExpressionContainerException : Exception
    {
        public Expression Container { get; private set; }

        public ExpressionContainerException(Expression container)
        {
            Container = container;
        }
    }
}
