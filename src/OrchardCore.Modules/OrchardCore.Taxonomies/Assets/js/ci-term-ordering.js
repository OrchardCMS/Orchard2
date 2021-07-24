function updateContentItemOrders(oldIndex, newIndex) {
    var url = $('#ordering-url').data("url");
    var taxonomyContentItemId = $('#taxonomy-id').data("id");
    var taxonomyItemId = $('#term-id').data("id");
    var before = $('#pager-before').data("before");
    var after = $('#pager-after').data("after");
    var pageSize = $('#pager-pagesize').data("pagesize");
    $.ajax({
        url: url,
        method: 'POST',
        data: {
            __RequestVerificationToken: $("input[name='__RequestVerificationToken']").val(),
            taxonomyContentItemId: taxonomyContentItemId,
            taxonomyItemId: taxonomyItemId,
            oldIndex: oldIndex,
            newIndex: newIndex,
            pagerSlimParameters: {
                before: before,
                after: after
            },
            pageSize: pageSize
        },
        error: function (error) {
            alert($('#update-order-error-message').data("message"));
        }
    });
}

$(function () {
    var sortable = document.getElementById("ci-sortable");

    sortable = Sortable.create(sortable, {
        handle: ".ui-sortable-handle",
        onSort: function (evt) {
            var oldIndex = evt.oldIndex;
            var newIndex = evt.newIndex;
            updateContentItemOrders(oldIndex, newIndex);
        }
    });
});
