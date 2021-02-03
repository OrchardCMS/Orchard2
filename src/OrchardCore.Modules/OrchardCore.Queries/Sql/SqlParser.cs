using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;
using SqlParser.Core.Statements;
using SqlParser.Core.Syntax;
using YesSql;
using SQLParser = SqlParser.Core.SqlParser;

namespace OrchardCore.Queries.Sql
{
    public class SqlParser
    {
        private readonly IEnumerable<SelectStatement> _statements;

        private StringBuilder _builder;
        private IDictionary<string, object> _parameters;
        private ISqlDialect _dialect;
        private string _tablePrefix;
        private HashSet<string> _aliases = new HashSet<string>();
        private ParseTree _tree;
        private static LanguageData language = new LanguageData(new SqlGrammar());
        private Stack<FormattingModes> _modes;

        private string _limit;
        private string _offset;
        private string _select;
        private string _from;
        private string _where;
        private string _having;
        private string _groupBy;
        private string _orderBy;

        private SqlParser(IEnumerable<SelectStatement> statements, ISqlDialect dialect, string tablePrefix, IDictionary<string, object> parameters)
        {
            _statements = statements;
            _dialect = dialect;
            _tablePrefix = tablePrefix;
            _parameters = parameters;
            _builder = new StringBuilder();
            _modes = new Stack<FormattingModes>();
        }

        private SqlParser(ParseTree tree, ISqlDialect dialect, string tablePrefix, IDictionary<string, object> parameters)
        {
            _tree = tree;
            _dialect = dialect;
            _tablePrefix = tablePrefix;
            _parameters = parameters;
            _builder = new StringBuilder(tree.SourceText.Length);
            _builder = new StringBuilder();
            _modes = new Stack<FormattingModes>();
        }

        public static bool TryParse(string sql, ISqlDialect dialect, string tablePrefix, IDictionary<string, object> parameters, out string query, out IEnumerable<string> messages)
        {
            try
            {
                var statements = new SQLParser().Parse(sql);
                if (statements.Count() == 0)
                {
                    query = null;
                    messages = Enumerable.Empty<string>();

                    return false;
                }

                var sqlParser = new SqlParser(statements.OfType<SelectStatement>(), dialect, tablePrefix, parameters);

                query = sqlParser.Evaluate();

                messages = Array.Empty<string>();

                return true;
            }
            catch (SqlParserException se)
            {
                query = null;
                messages = new string[] { se.Message };
            }
            catch (Exception e)
            {
                query = null;
                messages = new string[] { "Unexpected error: " + e.Message };
            }

            return false;
        }

        private string Evaluate()
        {
            var statementsBuilder = new StringBuilder();

            foreach (var selectStatement in _statements)
            {
                statementsBuilder.Append(EvaluateSelectStatement(selectStatement)).Append(';');
            }

            return statementsBuilder.ToString();
        }

        private string EvaluateSelectStatement(SelectStatement statement)
        {
            _select = null;
            _from = null;

            var previousContent = _builder.Length > 0 ? _builder.ToString() : null;
            _builder.Clear();

            var sqlBuilder = _dialect.CreateBuilder(_tablePrefix);

            EvaluateSelectClause(statement.Nodes[0]);

            sqlBuilder.Select();
            sqlBuilder.Selector(_select);

            if (statement.Nodes.Count > 1)
            {
                EvaluateFromClause(statement.Nodes[1]);
            }

            if (!String.IsNullOrEmpty(_from))
            {
                sqlBuilder.From(_from);
            }

            if (previousContent != null)
            {
                _builder.Clear();
                _builder.Append(new StringBuilder(previousContent));
            }

            return sqlBuilder.ToSqlString();
        }

