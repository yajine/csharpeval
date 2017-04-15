using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ExpressionEvaluator.Parser;

namespace ExpressionEvaluator
{
    public abstract class ExpressionCompiler
    {

        public Expression Expression { get; set; }
        public ExpressionType ExpressionType { get; set; }
        public LambdaExpression LambdaExpression { get; set; }

        protected IParser Parser = null;
        public TypeRegistry TypeRegistry { get; set; }

        public Dictionary<string, Type> DynamicTypeLookup { get; set; }
        public CompilationContext Context { get; set; }

        protected string Pstr = null;

        public string StringToParse
        {
            get { return Parser.ExpressionString; }
            set
            {
                Parser.ExpressionString = value;
                Expression = null;
                ClearCompiledMethod();
            }
        }

        protected Expression BuildTree(Expression scopeParam = null, bool isCall = false)
        {
            Parser.TypeRegistry = TypeRegistry;
            Parser.ExpressionType = ExpressionType;
            Parser.DynamicTypeLookup = DynamicTypeLookup;
            Parser.Context = Context;
            return Expression = Parser.Parse(scopeParam, isCall);
        }

        protected abstract void ClearCompiledMethod();

        /// <summary>
        /// Compiles an expression to a delegate of type T and specifies the given parameters as parameter variable names in the generated expression
        /// </summary>
        /// <typeparam name="T">The delegate type</typeparam>
        /// <param name="parameters">A list of parameter names</param>
        /// <returns></returns>
        public T Compile<T>(params string[] parameters)
        {
            var f = typeof(T);

            //if (!typeof(T).IsSubclassOf(typeof(Delegate)))
            //{
            //    throw new InvalidOperationException(typeof(T).Name + " is not a delegate type");
            //}

            var argTypes = f.GetGenericArguments();
            var argParams = parameters.Select((t, i) => Expression.Parameter(argTypes[i], t)).ToList();
            Parser.ExternalParameters = argParams;
            Expression = BuildTree();
            return Expression.Lambda<T>(Expression, argParams).Compile();
        }

        /// <summary>
        /// Parses, but dues not compile the current expression, with a parameter named scope
        /// </summary>
        public void ScopeParse()
        {
            var scopeParam = Expression.Parameter(typeof(object), "scope");
            Expression = BuildTree(scopeParam);
        }

        protected Expression WrapExpression(Expression source, bool castToObject)
        {
            if (source.Type == typeof(void))
            {
                return WrapToNull(source);
            }
            return castToObject ? Expression.Convert(source, typeof(object)) : Expression;
        }

        protected Expression WrapExpression<T>(Expression source)
        {
            // Attempt to wrap the expression into the proper return type
            var returnType = typeof(T);

            // Check if this expression is a statement that does not return a value
            // We need to return a value since the delegate we are using is a Func<>, not an Action<>
            if (source.Type == typeof(void))
            {
                // Wrap the expression in a code block that returns null
                return WrapToNull(source);
            }

            // If we are passing in a Func<>, the first argument is the Scope type, the second argument is the return type
            if (returnType.IsGenericType)
            {
                var typeargs = returnType.GetGenericArguments();
                returnType = typeargs[typeargs.Count() - 1];
            }

            // probably need to check inheritance and interfaces...
            return source.Type != returnType ? Expression.Convert(source, returnType) : Expression;
        }

        protected Expression WrapExpressionCall<T>(Expression source)
        {
            // Attempt to wrap the expression into the proper return type
            var returnType = typeof(T);

            // Check if this expression is a statement that does not return a value
            // We need to return a value since the delegate we are using is a Func<>, not an Action<>
            if (source.Type == typeof(void))
            {
                // Wrap the expression in a code block that returns null
                return WrapToNull(source);
            }

            // If we are passing in a Func<>, the first argument is the Scope type, the second argument is the return type
            if (returnType.IsGenericType)
            {
                var typeargs = returnType.GetGenericArguments();
                returnType = typeargs[typeargs.Count() - 1];
            }

            // probably need to check inheritance and interfaces...
            return source.Type != returnType ? Expression.Convert(source, returnType) : Expression;
        }

        protected Expression WrapToVoid(Expression source)
        {
            return Expression.Block(source, Expression.Empty());
        }

        protected Expression WrapToNull(Expression source)
        {
            return Expression.Block(source, Expression.Constant(null));
        }

        public override string ToString()
        {
            return Expression.ToString();
        }
    }
}