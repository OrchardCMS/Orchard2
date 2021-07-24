//variables used in FlowPart.Edit sortable
var widgetDragItem, lastContainer, widgetItemSourceId, widgetItemDestId;


$(function () {

    function guid() {
        function s4() {
            return Math
                .floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
        }
        return s4() + s4() + s4() + s4() + s4() + s4() + s4() + s4();
    }    
    $(document).on('click', '.add-widget', function (event) {
        var type = $(this).data("widget-type");
        var targetId = $(this).data("target-id");
        var createEditorUrl = $('#' + targetId).data("buildeditorurl");
        var prefixesName = $(this).data("prefixes-name");
        var flowmetadata = $(this).data("flowmetadata");
        var parentContentType = $(this).data("parent-content-type");
        var partName = $(this).data("part-name");
        var prefix = guid();
        var contentTypesName = $(this).data("contenttypes-name");
        $.ajax({
            url: createEditorUrl + "?id=" + type + "&prefix=" + prefix + "&prefixesName=" + prefixesName + "&contentTypesName=" + contentTypesName + "&targetId=" + targetId + "&flowmetadata=" + flowmetadata + "&parentContentType=" + parentContentType +"&partName=" + partName
        }).done(function (data) {
            var result = JSON.parse(data);
            $(document.getElementById(targetId)).append(result.Content);

            var dom = $(result.Scripts);
            dom.filter('script').each(function () {
                $.globalEval(this.text || this.textContent || this.innerHTML || '');
            });
            document.dispatchEvent(new CustomEvent('widget-added', { detail: { targetId } }));
        });
    });

    $(document).on('click', '.insert-widget', function (event) {
        var type = $(this).data("widget-type");
        var target = $(this).closest('.widget-template');
        var targetId = $(this).data("target-id");
        var createEditorUrl = $('#' + targetId).data("buildeditorurl");
        var flowmetadata = $(this).data("flowmetadata");
        var prefixesName = $(this).data("prefixes-name");
        var parentContentType = $(this).data("parent-content-type");
        var partName = $(this).data("part-name");
        var prefix = guid();
        var contentTypesName = $(this).data("contenttypes-name");
        $.ajax({
            url: createEditorUrl + "?id=" + type + "&prefix=" + prefix + "&prefixesName=" + prefixesName + "&contentTypesName=" + contentTypesName + "&targetId=" + targetId + "&flowmetadata=" + flowmetadata + "&parentContentType=" + parentContentType + "&partName=" + partName
        }).done(function (data) {
            var result = JSON.parse(data);
            $(result.Content).insertBefore(target);

            var dom = $(result.Scripts);
            dom.filter('script').each(function () {
                $.globalEval(this.text || this.textContent || this.innerHTML || '');
            });
            document.dispatchEvent(new CustomEvent('widget-added', { detail: { targetId } }));
        });
    });

    $(document).on('click', '.widget-delete', function () {
        var $this = $(this);
        confirmDialog({...$this.data(), callback: function(r) { 
                if (r) {
                    $this.closest('.widget-template').remove();
                    $(document).trigger('contentpreview:render');
                }
            }});
    });

    $(document).on('change', '.widget-editor-footer label, .widget-editor-header label', function () {

        var $tmpl = $(this).closest('.widget-template');
        var $radio = $(this).find("input:first-child");
        if ($radio[0].id !== 'undefined' && $radio[0].id.indexOf('Size') > 0) {
            var $radioSize = $(this).find("input:first-child").val();
            var classList = $tmpl.attr('class').split(' ');
            $.each(classList, function (id, item) {
                if (item.indexOf('col-md-') === 0) $tmpl.removeClass(item);
            });
            var colSize = Math.round($radioSize / 100 * 12);
            $tmpl.addClass('col-md-' + colSize);

            var dropdown = $(this).closest('.dropdown-menu');
            dropdown.prev('button').text($radioSize + '%');
        } else if ($radio[0].id !== 'undefined' && $radio[0].id.indexOf('Alignment') > 0) {
            var svg = $(this).find('svg')[0].outerHTML;
            var alignDropdown = $(this).closest('.dropdown-menu');
            var $btn = alignDropdown.prev('button');
            $btn.html(svg );

        }
        $(document).trigger('contentpreview:render');
    });

    $(document).on('click', '.widget-editor-btn-toggle', function () {
        $(this).closest('.widget-editor').toggleClass('collapsed');
    });

    $(document).on('keyup', '.widget-editor-body .form-group input.content-caption-text', function () {
        var headerTextLabel = $(this).closest('.widget-editor').find('.widget-editor-header:first .widget-editor-header-text');
        var contentTypeDisplayText = headerTextLabel.data('content-type-display-text');
        var title = $(this).val();
        var newDisplayText = title + ' ' + contentTypeDisplayText;

        headerTextLabel.text(newDisplayText);
    });
});
