using System;
using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser
{
    public class CSharpEvalVisitor : CSharp4BaseVisitor<Expression>
    {
        public override Expression VisitPrimary_expression(CSharp4Parser.Primary_expressionContext context)
        {
            return base.VisitPrimary_expression(context);
        }

        public override Expression VisitPrimary_expression_start(CSharp4Parser.Primary_expression_startContext context)
        {
            return base.VisitPrimary_expression_start(context);
        }

        public override Expression VisitUnary_expression(CSharp4Parser.Unary_expressionContext context)
        {
            return base.VisitUnary_expression(context);
        }


        public override Expression VisitLiteral(CSharp4Parser.LiteralContext context)
        {
            if (context.INTEGER_LITERAL() != null)
            {
                // TODO: Parse Hex literals as well
                return ExpressionHelper.ParseIntLiteral(context.INTEGER_LITERAL().GetText());
            }
            if (context.boolean_literal() != null)
            {
                var bool_text = context.boolean_literal().GetText();
                if (bool_text == "true") return Expression.Constant(true);
                if (bool_text == "false") return Expression.Constant(false);
            }
            if (context.STRING_LITERAL() != null)
            {
                return ExpressionHelper.ParseStringLiteral(context.STRING_LITERAL().GetText());
            }
            if (context.CHARACTER_LITERAL() != null)
            {
                return ExpressionHelper.ParseCharLiteral(context.CHARACTER_LITERAL().GetText());
            }
            if (context.NULL() != null)
            {
                return Expression.Constant(null);
            }
            if (context.REAL_LITERAL() != null)
            {
                return ExpressionHelper.ParseIntLiteral(context.REAL_LITERAL().GetText());
            }
            throw new InvalidOperationException();
        }


        public override Expression VisitShiftExpression(CSharp4Parser.ShiftExpressionContext context)
        {
            var lex = Visit(context.expression(0));
            var rex = Visit(context.expression(1));
            switch (context.op.Type)
            {
                case CSharp4Parser.OP_LEFT_SHIFT:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.LeftShift);
                case CSharp4Parser.OP_RIGHT_SHIFT:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.RightShift);
            }
            throw new InvalidOperationException();
        }

        public override Expression VisitRelationalExpression(CSharp4Parser.RelationalExpressionContext context)
        {
            var lex = Visit(context.expression(0));
            var rex = Visit(context.expression(1));
            switch (context.op.Type)
            {
                case CSharp4Parser.LT:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.LessThan);
                case CSharp4Parser.GT:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.GreaterThan);
                case CSharp4Parser.OP_LE:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.LessThanOrEqual);
                case CSharp4Parser.OP_GE:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.GreaterThanOrEqual);
            }
            throw new InvalidOperationException();
        }

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

        public override Expression VisitBitwiseExpression(CSharp4Parser.BitwiseExpressionContext context)
        {
            var lex = Visit(context.expression(0));
            var rex = Visit(context.expression(1));
            switch (context.op.Type)
            {
                case CSharp4Parser.AMP:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.And);
                case CSharp4Parser.BITWISE_OR:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.Or);
                case CSharp4Parser.CARET:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.ExclusiveOr);
            }
            throw new InvalidOperationException();
        }

        public override Expression VisitShortCircuitExpression(CSharp4Parser.ShortCircuitExpressionContext context)
        {
            var lex = Visit(context.expression(0));
            var rex = Visit(context.expression(1));
            switch (context.op.Type)
            {
                case CSharp4Parser.OP_AND:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.AndAlso);
                case CSharp4Parser.OP_OR:
                    return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.OrElse);
            }
            throw new InvalidOperationException();
        }

        public override Expression VisitConditionalExpression(CSharp4Parser.ConditionalExpressionContext context)
        {
            var condition = Visit(context.expression(0));
            var iftrue = Visit(context.expression(1));
            var iffalse = Visit(context.expression(2));
            return ExpressionHelper.Condition(condition, iftrue, iffalse);
        }

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

    }
}