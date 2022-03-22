﻿using System;
using QueryBuilder.Core.Enums;
using QueryBuilder.Core.Interfaces;
using QueryBuilder.Core.Models;

namespace QueryBuilder.Parser
{
    public class Parser : IParser
    {
        private readonly IScanner _scanner;
        private readonly ILogger _logger;
        private Token _lookAhead;

        public Parser(IScanner scanner, ILogger logger)
        {
            this._scanner = scanner;
            this._logger = logger;
            this._lookAhead = this._scanner.GetNextToken();
        }

        public void Parse()
        {
            Code();
        }

        private void Code()
        {
            Match(TokenType.DefKeyword);
            Match(TokenType.TablesKeyword);
            TableDef();
            TableDefs();
            Match(TokenType.EndKeyWord);
            Match(TokenType.DefKeyword);
            Match(TokenType.RelationshipsKeyword);
            Relationships();
            Match(TokenType.EndKeyWord);
            Queries();
        }

        private void Relationships()
        {
            if (_lookAhead == TokenType.Identifier)
            {
                Relationship();
                Relationships();
            }
        }

        private void Relationship()
        {
            Match(TokenType.Identifier);
            Match(TokenType.Dot);
            if (_lookAhead == TokenType.ManyKeyword)
            {
                Match(TokenType.ManyKeyword);
            }
            else
            {
                Match(TokenType.OneKeyword);
            }

            Match(TokenType.LeftParens);
            Match(TokenType.Identifier);
            Match(TokenType.RightParens);
            Match(TokenType.Semicolon);
        }

        private void Queries()
        {
            if (_lookAhead == TokenType.Identifier)
            {
                Query();
                Queries();
            }
        }

        private void Query()
        {
            Match(TokenType.Identifier);
            Match(TokenType.Dot);
            switch (_lookAhead.TokenType)
            {
                case TokenType.AddKeyword:
                    Insert();
                    break;
                case TokenType.UpdateKeyword:
                    Match(TokenType.UpdateKeyword);
                    Match(TokenType.LeftParens);
                    Json();
                    Match(TokenType.RightParens);
                    Update();
                    break;
                default:
                    Match(TokenType.SelectKeyword);
                    Match(TokenType.LeftParens);
                    Args();
                    Match(TokenType.RightParens);
                    Select();
                    break;
            }
        }

        private void Select()
        {
            if (_lookAhead == TokenType.Semicolon)
            {
                Match(TokenType.Semicolon);
            }
            else
            {
                Match(TokenType.Dot);
                Match(TokenType.WhereKeyword);
                Match(TokenType.LeftParens);
                LogicalOrExpr();
                Match(TokenType.RightParens);
                Match(TokenType.Semicolon);
            }
        }

        private void Args()
        {
            Match(TokenType.Identifier);
            ArgsPrime();
        }

        private void ArgsPrime()
        {
            if (_lookAhead == TokenType.Comma)
            {
                Match(TokenType.Comma);
                Match(TokenType.Identifier);
                ArgsPrime();
            }
        }


        private void Update()
        {
            if (_lookAhead == TokenType.Semicolon)
            {
                Match(TokenType.Semicolon);
            }
            else
            {
                Match(TokenType.Dot);
                Match(TokenType.WhereKeyword);
                Match(TokenType.LeftParens);
                LogicalOrExpr();
                Match(TokenType.RightParens);
                Match(TokenType.Semicolon);
            }
        }

        private void Json()
        {
            Match(TokenType.LeftBrace);
            JsonElementsOptional();
            Match(TokenType.RightBrace);
        }

        private void JsonElementsOptional()
        {
            if (_lookAhead == TokenType.Identifier)
            {
                JsonElements();
            }
        }

        private void JsonElements()
        {
            JsonElementBlock();
            while (_lookAhead == TokenType.Comma)
            {
                Match(TokenType.Comma);
                JsonElementBlock();
            }
        }

        private void JsonElementBlock()
        {
            Match(TokenType.Identifier);
            Match(TokenType.Colon);
            LogicalOrExpr();
        }

        private void Insert()
        {
            Match(TokenType.AddKeyword);
            Match(TokenType.LeftParens);
            Json();
            Match(TokenType.RightParens);
            Match(TokenType.Semicolon);
        }

        private void TableDefs()
        {
            if (_lookAhead == TokenType.Identifier)
            {
                TableDef();
                TableDefs();
            }
        }