        private void EvaluateSelectClause(SyntaxNode selectClauseNode)
        {
            foreach (var node in selectClauseNode.ChildNodes.Skip(1))
            {
                switch (node.Kind)
                {
                    case SyntaxKind.IdentifierToken:
                        if (node.ChildNodes.Any())
                        {
                            switch (node.ChildNodes.Count)
                            {
                                case 2:
                                    _builder.Append(SurroundWithBrakets(node.Token.Value.ToString()));
                                    _builder.Append(" ").Append(node.ChildNodes[0].Token.Value).Append(" ");
                                    _builder.Append(node.ChildNodes[1].Token.Value);
                                    break;
                                case 3:
                                    _builder.Append(node.Token.Value);
                                    _builder.Append(node.ChildNodes[0].Token.Value);
                                    _builder.Append(SurroundWithBrakets(node.ChildNodes[1].Token.Value.ToString()));
                                    _builder.Append(node.ChildNodes[2].Token.Value);
                                    break;
                                case 5:
                                    _builder.Append(node.Token.Value);
                                    _builder.Append(node.ChildNodes[0].Token.Value);
                                    _builder.Append(SurroundWithBrakets(node.ChildNodes[1].Token.Value.ToString()));
                                    _builder.Append(node.ChildNodes[2].Token.Value);
                                    _builder.Append(" ").Append(node.ChildNodes[3].Token.Value).Append(" ");
                                    _builder.Append(node.ChildNodes[4].Token.Value);
                                    break;
                            }
                        }
                        else
                        {
                            _builder.Append($"[{node.Token.Value}]");
                        }

                        break;
                    case SyntaxKind.AsteriskToken:
                    case SyntaxKind.CommaToken:
                        _builder.Append(node.Token.Value).Append(" ");
                        break;
                    case SyntaxKind.DotToken:
                        AppendCompositeNode(node);
                        break;
                    case SyntaxKind.AsKeyword:
                        if (node.ChildNodes[0].Kind == SyntaxKind.DotToken)
                        {
                            AppendCompositeNode(node.ChildNodes[0]);

                            _builder.Append(node.Token.Value);
                            _builder.Append($"[{node.ChildNodes[1].Token.Value}]");
                        }
                        else
                        {
                            AppendCompositeNode(node);
                        }

                        break;
                }
            }

            _select = _builder.ToString();
        }

        private void EvaluateFromClause(SyntaxNode fromClauseNode)
        {
            _builder.Clear();

            _modes.Push(FormattingModes.FromClause);

            foreach (var node in fromClauseNode.ChildNodes.Skip(1))
            {
                switch (node.Kind)
                {
                    case SyntaxKind.IdentifierToken:
                        _builder.Append(SurroundWithBrakets(_tablePrefix + node.Token.Value.ToString()));
                        break;
                    case SyntaxKind.CommaToken:
                        _builder.Append(node.Token.Value).Append(" ");
                        break;
                    case SyntaxKind.AsKeyword:
                        _builder.Append(SurroundWithBrakets(_tablePrefix + node.ChildNodes[0].Token.Value.ToString()));
                        _builder.Append(" ").Append(node.Token.Value).Append(" ");
                        _builder.Append(node.ChildNodes[1].Token.Value);
                        break;
                }
            }

            _modes.Pop();

            _from = _builder.ToString();
        }

        private void AppendCompositeNode(SyntaxNode node)
        {
            _builder.Append(SurroundWithBrakets(_tablePrefix + node.ChildNodes[0].Token.Value.ToString()));
            _builder.Append(node.Token.Value);
            _builder.Append(SurroundWithBrakets(node.ChildNodes[1].Token.Value.ToString()));
        }

        private static string SurroundWithBrakets(string value) => $"[{value}]";

