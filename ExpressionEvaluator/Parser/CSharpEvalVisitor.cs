using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Antlr4.Runtime.Tree;
using ExpressionEvaluator.Parser.Expressions;

namespace ExpressionEvaluator.Parser
{
    public class CSharpEvalVisitor : CSharp4BaseVisitor<Expression>
    {
        public TypeRegistry TypeRegistry { get; set; }
        public Expression Scope { get; set; }
        public CompilationContext CompilationContext { get; set; }
        public ParameterList ParameterList = new ParameterList();
        public CompilerState CompilerState = new CompilerState();

        public override Expression VisitSimple_name(CSharp4Parser.Simple_nameContext context)
        {
            return Visit(context.identifier());
        }

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

            if (_lambdaContextStack.Count > 0)
            {
                var lambdaContext = _lambdaContextStack.Peek();
                var p = lambdaContext.Parameters.FirstOrDefault(x => x.Identifier == identifier);
                if (p != null) return p.Expression;
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

        public override Expression VisitStatement_expression_list(CSharp4Parser.Statement_expression_listContext context)
        {
            var list = new ExpressionList();
            foreach (var statement in context.statement_expression())
            {
                list.Expressions.Add(Visit(statement));
            }
            return list;
        }

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
                if (initializer.GetType() == typeof(LocalVariableDeclarationExpression))
                {
                    foreach (var initializerVariable in ((LocalVariableDeclarationExpression)initializer).Variables)
                    {
                        ParameterList.Add(initializerVariable);
                    }
                }
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

            var typeName = context.local_variable_type().GetText();
            Type type = null;

            if (typeName == "var")
            {
                if (iterator.Type.IsGenericType)
                {
                    type = iterator.Type.GetGenericArguments()[0];
                }
                else if (iterator.Type.IsArray)
                {
                    type = iterator.Type.GetElementType();
                }
                else
                {
                    type = typeof(object);
                }
            }
            else
            {
                type = GetType(typeName);
            }

            var parameter = Expression.Parameter(type, context.identifier().GetText());

            ParameterList.Add(parameter);

            // The parameter must have been parsed first and added to the parameterList before the body
            // is parsed, otherwise it's usage as an identifier will not be recognized
            var body = Visit(context.embedded_statement());

            var retval = ExpressionHelper.ForEach(breaklabel, continuelabel, parameter, iterator, body);
            CompilerState.PopContinue();
            CompilerState.PopBreak();
            return retval;
        }


        public override Expression VisitArrayCreationExpression(CSharp4Parser.ArrayCreationExpressionContext context)
        {
            var list = new List<Expression>();
            foreach (
                var vi in
                context.array_creation_expression()
                    .array_initializer()
                    .variable_initializer_list()
                    .variable_initializer())
            {
                list.Add(Visit(vi));
            }
            ;

            return Expression.NewArrayInit(TypeConversion.GetBaseCommonType(list), list);
        }


        public override Expression VisitArray_creation_expression(CSharp4Parser.Array_creation_expressionContext context)
        {
            var list = new List<Expression>();
            foreach (var vi in context.array_initializer().variable_initializer_list().variable_initializer())
            {
                list.Add(Visit(vi));
            }
            ;

            return Expression.NewArrayInit(TypeConversion.GetBaseCommonType(list), list);
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
                                MethodCandidates = candidates,
                                Instance = Scope,
                                Type = Scope.Type
                            });

