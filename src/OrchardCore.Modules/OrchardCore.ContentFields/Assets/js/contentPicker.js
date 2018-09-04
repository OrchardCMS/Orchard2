function initializeContentPickerFieldEditor(elementId, selectedItems, contentItemIds, tenantPath, partName, fieldName) {

    Vue.component('vue-multiselect', window.VueMultiselect.default);

    new Vue({
        el: '#' + elementId,
        data: {
            value: selectedItems,
            options: [],
            selectedIds: contentItemIds
        },
        created: function () {
            var self = this;
            self.asyncFind();
        },
        methods: {
            asyncFind: function (query) {
                var self = this;
                self.isLoading = true;
                var searchUrl = tenantPath + '/ContentPicker?part=' + partName + '&field=' + fieldName;                
                if (query) {
                    searchUrl += '&query=' + query;
                }
                _.debounce(function (searchUrl) {
                    fetch(searchUrl) 
                        .then(function (res) {
                            res.json().then(function (json) {
                                self.options = json;
                                self.isLoading = false;
                            })
                        })
                }, 350)(searchUrl);
            },
            onInput: function (value) {
                var self = this;
                if (Array.isArray(value)) {
                    self.selectedIds = value.map(function (x) { return x.contentItemId }).join(',');
                } else if (value) {
                    self.selectedIds = value.contentItemId;
                } else {
                    self.selectedIds = '';
                }
            }
        }
    })
}