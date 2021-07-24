$(function () {
    //function scoped variables    
    function guid() {
        function s4() {
            return Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
        }
        return s4() + s4() + s4() + s4() + s4() + s4() + s4() + s4();
    }

    $(document).on('click', '.add-list-widget', function (event) {
        var type = $(this).data("widget-type");
        var targetId = $(this).data("target-id");
        var createEditorUrl = $('#' + targetId).data("buildeditorurl");
        var prefixesName = $(this).data("prefixes-name");
        var parentContentType = $(this).data("parent-content-type");
        var partName = $(this).data("part-name");
        var zonesName = $(this).data("zones-name");
        var zone = $(this).data("zone");
        var prefix = guid();
        var contentTypesName = $(this).data("contenttypes-name");
        $.ajax({
            url: createEditorUrl + "?id=" + type + "&prefix=" + prefix + "&prefixesName=" + prefixesName + "&contentTypesName=" + contentTypesName + "&zonesName=" + zonesName + "&zone=" + zone + "&targetId=" + targetId + "&parentContentType=" + parentContentType + "&partName=" + partName
        })
        .done(function (data) {
            var result = JSON.parse(data);
            $(document.getElementById(targetId)).append(result.Content);

            var dom = $(result.Scripts);
            dom.filter('script').each(function () {
                $.globalEval(this.text || this.textContent || this.innerHTML || '');
            });
            document.dispatchEvent(new CustomEvent('widget-added', { detail: { targetId } }));
        });
    });

    $(document).on('click', '.insert-list-widget', function (event) {
        var type = $(this).data("widget-type");
        var target = $(this).closest('.widget-template');
        var targetId = $(this).data("target-id");
        var createEditorUrl = $('#' + targetId).data("buildeditorurl");
        var prefixesName = $(this).data("prefixes-name");
        var parentContentType = $(this).data("parent-content-type");
        var partName = $(this).data("part-name");
        var zonesName = $(this).data("zones-name");
        var zone = $(this).data("zone");
        var prefix = guid();
        var contentTypesName = $(this).data("contenttypes-name");
        $.ajax({
            url: createEditorUrl + "?id=" + type + "&prefix=" + prefix + "&prefixesName=" + prefixesName + "&contentTypesName=" + contentTypesName + "&zonesName=" + zonesName + "&zone=" + zone + "&targetId=" + targetId + "&parentContentType=" + parentContentType + "&partName=" + partName
        })
        .done(function (data) {
            var result = JSON.parse(data);
            $(result.Content).insertBefore(target);

            var dom = $(result.Scripts);
            dom.filter('script').each(function () {
                $.globalEval(this.text || this.textContent || this.innerHTML || '');
            });
            document.dispatchEvent(new CustomEvent('widget-added', { detail: { targetId } }));
        });
    });

    $(document).on('click', '.widget-list-delete', function () {
        var $this = $(this);
        confirmDialog(_objectSpread({}, $this.data(), {
            callback: function callback(r) {
                if (r) {
                    $this.closest('.widget-template').remove();
                    $(document).trigger('contentpreview:render');
                }
            }
        }));
    });

    $(document).on('change', '.widget-editor-footer label', function () {
        $(document).trigger('contentpreview:render');
    });

    $(document).on('click', '.widget-list-editor-btn-toggle', function () {
        $(this).closest('.widget-editor').toggleClass('collapsed');
    });

});
