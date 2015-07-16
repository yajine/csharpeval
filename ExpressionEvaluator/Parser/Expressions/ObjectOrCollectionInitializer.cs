using System.Collections.Generic;
using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser.Expressions
{
    public class ObjectOrCollectionInitializer
    {
        public List<MemberInitializer> ObjectInitializer { get; set; }
        public List<Expression> CollectionInitializer{ get; set; }
    }
}