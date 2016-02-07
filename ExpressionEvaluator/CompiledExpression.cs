using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ExpressionEvaluator.Parser;

namespace ExpressionEvaluator
{
    /// <summary>
    /// Creates compiled expressions with return values that are of type TResult
    /// </summary>
    public class CompiledExpression<TResult> : ExpressionCompiler
    {
        private Func<TResult> _compiledMethod = null;
        private Action _compiledAction = null;

        public CompiledExpression()
        {
            Parser = new ExpressionParser { ReturnType = typeof(TResult) };
        }

        public CompiledExpression(string expression)
        {
            Parser = new ExpressionParser(expression) { ReturnType = typeof(TResult) };
        }

        public Func<TResult> Compile()
        {
            Expression = WrapExpression<TResult>(BuildTree());
            return Expression.Lambda<Func<TResult>>(Expression).Compile();
        }

        public Expression<T> GenerateLambda<T, TParam>(bool withScope, bool asCall)
        {
            var scopeParam = Expression.Parameter(typeof(TParam), "scope");
            var expression = withScope ? BuildTree(scopeParam, asCall) : BuildTree();
            Expression = WrapExpression(expression, false);
            return withScope ?
                Expression.Lambda<T>(Expression, new ParameterExpression[] { scopeParam }) :
                Expression.Lambda<T>(Expression)
                ;
        }

        private T Compile<T, TParam>(bool withScope, bool asCall)
        {
            return GenerateLambda<T, TParam>(withScope, asCall).Compile();
        }

        //    public LambdaExpression GenerateLambda()
        //    {
        //        var scopeParam = Expression.Parameter(typeof(object), "scope");
        //        Expression = WrapExpression(BuildTree(scopeParam), true);
        //        return Expression.Lambda<Func<dynamic, object>>(Expression, new ParameterExpression[] { scopeParam });
        //    }

        /// <summary>
        /// Compiles the expression to a function that returns void
        /// </summary>
        /// <returns></returns>
        public Action CompileCall()
        {
            Expression = BuildTree(null, true);
            return Expression.Lambda<Action>(Expression).Compile();
        }

        /// <summary>
        /// Compiles the expression to a function that takes an object as a parameter and returns an object
        /// </summary>
        /// <returns></returns>
        public Action<object> ScopeCompileCall()
        {
            return ScopeCompileCall<object>();
        }

        /// <summary>
        /// Compiles the expression to a function that takes an object as a parameter and returns an object
        /// </summary>s
        /// <returns></returns>
        public Action<TParam> ScopeCompileCall<TParam>()
        {
            return CompileWithScope<Action<TParam>, TParam>(true);
        }

        /// <summary>
        /// Compiles an expression to a delegate of that accepts a scope context object parameter of type object and a return type of type TResult using scoping to resolve symbols.
        /// The parameter of the delegate should be the scope context object to be executed against the expression at "run" time
        /// </summary>
        /// <returns></returns>
        public Func<object, TResult> ScopeCompile()
        {
            return ScopeCompile<object>();
        }

        /// <summary>
        /// Compiles an expression to a delegate of that accepts a scope context object parameter of type TScope and a return type of type TResult using scoping to resolve symbols.
        /// The parameter of the delegate should be the scope context object to be executed against the expression at "run" time
        /// </summary>
        /// <typeparam name="TScope">The type of the scope parameter that will be passed into the compiled function</typeparam>
        /// <returns></returns>
        public Func<TScope, TResult> ScopeCompile<TScope>()
        {
            return CompileWithScope<Func<TScope, TResult>, TScope>(false);
        }

        private T CompileWithScope<T, TScope>(bool asCall)
        {
            var scopeParam = Expression.Parameter(typeof(TScope), "scope");
            Expression = BuildTree(scopeParam, asCall);
            if (!asCall)
            {
                Expression = WrapExpression<T>(Expression);
            }
            return Expression.Lambda<T>(Expression, new ParameterExpression[] { scopeParam }).Compile();
        }

        protected override void ClearCompiledMethod()
        {
            _compiledMethod = null;
            _compiledAction = null;
        }

        public TResult Eval()
        {
            if (_compiledMethod == null) _compiledMethod = Compile();
            return _compiledMethod();
        }

        public void Call()
        {
            if (_compiledAction == null) _compiledAction = CompileCall();
            _compiledAction();
        }

        public object Global
        {
            set
            {
                Parser.Global = value;
            }
        }

    }

    /// <summary>
    /// Creates compiled expressions with return values that are cast to type Object 
    /// </summary>
    public class CompiledExpression : CompiledExpression<object>
    {
        public CompiledExpression()
        {
        }

        public CompiledExpression(string expression)
            : base(expression)
        {

        }
    }

}
