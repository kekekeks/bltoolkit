﻿using System;
using System.Linq;
using System.Linq.Expressions;

namespace BLToolkit.Data.Linq.Parser
{
	using BLToolkit.Linq;
	using Data.Sql;

	class JoinParser : MethodCallParser
	{
		protected override bool CanParseMethodCall(ExpressionParser parser, MethodCallExpression methodCall, ParseInfo parseInfo)
		{
			if (!methodCall.IsQueryable("Join", "GroupJoin") || methodCall.Arguments.Count != 5)
				return false;

			var body = ((LambdaExpression)methodCall.Arguments[2].Unwrap()).Body.Unwrap();

			if (body.NodeType == ExpressionType	.MemberInit)
			{
				var mi = (MemberInitExpression)body;
				bool throwExpr;

				if (mi.NewExpression.Arguments.Count > 0 || mi.Bindings.Count == 0)
					throwExpr = true;
				else
					throwExpr = mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment);

				if (throwExpr)
					throw new NotSupportedException(string.Format("Explicit construction of entity type '{0}' in join is not allowed.", body.Type));
			}

			return true;
		}

		protected override IParseContext ParseMethodCall(ExpressionParser parser, MethodCallExpression methodCall, ParseInfo parseInfo)
		{
			var isGroup      = methodCall.Method.Name == "GroupJoin";
			var outerContext = parser.ParseSequence(new ParseInfo(parseInfo, methodCall.Arguments[0], parseInfo.SqlQuery));
			var innerContext = parser.ParseSequence(new ParseInfo(parseInfo, methodCall.Arguments[1], new SqlQuery()));
			var countContext = parser.ParseSequence(new ParseInfo(parseInfo, methodCall.Arguments[1], new SqlQuery()));

			var context  = new SubQueryContext(outerContext);
			innerContext = isGroup ? new GroupJoinSubQueryContext(innerContext, methodCall) : new SubQueryContext(innerContext);
			countContext = new SubQueryContext(countContext);

			var join = innerContext.SqlQuery.InnerJoin();
			var sql  = context.SqlQuery;

			sql.From.Tables[0].Joins.Add(join.JoinedTable);

			var selector = (LambdaExpression)methodCall.Arguments[4].Unwrap();

			context.SetAlias(selector.Parameters[0].Name);
			innerContext.SetAlias(selector.Parameters[1].Name);

			var outerKeyLambda = ((LambdaExpression)methodCall.Arguments[2].Unwrap());
			var innerKeyLambda = ((LambdaExpression)methodCall.Arguments[3].Unwrap());

			var outerKeySelector = outerKeyLambda.Body.Unwrap();
			var innerKeySelector = innerKeyLambda.Body.Unwrap();

			var outerParent = context.     Parent;
			var innerParent = innerContext.Parent;
			var countParent = countContext.Parent;

			var outerKeyContext = new ExpressionContext(parseInfo.Parent, context,      outerKeyLambda);
			var innerKeyContext = new ExpressionContext(parseInfo.Parent, innerContext, innerKeyLambda);
			var countKeyContext = new ExpressionContext(parseInfo.Parent, countContext, innerKeyLambda);

			// Process counter.
			//
			var counterSql = ((SubQueryContext)countContext).SqlQuery;

			// Make join and where for the counter.
			//
			if (outerKeySelector.NodeType == ExpressionType.New)
			{
				var new1 = (NewExpression)outerKeySelector;
				var new2 = (NewExpression)innerKeySelector;

				for (var i = 0; i < new1.Arguments.Count; i++)
				{
					var arg1 = new1.Arguments[i];
					var arg2 = new2.Arguments[i];

					ParseJoin(parser, join, outerKeyContext, arg1, innerKeyContext, arg2, countKeyContext, counterSql);
				}
			}
			else if (outerKeySelector.NodeType == ExpressionType.MemberInit)
			{
				var mi1 = (MemberInitExpression)outerKeySelector;
				var mi2 = (MemberInitExpression)innerKeySelector;

				for (var i = 0; i < mi1.Bindings.Count; i++)
				{
					if (mi1.Bindings[i].Member != mi2.Bindings[i].Member)
						throw new LinqException(string.Format("List of member inits does not match for entity type '{0}'.", outerKeySelector.Type));

					var arg1 = ((MemberAssignment)mi1.Bindings[i]).Expression;
					var arg2 = ((MemberAssignment)mi2.Bindings[i]).Expression;

					ParseJoin(parser, join, outerKeyContext, arg1, innerKeyContext, arg2, countKeyContext, counterSql);
				}
			}
			else
			{
				ParseJoin(parser, join, outerKeyContext, outerKeySelector, innerKeyContext, innerKeySelector, countKeyContext, counterSql);
			}

			context.     Parent = outerParent;
			innerContext.Parent = innerParent;
			countContext.Parent = countParent;

			if (isGroup)
			{
				counterSql.ParentSql = sql;
				counterSql.Select.Columns.Clear();

				var inner = (GroupJoinSubQueryContext)innerContext;

				inner.Join       = join.JoinedTable;
				inner.CounterSql = counterSql;
				return new GroupJoinContext(parseInfo.Parent, selector, context, inner);
			}

			return new JoinContext(parseInfo.Parent, selector, context, innerContext);
		}

