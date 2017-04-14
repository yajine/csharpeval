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
        public TypeRegistry TypeRegistry { get; set; }
        public Expression Scope { get; set; }
        public CompilationContext CompilationContext { get; set; }
        public ParameterList ParameterList = new ParameterList();
        public CompilerState CompilerState = new CompilerState();
        Stack<MethodInvocationContext> methodInvocationContextStack = new Stack<MethodInvocationContext>();
        Stack<TypeInferenceBounds> typeInferenceBoundsStack = new Stack<TypeInferenceBounds>();


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


        private ParameterExpression MakeParameterWithExpression(string typeName, string identifier, Expression expression)
        {
            Type type = null;

            if (typeName == "var")
            {
                if (expression.Type.IsGenericType)
                {
                    type = expression.Type.GetGenericArguments()[0];
                }
                else if (expression.Type.IsArray)
                {
                    type = expression.Type.GetElementType();
                }
                else
                {
                    type = expression.Type;
                }
            }
            else
            {
                type = GetType(typeName);
            }

            var parameter = Expression.Parameter(type, identifier);


            return parameter;
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
            var list = new ArgumentListExpression();
            foreach (var argumentContext in context.argument())
            {
                list.Add((ArgumentExpression) Visit(argumentContext));
            }
            return list;
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
            ParameterList.Push();
            foreach (var statement in context.statement())
            {
                var ex = Visit(statement);
                if (ex.GetType() == typeof(ExpressionList))
                {
                    expressions.AddRange(((ExpressionList)ex).Expressions);
                }
                else
                                if (ex.GetType() == typeof(LocalVariableDeclarationExpression))
                {
                    expressions.AddRange(((LocalVariableDeclarationExpression)ex).Initializers);
                }
                else
                {
                    expressions.Add(ex);
                }
            }

            var variables = ParameterList.Current;
            var retval =  Expression.Block(variables, expressions);
            ParameterList.Pop();
            return retval;
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
            var typeName = context.local_variable_type().type().GetText();
            var lvds = (ExpressionList)Visit(context.local_variable_declarators());

            if (typeName != "var")
            {
                var type = GetType(typeName);
                foreach (var localVarDeclarator in lvds.Expressions)
                {
                    var x = (LocalVariableDeclaratorExpression)localVarDeclarator;
                    var variable = Expression.Parameter(type, x.Identifer);
                    ParameterList.Add(variable);
                    if (x.Expression != null)
                    {
                        list.Initializers.Add(Expression.Assign(variable, x.Expression));
                    }
                }
            }
            else
            {
                if (lvds.Expressions.Count > 1)
                {
                    throw new CompilerException("Implicitly-typed local variables cannot have multiple declarators");
                }
                var expression = (LocalVariableDeclaratorExpression) lvds.Expressions[0];
                var variable = MakeParameterWithExpression(typeName, expression.Identifer, expression.Expression);
                ParameterList.Add(variable);
                list.Initializers.Add(Expression.Assign(variable, expression.Expression));

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

                var partBracketExpressions = part_context.bracket_expression();
                if (partBracketExpressions.Any())
                {
                    foreach (var bracketExpression in partBracketExpressions)
                    {
                        var expressionList = (ExpressionList)Visit(bracketExpression);
                        value = ExpressionHelper.GetPropertyIndex(value, expressionList.Expressions);
                    }
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


        public override Expression VisitParenExpression(CSharp4Parser.ParenExpressionContext context)
        {
            return Visit(context.expression());
        }

        public override Expression VisitNewExpression(CSharp4Parser.NewExpressionContext context)
        {
            var objectCreationExpressionTree = context.object_creation_expression2();
            if (objectCreationExpressionTree != null)
            {
                var argList = (ArgumentListExpression)Visit(context.object_creation_expression2().argument_list());
                return ExpressionHelper.New(GetType(context.type().GetText()), argList);
            }

            throw new NotImplementedException();
        }


        public override Expression VisitObject_creation_expression2(CSharp4Parser.Object_creation_expression2Context context)
        {
           // context.object_or_collection_initializer()

            return base.VisitObject_creation_expression2(context);
        }

//        public object_creation_expression returns[Expression value]: 
//    // 'new'
//    type
//        ( '('   argument_list?   ')'  first= object_or_collection_initializer ?
//          | second = object_or_collection_initializer )
//        {
//			$value = ExpressionHelper.New(GetType($type.text), $argument_list.values, $first.value ?? $second.value);
//        }
//    ;
//        public object_or_collection_initializer returns[ObjectOrCollectionInitializer value]
//@init{
//	$value = new ObjectOrCollectionInitializer();
//    }: 
//    '{'  (object_initializer  { $value.ObjectInitializer = $object_initializer.value; }
//        | collection_initializer)    '}';
//public collection_initializer: 
//    element_initializer_list  ;
//public element_initializer_list: 
//    element_initializer(',' element_initializer)* ;
//public element_initializer: 
//    non_assignment_expression 
//    | '{'   expression_list   '}' ;
//// object-initializer eg's
////    Rectangle r = new Rectangle {
////        P1 = new Point { X = 0, Y = 1 },
////        P2 = new Point { X = 2, Y = 3 }
////    };
//// TODO: comma should only follow a member_initializer_list
//public object_initializer returns[List < MemberInitializer > value]: 
//    member_initializer_list?  { $value = $member_initializer_list.value; }  ;
//public member_initializer_list returns[List < MemberInitializer > value]
//@init {
//	$value = new List<MemberInitializer>();
//}: 
//    first=member_initializer { $value.Add($first.value); }  (',' succeeding=member_initializer { $value.Add($succeeding.value); } ) ;
//public member_initializer returns[MemberInitializer value] : 
//    identifier   '='   initializer_value { $value = new MemberInitializer() { Identifier = $identifier.text, Value = $initializer_value.value }; };
//public initializer_value returns[Expression value]: 
//    expression { $value = $expression.value; }
//    | object_or_collection_initializer ;

    }
}