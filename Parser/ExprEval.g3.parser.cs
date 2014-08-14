using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Antlr.Runtime;
using ExpressionEvaluator;
using ExpressionEvaluator.Parser.Expressions;

namespace ExpressionEvaluator.Parser
{
    public partial class ExprEvalParser
    {
        private CompilerState compilerState = new CompilerState();

        public Expression Scope { get; set; }

        public bool IsCall { get; set; }
        public LabelTarget ReturnTarget { get; set; }
        public bool HasReturn { get; private set; }
        public TypeRegistry TypeRegistry { get; set; }

        public Dictionary<string, Type> DynamicTypeLookup { get; set; }

        //partial void EnterRule(string ruleName, int ruleIndex)
        //{
        //    base.TraceIn(ruleName, ruleIndex);
        //    Debug.WriteLine("In: {0} {1}", ruleName, ruleIndex);
        //}

        //partial void LeaveRule(string ruleName, int ruleIndex)
        //{
        //    Debug.WriteLine("Out: {0} {1}", ruleName, ruleIndex);
        //}

        //protected Type GetPropertyType(Expression expression, string propertyName)
        //{
        //    var pe = (ParameterExpression) expression;   
        //    var methodInfo = pe.Type.GetMethod("getType", BindingFlags.Instance);
        //    if (methodInfo != null)
        //    {
        //        return (Type)methodInfo.Invoke(pe., new object[] { propertyName });
        //    }
        //    return null;
        //}


        protected Expression GetPrimaryExpressionPart(PrimaryExpressionPart primary_expression_part2, ITokenStream input, Expression value, TypeOrGeneric method, bool throwsException = true)
        {
            if (primary_expression_part2.GetType() == typeof(AccessIdentifier))
            {
                if (input.LT(1).Text == "(")
                {
                    method = ((AccessIdentifier)primary_expression_part2).Value;
                }
                else
                {
                    value = ExpressionHelper.GetProperty(value, ((AccessIdentifier)primary_expression_part2).Value.Identifier);
                    if (value == null && throwsException)
                    {
                        throw new ExpressionParseException(string.Format("Cannot resolve symbol \"{0}\"", input.LT(-1).Text), input);
                    }
                }
            }
            else if (primary_expression_part2.GetType() == typeof(Brackets))
            {
                value = ExpressionHelper.GetPropertyIndex(value, ((Brackets)primary_expression_part2).Values);
            }
            else if (primary_expression_part2.GetType() == typeof(Arguments))
            {
                if (method != null)
                {
                    value = ExpressionHelper.GetMethod(value, method, ((Arguments)primary_expression_part2).Values, false);
                    if (value == null && throwsException)
                    {
                        throw new ExpressionParseException(string.Format("Cannot resolve symbol \"{0}\"", input.LT(-1).Text), input);
                    }
                }
                else
                {
                    // value = GetMethod(value, membername, ((Arguments)primary_expression_part2).Values);
                }
            }
            else if (primary_expression_part2.GetType() == typeof(PostIncrement))
            {
                value = Expression.Assign(value, Expression.Increment(value));
            }
            else if (primary_expression_part2.GetType() == typeof(PostDecrement))
            {
                value = Expression.Assign(value, Expression.Decrement(value));
            }

            return value;
        }

        public override void ReportError(RecognitionException e)
        {
            base.ReportError(e);
            string message;
            if (e.GetType() == typeof(MismatchedTokenException))
            {
                var ex = (MismatchedTokenException)e;
                message = string.Format("Mismatched token '{0}', expected {1}", e.Token.Text, ex.Expecting);
            }
            else
            {
                message = string.Format("Error parsing token '{0}'", e.Token.Text);
            }

            throw new ExpressionParseException(message, input);

            Console.WriteLine("Error in parser at line " + e.Line + ":" + e.CharPositionInLine);
        }

        private Expression GetIdentifier(string identifier)
        {
            ParameterExpression parameter;

            if (ParameterList.TryGetValue(identifier, out parameter))
            {
                return parameter;
            }

            object result = null;

            if (TypeRegistry.TryGetValue(identifier, out result))
            {
                if (result.GetType() == typeof(ValueType))
                {
                    var typeValue = (ValueType)result;
                    return Expression.Constant(typeValue.Value, typeValue.Type);
                }
                if (result.GetType() == typeof(DelegateType))
                {
                    var typeValue = (DelegateType)result;
                    return Expression.Constant(typeValue.Value(), typeValue.Type);
                }
                return Expression.Constant(result);
            }

            return null;

            // throw new UnknownIdentifierException(identifier);
        }

        public Type GetType(string type)
        {
            object _type;

            if (TypeRegistry.TryGetValue(type, out _type))
            {
                return (Type)_type;
            }
            return Type.GetType(type);
        }

        public ParameterList ParameterList = new ParameterList();

    }
}
