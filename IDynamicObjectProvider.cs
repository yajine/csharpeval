using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionEvaluator
{
    public interface IDynamicObjectProvider
    {
        void setVar(string propertyname, object value);
        object getVar(string propertyname);
    }
}
