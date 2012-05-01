using System.Collections.Generic;
using System.Linq.Expressions;

namespace FastLinq
{
    public class ParameterToConstantVisitor : ExpressionVisitor
    {
        Dictionary<string, Expression> _replaces;

        public Expression Replace(Expression expression, Dictionary<string, Expression> replaces)
        {
            _replaces = replaces;

            return Visit(expression);
        }
#if RUNNING_ON_4
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Expression body = Visit(node.Body);
            if (body != node.Body)
            {
                return Expression.Lambda(node.Type, body, node.Parameters);
            }
            return node;
        }
#endif
#if NOT_RUNNING_ON_4
        protected virtual Expression VisitLambda(LambdaExpression node)
        {
            Expression body = Visit(node.Body);
            if (body != node.Body)
            {
                return Expression.Lambda(node.Type, body, node.Parameters);
            }
            return node;
        }
#endif
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (_replaces.ContainsKey(node.Name))
            {
                return _replaces[node.Name];
            }
            return base.VisitParameter(node);
        }
    }
}