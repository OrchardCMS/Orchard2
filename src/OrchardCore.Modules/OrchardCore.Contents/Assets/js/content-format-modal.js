
// Run this function on document.ready
$(function () {
    const element = document.querySelector("#contentFormatModal");
    if(element)
    {
        const elementId = element.id;
        var searchUrl = element.dataset.searchUrl;
        const app = new Vue({
            el: '#' + elementId,
            data : function () {
                
                var contentTypes = JSON.parse(element.dataset.types || "[]");

                return {
                    listGroupClass: 'list-group-item list-group-item-action',
                    activeClass: 'active',
                    filter: '',
                    allTypes: contentTypes,
                    filteredTypes: contentTypes,
                    contentFormat: '',
                    selectedType:'',
                }
            },
            watch:
            {
                filter(filter) {
                    if (filter) {
                        var lower = filter.toLowerCase();
                        this.filteredTypes = this.allTypes
                            .filter(s => s.key.toLowerCase().startsWith(lower));
                    } else {
                        this.filteredTypes = this.allTypes;
                    }
                }
            }, 
            mounted() {
                const self = this;
                $(element).on('shown.bs.modal', function (e) {
                    self.$refs.filter.focus();
                });
            },        
            methods: {
                selectType(type)
                {
                    this.selectedType = type;
                    var searchFullUrl = searchUrl + '?contentTypeName=' + type;
                    const self = this;
                    fetch(searchFullUrl).then(function (res) {
                        res.json().then(function (json) {
                            self.contentFormat = JSON.stringify(json, null, 1);
                        })
                    });
                }, 
            }
        });
    }
});


