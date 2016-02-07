using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ExpressionEvaluator.Parser.Expressions;

namespace ExpressionEvaluator.Parser
{
    public class MethodInvocationContext
    {
        public bool IsExtensionMethod { get; set; }
        public Expression ThisParameter { get; set; }
        public TypeOrGeneric Method { get; set; }
        public IEnumerable<MethodInfo> MethodCandidates { get; set; }
    }

    public class CSharpEvalVisitor : CSharp4BaseVisitor<Expression>
    {
        public TypeRegistry TypeRegistry { get; set; }
        public Expression Scope { get; set; }
        public CompilationContext CompilationContext { get; set; }

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

                    if (value == null)
                    {
                        var candidates = MethodResolution.GetCandidateMembers(Scope.Type, identifier_text);
                        if (candidates != null)
                        {
                            methodInvocationContextStack.Push(new MethodInvocationContext()
                            {
                                Method = new TypeOrGeneric()
                                {
                                    Identifier = identifier_text,
                                    // TODO: Type Arguments...
                                },
                                MethodCandidates = candidates
                            });

                            value = Scope;
                        }
                    }
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
            switch (context.op.Type)
            {
                case CSharp4Parser.ASSIGNMENT:
                    return ExpressionHelper.Assign(le, re);
                case CSharp4Parser.OP_ADD_ASSIGNMENT:
                    return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.AddAssign);
                case CSharp4Parser.OP_AND_ASSIGNMENT:
                    return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.AndAssign);
                case CSharp4Parser.OP_DIV_ASSIGNMENT:
                    return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.DivideAssign);
                case CSharp4Parser.OP_LEFT_SHIFT_ASSIGNMENT:
                    return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.LeftShiftAssign);
                case CSharp4Parser.OP_MOD_ASSIGNMENT:
                    return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.ModuloAssign);
                case CSharp4Parser.OP_MULT_ASSIGNMENT:
                    return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.MultiplyAssign);
                case CSharp4Parser.OP_OR_ASSIGNMENT:
                    return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.OrAssign);
                case CSharp4Parser.OP_SUB_ASSIGNMENT:
                    return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.SubtractAssign);
                case CSharp4Parser.OP_RIGHT_SHIFT_ASSIGNMENT:
                    return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.RightShiftAssign);
                case CSharp4Parser.OP_XOR_ASSIGNMENT:
                    return ExpressionHelper.GetBinaryOperator(le, re, ExpressionType.ExclusiveOrAssign);
            }
            throw new InvalidOperationException();
        }



        Stack<MethodInvocationContext> methodInvocationContextStack = new Stack<MethodInvocationContext>();

        public override Expression VisitPrimary_expression(CSharp4Parser.Primary_expressionContext context)
        {
            var value = Visit(context.primary_expression_start());
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
            foreach (var part_context in context.primary_expression_part())
            {
                if (part_context.member_access2() != null)
                {
                    var identifier = part_context.member_access2().identifier().GetText();
                    List<MethodInfo> methodCandidates = MethodResolution.GetCandidateMembers(value.Type, identifier);
                    var isExtensionMethod = false;
                    Expression thisParameter = null;
                    if (!methodCandidates.Any())
                    {
                        //var extensionmethodArgs = new List<Argument>() { new Argument() { Expression = instance } };
                        //extensionmethodArgs.AddRange(args);

                        foreach (var @namespace in CompilationContext.Namespaces)
                        {
                            foreach (var assembly in CompilationContext.Assemblies)
                            {
                                var q = from t in assembly.GetTypes()
                                        where t.IsClass && t.Namespace == @namespace
                                        select t;
                                foreach (var t in q)
                                {
                                    var extensionMethodCandidates = MethodResolution.GetCandidateMembers(t, identifier);
                                    if (extensionMethodCandidates.Any())
                                    {
                                        thisParameter = value;
                                        isExtensionMethod = true;
                                        methodCandidates.AddRange(extensionMethodCandidates);
                                    }
                                }
                            }
                        }
                    }


                    if (!methodCandidates.Any())
                    {
                        value = ExpressionHelper.GetProperty(value, identifier);
                    }
                    else
                    {
                        methodInvocationContextStack.Push(new MethodInvocationContext()
                        {
                            Method = new TypeOrGeneric()
                            {
                                Identifier = identifier,
                                // TODO: Type Arguments...
                            }
                            ,
                            MethodCandidates = methodCandidates
                            ,
                            IsExtensionMethod = isExtensionMethod
                            ,
                            ThisParameter = thisParameter
                        });
                    }
                }

                if (part_context.method_invocation2() != null)
                {
                    var method_invocation_context = part_context.method_invocation2();
                    var args = new List<Argument>();
                    var i = 0;

                    var methodInvocationContext = methodInvocationContextStack.Pop();

                    if (methodInvocationContext.IsExtensionMethod)
                    {
                        args.Add(new Argument() { Expression = methodInvocationContext.ThisParameter });

                        //var methods = methodInvocationContext.MethodCandidates.Where(x => x.GetParameters().Length == 2);

                        //var thisType = methodInvocationContext.ThisParameter.Type;
                        //var isGeneric = thisType.IsGenericType;
                        //if (isGeneric)
                        //{
                        //    var genericArgs = thisType.GetGenericArguments();
                        //    var genericTypeDef = thisType.GetGenericTypeDefinition();
                        //    foreach (var methodInfo in methods)
                        //    {
                        //        var methodThisParameter = methodInfo.GetParameters()[0];
                        //        if (methodThisParameter.ParameterType.IsGenericType)
                        //        {
                        //            var a = methodThisParameter.ParameterType.GetGenericTypeDefinition().IsAssignableFrom(genericTypeDef);
                        //            var b = genericTypeDef.IsAssignableFrom(methodThisParameter.ParameterType.GetGenericTypeDefinition());
                        //        }
                        //    }
                        //}
                    }

                    if (method_invocation_context.argument_list() != null)
                    {
                        foreach (var argument_context in method_invocation_context.argument_list().argument())
                        {
                            args.Add(new Argument() { Expression = Visit(argument_context) });
                        }
                    }

                    var applicableMembers = MethodResolution.GetApplicableMembers(methodInvocationContext.MethodCandidates, args).ToList();
                    value = ExpressionHelper.ResolveApplicableMembers(value.Type, value, applicableMembers, methodInvocationContext.Method, args);
                }
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
                return ExpressionHelper.ParseRealLiteral(context.REAL_LITERAL().GetText());
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


        private class LambdaContext
        {
            public List<LambdaParameter> Parameters { get; set; }
        }

        private Stack<LambdaContext> _lambdaContextStack = new Stack<LambdaContext>();

        public override Expression VisitLambda_expression(CSharp4Parser.Lambda_expressionContext context)
        {
            var parameters = new List<LambdaParameter>();

            var implicit_anonymous_function_parameter_list_context = context.anonymous_function_signature().implicit_anonymous_function_parameter_list();

            if (implicit_anonymous_function_parameter_list_context != null)
            {
                var implicit_anonymous_function_parameter_contexts = implicit_anonymous_function_parameter_list_context.implicit_anonymous_function_parameter();

                foreach (var implicit_anonymous_function_parameter_context in implicit_anonymous_function_parameter_contexts)
                {
                    var identfier_text = implicit_anonymous_function_parameter_context.identifier().GetText();
                    parameters.Add(new LambdaParameter()
                    {
                        Identifier = identfier_text,
                        Expression = Expression.Parameter(typeof(object), identfier_text)
                    });
                }
                _lambdaContextStack.Push(new LambdaContext()
                {
                    Parameters = parameters
                });
            }

            var body = Visit(context.anonymous_function_body());

            return Expression.Lambda(body, parameters.Select(x => x.Expression));
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

        //public override Expression VisitBitwiseExpression(CSharp4Parser.BitwiseExpressionContext context)
        //{
        //var lex = Visit(context.expression(0));
        //var rex = Visit(context.expression(1));
        //switch (context.op.Type)
        //{
        //    case CSharp4Parser.AMP:
        //        return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.And);
        //    case CSharp4Parser.BITWISE_OR:
        //        return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.Or);
        //    case CSharp4Parser.CARET:
        //        return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.ExclusiveOr);
        //}
        //throw new InvalidOperationException();
        //}

        //public override Expression VisitShortCircuitExpression(CSharp4Parser.ShortCircuitExpressionContext context)
        //{
        //    var lex = Visit(context.expression(0));
        //    var rex = Visit(context.expression(1));
        //    switch (context.op.Type)
        //    {
        //        case CSharp4Parser.OP_AND:
        //            return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.AndAlso);
        //        case CSharp4Parser.OP_OR:
        //            return ExpressionHelper.BinaryOperator(lex, rex, ExpressionType.OrElse);
        //    }
        //    throw new InvalidOperationException();
        //}

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