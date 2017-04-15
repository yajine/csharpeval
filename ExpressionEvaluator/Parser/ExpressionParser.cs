using System;
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
        public ExpressionType ExpressionType { get; set; }
        public List<ParameterExpression> ExternalParameters { get; set; }
        public object Global { get; set; }
        public Type ReturnType { get; set; }
        public TypeRegistry TypeRegistry { get; set; }
        public CompilationContext Context { get; set; }

        public Expression Parse(Expression scope, bool isCall = false)
        {
            var stream = new AntlrInputStream(ExpressionString);
            ITokenSource lexer = new CSharp4Lexer(stream);
            ITokenStream tokens = new CommonTokenStream(lexer);
            var parser = new CSharp4Parser(tokens) { BuildParseTree = true };
            parser.AddErrorListener(new ErrorListener());
            IParseTree tree = null;

            switch (ExpressionType)
            {
                case ExpressionType.Expression:
                    tree = parser.expression();
                    break;
                case ExpressionType.Statement:
                    tree = parser.statement();
                    break;
                case ExpressionType.StatementList:
                    tree = parser.statement_list();
                    break;
            }

            if (TypeRegistry == null) TypeRegistry = new TypeRegistry();

            var visitor = new CSharpEvalVisitor
            {
                CompilationContext = Context,
                TypeRegistry = TypeRegistry,
                Scope = scope
            };

            if (ExternalParameters != null)
            {
                visitor.ParameterList.Add(ExternalParameters);
            }

            return Expression = visitor.Visit(tree);
        }
    }
}