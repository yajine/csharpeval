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

        #region Equality Operators
        public override Expression VisitEqualityExpression(CSharp4Parser.EqualityExpressionContext context)
        {
            var lex = Visit(context.expression(0));
            var rex = Visit(context.expression(1));
            switch (context.op.Type)
            {
                case CSharp4Parser.OP_EQ:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.Equal);
                case CSharp4Parser.OP_NE:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.NotEqual);
            }
            throw new InvalidOperationException();
        }
        #endregion

        #region Conditional Operator
        public override Expression VisitConditionalExpression(CSharp4Parser.ConditionalExpressionContext context)
        {
            var condition = Visit(context.expression(0));
            var iftrue = Visit(context.expression(1));
            var iffalse = Visit(context.expression(2));
            return ExpressionHelper.Condition(condition, iftrue, iffalse);
        }
        #endregion

        #region Bitwise Logical Operators
        public override Expression VisitAndExpression(CSharp4Parser.AndExpressionContext context)
        {
            var lex = Visit(context.expression(0));
            var rex = Visit(context.expression(1));
            return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.And);
        }

        public override Expression VisitXorExpression(CSharp4Parser.XorExpressionContext context)
        {
            var lex = Visit(context.expression(0));
            var rex = Visit(context.expression(1));
            return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.ExclusiveOr);
        }

        public override Expression VisitOrExpression(CSharp4Parser.OrExpressionContext context)
        {
            var lex = Visit(context.expression(0));
            var rex = Visit(context.expression(1));
            return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.Or);
        }
        #endregion

        #region Conditional Logical Operators
        public override Expression VisitConditionalAndExpression(CSharp4Parser.ConditionalAndExpressionContext context)
        {
            var lex = Visit(context.expression(0));
            var rex = Visit(context.expression(1));
            return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.AndAlso);
        }

        public override Expression VisitConditionalOrExpression(CSharp4Parser.ConditionalOrExpressionContext context)
        {
            var lex = Visit(context.expression(0));
            var rex = Visit(context.expression(1));
            return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.OrElse);
        }
        #endregion

        #region Binary Operators

        public override Expression VisitMultiplicativeExpression(CSharp4Parser.MultiplicativeExpressionContext context)
        {
            var lex = Visit(context.expression(0));
            var rex = Visit(context.expression(1));
            switch (context.op.Type)
            {
                case CSharp4Parser.STAR:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.Multiply);
                case CSharp4Parser.DIV:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.Divide);
                case CSharp4Parser.PERCENT:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.Modulo);
            }
            throw new InvalidOperationException();
        }

        public override Expression VisitAdditiveExpression(CSharp4Parser.AdditiveExpressionContext context)
        {
            var lex = Visit(context.expression(0));
            var rex = Visit(context.expression(1));
            switch (context.op.Type)
            {
                case CSharp4Parser.PLUS:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.Add);

                case CSharp4Parser.MINUS:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.Subtract);
            }
            throw new InvalidOperationException();
        }

        #endregion

        #region Unary Operators

        public override Expression VisitNegateExpression(CSharp4Parser.NegateExpressionContext context)
        {
            return Expression.Negate(Visit(context.unary_expression()));
        }

        public override Expression VisitNotExpression(CSharp4Parser.NotExpressionContext context)
        {
            return Expression.Not(Visit(context.unary_expression()));
        }

        public override Expression VisitComplementExpression(CSharp4Parser.ComplementExpressionContext context)
        {
            return Expression.OnesComplement(Visit(context.unary_expression()));
        }

        #endregion
    }

}