		static void ParseJoin(
			ExpressionParser         parser,
			SqlQuery.FromClause.Join join,
			ExpressionContext outerKeyContext, Expression outerKeySelector,
			ExpressionContext innerKeyContext, Expression innerKeySelector,
			ExpressionContext countKeyContext, SqlQuery countSql)
		{
			var predicate = parser.ParseObjectComparison(
				ExpressionType.Equal,
				outerKeyContext, outerKeySelector,
				innerKeyContext, innerKeySelector);

			if (predicate != null)
				join.JoinedTable.Condition.Conditions.Add(new SqlQuery.Condition(false, predicate));
			else
				join
					.Expr(parser.ParseExpression(outerKeyContext, outerKeySelector)).Equal
					.Expr(parser.ParseExpression(innerKeyContext, innerKeySelector));

			predicate = parser.ParseObjectComparison(
				ExpressionType.Equal,
				outerKeyContext, outerKeySelector,
				countKeyContext, innerKeySelector);

			if (predicate != null)
				countSql.Where.SearchCondition.Conditions.Add(new SqlQuery.Condition(false, predicate));
			else
				countSql.Where
					.Expr(parser.ParseExpression(outerKeyContext, outerKeySelector)).Equal
					.Expr(parser.ParseExpression(countKeyContext, innerKeySelector));
		}

		internal class JoinContext : SelectContext
		{
			public JoinContext(IParseContext parent, LambdaExpression lambda, IParseContext outerContext, IParseContext innerContext)
				: base(parent, lambda, outerContext, innerContext)
			{
			}
		}

		internal class GroupJoinContext : JoinContext
		{
			public GroupJoinContext(
				IParseContext            parent,
				LambdaExpression         lambda,
				IParseContext            outerContext,
				GroupJoinSubQueryContext innerContext)
				: base(parent, lambda, outerContext, innerContext)
			{
				innerContext.GroupJoin = this;
			}
		}

		internal class GroupJoinSubQueryContext : SubQueryContext
		{
			readonly MethodCallExpression _methodCall;

			public SqlQuery.JoinedTable Join;
			public SqlQuery             CounterSql;
			public GroupJoinContext     GroupJoin;

			public GroupJoinSubQueryContext(IParseContext subQuery, MethodCallExpression methodCall)
				: base(subQuery)
			{
				_methodCall = methodCall;
			}

			Expression BuildJoin()
			{
				throw new NotImplementedException();
			}

			public override Expression BuildExpression(Expression expression, int level)
			{
				if (expression == null)
					return BuildJoin();

				return base.BuildExpression(expression, level);
			}

			public override IParseContext GetContext(Expression expression, int level, ParseInfo parseInfo)
			{
				if (expression == null)
					return this;

				return base.GetContext(expression, level, parseInfo);
			}

			Expression _counterExpression;
			SqlInfo[]  _counterInfo;

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				if (expression != null && expression == _counterExpression)
					return _counterInfo ?? (_counterInfo = new[]
					{
						new SqlInfo
						{
							Query = CounterSql.ParentSql,
							Index = CounterSql.ParentSql.Select.Add(CounterSql),
							Sql   = CounterSql
						}
					});

				return base.ConvertToIndex(expression, level, flags);
			}

			public SqlQuery GetCounter(Expression expr)
			{
				Join.IsWeak = true;

				_counterExpression = expr;

				return CounterSql;
			}
		}
	}
}
