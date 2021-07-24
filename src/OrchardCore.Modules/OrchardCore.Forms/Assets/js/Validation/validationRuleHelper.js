
'use strict';
var validationSet = new Set();

function validationEvent(validationApi, contentItemId, elementId) {
    var elementDom = $(elementId);
    var elementName = elementDom.attr('name');
    var elementValue = elementDom.val();
    var textDangerElement = elementDom.next();

    $.ajax({
        url: validationApi,
        method: 'POST',
        dataType: "json",
        data: {
            ContentItemId: contentItemId,
            FormName: elementName,
            FormValue: elementValue,
        },
    })
        .done(function (data) {
            if (data.result) {
                textDangerElement.hide();
                validationSet.delete(elementId);
                if (validationSet.size === 0) {
                    var formDom = $(elementId).closest('form');
                    formDom.find(':submit').removeAttr('disabled');
                }
            } else {
                textDangerElement.show();
                validationSet.add(elementId);
                $(elementId).closest('form').find(':submit').attr('disabled', 'true');
            }
        });
}

function validationFormEvent(validationFormApi, contentItemId, formDom) {

    $.ajax({
        url: validationFormApi,
        method: 'POST',
        dataType: "json",
        data: {
            ContentItemId: contentItemId,
            FormData: formDom.serialize(),
        },
    })
        .done(function (data) {
            if (data.result === '') {
                formDom.submit();
            } else {
                formDom.find(':submit').attr('disabled', 'true');
                var errorNames = data.result.split(",");
                for (var i = 0; i < errorNames.length; i++) {
                    var textDangerElement = formDom.find(":input[name='" + errorNames[i] + "']").next();
                    textDangerElement.show();
                }
            }
        });
}

function validationElementFunction(validationApi, validationFormApi, contentItemId, originElementId) {
    var elementId = '#' + originElementId;
    var formDom = $(elementId).closest('form');
    var submitId = '#' + formDom.find(':submit')[0].id;

    $(elementId).bind('change', function () {
        validationEvent(validationApi, contentItemId, elementId);
    })

    $(submitId).off('click').on('click', function (e) {
        e.preventDefault();
        validationFormEvent(validationFormApi, contentItemId, formDom);
    })
}

function validationTypeChange(e) {
    var selectedValidationOptionItem = e.options[e.options.selectedIndex];
    var isShowOption = selectedValidationOptionItem.getAttribute('is-show-option');
    var dataValidationOption = selectedValidationOptionItem.getAttribute('data-validation-option');
    var isShowValidationMessage = selectedValidationOptionItem.getAttribute('is-show-validationMessage');
    var validationErrorMessage = $(e.closest('.edit-item-parts')).find('.validation-error-message');
    var validationOption = $(e.closest('.edit-item-parts')).find('.validation-option');
    var validationErrorMessageInput = validationErrorMessage.find('input');
    var validationOptionInput = validationOption.find('input');

    validationOptionInput.val('');
    validationErrorMessageInput.val('');

    if (isShowOption === "False") {
        validationOption.css('display', 'none');
    }
    else {
        validationOption.css('display', 'block');
        validationOptionInput.attr('placeholder', dataValidationOption);
    }

    if (isShowValidationMessage === 'False') {
        validationErrorMessage.css('display', 'none');
    }
    else {
        validationErrorMessage.css('display', 'block');
    }
}