        private string EvaluateSelectStatement(ParseTreeNode selectStatement)
        {
            _limit = null;
            _offset = null;
            _select = null;
            _from = null;
            _where = null;
            _having = null;
            _groupBy = null;
            _orderBy = null;

            var previousContent = _builder.Length > 0 ? _builder.ToString() : null;
            _builder.Clear();

            var sqlBuilder = _dialect.CreateBuilder(_tablePrefix);

            EvaluateSelectRestriction(selectStatement.ChildNodes[1]);
            EvaluateSelectorList(selectStatement.ChildNodes[2]);

            sqlBuilder.Select();
            sqlBuilder.Selector(_select);

            EvaluateFromClause(selectStatement.ChildNodes[3]);

            if (!String.IsNullOrEmpty(_from))
            {
                sqlBuilder.From(_from);
            }

            EvaluateWhereClause(selectStatement.ChildNodes[4]);

            if (!String.IsNullOrEmpty(_where))
            {
                sqlBuilder.WhereAnd(_where);
            }

            EvaluateGroupClause(selectStatement.ChildNodes[5]);

            if (!String.IsNullOrEmpty(_groupBy))
            {
                sqlBuilder.GroupBy(_groupBy);
            }

            EvaluateHavingClause(selectStatement.ChildNodes[6]);

            if (!String.IsNullOrEmpty(_having))
            {
                sqlBuilder.Having(_having);
            }

            EvaluateOrderClause(selectStatement.ChildNodes[7]);

            if (!String.IsNullOrEmpty(_orderBy))
            {
                sqlBuilder.OrderBy(_orderBy);
            }

            EvaluateLimitClause(selectStatement.ChildNodes[8]);

            if (!String.IsNullOrEmpty(_limit))
            {
                sqlBuilder.Take(_limit);
            }

            EvaluateOffsetClause(selectStatement.ChildNodes[9]);

            if (!String.IsNullOrEmpty(_offset))
            {
                sqlBuilder.Skip(_offset);
            }

            if (previousContent != null)
            {
                _builder.Clear();
                _builder.Append(new StringBuilder(previousContent));
            }

            return sqlBuilder.ToSqlString();
        }

        private void EvaluateLimitClause(ParseTreeNode parseTreeNode)
        {
            if (parseTreeNode.ChildNodes.Count == 0)
            {
                return;
            }

            _builder.Clear();

            // Evaluating so that the value can be transformed as a parameter
            EvaluateExpression(parseTreeNode.ChildNodes[1]);

            _limit = _builder.ToString();
        }

        private void EvaluateOffsetClause(ParseTreeNode parseTreeNode)
        {
            if (parseTreeNode.ChildNodes.Count == 0)
            {
                return;
            }

            _builder.Clear();

            // Evaluating so that the value can be transformed as a parameter
            EvaluateExpression(parseTreeNode.ChildNodes[1]);

            _offset = _builder.ToString();
        }

        private void EvaluateOrderClause(ParseTreeNode parseTreeNode)
        {
            if (parseTreeNode.ChildNodes.Count == 0)
            {
                return;
            }

            _builder.Clear();

            var idList = parseTreeNode.ChildNodes[2];

            _modes.Push(FormattingModes.SelectClause);
            for (var i = 0; i < idList.ChildNodes.Count; i++)
            {
                var id = idList.ChildNodes[i].ChildNodes[0];

                if (i > 0)
                {
                    _builder.Append(", ");
                }

                EvaluateId(id);

                if (idList.ChildNodes[i].ChildNodes[1].ChildNodes.Count > 0)
                {
                    _builder.Append(" ").Append(idList.ChildNodes[i].ChildNodes[1].ChildNodes[0].Term.Name);
                }
            }

            _orderBy = _builder.ToString();

            _modes.Pop();
        }

        private void EvaluateHavingClause(ParseTreeNode parseTreeNode)
        {
            if (parseTreeNode.ChildNodes.Count == 0)
            {
                return;
            }

            _builder.Clear();

            _modes.Push(FormattingModes.SelectClause);
            EvaluateExpression(parseTreeNode.ChildNodes[1]);

            _having = _builder.ToString();

            _modes.Pop();
        }

