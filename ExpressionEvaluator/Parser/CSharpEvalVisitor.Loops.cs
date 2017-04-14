using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Antlr4.Runtime.Tree;
using ExpressionEvaluator.Parser.Expressions;

namespace ExpressionEvaluator.Parser
{
    public partial class CSharpEvalVisitor : CSharp4BaseVisitor<Expression>
    {


        public override Expression VisitFor_statement(CSharp4Parser.For_statementContext context)
        {
            var breaklabel = CompilerState.PushBreak();
            var continuelabel = CompilerState.PushContinue();

            Expression condition = null;
            IParseTree conditionTree;
            ExpressionList iterator = null;
            IParseTree iteratorTree;
            Expression initializer = null;
            IParseTree initializerTree;

            if ((initializerTree = context.for_initializer()) != null)
            {
                initializer = Visit(initializerTree);
                //if (initializer.GetType() == typeof(LocalVariableDeclarationExpression))
                //{
                //    foreach (var initializerVariable in ((LocalVariableDeclarationExpression)initializer).Variables)
                //    {
                //        ParameterList.Add(initializerVariable);
                //    }
                //}
            }


            if ((conditionTree = context.for_condition()) != null)
            {
                condition = Visit(conditionTree);
            }

            if ((iteratorTree = context.for_iterator()) != null)
            {
                iterator = (ExpressionList)Visit(iteratorTree);
            }

            var body = Visit(context.embedded_statement());

            var val = ExpressionHelper.For(breaklabel, continuelabel, initializer, condition, iterator, body);
            CompilerState.PopContinue();
            CompilerState.PopBreak();
            return val;
        }

        public override Expression VisitForeach_statement(CSharp4Parser.Foreach_statementContext context)
        {
            var breaklabel = CompilerState.PushBreak();
            var continuelabel = CompilerState.PushContinue();
            var iterator = Visit(context.expression());

            var parameter = MakeParameterWithExpression(context.local_variable_type().GetText(), context.identifier().GetText(), iterator);
            ParameterList.Add(parameter);
            // The parameter must have been parsed first and added to the parameterList before the body
            // is parsed, otherwise it's usage as an identifier will not be recognized
            var body = Visit(context.embedded_statement());

            var retval = ExpressionHelper.ForEach(breaklabel, continuelabel, parameter, iterator, body);
            CompilerState.PopContinue();
            CompilerState.PopBreak();
            return retval;
        }
        public override Expression VisitContinue_statement(CSharp4Parser.Continue_statementContext context)
        {
            return CompilerState.Continue();
        }

        public override Expression VisitBreak_statement(CSharp4Parser.Break_statementContext context)
        {
            return CompilerState.Break();
        }

        public override Expression VisitWhile_statement(CSharp4Parser.While_statementContext context)
        {
            var breakTarget = CompilerState.PushBreak();
            var continueTarget = CompilerState.PushContinue();
            var expression = Visit(context.expression());
            var body = Visit(context.embedded_statement());
            var retval = ExpressionHelper.While(breakTarget, continueTarget, null, expression, body);
            CompilerState.PopContinue();
            CompilerState.PopBreak();
            return retval;
        }

        public override Expression VisitDo_statement(CSharp4Parser.Do_statementContext context)
        {
            var breakTarget = CompilerState.PushBreak();
            var continueTarget = CompilerState.PushContinue();
            var expression = Visit(context.expression());
            var body = Visit(context.embedded_statement());
            var retval = ExpressionHelper.DoWhile(breakTarget, continueTarget, body, expression);
            CompilerState.PopContinue();
            CompilerState.PopBreak();
            return retval;
        }

    }

}