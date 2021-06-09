// This component receives a list of all the items, unpaged.
// As the user interacts with the pager, it raises events with the items in the current page.
// It's the parent's responsibility to listen for these events and display the received items
// <pager> component
Vue.component('pager', {
    template: `
    <div>
        <nav id="media-pager" class="d-flex justify-content-center" aria-label="Pagination Navigation" role="navigation" :data-computed-trigger="itemsInCurrentPage.length">
            <ul class="pagination pagination-sm m-0">
                <li class="page-item media-first-button" :class="{disabled : !canDoFirst}">
                    <a class="page-link" href="#" :tabindex="canDoFirst ? 0 : -1" v-on:click="goFirst">{{ T.pagerFirstButton }}</a>
                </li>
                <li class="page-item" :class="{disabled : !canDoPrev}">
                    <a class="page-link" href="#" :tabindex="canDoPrev ? 0 : -1" v-on:click="previous">{{ T.pagerPreviousButton }}</a>
                </li>
                <li v-if="link !== -1" class="page-item page-number"  :class="{active : current == link - 1}" v-for="link in pageLinks">
                    <a class="page-link" href="#" v-on:click="goTo(link - 1)" :aria-label="'Goto Page' + link">
                        {{link}}
                        <span v-if="current == link -1" class="visually-hidden">(current)</span>
                    </a>
                </li>
                <li class="page-item" :class="{disabled : !canDoNext}">
                    <a class="page-link" href="#" :tabindex="canDoNext ? 0 : -1" v-on:click="next">{{ T.pagerNextButton }}</a>
                </li>
                <li class="page-item media-last-button" :class="{disabled : !canDoLast}">
                    <a class="page-link" href="#" :tabindex="canDoLast ? 0 : -1" v-on:click="goLast">{{ T.pagerLastButton }}</a>
                </li>
                <li class="page-item ms-4 page-size-info">
                    <div style="display: flex;">
                        <span class="page-link disabled text-muted page-size-label">{{ T.pagerPageSizeLabel }}</span>
                        <select id="pageSizeSelect" class="page-link" v-model="pageSize">
                            <option v-for="option in pageSizeOptions" v-bind:value="option">
                                {{option}}
                            </option>
                        </select>
                    </div>
                </li>
            </ul>
        </nav>
        <nav class="d-flex justify-content-center">
            <ul class="pagination pagination-sm m-0 mt-2">
                <li class="page-item ms-4 page-info">
                    <span class="page-link disabled text-muted ">{{ T.pagerPageLabel }} {{current + 1}}/{{totalPages}}</span>
                </li>
                <li class="page-item ms-4 total-info">
                    <span class="page-link disabled text-muted "> {{ T.pagerTotalLabel }} {{total}}</span>
                </li>
            </ul>
        </nav>
        </div>
        `,
    props: {
        sourceItems: Array
    },
    data: function () {
        return {
            pageSize: 10,
            pageSizeOptions: [10, 30, 50, 100],
            current: 0,
            T: {}
        };
    },
    created: function () {
        var self = this;

        // retrieving localized strings from view
        self.T.pagerFirstButton = $('#t-pager-first-button').val();
        self.T.pagerPreviousButton = $('#t-pager-previous-button').val();
        self.T.pagerNextButton = $('#t-pager-next-button').val();
        self.T.pagerLastButton = $('#t-pager-last-button').val();
        self.T.pagerPageSizeLabel = $('#t-pager-page-size-label').val();
        self.T.pagerPageLabel = $('#t-pager-page-label').val();
        self.T.pagerTotalLabel = $('#t-pager-total-label').val();        
    },
    methods: {
        next: function () {
            this.current = this.current + 1;
        },
        previous: function () {
            this.current = this.current - 1;
        },
        goFirst: function () {
            this.current = 0;
        },
        goLast: function () {
            this.current = this.totalPages - 1;
        },
        goTo: function (targetPage) {
            this.current = targetPage;
        }
    },
    computed: {
        total: function () {
            return this.sourceItems ? this.sourceItems.length : 0;
        },
        totalPages: function () {
            var pages = Math.ceil(this.total / this.pageSize);
            return pages > 0 ? pages : 1;
        },
        isLastPage: function () {
            return this.current + 1 >= this.totalPages;
        },
        isFirstPage: function () {
            return this.current === 0;
        },
        canDoNext: function () {
            return !this.isLastPage;
        },
        canDoPrev: function () {
            return !this.isFirstPage;
        },
        canDoFirst: function () {
            return !this.isFirstPage;
        },
        canDoLast: function () {
            return !this.isLastPage;
        },
        // this computed is only to have a central place where we detect changes and leverage Vue JS reactivity to raise our event.
        // That event will be handled by the parent media app to display the items in the page.
        // this logic will not run if the computed property is not used in the template. We use a dummy "data-computed-trigger" attribute for that.
        itemsInCurrentPage: function () {
            var start = this.pageSize * this.current;
            var end = start + this.pageSize;
            var result = this.sourceItems.slice(start, end);
            bus.$emit('pagerEvent', result);
            return result;
        },
        pageLinks: function () {

            var links = [];

            links.push(this.current + 1);

            // Add 2 items before current
            var beforeCurrent = this.current > 0 ? this.current : -1;
            links.unshift(beforeCurrent);

            var beforeBeforeCurrent = this.current > 1 ? this.current - 1 : -1;
            links.unshift(beforeBeforeCurrent);


            // Add 2 items after current
            var afterCurrent = this.totalPages - this.current > 1 ? this.current + 2 : -1;
            links.push(afterCurrent);

            var afterAfterCurrent = this.totalPages - this.current > 2 ? this.current + 3 : -1;
            links.push(afterAfterCurrent);

            return links;
        }
    },
    watch: {
        sourceItems: function () {
            this.current = 0; // resetting current page after receiving a new list of unpaged items
        },
        pageSize: function () {
            this.current = 0;
        }
    }
});