        private void EvaluateGroupClause(ParseTreeNode parseTreeNode)
        {
            if (parseTreeNode.ChildNodes.Count == 0)
            {
                return;
            }

            _builder.Clear();

            var idList = parseTreeNode.ChildNodes[2];

            _modes.Push(FormattingModes.SelectClause);
            for (var i = 0; i < idList.ChildNodes.Count; i++)
            {
                var columnSource = idList.ChildNodes[i];

                if (i > 0)
                {
                    _builder.Append(", ");
                }

                if (columnSource.ChildNodes[0].Term.Name == "Id")
                {
                    EvaluateId(columnSource.ChildNodes[0]);
                }
                else
                {
                    EvaluateFunCall(columnSource.ChildNodes[0]);
                }
            }

            _groupBy = _builder.ToString();

            _modes.Pop();
        }

        private void EvaluateWhereClause(ParseTreeNode parseTreeNode)
        {
            if (parseTreeNode.ChildNodes.Count == 0)
            {
                // EMPTY
                return;
            }

            _builder.Clear();

            _modes.Push(FormattingModes.SelectClause);
            EvaluateExpression(parseTreeNode.ChildNodes[1]);

            _where = _builder.ToString();

            _modes.Pop();
        }

        private void EvaluateExpression(ParseTreeNode parseTreeNode)
        {
            switch (parseTreeNode.Term.Name)
            {
                case "unExpr":
                    _builder.Append(parseTreeNode.ChildNodes[0].Term.Name);
                    EvaluateExpression(parseTreeNode.ChildNodes[1]);
                    break;
                case "binExpr":
                    EvaluateExpression(parseTreeNode.ChildNodes[0]);
                    _builder.Append(" ");
                    _builder.Append(parseTreeNode.ChildNodes[1].ChildNodes[0].Term.Name).Append(" ");
                    EvaluateExpression(parseTreeNode.ChildNodes[2]);
                    break;
                case "betweenExpr":
                    EvaluateExpression(parseTreeNode.ChildNodes[0]);
                    _builder.Append(" ");
                    if (parseTreeNode.ChildNodes[1].ChildNodes.Count > 0)
                    {
                        _builder.Append("NOT ");
                    }
                    _builder.Append("BETWEEN ");
                    EvaluateExpression(parseTreeNode.ChildNodes[3]);
                    _builder.Append(" ");
                    _builder.Append("AND ");
                    EvaluateExpression(parseTreeNode.ChildNodes[5]);
                    break;
                case "inExpr":
                    EvaluateExpression(parseTreeNode.ChildNodes[0]);
                    _builder.Append(" ");
                    if (parseTreeNode.ChildNodes[1].ChildNodes.Count > 0)
                    {
                        _builder.Append("NOT ");
                    }
                    _builder.Append("IN (");
                    EvaluateInArgs(parseTreeNode.ChildNodes[3]);
                    _builder.Append(")");
                    break;
                // Term and Tuple are transient, to they appear directly
                case "Id":
                    EvaluateId(parseTreeNode);
                    break;
                case "boolean":
                    _builder.Append(_dialect.GetSqlValue(parseTreeNode.ChildNodes[0].Term.Name == "TRUE"));
                    break;
                case "string":
                    _builder.Append(_dialect.GetSqlValue(parseTreeNode.Token.ValueString));
                    break;
                case "number":
                    _builder.Append(_dialect.GetSqlValue(parseTreeNode.Token.Value));
                    break;
                case "funCall":
                    EvaluateFunCall(parseTreeNode);
                    break;
                case "exprList":
                    _builder.Append("(");
                    EvaluateExpression(parseTreeNode.ChildNodes[0]);
                    _builder.Append(")");
                    break;
                case "parSelectStmt":
                    _builder.Append("(");
                    _builder.Append(EvaluateSelectStatement(parseTreeNode.ChildNodes[0]));
                    _builder.Append(")");
                    break;
                case "parameter":
                    var name = parseTreeNode.ChildNodes[1].ChildNodes[0].Token.ValueString;

                    _builder.Append("@" + name);

                    if (_parameters != null && !_parameters.ContainsKey(name))
                    {
                        // If a parameter is not set and there is no default value, report it
                        if (parseTreeNode.ChildNodes.Count < 3)
                        {
                            throw new SqlParserException("Missing parameters: " + name);
                        }
                        else
                        {
                            if (parseTreeNode.ChildNodes[3].Token != null)
                            {
                                _parameters[name] = parseTreeNode.ChildNodes[3].Token.Value;
                            }
                            else
                            {
                                // example: true
                                if (parseTreeNode.ChildNodes[3].ChildNodes[0].Token != null)
                                {
                                    _parameters[name] = parseTreeNode.ChildNodes[3].ChildNodes[0].Token.Value;
                                }
                                else
                                {
                                    throw new SqlParserException("Unsupported syntax for parameter: " + name);
                                }
                            }
                        }
                    }

                    break;
                case "*":
                    _builder.Append("*");
                    break;
            }
        }