        private void TableDef()
        {
            Match(TokenType.Identifier);
            Match(TokenType.LeftBrace);
            TableColumns();
            Match(TokenType.RightBrace);
        }

        private void TableColumns()
        {
            while (_lookAhead == TokenType.LeftBracket || _lookAhead == TokenType.Identifier)
            {
                switch (_lookAhead)
                {
                    case { TokenType: TokenType.LeftBracket }:
                        Match(TokenType.LeftBracket);
                        Match(TokenType.PrimaryKeyword);
                        Match(TokenType.RightBracket);
                        Match(TokenType.Identifier);
                        Match(TokenType.Colon);
                        Type();
                        Match(TokenType.Semicolon);
                        break;
                    default:
                        Match(TokenType.Identifier);
                        Match(TokenType.Colon);
                        Type();
                        Match(TokenType.Semicolon);
                        break;
                }
            }
        }
        private void Type()
        {
            switch (_lookAhead)
            {
                case {TokenType: TokenType.IntKeyword}:
                    Match(TokenType.IntKeyword);
                    break;
                case {TokenType: TokenType.FloatKeyword}:
                    Match(TokenType.FloatKeyword);
                    break;
                case {TokenType: TokenType.BoolKeyword}:
                    Match(TokenType.BoolKeyword);
                    break;
                default:
                    Match(TokenType.StringKeyword);
                    break;
            }
        }

        private void LogicalOrExpr()
        {
            LogicalAndExpr();
            while (this._lookAhead.TokenType == TokenType.LogicalOr)
            {
                this.Move();
                LogicalAndExpr();
            }
        }

        private void LogicalAndExpr()
        {
            Eq();
            while (this._lookAhead.TokenType == TokenType.LogicalAnd)
            {
                this.Move();
                Eq();
            }
        }

        private void Eq()
        {
            Rel();
            while (this._lookAhead.TokenType == TokenType.Equal)
            {
                this.Move();
                Rel();
            }
        }


        private void Rel()
        {
            Expr();
            while (this._lookAhead.TokenType == TokenType.LessThan ||
                   this._lookAhead.TokenType == TokenType.GreaterThan ||
                   this._lookAhead.TokenType == TokenType.LessOrEqualThan ||
                   this._lookAhead.TokenType == TokenType.GreaterOrEqualThan)
            {
                this.Move();
                Expr();
            }
        }

        private void Expr()
        {
            Term();
            while (this._lookAhead.TokenType == TokenType.Plus || this._lookAhead.TokenType == TokenType.Minus)
            {
                this.Move();
                Term();
            }
        }

        private void Term()
        {
            Factor();
            while (this._lookAhead.TokenType == TokenType.Multiplication ||
                   this._lookAhead.TokenType == TokenType.Division)
            {
                this.Move();
                Factor();
            }
        }

        private void Factor()
        {
            switch (this._lookAhead.TokenType)
            {
                case TokenType.LeftParens:
                    this.Match(TokenType.LeftParens);
                    LogicalOrExpr();
                    this.Match(TokenType.RightParens);
                    break;
                case TokenType.Identifier:
                    Match(TokenType.Identifier);
                    break;
                case TokenType.IntConstant:
                    this.Match(TokenType.IntConstant);
                    break;
                case TokenType.FloatConstant:
                    this.Match(TokenType.FloatConstant);
                    break;
                case TokenType.TrueKeyword:
                    this.Match(TokenType.TrueKeyword);
                    break;
                case TokenType.FalseKeyword:
                    this.Match(TokenType.FalseKeyword);
                    break;
                default:
                    this.Match(TokenType.StringLiteral);
                    break;
            }
        }

        private void Move()
        {
            this._lookAhead = this._scanner.GetNextToken();
        }

        private void Match(TokenType expectedTokenType)
        {
            if (this._lookAhead != expectedTokenType)
            {
                this._logger.Error(
                    $"Syntax Error! expected token {expectedTokenType} but found {this._lookAhead.TokenType} on line {this._lookAhead.Line} and column {this._lookAhead.Column}");
                throw new ApplicationException(
                    $"Syntax Error! expected token {expectedTokenType} but found {this._lookAhead.TokenType} on line {this._lookAhead.Line} and column {this._lookAhead.Column}");
            }

            this.Move();
        }
    }
}