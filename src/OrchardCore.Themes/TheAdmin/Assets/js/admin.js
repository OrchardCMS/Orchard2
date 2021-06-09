function confirmDialog({callback, ...options}) {
    const defaultOptions = $('#confirmRemoveModalMetadata').data();
    const { title, message, okText, cancelText, okClass, cancelClass } = $.extend({}, defaultOptions, options);

    $('<div id="confirmRemoveModal" class="modal" tabindex="-1" role="dialog">\
        <div class="modal-dialog modal-dialog-centered" role="document">\
            <div class="modal-content">\
                <div class="modal-header">\
                    <h5 class="modal-title">' + title + '</h5>\
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>\
                </div>\
                <div class="modal-body">\
                    <p>' + message +'</p>\
                </div>\
                <div class="modal-footer">\
                    <button id="modalOkButton" type="button" class="btn ' + okClass + '">' + okText + '</button>\
                    <button id="modalCancelButton" type="button" class="btn ' + cancelClass + '" data-bs-dismiss="modal">' + cancelText + '</button>\
                </div>\
            </div>\
        </div>\
    </div>').appendTo("body");
    $("#confirmRemoveModal").modal({
        backdrop: 'static',
        keyboard: false
    });

    $("#confirmRemoveModal").on('hidden.bs.modal', function () {
        $("#confirmRemoveModal").remove();
    });

    $("#modalOkButton").click(function () {
        callback(true);
        $("#confirmRemoveModal").modal("hide");
    });

    $("#modalCancelButton").click(function () {
        callback(false);
        $("#confirmRemoveModal").modal("hide");
    });
}

// Prevents page flickering while downloading css
$(window).on("load", function() {
    $("body").removeClass("preload");
});

$(function () {
    $("body").on("click", "[data-url-af~='RemoveUrl'], a[itemprop~='RemoveUrl']", function () {
        var _this = $(this);
        if(_this.filter("a[itemprop~='UnsafeUrl']").length == 1)
        {
            console.warn('Please use data-url-af instead of itemprop attribute for confirm modals. Using itemprop will eventually become deprecated.')
        }
        // don't show the confirm dialog if the link is also UnsafeUrl, as it will already be handled below.
        if (_this.filter("[data-url-af~='UnsafeUrl'], a[itemprop~='UnsafeUrl']").length == 1) {
            return false;
        }
        confirmDialog({..._this.data(),
             callback: function(resp) {
                if (resp) {
                    var url = _this.attr('href');
                    if (url == undefined) {
                        var form = _this.parents('form');
                        // This line is reuired in case we used the FormValueRequiredAttribute
                        form.append($("<input type=\"hidden\" name=\"" + _this.attr('name') + "\" value=\"" + _this.attr('value') + "\" />"));
                        form.submit();
                    }
                    else {
                        window.location = url;
                    }
                }
            }});

        return false;
    });
});

$(function () {
    var magicToken = $("input[name=__RequestVerificationToken]").first();
    if (magicToken) {
        $("body").on("click", "a[data-url-af~='UnsafeUrl'], a[itemprop~='UnsafeUrl']", function () {
            var _this = $(this);
            if(_this.filter("a[itemprop~='UnsafeUrl']").length == 1)
            {
                console.warn('Please use data-url-af instead of itemprop attribute for confirm modals. Using itemprop will eventually become deprecated.')
            }
            var hrefParts = _this.attr("href").split("?");
            var form = $("<form action=\"" + hrefParts[0] + "\" method=\"POST\" />");
            form.append(magicToken.clone());
            if (hrefParts.length > 1) {
                var queryParts = hrefParts[1].split("&");
                for (var i = 0; i < queryParts.length; i++) {
                    var queryPartKVP = queryParts[i].split("=");
                    //trusting hrefs in the page here
                    form.append($("<input type=\"hidden\" name=\"" + decodeURIComponent(queryPartKVP[0]) + "\" value=\"" + decodeURIComponent(queryPartKVP[1]) + "\" />"));
                }
            }
            form.css({ "position": "absolute", "left": "-9999em" });
            $("body").append(form);

            var unsafeUrlPrompt = _this.data("unsafe-url");

            if (unsafeUrlPrompt && unsafeUrlPrompt.length > 0) {
                confirmDialog({..._this.data(),
                    callback: function(resp) {
                        if (resp) {
                            form.submit();
                        }
                    }
                });

                return false;
            }

            if (_this.filter("[data-url-af~='RemoveUrl'], a[itemprop~='RemoveUrl']").length == 1) {
                confirmDialog({..._this.data(), 
                    callback: function(resp) {
                        if (resp) {
                            form.submit();
                        }
                    }
                });

                return false;
            }

            form.submit();
            return false;
        });
    }
});

$(function () {
    $('input[data-bs-toggle="collapse"]').each(function () {
        // Prevent bootstrap from altering its behavior
        // c.f. https://github.com/twbs/bootstrap/issues/21079
        $(this).removeAttr("data-toggle");

        // Expand the section if necessary
        var target = $($(this).data('target'));
        if ($(this).prop('checked')) {
            target.addClass('visible');
        }

        $(this).on('change', function (e) {
            // During a double-click, ignore state changes while the element is collapsing
            if (target.hasClass('collapsing')) {
                $(this).prop('checked', !$(this).prop('checked'));
            }
            target.collapse($(this).prop('checked') ? 'show' : 'hide');
        });
    });
});

$(function () {
    $('input[data-bs-toggle="collapse active"]').each(function () {
        // Prevent bootstrap from altering its behavior for inputs that hide target when input value is checked
        // c.f. https://github.com/twbs/bootstrap/issues/21079
        $(this).removeAttr("data-toggle");

        // Expand the section if necessary
        var target = $($(this).data('target'));
        if (!$(this).prop('checked')) {
            target.addClass('show');
        }

        $(this).on('change', function (e) {
            // During a double-click, ignore state changes while the element is collapsing
            if (target.hasClass('collapsing')) {
                console.log('collapsing');
                $(this).prop('checked', !$(this).prop('checked'));
            }
            target.collapse($(this).prop('checked') ? 'hide' : 'show');
        });
    });

});

function getTechnicalName(name) {
    var result = "", c;

    if (!name || name.length == 0) {
        return "";
    }

    name = removeDiacritics(name);

    for (i = 0; i < name.length; i++) {
        c = name[i];
        if (isLetter(c) || (isNumber(c) && i > 0)) {
            result += c;
        }
    }

    return result;
}

function isLetter(str) {
    return str.length === 1 && str.match(/[a-z]/i);
}

function isNumber(str) {
    return str.length === 1 && str.match(/[0-9]/i);
}

//$('[data-bs-toggle="tooltip"]').tooltip();
