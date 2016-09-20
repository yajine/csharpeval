using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Antlr.Runtime;
using ExpressionEvaluator;
using ExpressionEvaluator.Parser.Expressions;

namespace ExpressionEvaluator.Parser
{

    public partial class ExprEvalParser
    {
        public CompilationContext Context { get; set; } 

        private CompilerState compilerState = new CompilerState();

        public Expression Scope { get; set; }

        public bool IsCall { get; set; }
        public LabelTarget ReturnTarget { get; set; }
        public bool HasReturn { get; private set; }
        public TypeRegistry TypeRegistry { get; set; }

        public Dictionary<string, Type> DynamicTypeLookup { get; set; }

        private Stack<TypeOrGeneric> methodStack = new Stack<TypeOrGeneric>();

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
        public object Eval(string expressionString)
        {
            return null;
            //var ms = new MemoryStream(Encoding.UTF8.GetBytes(expressionString));
            //var input = new ANTLRInputStream(ms);
            //var lexer = new ExprEvalLexer(input);
            //var tokens = new TokenRewriteStream(lexer);
            //if (TypeRegistry == null) TypeRegistry = new TypeRegistry();
            //var parser = new ExprEvalParser(tokens) { TypeRegistry = TypeRegistry, Scope = Scope, IsCall = IsCall, DynamicTypeLookup = DynamicTypeLookup };

            //Expression expression = parser.single_expression();

            //if (expression == null)
            //{
            //    var statement = parser.statement();
            //    if (statement != null)
            //    {
            //        expression = statement.Expression;
            //    }
            //    if (expression == null)
            //    {
            //        var statements = parser.statement_list();
            //        expression = statements.ToBlock();
            //    }
            //}

            //if (Scope != null)
            //{
            //    var func = Expression.Lambda<Func<object, object>>(Expression.Convert(expression, typeof(Object)), new ParameterExpression[] { (ParameterExpression)Scope }).Compile();
            //    return func(Scope);
            //}
            //else
            //{
            //    var func = Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(Object))).Compile();
            //    return func();
            //}

        }


        protected Expression GetPrimaryExpressionPart(PrimaryExpressionPart primary_expression_part2, ITokenStream input, Expression value, bool throwsException = true)
        {
            if (primary_expression_part2.GetType() == typeof(AccessIdentifier))
            {
                if (input.LT(1).Text == "(")
                {
                    methodStack.Push(((AccessIdentifier)primary_expression_part2).Value);
                }
                else
                {
                    var memberName = ((AccessIdentifier)primary_expression_part2).Value.Identifier;

                    try
                    {
                        var newValue = ExpressionHelper.GetProperty(value, memberName);
                        if (newValue == null && throwsException)
                        {
                            throw new ExpressionParseException(string.Format("Cannot resolve member \"{0}\" on type \"{1}\"", memberName, value.Type.Name), input);
                            //throw new ExpressionParseException(string.Format("Cannot resolve symbol \"{0}\"", input.LT(-1).Text), input);
                        }
                        value = newValue;
                    }
                    catch (ExpressionContainerException ex)
                    {
                        value = ExpressionHelper.MethodInvokeExpression(typeof(ExprEvalParser), Expression.Constant(this),
                            new TypeOrGeneric() { Identifier = "Eval" }, new Argument[] { new Argument() { Expression = ex.Container } });
                    }


                }
            }
            else if (primary_expression_part2.GetType() == typeof(Brackets))
            {
                value = ExpressionHelper.GetPropertyIndex(value, ((Brackets)primary_expression_part2).Values);
            }
            else if (primary_expression_part2.GetType() == typeof(Arguments))
            {
                if (methodStack.Count > 0)
                {
                    var method = methodStack.Pop();
                    var newValue = ExpressionHelper.GetMethod(value, method, ((Arguments)primary_expression_part2).Values, false, Context);
                    if (newValue == null && throwsException)
                    {
                        throw new ExpressionParseException(string.Format("Cannot resolve member \"{0}\" on type \"{1}\"", method.Identifier, value.Type.Name), input);
                        //throw new ExpressionParseException(string.Format("Cannot resolve symbol \"{0}\"", input.LT(-1).Text), input);
                    }
                    value = newValue;
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

        //public override void ReportError(RecognitionException e)
        //{
        //    base.ReportError(e);
        //    string message;
        //    if (e.GetType() == typeof(MismatchedTokenException))
        //    {
        //        var ex = (MismatchedTokenException)e;
        //        message = string.Format("Mismatched token '{0}', expected {1}", e.Token.Text, ex.Expecting);
        //    }
        //    else
        //    {
        //        message = string.Format("Error parsing token '{0}'", e.Token.Text);
        //    }

        //    throw new ExpressionParseException(message, input);

        //    Console.WriteLine("Error in parser at line " + e.Line + ":" + e.CharPositionInLine);
        //}

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
                if (result.GetType() == typeof(DelegateType<>))
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
