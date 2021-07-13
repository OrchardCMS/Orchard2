using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PoParser.Core;
using PoParser.Core.Statements;

namespace OrchardCore.Localization.PortableObject
{
    /// <summary>
    /// Represents a parser for portable objects.
    /// </summary>
    public class PortableObjectParser
    {
        /// <summary>
        /// Parses a .po file.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader"/>.</param>
        /// <returns>A list of culture records.</returns>
        public IEnumerable<CultureDictionaryRecord> Parse(TextReader reader)
        {
            var entryBuilder = new DictionaryRecordBuilder();
            var content = reader.ReadToEnd();
            var parser = new PoParser.Core.PoParser();
            var syntaxTree = parser.Parse(content);

            foreach (var statement in syntaxTree.Statements)
            {
                if (statement.Kind == StatementKind.Comment || statement.Kind == StatementKind.Literal)
                {
                    continue;
                }

                // msgid or msgctxt are first lines of the entry. If builder contains valid entry return it and start building a new one.
                if ((statement.Kind == StatementKind.MessageId || statement.Kind == StatementKind.PluralMessageId || statement.Kind == StatementKind.Context) && entryBuilder.ShouldFlushRecord)
                {
                    yield return entryBuilder.BuildRecordAndReset();
                }

                entryBuilder.Set(statement);
            }

            if (entryBuilder.ShouldFlushRecord)
            {
                yield return entryBuilder.BuildRecordAndReset();
            }
        }

        private class DictionaryRecordBuilder
        {
            public DictionaryRecordBuilder()
            {
                Values = new List<string>();
            }

            public IList<string> Values { get; }

            public IEnumerable<string> ValidValues => Values.Where(v => !String.IsNullOrEmpty(v));

            public bool IsValid => !String.IsNullOrEmpty(MessageId) && ValidValues.Any();

            public bool ShouldFlushRecord => IsValid && Kind == StatementKind.Translation;

            public string MessageId { get; private set; }

            public string MessageContext { get; private set; }

            public StatementKind Kind { get; set; }

            public void Set(Statement statement)
            {
                switch (statement.Kind)
                {
                    case StatementKind.MessageId:
                        // If the MessageId has been set to an empty string and now gets set again
                        // before flushing the values should be reset.
                        if (String.IsNullOrEmpty(MessageId))
                        {
                            Values.Clear();
                        }

                        MessageId = (statement as MessageIdentifierStatement).Identifier;

                        break;
                    case StatementKind.Context:
                        MessageContext = (statement as MessageContextStatement).Context;
                        break;
                    case StatementKind.Translation:
                        if (statement is PluralTranslationStatement)
                        {
                            Values.Add((statement as PluralTranslationStatement).Value);
                        }
                        else
                        {
                            Values.Add((statement as TranslationStatement).Value);
                        }

                        break;
                }

                Kind = statement.Kind;
            }

            public CultureDictionaryRecord BuildRecordAndReset()
            {
                if (!IsValid)
                {
                    return null;
                }

                var result = new CultureDictionaryRecord(MessageId, MessageContext, ValidValues.ToArray());
                MessageId = null;
                MessageContext = null;

                Values.Clear();

                return result;
            }
        }
    }
}
