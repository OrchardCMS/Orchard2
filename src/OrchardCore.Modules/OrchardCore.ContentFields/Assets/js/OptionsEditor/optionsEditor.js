function initializeOptionsEditor(elem, data, defaultValue, modalBodyElement) {

    var previouslyChecked;
    var currentData = data;

    var store = {
        debug: true,
        state: {
            options: data,
            selected: defaultValue
        },
        methods: {
            setMessageAction: function(newValue) {
                if (this.debug) { console.log('setMessageAction triggered with', newValue) };
            },
            clearMessageAction: function() {
                if (this.debug) { console.log('clearMessageAction triggered') };
            },
            reloadData: function(data) {
                this.options = data;
            }
        }
    }

    var optionsTable = {
        template: '#options-table',
        props: ['data'],
        name: 'options-table',
        methods: {
            add: function () {
                var exist = this.data.options.filter(function (x) { return IsNullOrWhiteSpace(x.value) }).length;
                if (!exist) {
                    this.data.options.push({ name: '', value: '' });
                }
            },
            remove: function (index) {
                this.data.options.splice(index, 1);
            },
            uncheck: function (index, value) {
                if (index == previouslyChecked) {
                    $('#customRadio_' + index)[0].checked = false;
                    previouslyChecked = null;
                }
                else {
                    previouslyChecked = index;
                }

            },
            getFormattedList: function () {
                return JSON.stringify(this.data.options.filter(function (x) { return !IsNullOrWhiteSpace(x.name) && !IsNullOrWhiteSpace(x.value) }));
            }
        }
    };

    var optionsModal = {
        template: '#options-modal',
        props: ['data'],
        name: 'options-modal',
        methods: {
            getFormattedList: function () {
                return JSON.stringify(this.data.options.filter(function (x) { return !IsNullOrWhiteSpace(x.name) && !IsNullOrWhiteSpace(x.value) }));
            },
            setFormattedListToObject: function (element) {
                this.data.options.push(JSON.parse(element));
            },
            showModal: function (event) {
                var modal = $(modalBodyElement).modal();
            },
            closeModal: function () {
                var modal = $(modalBodyElement).modal();
                modal.modal('hide');
            }
        }
    };

    var optionTableApp = new Vue({
        components: {
            optionsTable: optionsTable,
            optionsModal: optionsModal
        },
        data: {
            sharedState: store.state
        },
        el: elem,
        methods: {
            showModal: function (event) {
                optionsModal.methods.showModal(event);
            }
        }
    });

}

function IsNullOrWhiteSpace(str) {
    return str === null || str.match(/^ *$/) !== null;
}