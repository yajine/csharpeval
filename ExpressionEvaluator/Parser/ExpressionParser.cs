﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace ExpressionEvaluator.Parser
{
    public class ExpressionParser : IParser
    {
        public ExpressionParser()
        {
        }

        public ExpressionParser(string expression)
        {
            ExpressionString = expression;
        }

        public Dictionary<string, Type> DynamicTypeLookup { get; set; }
        public Expression Expression { get; set; }
        public string ExpressionString { get; set; }
        public CompiledExpressionType ExpressionType { get; set; }
        public List<ParameterExpression> ExternalParameters { get; set; }
        public object Global { get; set; }
        public Type ReturnType { get; set; }
        public TypeRegistry TypeRegistry { get; set; }
        public Expression Parse(Expression scope, bool isCall = false)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(ExpressionString);
            using (MemoryStream mstream = new MemoryStream(byteArray))
            {
                AntlrInputStream stream = new AntlrInputStream(mstream);
                ITokenSource lexer = new CSharp4Lexer(stream);
                ITokenStream tokens = new CommonTokenStream(lexer);
                var parser = new CSharp4Parser(tokens);
                parser.BuildParseTree = true;
                parser.AddErrorListener(new ErrorListener());
                IParseTree tree = parser.expression();

                //var listener = new MyGraphingCalcListener(d);
                //var walker = new ParseTreeWalker();
                //walker.Walk(listener, tree);
                //var exp = listener.Result;
                var visitor = new CSharpEvalVisitor();
                return visitor.Visit(tree);
                //return Expression.Lambda<Action<CalculatorContext>>(exp, pcontext).Compile();
            }
        }
    }
}