function initializeOptionsEditor(elem, data, defaultValue, selectedValue, list) {

    var optionsTable = {
        name: 'options-table',
        template: '#options-table',
        props: ['data', 'defaultValue', 'selectedValue', 'list'],
        data: function () {
            return {
                defaultCulture: this.defaultValue,
                selectedCulture: typeof this.selectedValue != 'undefined' ? this.selectedValue : selectedValue,
                allCultures: this.list
            }
        },
        methods: {
            add: function () {
                if (!this.data.includes(this.selectedCulture)) {
                    this.data.push(this.selectedCulture);

                    var culture = this.selectedCulture;
                    var selectedCulture = this.allCultures.filter(function (ele) {
                        return ele.Name == culture;
                    });

                    if (selectedCulture.length > 0) {
                        selectedCulture[0].Supported = true;
                    }

                    var allUnsupportedCultures = this.allCultures.filter(function (ele) {
                        return ele.Supported == false
                    });

                    if (allUnsupportedCultures.length > 0) {
                        this.selectedCulture = allUnsupportedCultures[0].Name;
                    }
                }
            },
            remove: function (index) {

                var selectedCulture = this.data[index];
                cultureRemoved = this.list.filter(function (ele) {
                    return ele.Name == selectedCulture;
                });

                if (cultureRemoved.length > 0) {
                    cultureRemoved[0].Supported = false;
                }

                this.data.splice(index, 1);

                var allUnsupportedCultures = this.allCultures.filter(function (ele) {
                    return ele.Supported == false
                });

                if (allUnsupportedCultures.length > 0) {
                    this.selectedCulture = allUnsupportedCultures[0].Name;
                }
            },
            getSupportedCultures: function () {
                return JSON.stringify(this.data);
            },
            getDefaultCulture: function () {
                var result = defaultValue;
                if (this.defaultCulture != null) {
                    result = this.defaultCulture;
                }
                return result;
            },
            getAllCultures: function () {
                return this.AllCultures;
            }
        }
    };

    new Vue({
        components: {
            optionsTable: optionsTable
        },
        data: {
            allCultures: list,
            supportedCultures: data,
            defaultCulture: defaultValue,
            selectedCulture: selectedValue
        },
        el: elem
    });

}
