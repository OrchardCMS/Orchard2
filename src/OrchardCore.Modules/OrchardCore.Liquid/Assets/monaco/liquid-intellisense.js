var liquidTags = [
    'if',
    'else',
    'elseif',
    'endif',
    'render',
    'assign',
    'capture',
    'endcapture',
    'case',
    'endcase',
    'comment',
    'endcomment',
    'cycle',
    'decrement',
    'for',
    'endfor',
    'include',
    'increment',
    'layout',
    'raw',
    'endraw',
    'render',
    'tablerow',
    'endtablerow',
    'unless',
    'endunless'
];
var liquidFilters = [
    'abs',
    'append',
    'at_least',
    'at_most',
    'capitalize',
    'ceil',
    'compact',
    'date',
    'default',
    'divided_by',
    'downcase',
    'escape',
    'escape_once',
    'first',
    'floor',
    'join',
    'json',
    'last',
    'lstrip',
    'map',
    'minus',
    'modulo',
    'newline_to_br',
    'plus',
    'prepend',
    'remove',
    'remove_first',
    'replace',
    'replace_first',
    'reverse',
    'round',
    'rstrip',
    'size',
    'slice',
    'sort',
    'sort_natural',
    'split',
    'strip',
    'strip_html',
    'strip_newlines',
    'times',
    'truncate',
    'truncatewords',
    'uniq',
    'upcase',
    'url_decode',
    'url_encode',
    'where'
];
function getLiquidContextInfo(model, position, triggerCharacter) {
    var inTag;
    var inObject;
    var showTags;
    var showFilters;
    var findStart = model.findPreviousMatch('\\{(%|\\{)', position, true, false, null, true);
    if (findStart && findStart.matches && !position.isBefore(findStart.range.getEndPosition())) {
        if (findStart.matches[1] == '%') {
            inTag = true;
        }
        else if (findStart.matches[1] == '{') {
            inObject = true;
        }
        var searchPattern = inTag ? '%}' : '}}';
        var findEnd = model.findNextMatch(searchPattern, position, false, false, null, true);
        var currentRange = findStart.range.plusRange(findEnd.range);
        if (currentRange.containsPosition(position)) {
            if (inTag) {
                var findTagName = model.findNextMatch('\\{%\\s*([a-zA-Z-_]+)', findStart.range.getStartPosition(), true, false, null, true);
                if (findTagName && currentRange.containsRange(findTagName.range) && findTagName.matches.length > 1) {
                    if (findTagName.matches[1] == 'assign') {
                        showFilters = true;
                    }
                    else {
                        showTags = false;
                    }
                }
                else {
                    showTags = true;
                }
            }
            else {
                showFilters = true;
            }
        }
    }
    return {
        showFilters: showFilters,
        showTags: showTags,
        inTag: inTag,
        inObject: inObject
    };
}
var completionItemProvider = {
    triggerCharacters: [' '],
    provideCompletionItems: function (model, position, context, token) {
        var items = [];
        if (context.triggerCharacter == ' ') {
            var startTrigger = model.getValueInRange(new monaco.Range(position.lineNumber, position.column - 3, position.lineNumber, position.column - 1));
            if (startTrigger != '{%' && !startTrigger.endsWith('|')) {
                return null;
            }
        }
        var liquidContext = getLiquidContextInfo(model, position, context.triggerCharacter);
        if (liquidContext.showFilters) {
            items = liquidFilters;
        }
        else if (liquidContext.showTags) {
            items = liquidTags.filter(function (value) { return !value.startsWith('end'); });
        }
        var suggestions = items.map(function (value) {
            return {
                label: value,
                kind: monaco.languages.CompletionItemKind.Keyword,
                insertText: value,
                insertTextRules: monaco.languages.CompletionItemInsertTextRule.KeepWhitespace
            };
        });
        return { suggestions: suggestions };
    }
};
function ConfigureLiquidIntellisense(monaco, suggestHtml) {
    if (suggestHtml === void 0) { suggestHtml = true; }
    if (suggestHtml) {
        var modeConfiguration = {
            completionItems: true,
            colors: true,
            foldingRanges: true,
            selectionRanges: true,
            diagnostics: false,
            documentFormattingEdits: true,
            documentRangeFormattingEdits: true
        };
        var options = {
            format: monaco.languages.html.htmlDefaults.options.format,
            suggest: { html5: true }
        };
        monaco.languages.html.registerHTMLLanguageService('liquid', options, modeConfiguration);
    }
    monaco.languages.registerCompletionItemProvider('liquid', completionItemProvider);
}
