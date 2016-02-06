using System;
using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser
{
    public class CSharpEvalVisitor : CSharp4BaseVisitor<Expression>
    {
        public TypeRegistry TypeRegistry { get; set; }
        public Expression Scope { get; set; }

        public override Expression VisitSimple_name(CSharp4Parser.Simple_nameContext context)
        {
            return Visit(context.identifier());
        }

        public ParameterList ParameterList = new ParameterList();


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


        public override Expression VisitIdentifier(CSharp4Parser.IdentifierContext context)
        {
            var identifier_text = context.IDENTIFIER().GetText();

            var value = GetIdentifier(identifier_text);

            if (value == null)
            {
                if (Scope != null)
                {
                    value = ExpressionHelper.GetProperty(Scope, identifier_text);
                }
            }

            return value;
        }

        public override Expression VisitScope_member_access(CSharp4Parser.Scope_member_accessContext context)
        {
            var memberName = context.identifier().GetText();
            var value = ExpressionHelper.GetProperty(Scope, memberName);

            if (value == null)
            {
                throw new Exception(string.Format("Cannot resolve symbol \"{0}\"", memberName));
            }

            return value;
        }

        public override Expression VisitAssignment(CSharp4Parser.AssignmentContext context)
        {
            var le = Visit(context.unary_expression());
            var re = Visit(context.expression());
            if (context.assignment_operator().ASSIGNMENT() != null)
            {
                return ExpressionHelper.Assign(le, re);
            }
            if (context.assignment_operator().OP_ADD_ASSIGNMENT() != null)
            {
                return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.AddAssign);
            }
            if (context.assignment_operator().OP_AND_ASSIGNMENT() != null)
            {
                return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.AndAssign);
            }
            if (context.assignment_operator().OP_DIV_ASSIGNMENT() != null)
            {
                return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.DivideAssign);
            }
            if (context.assignment_operator().OP_LEFT_SHIFT_ASSIGNMENT() != null)
            {
                return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.LeftShiftAssign);
            }
            if (context.assignment_operator().OP_MOD_ASSIGNMENT() != null)
            {
                return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.ModuloAssign);
            }
            if (context.assignment_operator().OP_MULT_ASSIGNMENT() != null)
            {
                return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.MultiplyAssign);
            }
            if (context.assignment_operator().OP_OR_ASSIGNMENT() != null)
            {
                return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.OrAssign);
            }
            if (context.assignment_operator().OP_SUB_ASSIGNMENT() != null)
            {
                return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.SubtractAssign);
            }
            if (context.assignment_operator().OP_RIGHT_SHIFT_ASSIGNMENT() != null)
            {
                return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.RightShiftAssign);
            }
            if (context.assignment_operator().OP_XOR_ASSIGNMENT() != null)
            {
                return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.ExclusiveOrAssign);
            }
            throw new InvalidOperationException();
        }

        public override Expression VisitPrimary_expression(CSharp4Parser.Primary_expressionContext context)
        {
            var value = Visit(context.pe);

            // Expression evaluator customization:		

            //	$value = Scope;

            //                      var text = $primary_expression_start.text;

            //                      var method1 = new TypeOrGeneric() { Identifier = "getVar" };
            //                      var args1 = new List<Argument>() { new Argument() { Expression = Expression.Constant(text, typeof(string)) } };

            //                      if (DynamicTypeLookup != null && DynamicTypeLookup.ContainsKey(text))
            //                      {
            //                          var type1 = DynamicTypeLookup[text];
            //		$value = Expression.Convert(ExpressionHelper.GetMethod($value, method1, args1, false), type1);
            //                      }
            //                      else
            //                      {		
            //		$value = ExpressionHelper.GetMethod($value, method1, args1, false);
            //                      }

            //                      if ($value == null)
            //	{
            //                          throw new ExpressionParseException(string.Format("Cannot resolve symbol \"{0}\"", input.LT(-1).Text), input);
            //                      }

            //                  }

            if (context.member_access2(0) != null)
            {
                var identifier = context.member_access2(0).identifier().GetText();
                value = ExpressionHelper.GetProperty(value, identifier);
            }

            return value;
        }

        public override Expression VisitLiteral(CSharp4Parser.LiteralContext context)
        {
            if (context.INTEGER_LITERAL() != null)
            {
                // TODO: Parse Hex literals as well
                var int_literal_text = context.INTEGER_LITERAL().GetText();
                if (int_literal_text.StartsWith("0x") || int_literal_text.StartsWith("0X"))
                {
                    return ExpressionHelper.ParseHexLiteral(int_literal_text);
                }
                return ExpressionHelper.ParseIntLiteral(int_literal_text);
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

        public override Expression VisitParenExpression(CSharp4Parser.ParenExpressionContext context)
        {
            return Visit(context.expression());
        }
    }
}