        private void EvaluateInArgs(ParseTreeNode inArgs)
        {
            if (inArgs.ChildNodes[0].Term.Name == "selectStatement")
            {
                // selectStatement
                _builder.Append(EvaluateSelectStatement(inArgs.ChildNodes[0]));
            }
            else
            {
                // expressionList
                EvaluateExpressionList(inArgs.ChildNodes[0]);
            }
        }

        private void EvaluateFunCall(ParseTreeNode funCall)
        {
            var funcName = funCall.ChildNodes[0].ChildNodes[0].Token.ValueString;
            IList<string> arguments;
            var tempBuilder = _builder;

            if (funCall.ChildNodes[1].ChildNodes[0].Term.Name == "selectStatement")
            {
                // selectStatement
                _builder = new StringBuilder();
                _builder.Append(EvaluateSelectStatement(funCall.ChildNodes[1].ChildNodes[0]));
                arguments = new string[] { _builder.ToString() };
                _builder = tempBuilder;
            }
            else if (funCall.ChildNodes[1].ChildNodes[0].Term.Name == "*")
            {
                arguments = new string[] { "*" };
            }
            else
            {
                // expressionList
                arguments = new List<string>();
                for (var i = 0; i < funCall.ChildNodes[1].ChildNodes[0].ChildNodes.Count; i++)
                {
                    _builder = new StringBuilder();
                    EvaluateExpression(funCall.ChildNodes[1].ChildNodes[0].ChildNodes[i]);
                    arguments.Add(_builder.ToString());
                    _builder = tempBuilder;
                }
            }

            _builder.Append(_dialect.RenderMethod(funcName, arguments.ToArray()));
        }

        private void EvaluateExpressionList(ParseTreeNode expressionList)
        {
            for (var i = 0; i < expressionList.ChildNodes.Count; i++)
            {
                if (i > 0)
                {
                    _builder.Append(", ");
                }

                EvaluateExpression(expressionList.ChildNodes[i]);
            }
        }

        private void EvaluateFromClause(ParseTreeNode parseTreeNode)
        {
            if (parseTreeNode.ChildNodes.Count == 0)
            {
                // EMPTY
                return;
            }

            _builder.Clear();

            var aliasList = parseTreeNode.ChildNodes[1];

            _modes.Push(FormattingModes.FromClause);
            EvaluateAliasList(aliasList);
            _modes.Pop();

            var joins = parseTreeNode.ChildNodes[2];

            // process join statements
            if (joins.ChildNodes.Count != 0)
            {
                foreach (var joinStatement in joins.ChildNodes)
                {
                    _modes.Push(FormattingModes.FromClause);

                    var jointKindOpt = joinStatement.ChildNodes[0];

                    if (jointKindOpt.ChildNodes.Count > 0)
                    {
                        _builder.Append(" ").Append(jointKindOpt.ChildNodes[0].Term.Name);
                    }

                    _builder.Append(" JOIN ");

                    EvaluateAliasList(joinStatement.ChildNodes[2]);

                    _builder.Append(" ON ");
                    _modes.Push(FormattingModes.SelectClause);

                    EvaluateId(joinStatement.ChildNodes[4]);

                    _builder.Append(" = ");

                    EvaluateId(joinStatement.ChildNodes[6]);
                    _modes.Pop();
                }
            }

            _from = _builder.ToString();
        }

