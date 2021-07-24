require.config({ paths: { 'vs': document.currentScript.dataset.tenantPrefix + '/OrchardCore.Resources/Scripts/monaco/vs' } });

const $document = $(document);

// initialize all monaco editors
$document.ready(function () {
    function debounce(func, wait, immediate) {
        var timeout;
        return function() {
            var context = this, args = arguments;
            var later = function() {
                timeout = null;
                if (!immediate) func.apply(context, args);
            };
            var callNow = immediate && !timeout;
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
            if (callNow) func.apply(context, args);
        };
    };
    require(['vs/editor/editor.main'], function () {

        // set the theme from the html element
        const html = document.getElementsByTagName("html")[0];
        const mutationObserver = new MutationObserver(setTheme);
        mutationObserver.observe(html, { attributes: true });

        function setTheme() {
            let theme = html.dataset.theme;
            if (theme === "darkmode") {
                monaco.editor.setTheme('vs-dark')
            } else {
                monaco.editor.setTheme('vs')
            }
        }
        setTheme();

        function initMonacoEditor(el) {
            const $el = $(el);
            if($el.data("monaco") != undefined)
            {
                // this dom node has already been initialized, skip initialization
                return;
            }

            // sensible defaults for all monaco editors
            const defaultSettings = {
                "scrollBeyondLastLine": false,
                "scrollbar": {
                    // allow the page to scroll when bottom of editor is  scrolling
                    "alwaysConsumeMouseWheel": false,
                }
            }

            let editorSettings = {};
            if(el.dataset.settings)
            {
                editorSettings = JSON.parse(el.dataset.settings);
            }
            
            const settings = Object.assign({}, defaultSettings, editorSettings);
            // automaticLayout no longer required as we manage the height / width automatically below
            settings.automaticLayout = false;

            const editor = monaco.editor.create(el, settings);
            const linkedElement = document.getElementById(el.dataset.linkedId);
            
            // set the initial value of the monaco editor based on the value of the textarea
            editor.getModel().setValue(linkedElement.value);

            window.addEventListener("submit", function () {
                linkedElement.value = editor.getValue();
            });

            // debounced change handler that will trigger the preview
            const debouncedOnChange = debounce(function(event) {
                linkedElement.value = editor.getValue();
                $document.trigger('contentpreview:render');
            }, 250);
            // trigger preview when content changes (debounced)
            editor.getModel().onDidChangeContent(debouncedOnChange);

            // dynamically update the height of the monaco editor to a max of 650px 
            const updateHeight = () => {
                let width = el.parentElement.clientWidth;
                const contentHeight = Math.min(650, editor.getContentHeight());
                el.style.width = `${width}px`;
                el.style.height = `${contentHeight + 20}px`;
                editor.layout({ width: width - 20,  height: contentHeight });
            };
            editor.onDidContentSizeChange(updateHeight);
            updateHeight();

            // dynamically set the width when the browser is resized.
            const debouncedUpdateHeight = debounce(updateHeight,150);
            window.addEventListener('resize', debouncedUpdateHeight);
            
            // include the shortcodes action for html editors
            if(el.dataset.shortcodes)
            {
                const shortcodesAction = {
                    id: "shortcodes",
                    label: "Add Shortcode",
                    run: function (editor) {
                        shortcodesApp.init(function (value) {
                            if (value) {
                                var selection = editor.getSelection();
                                var text = value;
                                var op = { range: selection, text: text, forceMoveMarkers: true };
                                editor.executeEdits("shortcodes", [op]);
                            }
                            editor.focus();
                        })
                    },
                    contextMenuGroupId: 'orchardcore',
                    contextMenuOrder: 0,
                    keybindings: [
                        monaco.KeyMod.Alt | monaco.KeyCode.KEY_S, 
                    ]
                }
                editor.addAction(shortcodesAction);
            }

            // set the jQuery monaco data to the dom node. 
            // The data object is used to allow other components to use the editor functions or updateHeight function
            $el.data("monaco", { editor, updateHeight });
        }

        // loop through and initialize all monaco editors on the page
        document.querySelectorAll("[data-monaco-oc]").forEach(initMonacoEditor);

        // initialize the monaco editor when a widget is dynamically added to the page
        document.addEventListener('widget-added', function (e) {
            document.querySelectorAll("[data-monaco-oc]").forEach(initMonacoEditor);
        }, false);

        // handle resizing when expanding a widget
        $document.on('click', '.widget-editor-btn-toggle', function () {
            window.requestAnimationFrame(()=>{
                const widgetEditor = $(this).closest('.widget-editor');
                if(!widgetEditor.hasClass("collapsed"))
                {
                    widgetEditor.find("[data-monaco-oc]").each((k, v) => {
                        let monacoData = $(v).data("monaco");
                        if(monacoData)
                        {
                            monacoData.updateHeight();
                        }
                    });
                }
            });
        });
    });
});