                            value = Scope;
                        }
                    }
                }
            }

            return value;
        }

        public override Expression VisitArgument_list(CSharp4Parser.Argument_listContext context)
        {
            return base.VisitArgument_list(context);
        }

        public override Expression VisitArgument(CSharp4Parser.ArgumentContext context)
        {
            return new ArgumentExpression()
            {
                Expression = base.VisitArgument(context),
                Name = context.argument_name()?.GetText()
            };
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
        Stack<TypeInferenceBounds> typeInferenceBoundsStack = new Stack<TypeInferenceBounds>();

        public ParameterInfo CurrentParameterInfo { get; set; }

        public override Expression VisitIf_statement(CSharp4Parser.If_statementContext context)
        {
            var boolExp = Visit(context.expression());

            if (boolExp.Type != typeof(bool))
            {
                boolExp = Expression.Convert(boolExp, typeof(bool));
            }

            var ifBodyExpression = Visit(context.if_body(0));
            var elseBody = context.if_body(1);

            if (elseBody == null)
            {
                return Expression.IfThen(boolExp, ifBodyExpression);
            }

            return Expression.IfThenElse(boolExp, ifBodyExpression, Visit(elseBody));
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



        public override Expression VisitDeclaration_statement(CSharp4Parser.Declaration_statementContext context)
        {
            IParseTree tree = context.local_variable_declaration();
            if (tree != null)
            {
                return Visit(tree);
            }
            else
            {
                tree = context.local_constant_declaration();
                return Visit(tree);
            }
            return null;
        }

        public override Expression VisitStatement(CSharp4Parser.StatementContext context)
        {
            IParseTree tree = context.declaration_statement();
            if (tree != null)
            {
                return Visit(tree);
            }
            else
            {
                tree = context.embedded_statement();
                return Visit(tree);
            }
            return null;
        }

        public override Expression VisitEmbedded_statement(CSharp4Parser.Embedded_statementContext context)
        {
            IParseTree tree = context.block();
            if (tree != null)
            {
                return Visit(tree);
            }
            else
            {
                tree = context.simple_embedded_statement();
                var x = Visit(tree);
                return x;
            }
            return null;
        }

        private Expression VisitEach<T>(T context, params Func<T, IParseTree>[] parseFunc)
        {
            IParseTree tree = parseFunc[0](context);
            int i = 1;
            while (tree == null && i < parseFunc.Length)
            {
                tree = parseFunc[i++](context);
            }
            if (i > parseFunc.Length) throw new Exception("No Match");
            return Visit(tree);
        }

        public override Expression VisitExpression_statement(CSharp4Parser.Expression_statementContext context)
        {
            return Visit(context.statement_expression());
        }

        public override Expression VisitStatement_expression(CSharp4Parser.Statement_expressionContext context)
        {
            return Visit(context.expression());
        }

        public override Expression VisitSimple_embedded_statement(CSharp4Parser.Simple_embedded_statementContext context)
        {
            return VisitEach(context,
                c => c.empty_statement()
                , c => c.expression_statement()
                , c => c.selection_statement()
                , c => c.iteration_statement()
                , c => c.jump_statement()
                , c => c.try_statement()
                , c => c.checked_statement()
                , c => c.unchecked_statement()
                , c => c.lock_statement()
                , c => c.using_statement()
                , c => c.yield_statement()
                , c => c.embedded_statement_unsafe()
                );
        }

        public override Expression VisitBlock(CSharp4Parser.BlockContext context)
        {
            return Visit(context.statement_list());
        }

        public override Expression VisitStatement_list(CSharp4Parser.Statement_listContext context)
        {
            var expressions = new List<Expression>();

            foreach (var statement in context.statement())
            {
                var ex = Visit(statement);
                if (ex.GetType() == typeof(ExpressionList))
                {
                    expressions.AddRange(((ExpressionList)ex).Expressions);
                }
                else
                {
                    expressions.Add(ex);
                }
            }

            var variables = expressions.OfType<LocalVariableDeclarationExpression>().SelectMany(x => x.Variables).ToList();
            var initializers = expressions.OfType<LocalVariableDeclarationExpression>().SelectMany(x => x.Initializers).ToList();
            expressions.RemoveAll(x => x.GetType() == typeof(LocalVariableDeclarationExpression));
            return Expression.Block(variables, initializers.Concat(expressions));
        }

        public override Expression VisitExpression_list(CSharp4Parser.Expression_listContext context)
        {
            var expressionList = new ExpressionList() { Expressions = new List<Expression>() };
            foreach (var expression in context.expression())
            {
                expressionList.Expressions.Add(Visit(expression));
            }
            return expressionList;
        }

        public override Expression VisitLocal_variable_declaration(CSharp4Parser.Local_variable_declarationContext context)
        {
            var list = new LocalVariableDeclarationExpression();
            var type = context.local_variable_type().type().GetText();
            Type t = null;
            if (type != "var")
            {
                t = GetType(type);
            }
            var lvds = (ExpressionList)Visit(context.local_variable_declarators());
            foreach (var localVarDeclarator in lvds.Expressions)
            {
                var x = (LocalVariableDeclaratorExpression)localVarDeclarator;
                t = t == null ? x.Expression.Type : t;
                var variable = Expression.Parameter(t, x.Identifer);
                list.Variables.Add(variable);
                ParameterList.Add(variable);
                if (x.Expression != null)
                {
                    list.Initializers.Add(Expression.Assign(variable, x.Expression));
                }
            }
            return list;
        }

        public override Expression VisitLocal_variable_declarators(CSharp4Parser.Local_variable_declaratorsContext context)
        {
            var list = new ExpressionList() { Expressions = new List<Expression>() };
            foreach (var lvd in context.local_variable_declarator())
            {
                list.Expressions.Add(Visit(lvd));
            }
            return list;
        }

        public override Expression VisitLocal_variable_declarator(CSharp4Parser.Local_variable_declaratorContext context)
        {
            var lvi = context.local_variable_initializer();
            return new LocalVariableDeclaratorExpression()
            {
                Identifer = context.identifier().GetText(),
                Expression = lvi != null ? Visit(lvi) : null
            };
        }


        public override Expression VisitBracket_expression(CSharp4Parser.Bracket_expressionContext context)
        {
            return Visit(context.expression_list());
        }

        public override Expression VisitObject_creation_expression(CSharp4Parser.Object_creation_expressionContext context)
        {
            return base.VisitObject_creation_expression(context);
        }

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

            var bracketExpressions = context.bracket_expression();
            if (bracketExpressions.Any())
            {
                foreach (var bracketExpression in bracketExpressions)
                {
                    var expressionList = (ExpressionList)Visit(bracketExpression);
                    value = ExpressionHelper.GetPropertyIndex(value, expressionList.Expressions);
                }
            }


            foreach (var part_context in context.primary_expression_part())
            {
                CSharp4Parser.Method_invocation2Context methodInvocation2Context;
                CSharp4Parser.Member_access2Context memberAccess2Context;
                if ((memberAccess2Context = part_context.member_access2()) != null)
                {
                    var identifier = memberAccess2Context.identifier().GetText();

                    List<MethodInfo> methodCandidates = null;

                    var type = value.Type;
                    var isStaticMethodInvocation = false;

                    if (value.NodeType == ExpressionType.Constant)
                    {
                        var valueValue = ((ConstantExpression)value).Value;
                        if (typeof(Type).IsAssignableFrom(valueValue.GetType()))
                        {
                            type = (Type)valueValue;
                            isStaticMethodInvocation = true;
                        }
                    }

                    methodCandidates = MethodResolution.GetCandidateMembers(type, identifier);

                    var isExtensionMethod = false;
                    Expression thisParameter = null;

                    if (!methodCandidates.Any())
                    {
                        if (CompilationContext != null)
                        {
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
                            },
                            MethodCandidates = methodCandidates,
                            IsStaticMethod = isStaticMethodInvocation,
                            IsExtensionMethod = isExtensionMethod,
                            ThisParameter = thisParameter,
                            Type = type,
                            Instance = value
                        });
                    }
                }
                else if ((methodInvocation2Context = part_context.method_invocation2()) != null)
                {
                    var methodInvocationContext = methodInvocationContextStack.Peek();
                    var argListContext = methodInvocation2Context.argument_list();
                    methodInvocationContext.Visitor = this;

                    if (argListContext != null)
                    {
                        methodInvocationContext.ArgumentContext = argListContext.argument();
                    }
                    else
                    {
                        methodInvocationContext.ArgumentContext = new CSharp4Parser.ArgumentContext[0];
                    }

                    value = methodInvocationContext.GetInvokeMethodExpression();
                }
                else if (part_context.OP_INC() != null)
                {
                    value = Expression.Assign(value, Expression.Increment(value));
                }
                else if (part_context.OP_DEC() != null)
                {
                    value = Expression.Assign(value, Expression.Decrement(value));
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
            var implicit_anonymous_function_parameter_context_1 = context.anonymous_function_signature().implicit_anonymous_function_parameter();

            var argtypes = CurrentParameterInfo.ParameterType.GetGenericArguments();
            int i = 0;
            var argtype = argtypes[i];

            var currentTypeInferenceBoundsList = methodInvocationContextStack.Peek().TypeInferenceBoundsList;

            foreach (var bound in currentTypeInferenceBoundsList.FirstOrDefault(x => x.TypeArgument == argtype).Bounds)
            {
                if (implicit_anonymous_function_parameter_context_1 != null)
                {
                    var identfier_text = implicit_anonymous_function_parameter_context_1.identifier().GetText();

                    parameters.Add(new LambdaParameter()
                    {
                        Identifier = identfier_text,
                        Expression = Expression.Parameter(bound, identfier_text)
                    });

                    _lambdaContextStack.Push(new LambdaContext()
                    {
                        Parameters = parameters
                    });
                }

                //if (implicit_anonymous_function_parameter_list_context != null)
                //{
                //    var implicit_anonymous_function_parameter_contexts = implicit_anonymous_function_parameter_list_context.implicit_anonymous_function_parameter();

                //    foreach (var implicit_anonymous_function_parameter_context in implicit_anonymous_function_parameter_contexts)
                //    {
                //        var identfier_text = implicit_anonymous_function_parameter_context.identifier().GetText();

                //        parameters.Add(new LambdaParameter()
                //        {
                //            Identifier = identfier_text,
                //            Expression = Expression.Parameter(bound, identfier_text)
                //        });
                //    }
                //    _lambdaContextStack.Push(new LambdaContext()
                //    {
                //        Parameters = parameters
                //    });
                //}

                var body = Visit(context.anonymous_function_body());

                return Expression.Lambda(body, parameters.Select(x => x.Expression));

            }
            return null;
        }

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

        public override Expression VisitParenExpression(CSharp4Parser.ParenExpressionContext context)
        {
            return Visit(context.expression());
        }
    }
}