        private void EvaluateAliasList(ParseTreeNode aliasList)
        {
            for (var i = 0; i < aliasList.ChildNodes.Count; i++)
            {
                var aliasItem = aliasList.ChildNodes[i];

                if (i > 0)
                {
                    _builder.Append(", ");
                }

                EvaluateId(aliasItem.ChildNodes[0]);

                if (aliasItem.ChildNodes.Count > 1)
                {
                    EvaluateAliasOptional(aliasItem.ChildNodes[1]);
                }
            }
        }

        private void EvaluateSelectorList(ParseTreeNode parseTreeNode)
        {
            var selectorList = parseTreeNode.ChildNodes[0];

            if (selectorList.Term.Name == "*")
            {
                _builder.Append("*");
            }
            else
            {
                _modes.Push(FormattingModes.SelectClause);

                // columnItemList
                for (var i = 0; i < selectorList.ChildNodes.Count; i++)
                {
                    if (i > 0)
                    {
                        _builder.Append(", ");
                    }

                    var columnItem = selectorList.ChildNodes[i];

                    // columnItem
                    var columnSource = columnItem.ChildNodes[0];
                    var funCallOrId = columnSource.ChildNodes[0];
                    if (funCallOrId.Term.Name == "Id")
                    {
                        EvaluateId(funCallOrId);
                    }
                    else
                    {
                        EvaluateFunCall(funCallOrId);
                    }

                    if (columnItem.ChildNodes.Count > 1)
                    {
                        // AS
                        EvaluateAliasOptional(columnItem.ChildNodes[1]);
                    }
                }

                _modes.Pop();
            }

            _select = _builder.ToString();
        }

        private void EvaluateId(ParseTreeNode id)
        {
            switch (_modes.Peek())
            {
                case FormattingModes.SelectClause:
                    EvaluateSelectId(id);
                    break;
                case FormattingModes.FromClause:
                    EvaluateFromId(id);
                    break;
            }
        }

        private void EvaluateSelectId(ParseTreeNode id)
        {
            for (var i = 0; i < id.ChildNodes.Count; i++)
            {
                if (i == 0 && id.ChildNodes.Count > 1 && !_aliases.Contains(id.ChildNodes[i].Token.ValueString))
                {
                    _builder.Append(_dialect.QuoteForTableName(_tablePrefix + id.ChildNodes[i].Token.ValueString));
                }
                else
                {
                    if (i > 0)
                    {
                        _builder.Append(".");
                    }

                    if (_aliases.Contains(id.ChildNodes[i].Token.ValueString))
                    {
                        _builder.Append(id.ChildNodes[i].Token.ValueString);
                    }
                    else
                    {
                        _builder.Append(_dialect.QuoteForColumnName(id.ChildNodes[i].Token.ValueString));
                    }
                }
            }
        }

        private void EvaluateFromId(ParseTreeNode id)
        {
            for (var i = 0; i < id.ChildNodes.Count; i++)
            {
                if (i == 0 && !_aliases.Contains(id.ChildNodes[i].Token.ValueString))
                {
                    _builder.Append(_dialect.QuoteForTableName(_tablePrefix + id.ChildNodes[i].Token.ValueString));
                }
                else
                {
                    _builder.Append(_dialect.QuoteForColumnName(id.ChildNodes[i].Token.ValueString));
                }
            }
        }

        private void EvaluateAliasOptional(ParseTreeNode parseTreeNode)
        {
            if (parseTreeNode.ChildNodes.Count > 0)
            {
                _builder.Append(" AS ");
                _builder.Append(parseTreeNode.ChildNodes[0].Token.ValueString);
            }
        }

        private void EvaluateSelectRestriction(ParseTreeNode parseTreeNode)
        {
            _builder.Clear();

            if (parseTreeNode.ChildNodes.Count > 0)
            {
                _builder.Append(parseTreeNode.ChildNodes[0].Term.Name).Append(" ");
            }
        }

        private enum FormattingModes
        {
            SelectClause,
            FromClause
        }
    }
}
