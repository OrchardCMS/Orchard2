/*
** NOTE: This file is generated by Gulp and should not be edited directly!
** Any changes made directly to this file will be overwritten next time its asset group is processed by Gulp.
*/

function initializeOptionsEditor(el, data, defaultValue) {

    var checked = defaultValue;

    var optionsEditor = $(el);
    var previouslyChecked;

    var optionsTable = {
        template: '#options-table',
        props: ['value'],
        name: 'options-table',
        computed: {
            list: {
                get: function() {
                    return this.value;
                },
                set: function(value) {
                    this.$emit('input', value);
                }
            },
            defaultValue: {
                get: function () {
                    return defaultValue;
                },
                set: function (value) {
                    defaultValue = value;
                }
            }
        },
        methods: {
            add: function () {
                this.list.push({ name: '', value: ''});
            },
            remove: function (index) {
                this.list.splice(index, 1);
            },
            uncheck: function (index) {
                if (index == previouslyChecked) {
                    $('#customRadio_' + index)[0].checked = false;
                    previouslyChecked = null;
                }
                else {
                    previouslyChecked = index;
                }

            },
            getFormattedList: function () {
                return JSON.stringify(this.list.filter(function (x) { return !IsNullOrWhiteSpace(x.name) && !IsNullOrWhiteSpace(x.value) }));
            }
        }
    };

    new Vue({
        components: {
            optionsTable: optionsTable
        },
        el: optionsEditor.get(0),
        data: {
            option: data,
            defaultValue: checked,
            dragging: false
        }
    });

}

function IsNullOrWhiteSpace(str) {
    return str === null || str.match(/^ *$/) !== null;
}