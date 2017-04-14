using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser
{
    public class ParameterList
    {
        private readonly Stack<List<ParameterExpression>> _parameterListStack = new Stack<List<ParameterExpression>>();
        private List<ParameterExpression> _parameters = new List<ParameterExpression>();

        public List<ParameterExpression> Current
        {
            get { return _parameters; }
        }

        public void Push()
        {
            _parameterListStack.Push(_parameters);
            _parameters = new List<ParameterExpression>();
        }

        public void Pop()
        {
            _parameters = _parameterListStack.Pop();
        }

        public void Add(ParameterExpression parameter)
        {
            if (_parameterListStack.SelectMany(x => x.Select(y => y)).Concat(_parameters).All(p => p.Name != parameter.Name))
            {
                _parameters.Add(parameter);
            }
            else
            {
                throw new Exception(string.Format("A local variable named '{0}' cannot be declared in this scope because it would give a different meaning to '{0}', which is already used in a 'parent or current' scope to denote something else", parameter.Name));
            }

        }

        public void Add(List<ParameterExpression> list)
        {
            foreach (var parameterExpression in list)
            {
                Add(parameterExpression);
            }
        }


        public bool TryGetValue(string name, out ParameterExpression parameter)
        {
            parameter = _parameterListStack.SelectMany(x => x.Select(y => y))
                .Concat(_parameters)
                .SingleOrDefault(p => p.Name == name);
            return parameter != null;
        }

        public void Remove(List<ParameterExpression> list)
        {
            foreach (var parameterExpression in list)
            {
                ParameterExpression p;

                if (TryGetValue(parameterExpression.Name, out p))
                {
                    _parameters.Remove(parameterExpression);
                }
            }
        }

    }
}